using System.Collections.ObjectModel;

namespace TrainingDay.Maui.Models.Serialize;

[Serializable]
public class TrainingSerialize
{
    public TrainingSerialize()
    {
        Items = new ObservableCollection<TrainingExerciseSerialize>();
    }

    public string Title { get; set; }

    public ObservableCollection<TrainingExerciseSerialize> Items { get; set; }

    public int Id { get; set; }
}