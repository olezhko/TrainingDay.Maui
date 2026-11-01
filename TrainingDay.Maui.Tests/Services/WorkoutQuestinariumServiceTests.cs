using System.Text.Json;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Questions;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Tests.Services
{
    [TestFixture]
    public class WorkoutQuestinariumServiceTests
    {
        private static readonly List<ExerciseEntity> Exercises =
        [
            Exercise(1, "Chest,Triceps", DifficultTypes.Easy, ExerciseTags.ExerciseByRepsAndWeight, ExerciseTags.BarbellExist, ExerciseTags.BenchExist),
            Exercise(2, "Chest,ShouldersFront", DifficultTypes.Easy, ExerciseTags.ExerciseByRepsAndWeight, ExerciseTags.MachineExist),
            Exercise(3, "Chest,Triceps", DifficultTypes.Easy, ExerciseTags.ExerciseByReps, ExerciseTags.CanDoAtHome),
            Exercise(4, "WidestBack,Biceps", DifficultTypes.Hard, ExerciseTags.ExerciseByRepsAndWeight, ExerciseTags.PullUpBarExist),
            Exercise(5, "MiddleBack,WidestBack", DifficultTypes.Medium, ExerciseTags.ExerciseByRepsAndWeight, ExerciseTags.DumbbellExist),
            Exercise(6, "Quadriceps,Buttocks", DifficultTypes.Easy, ExerciseTags.ExerciseByReps, ExerciseTags.CanDoAtHome),
            Exercise(7, "Abdominal", DifficultTypes.Easy, ExerciseTags.ExerciseByTime, ExerciseTags.CanDoAtHome),
            Exercise(8, "Cardio,Quadriceps", DifficultTypes.Easy, ExerciseTags.ExerciseByTime, ExerciseTags.CanDoAtHome),
        ];

        private static readonly Dictionary<int, ExerciseLevels> Levels = new()
        {
            [1] = new ExerciseLevels
            {
                CodeNum = 1,
                Beginner = new ExerciseLevelValues { Weight = 30, Repetitions = 12 },
                Medium = new ExerciseLevelValues { Weight = 50, Repetitions = 10 },
                Profi = new ExerciseLevelValues { Weight = 80, Repetitions = 8 },
            },
            [7] = new ExerciseLevels
            {
                CodeNum = 7,
                Beginner = new ExerciseLevelValues { Seconds = 20 },
                Medium = new ExerciseLevelValues { Seconds = 60 },
                Profi = new ExerciseLevelValues { Seconds = 120 },
            },
            [8] = new ExerciseLevels
            {
                CodeNum = 8,
                Beginner = new ExerciseLevelValues { Seconds = 600 },
                Medium = new ExerciseLevelValues { Seconds = 1200 },
                Profi = new ExerciseLevelValues { Seconds = 1800 },
            },
        };

        [Test]
        public void BuildWorkout_UsesLevelValues()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest]);

            var workout = Build(answers);

            var benchPress = Sets(workout.Single(item => item.Exercise.CodeNum == 1));
            Assert.That(benchPress, Has.Count.EqualTo(4));
            Assert.That(benchPress.Select(set => set.Weight), Has.All.EqualTo(50));
            Assert.That(benchPress.Select(set => set.Repetitions), Has.All.EqualTo(10));
        }

        [Test]
        public void BuildWorkout_UsesBeginnerValues_ForBeginner()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest, WorkoutMuscleGroup.Abdominals]) with { Level = WorkoutLevel.Beginner };

            var workout = Build(answers);

            Assert.That(Sets(workout.Single(item => item.Exercise.CodeNum == 1)).Select(set => set.Weight), Has.All.EqualTo(30));
            Assert.That(JsonSerializer.Deserialize<TimeSpan>(workout.Single(item => item.Exercise.CodeNum == 7).WeightAndRepsString), Is.EqualTo(TimeSpan.FromSeconds(20)));
        }

        [Test]
        public void BuildWorkout_Female_UsesLighterWeights()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest]) with { Gender = WorkoutGender.Female };

            var sets = Sets(Build(answers).Single(item => item.Exercise.CodeNum == 1));

            Assert.That(sets.Select(set => set.Weight), Has.All.EqualTo(30));
            Assert.That(sets.Select(set => set.Repetitions), Has.All.EqualTo(10));
        }

        [Test]
        public void BuildWorkout_StrengthGoal_HeavierWithFewerRepetitions()
        {
            var answers = Answers(goals: [WorkoutGoal.Strength], muscles: [WorkoutMuscleGroup.Chest]);

            var sets = Sets(Build(answers).Single(item => item.Exercise.CodeNum == 1));

            Assert.That(sets.Select(set => set.Weight), Has.All.GreaterThan(50));
            Assert.That(sets.Select(set => set.Repetitions), Has.All.LessThan(10));
        }

        [Test]
        public void BuildWorkout_ConvertsWeightToPounds()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest]);

            var workout = Build(answers, MeasureWeightTypes.Lbs);

            Assert.That(Sets(workout.Single(item => item.Exercise.CodeNum == 1)).Select(set => set.Weight), Has.All.EqualTo(110));
        }

        [Test]
        public void BuildWorkout_PyramidDirect_IncreasesWeight()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest], techniques: [WorkoutTechnique.PyramidDirect]);

            var weights = Sets(Build(answers).Single(item => item.Exercise.CodeNum == 1)).Select(set => set.Weight).ToList();

            Assert.That(weights, Is.Ordered.Ascending);
            Assert.That(weights.First(), Is.LessThan(weights.Last()));
        }

        [Test]
        public void BuildWorkout_DefaultTechnique_UsesStraightSets()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest], techniques: [WorkoutTechnique.Default]);

            var workout = Build(answers);

            Assert.That(workout.Select(item => item.SuperSetNumber), Has.All.EqualTo(0));
            var sets = Sets(workout.Single(item => item.Exercise.CodeNum == 1));
            Assert.That(sets.Select(set => set.Weight).Distinct().Count(), Is.EqualTo(1));
            Assert.That(sets.Select(set => set.Repetitions).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Parse_MapsOptionsToEnums_AndIgnoresUnknown()
        {
            var answers = WorkoutQuestinariumAnswers.Parse(new Dictionary<WorkoutQuestion, IReadOnlyCollection<string>>
            {
                [WorkoutQuestion.Gender] = ["Female"],
                [WorkoutQuestion.Level] = ["Advanced"],
                [WorkoutQuestion.Goals] = ["MuscleGain", "Something"],
                [WorkoutQuestion.Duration] = ["Minutes90"],
            });

            Assert.That(answers.Gender, Is.EqualTo(WorkoutGender.Female));
            Assert.That(answers.Level, Is.EqualTo(WorkoutLevel.Advanced));
            Assert.That(answers.Goals, Is.EquivalentTo(new[] { WorkoutGoal.MuscleGain }));
            Assert.That(answers.Equipment, Is.Empty);
            Assert.That(answers.Duration, Is.EqualTo(WorkoutDuration.Minutes90));
        }

        [Test]
        public void BuildWorkout_UsesOnlyAvailableEquipmentAndBodyweight()
        {
            var answers = Answers(equipment: [WorkoutEquipment.Machines]);

            var workout = Build(answers);

            var codes = workout.Select(item => item.Exercise.CodeNum).ToList();
            Assert.That(codes, Is.Not.Empty);
            Assert.That(codes.Intersect([1, 4, 5]), Is.Empty);
        }

        [Test]
        public void BuildWorkout_PicksOnlySelectedMuscleGroups()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest]);

            var workout = Build(answers);

            Assert.That(workout.Select(item => item.Exercise.CodeNum), Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void BuildWorkout_StartsWithCardio_ForWeightLoss()
        {
            var answers = Answers(goals: [WorkoutGoal.WeightLoss]);

            var workout = Build(answers);

            Assert.That(workout[0].Exercise.CodeNum, Is.EqualTo(8));
            Assert.That(JsonSerializer.Deserialize<TimeSpan>(workout[0].WeightAndRepsString), Is.EqualTo(TimeSpan.FromMinutes(15)));
        }

        [Test]
        public void BuildWorkout_PyramidDirect_DecreasesRepetitions()
        {
            var answers = Answers(muscles: [WorkoutMuscleGroup.Chest], techniques: [WorkoutTechnique.PyramidDirect]);

            var workout = Build(answers);

            var reps = Sets(workout[0]).Select(set => set.Repetitions).ToList();
            Assert.That(reps, Is.Ordered.Descending);
            Assert.That(reps.Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void BuildWorkout_Supersets_PairsExercises()
        {
            var answers = Answers(techniques: [WorkoutTechnique.Supersets]);

            var workout = Build(answers);

            var pairs = workout.Where(item => item.SuperSetNumber != 0).GroupBy(item => item.SuperSetNumber).ToList();
            Assert.That(pairs, Is.Not.Empty);
            Assert.That(pairs.Select(group => group.Count()), Has.All.EqualTo(2));
        }

        private static WorkoutQuestinariumAnswers Answers(
            WorkoutGoal[]? goals = null,
            WorkoutEquipment[]? equipment = null,
            WorkoutTechnique[]? techniques = null,
            WorkoutMuscleGroup[]? muscles = null)
        {
            return new WorkoutQuestinariumAnswers(
                WorkoutGender.Male,
                WorkoutLevel.Intermediate,
                goals ?? [WorkoutGoal.MuscleGain],
                equipment ?? Enum.GetValues<WorkoutEquipment>(),
                techniques ?? [],
                muscles ?? [],
                WorkoutDuration.Minutes60);
        }

        private static IReadOnlyList<GeneratedWorkoutExercise> Build(WorkoutQuestinariumAnswers answers, MeasureWeightTypes weightUnit = MeasureWeightTypes.Kilograms)
        {
            return WorkoutQuestinariumService.BuildWorkout(answers, Exercises, Levels, weightUnit, new Random(1));
        }

        private static List<SetValues> Sets(GeneratedWorkoutExercise item)
        {
            return JsonSerializer.Deserialize<List<SetValues>>(item.WeightAndRepsString)!;
        }

        private static ExerciseEntity Exercise(int code, string muscles, DifficultTypes difficulty, params ExerciseTags[] tags)
        {
            return new ExerciseEntity
            {
                Id = code,
                CodeNum = code,
                MusclesString = muscles,
                DifficultType = difficulty,
                TagsValue = ExerciseExtensions.ConvertTagListToInt(tags),
            };
        }

        private sealed record SetValues(double Weight, int Repetitions);
    }
}
