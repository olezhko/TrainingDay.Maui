using Newtonsoft.Json;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Models;

public class TrainingUnionViewModel
{
    public TrainingUnionViewModel()
    {
        TrainingIDs = [];
    }

    public TrainingUnionViewModel(TrainingUnionEntity union)
    {
        Id = union.Id;
        Name = union.Name;
        if (!string.IsNullOrEmpty(union.TrainingIDsString))
        {
            TrainingIDs = JsonConvert.DeserializeObject<List<int>>(union.TrainingIDsString);
        }
        else
        {
            TrainingIDs = [];
        }
    }

    public int Id { get; set; }

    public string Name { get; set; }

    public List<int> TrainingIDs { get; set; }

    public TrainingUnionEntity Model =>
        new TrainingUnionEntity()
        {
            Id = Id,
            Name = Name,
            TrainingIDsString = JsonConvert.SerializeObject(TrainingIDs),
        };
}