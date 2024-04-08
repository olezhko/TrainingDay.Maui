namespace TrainingDay.Maui.Models.Serialize;

[Serializable]
public class TrainingExerciseSerialize
{
    public int ExerciseId { get; set; }
    public int TrainingId { get; set; }
    public string ExerciseItemName { get; set; }
    public string Description { get; set; }
    public int OrderNumber { get; set; }
    public string Muscles { get; set; }
    public int SuperSetId { get; set; } = -1;
    public string WeightAndRepsString { get; set; }
    public bool IsNotFinished { get; set; }
    public int SuperSetNum { get; set; }
    public int TagsValue { get; set; }
    public int TrainingExerciseId { get; set; }
    public int CodeNum { get; set; }
}