using Newtonsoft.Json;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.ViewModels;

public class DescriptionViewModel : BaseViewModel
{
    public Description Model;

    public string StartPosition
    {
        get => Model.StartPosition;
        set
        {
            Model.StartPosition = value;
            OnPropertyChanged();
        }
    }

    public string Execution
    {
        get => Model.Execution;
        set
        {
            Model.Execution = value;
            OnPropertyChanged();
        }
    }

    public string Advice
    {
        get => Model.Advice;
        set
        {
            Model.Advice = value;
            OnPropertyChanged();
        }
    }

    public static DescriptionViewModel ConvertFromJson(string jsonString)
    {
        DescriptionViewModel model = new DescriptionViewModel();

        if (jsonString.IsNotNullOrEmpty())
        {
            var obj = JsonConvert.DeserializeObject<Description>(jsonString);
            model.Advice = obj.Advice;
            model.Execution = obj.Execution;
            model.StartPosition = obj.StartPosition;
        }

        return model;
    }
}