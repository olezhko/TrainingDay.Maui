namespace TrainingDay.Maui.Models.Questions;

/// <summary>Questionnaire steps, matching <c>number</c> in <c>questions_*.json</c>.</summary>
public enum WorkoutQuestion
{
    Level = 1,
    Goals = 2,
    Equipment = 3,
    Techniques = 4,
    Muscles = 5,
    Duration = 6,
    Gender = 7,
}

public enum WorkoutGender
{
    Male,
    Female,
}

public enum WorkoutLevel
{
    Beginner,
    Intermediate,
    Advanced,
}

public enum WorkoutGoal
{
    WeightLoss,
    MuscleGain,
    Endurance,
    Strength,
    OverallFitness,
}

public enum WorkoutEquipment
{
    Dumbbells,
    Barbell,
    PullUpBar,
    Machines,
    Bench,
    YogaMat,
    Bodyweight,
}

public enum WorkoutTechnique
{
    /// <summary>Straight sets: the same weight and repetitions in every set.</summary>
    Default,
    Supersets,
    PyramidDirect,
    PyramidBack,
    Ladder,
}

public enum WorkoutMuscleGroup
{
    Chest,
    Back,
    Shoulders,
    Arms,
    Abdominals,
    Legs,
}

/// <summary>Approximate workout length; the value is the number of minutes.</summary>
public enum WorkoutDuration
{
    Minutes30 = 30,
    Minutes60 = 60,
    Minutes90 = 90,
    Minutes120 = 120,
    Minutes180 = 180,
}
