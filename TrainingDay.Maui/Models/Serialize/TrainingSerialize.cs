using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Text;

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

    public static TrainingSerialize LoadFromData(string data)
    {
        try
        {
            var training = JsonConvert.DeserializeObject<TrainingSerialize>(data);
            return training;
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static TrainingSerialize LoadFromFile(string filename)
    {
        try
        {
            var content = File.ReadAllText(filename);
            var training = JsonConvert.DeserializeObject<TrainingSerialize>(content);
            return training;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveToFile(string filename)
    {
        var content = JsonConvert.SerializeObject(this);
        File.WriteAllText(filename, content, Encoding.UTF8);
    }
}