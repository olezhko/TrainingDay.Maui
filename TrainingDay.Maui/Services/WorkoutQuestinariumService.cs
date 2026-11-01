using System.Text.Json;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Questions;

namespace TrainingDay.Maui.Services;

public interface IWorkoutQuestinariumService
{
    /// <summary>Builds a workout from the questionnaire answers and saves it as a new training.</summary>
    /// <returns>Id of the created training, or 0 when no exercise matched the answers.</returns>
    Task<int> CreateWorkoutAsync(string name, WorkoutQuestinariumAnswers answers, CancellationToken token);
}

/// <summary>Builds a workout locally on the device from the questionnaire answers and the exercise library.</summary>
public class WorkoutQuestinariumService : IWorkoutQuestinariumService
{
    private const int MinStrengthExercises = 3;
    private const int MaxStrengthExercises = 16;
    private const int MinRepetitions = 3;

    private static readonly IReadOnlyDictionary<WorkoutMuscleGroup, MusclesEnum[]> MuscleGroups = new Dictionary<WorkoutMuscleGroup, MusclesEnum[]>
    {
        [WorkoutMuscleGroup.Chest] = [MusclesEnum.Chest],
        [WorkoutMuscleGroup.Back] = [MusclesEnum.WidestBack, MusclesEnum.MiddleBack, MusclesEnum.Trapezium, MusclesEnum.ErectorSpinae, MusclesEnum.LowerBack],
        [WorkoutMuscleGroup.Shoulders] = [MusclesEnum.ShouldersFront, MusclesEnum.ShouldersMiddle, MusclesEnum.ShouldersBack],
        [WorkoutMuscleGroup.Arms] = [MusclesEnum.Biceps, MusclesEnum.Triceps, MusclesEnum.Forearm],
        [WorkoutMuscleGroup.Abdominals] = [MusclesEnum.Abdominal],
        [WorkoutMuscleGroup.Legs] = [MusclesEnum.Quadriceps, MusclesEnum.Thighs, MusclesEnum.Buttocks, MusclesEnum.Caviar, MusclesEnum.ShinCamboloid, MusclesEnum.ShinAnteriorTibialis],
    };

    /// <summary>Order of muscle groups inside a workout: big compound groups first, abs last.</summary>
    private static readonly WorkoutMuscleGroup[] GroupOrder =
    [
        WorkoutMuscleGroup.Legs,
        WorkoutMuscleGroup.Back,
        WorkoutMuscleGroup.Chest,
        WorkoutMuscleGroup.Shoulders,
        WorkoutMuscleGroup.Arms,
        WorkoutMuscleGroup.Abdominals,
    ];

    private static readonly IReadOnlyDictionary<ExerciseTags, WorkoutEquipment> EquipmentTags = new Dictionary<ExerciseTags, WorkoutEquipment>
    {
        [ExerciseTags.BarbellExist] = WorkoutEquipment.Barbell,
        [ExerciseTags.DumbbellExist] = WorkoutEquipment.Dumbbells,
        [ExerciseTags.BenchExist] = WorkoutEquipment.Bench,
        [ExerciseTags.MachineExist] = WorkoutEquipment.Machines,
        [ExerciseTags.PullUpBarExist] = WorkoutEquipment.PullUpBar,
    };

    private static readonly IReadOnlyDictionary<WorkoutGoal, SetScheme> GoalSchemes = new Dictionary<WorkoutGoal, SetScheme>
    {
        [WorkoutGoal.Strength] = new(Sets: 5, RepetitionsFactor: 0.6, WeightFactor: 1.15, MinutesPerSet: 3.0),
        [WorkoutGoal.MuscleGain] = new(Sets: 4, RepetitionsFactor: 1.0, WeightFactor: 1.0, MinutesPerSet: 2.5),
        [WorkoutGoal.OverallFitness] = new(Sets: 3, RepetitionsFactor: 1.0, WeightFactor: 0.9, MinutesPerSet: 2.0),
        [WorkoutGoal.Endurance] = new(Sets: 3, RepetitionsFactor: 1.5, WeightFactor: 0.7, MinutesPerSet: 1.5),
        [WorkoutGoal.WeightLoss] = new(Sets: 3, RepetitionsFactor: 1.3, WeightFactor: 0.8, MinutesPerSet: 1.5),
    };

    /// <summary>Language-neutral resource with per-level values of the base exercises.</summary>
    private const string ExerciseLevelsResource = "exercise_levels";

    /// <summary>Used for exercises without level values, e.g. ones created by the user.</summary>
    private const int DefaultRepetitions = 12;

    /// <summary>Weight change between neighbouring sets of a pyramid, as a share of the working weight.</summary>
    private const double PyramidWeightStep = 0.075;

    /// <summary>Share of the level weights used for women; the level values are calibrated for men.</summary>
    private const double FemaleWeightFactor = 0.6;

    private const double PoundsInKilogram = 2.20462;

    private readonly Repository _repository;
    private readonly Random _random;

    public WorkoutQuestinariumService(Repository repository)
        : this(repository, Random.Shared)
    {
    }

    public WorkoutQuestinariumService(Repository repository, Random random)
    {
        _repository = repository;
        _random = random;
    }

    public async Task<int> CreateWorkoutAsync(string name, WorkoutQuestinariumAnswers answers, CancellationToken token)
    {
        var levels = (await ResourceExtension.LoadResourceAsync<ExerciseLevels>(ExerciseLevelsResource))
            .DistinctBy(item => item.CodeNum)
            .ToDictionary(item => item.CodeNum);
        var weightUnit = (MeasureWeightTypes)Settings.WeightMeasureType;

        return await Task.Run(() =>
        {
            var workout = BuildWorkout(answers, _repository.GetExerciseItems(), levels, weightUnit, _random);
            token.ThrowIfCancellationRequested();
            if (workout.Count == 0)
            {
                return 0;
            }

            int trainingId = 0;
            _repository.RunInTransaction(() =>
            {
                trainingId = _repository.SaveTrainingItem(new TrainingEntity { Title = name });

                var superSetIds = workout
                    .Where(item => item.SuperSetNumber != 0)
                    .GroupBy(item => item.SuperSetNumber)
                    .ToDictionary(group => group.Key, group => _repository.SaveSuperSetItem(new SuperSetEntity
                    {
                        TrainingId = trainingId,
                        Count = group.Count(),
                    }));

                int order = 0;
                foreach (var item in workout)
                {
                    _repository.SaveTrainingExerciseItem(new TrainingExerciseEntity
                    {
                        TrainingId = trainingId,
                        ExerciseId = item.Exercise.Id,
                        OrderNumber = order++,
                        SuperSetId = superSetIds.GetValueOrDefault(item.SuperSetNumber),
                        WeightAndRepsString = item.WeightAndRepsString,
                    });
                }
            });

            return trainingId;
        }, token);
    }

    /// <summary>Picks and orders exercises for the answers. Pure: no database access, randomness only from <paramref name="random"/>.</summary>
    /// <param name="levels">Per-level values of base exercises by <c>CodeNum</c>; weights are in kg.</param>
    /// <param name="weightUnit">Unit the generated weights are written in.</param>
    public static IReadOnlyList<GeneratedWorkoutExercise> BuildWorkout(
        WorkoutQuestinariumAnswers answers,
        IEnumerable<ExerciseEntity> exercises,
        IReadOnlyDictionary<int, ExerciseLevels> levels,
        MeasureWeightTypes weightUnit,
        Random random)
    {
        var candidates = exercises
            .Select(ToCandidate)
            .Where(candidate => IsEquipmentAvailable(candidate, answers.Equipment))
            .ToList();

        var scheme = GetSetScheme(answers);
        var result = new List<GeneratedWorkoutExercise>();

        int cardioBudgetMinutes = GetCardioMinutes(answers);
        var cardio = cardioBudgetMinutes == 0
            ? null
            : candidates
                .Where(candidate => candidate.IsCardio)
                .MaxBy(candidate => Score(candidate, answers) + random.NextDouble() * 2);
        double cardioMinutes = 0;
        if (cardio is not null)
        {
            var cardioTime = GetCardioTime(GetLevelValues(levels, cardio.Exercise, answers.Level), cardioBudgetMinutes);
            cardioMinutes = cardioTime.TotalMinutes;
            result.Add(new GeneratedWorkoutExercise(cardio.Exercise, JsonSerializer.Serialize(cardioTime), 0));
        }

        int count = Math.Clamp(
            (int)Math.Round(((int)answers.Duration - cardioMinutes) / (scheme.Sets * scheme.MinutesPerSet)),
            MinStrengthExercises,
            MaxStrengthExercises);
        var picked = PickStrengthExercises(candidates.Where(candidate => !candidate.IsCardio).ToList(), answers, count, random);

        bool useSuperSets = answers.Techniques.Contains(WorkoutTechnique.Supersets);
        if (!useSuperSets)
        {
            picked = picked
                .OrderBy(item => Array.IndexOf(GroupOrder, item.Group))
                .ThenByDescending(item => item.Candidate.Muscles.Count)
                .ToList();
        }

        var patterns = answers.Techniques.Where(technique => technique != WorkoutTechnique.Supersets).ToList();
        int superSetNumber = 0;
        GeneratedWorkoutExercise? pendingSuperSet = null;
        int repsExerciseIndex = 0;
        foreach (var item in picked)
        {
            var candidate = item.Candidate;
            var values = GetLevelValues(levels, candidate.Exercise, answers.Level);
            if (candidate.IsByTime)
            {
                result.Add(new GeneratedWorkoutExercise(candidate.Exercise, JsonSerializer.Serialize(GetHoldTime(values, answers)), 0));
                continue;
            }

            WorkoutTechnique? pattern = patterns.Count == 0 ? null : patterns[repsExerciseIndex % patterns.Count];
            repsExerciseIndex++;
            var sets = GetSets(pattern, scheme, values, GetGenderWeightFactor(answers.Gender), weightUnit);
            var generated = new GeneratedWorkoutExercise(candidate.Exercise, JsonSerializer.Serialize(sets), 0);

            if (!useSuperSets)
            {
                result.Add(generated);
            }
            else if (pendingSuperSet is null)
            {
                pendingSuperSet = generated;
            }
            else
            {
                superSetNumber++;
                result.Add(pendingSuperSet with { SuperSetNumber = superSetNumber });
                result.Add(generated with { SuperSetNumber = superSetNumber });
                pendingSuperSet = null;
            }
        }

        if (pendingSuperSet is not null)
        {
            result.Add(pendingSuperSet);
        }

        return result;
    }

    /// <summary>Round-robin over the selected muscle groups, taking the best-scored unused exercise for each.</summary>
    private static List<PickedExercise> PickStrengthExercises(List<Candidate> candidates, WorkoutQuestinariumAnswers answers, int count, Random random)
    {
        var groups = GroupOrder
            .Where(group => answers.Muscles.Count == 0 || answers.Muscles.Contains(group))
            .ToList();
        var scores = candidates.ToDictionary(candidate => candidate, candidate => Score(candidate, answers) + random.NextDouble() * 2);
        var used = new HashSet<Candidate>();
        var picked = new List<PickedExercise>();

        int failedInRow = 0;
        for (int i = 0; picked.Count < count && failedInRow < groups.Count; i++)
        {
            var group = groups[i % groups.Count];
            var groupMuscles = MuscleGroups[group];
            var available = candidates.Where(candidate => !used.Contains(candidate)).ToList();

            // Prefer exercises where the group is the main muscle, fall back to ones that only involve it.
            var best = available
                    .Where(candidate => candidate.PrimaryMuscle is { } primary && groupMuscles.Contains(primary))
                    .MaxBy(candidate => scores[candidate])
                ?? available
                    .Where(candidate => candidate.Muscles.Any(groupMuscles.Contains))
                    .MaxBy(candidate => scores[candidate]);

            if (best is null)
            {
                failedInRow++;
                continue;
            }

            failedInRow = 0;
            used.Add(best);
            picked.Add(new PickedExercise(best, group));
        }

        return picked;
    }

    private static double Score(Candidate candidate, WorkoutQuestinariumAnswers answers)
    {
        double score = (answers.Level, candidate.Exercise.DifficultType) switch
        {
            (WorkoutLevel.Beginner, DifficultTypes.Easy) => 3,
            (WorkoutLevel.Beginner, DifficultTypes.Medium) => 1,
            (WorkoutLevel.Beginner, _) => -5,
            (WorkoutLevel.Intermediate, DifficultTypes.Medium) => 3,
            (WorkoutLevel.Intermediate, _) => 1,
            (WorkoutLevel.Advanced, DifficultTypes.Hard) => 3,
            (WorkoutLevel.Advanced, DifficultTypes.Medium) => 2,
            _ => 0,
        };

        if (candidate.Equipment.Count > 0)
        {
            score += 2;
        }
        else
        {
            bool wantsBodyweight = answers.Equipment.Contains(WorkoutEquipment.Bodyweight) || answers.Equipment.Contains(WorkoutEquipment.YogaMat);
            score += wantsBodyweight ? 1 : -1;
        }

        if (answers.Goals.Contains(WorkoutGoal.Strength) || answers.Goals.Contains(WorkoutGoal.MuscleGain))
        {
            if (candidate.Tags.Contains(ExerciseTags.ExerciseByRepsAndWeight))
            {
                score += 1;
            }

            if (candidate.Muscles.Count >= 3)
            {
                score += 1;
            }
        }

        if ((answers.Goals.Contains(WorkoutGoal.WeightLoss) || answers.Goals.Contains(WorkoutGoal.Endurance))
            && candidate.Muscles.Contains(MusclesEnum.Cardio))
        {
            score += 2;
        }

        if (candidate.Tags.Contains(ExerciseTags.Flexibility))
        {
            score -= 1;
        }

        return score;
    }

    /// <summary>Exercises without equipment tags are bodyweight ones and are always available.</summary>
    private static bool IsEquipmentAvailable(Candidate candidate, IReadOnlyCollection<WorkoutEquipment> equipment)
    {
        return candidate.Equipment.All(equipment.Contains);
    }

    /// <summary>Average of the selected goals' schemes; beginners do one set less.</summary>
    private static SetScheme GetSetScheme(WorkoutQuestinariumAnswers answers)
    {
        IEnumerable<WorkoutGoal> goals = answers.Goals.Count == 0 ? [WorkoutGoal.OverallFitness] : answers.Goals;
        var schemes = goals.Select(goal => GoalSchemes[goal]).ToList();

        int sets = (int)Math.Round(schemes.Average(scheme => scheme.Sets));
        if (answers.Level == WorkoutLevel.Beginner)
        {
            sets = Math.Max(2, sets - 1);
        }

        return new SetScheme(
            sets,
            schemes.Average(scheme => scheme.RepetitionsFactor),
            schemes.Average(scheme => scheme.WeightFactor),
            schemes.Average(scheme => scheme.MinutesPerSet));
    }

    /// <summary>Values of the exercise for the answered level, or null for exercises without them (e.g. user-created).</summary>
    private static ExerciseLevelValues? GetLevelValues(IReadOnlyDictionary<int, ExerciseLevels> levels, ExerciseEntity exercise, WorkoutLevel level)
    {
        if (!levels.TryGetValue(exercise.CodeNum, out var exerciseLevels))
        {
            return null;
        }

        return level switch
        {
            WorkoutLevel.Beginner => exerciseLevels.Beginner,
            WorkoutLevel.Intermediate => exerciseLevels.Medium,
            _ => exerciseLevels.Profi,
        };
    }

    private static int GetCardioMinutes(WorkoutQuestinariumAnswers answers)
    {
        if (!answers.Goals.Contains(WorkoutGoal.WeightLoss) && !answers.Goals.Contains(WorkoutGoal.Endurance))
        {
            return 0;
        }

        // A quarter of the workout, rounded down to 5 minutes.
        return Math.Clamp((int)answers.Duration / 4 / 5 * 5, 5, 30);
    }

    /// <summary>The level's cardio time, but no longer than the cardio part of the workout.</summary>
    private static TimeSpan GetCardioTime(ExerciseLevelValues? values, int budgetMinutes)
    {
        var budget = TimeSpan.FromMinutes(budgetMinutes);
        return values is { Seconds: > 0 } && values.Seconds < budget.TotalSeconds
            ? TimeSpan.FromSeconds(values.Seconds)
            : budget;
    }

    private static TimeSpan GetHoldTime(ExerciseLevelValues? values, WorkoutQuestinariumAnswers answers)
    {
        double seconds = values is { Seconds: > 0 }
            ? values.Seconds
            : answers.Level switch
            {
                WorkoutLevel.Beginner => 30,
                WorkoutLevel.Intermediate => 45,
                _ => 60,
            };

        if (answers.Goals.Contains(WorkoutGoal.Endurance))
        {
            seconds *= 1.25;
        }

        return TimeSpan.FromSeconds(Math.Round(seconds / 5) * 5);
    }

    private static double GetGenderWeightFactor(WorkoutGender gender)
    {
        return gender == WorkoutGender.Female ? FemaleWeightFactor : 1;
    }

    /// <summary>Sets from the level's weight and repetitions, adjusted by the goals and gender and shaped by the technique.</summary>
    private static List<WeightAndRepsSet> GetSets(WorkoutTechnique? pattern, SetScheme scheme, ExerciseLevelValues? values, double genderWeightFactor, MeasureWeightTypes weightUnit)
    {
        int sets = scheme.Sets;
        double reps = (values is { Repetitions: > 0 } ? values.Repetitions : DefaultRepetitions) * scheme.RepetitionsFactor;
        double weight = (values?.Weight ?? 0) * scheme.WeightFactor * genderWeightFactor;

        return Enumerable.Range(0, sets).Select(i =>
        {
            // Position of the set relative to the middle one: -1.5, -0.5, 0.5, 1.5 for 4 sets.
            double offset = i - (sets - 1) / 2.0;
            double setReps = pattern switch
            {
                // Weight goes up, repetitions go down.
                WorkoutTechnique.PyramidDirect => reps - 2 * offset,
                // Weight goes down, repetitions go up.
                WorkoutTechnique.PyramidBack => reps + 2 * offset,
                // Repetitions climb to the middle set and back.
                WorkoutTechnique.Ladder => reps - 2 + 2 * Math.Min(i, sets - 1 - i),
                _ => reps,
            };
            double setWeightFactor = pattern switch
            {
                WorkoutTechnique.PyramidDirect => 1 + PyramidWeightStep * offset,
                WorkoutTechnique.PyramidBack => 1 - PyramidWeightStep * offset,
                _ => 1,
            };

            return new WeightAndRepsSet(
                RoundWeight(weight * setWeightFactor, weightUnit),
                Math.Max(MinRepetitions, (int)Math.Round(setReps)));
        }).ToList();
    }

    /// <summary>Converts kg to the user's unit and rounds to a step that can be loaded on a bar or picked as a dumbbell.</summary>
    private static double RoundWeight(double kilograms, MeasureWeightTypes weightUnit)
    {
        if (kilograms <= 0)
        {
            return 0;
        }

        bool isPounds = weightUnit == MeasureWeightTypes.Lbs;
        double value = isPounds ? kilograms * PoundsInKilogram : kilograms;
        double step = isPounds
            ? (value >= 40 ? 5 : 1)
            : (value >= 20 ? 2.5 : 1);

        return Math.Max(step, Math.Round(value / step) * step);
    }

    private static Candidate ToCandidate(ExerciseEntity exercise)
    {
        var tags = ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue).ToList();
        var muscles = ExerciseExtensions.ConvertMuscleStringToList(exercise.MusclesString);
        var equipment = tags
            .Where(EquipmentTags.ContainsKey)
            .Select(tag => EquipmentTags[tag])
            .ToList();

        return new Candidate(exercise, tags, muscles, equipment);
    }

    /// <summary>Goal-specific multipliers applied to the level values of an exercise.</summary>
    private sealed record SetScheme(int Sets, double RepetitionsFactor, double WeightFactor, double MinutesPerSet);

    /// <summary>Same shape as the stored weight-and-reps JSON of a set.</summary>
    private sealed record WeightAndRepsSet(double Weight, int Repetitions);

    private sealed record PickedExercise(Candidate Candidate, WorkoutMuscleGroup Group);

    private sealed record Candidate(
        ExerciseEntity Exercise,
        IReadOnlyList<ExerciseTags> Tags,
        IReadOnlyList<MusclesEnum> Muscles,
        IReadOnlyList<WorkoutEquipment> Equipment)
    {
        public bool IsByTime => Tags.Contains(ExerciseTags.ExerciseByTime);

        public bool IsCardio => IsByTime && Muscles.Contains(MusclesEnum.Cardio);

        /// <summary>Muscles are listed main-first; cardio is not a muscle group.</summary>
        public MusclesEnum? PrimaryMuscle => Muscles.Count == 0 || Muscles[0] == MusclesEnum.Cardio ? null : Muscles[0];
    }
}
