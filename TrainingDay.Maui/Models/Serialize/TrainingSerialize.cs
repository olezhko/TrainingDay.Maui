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

    public static TrainingSerialize LoadFromFile(byte[] data)
    {
        try
        {
            MemoryStream dataStream = new MemoryStream(data);
            BinaryReader b = new BinaryReader(dataStream);
            //b.re
            //var training = b.Deserialize(dataStream) as TrainingSerialize;
            //dataStream.Close();
            return new TrainingSerialize();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SaveToFile(string filename)
    {
        var stream = new FileStream(filename, FileMode.Create);
        BinaryWriter b = new BinaryWriter(stream);
        //b.Write(prop.GetValue(this, null).c);
        stream.Close();
    }
}