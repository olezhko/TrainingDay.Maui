using Newtonsoft.Json;
using System.Collections.ObjectModel;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

[QueryProperty(nameof(LoadId), nameof(LoadId))]
public class ExerciseViewModel : BaseViewModel
{
    private string _name;
    private int _id;
    private List<ExerciseTags> _tags;
    private DescriptionViewModel _descriptionItem;
    private int codeNum;
    private ObservableCollection<MuscleViewModel> muscles;


    public ExerciseViewModel()
    {
        Muscles = new ObservableCollection<MuscleViewModel>();
        Tags = new List<ExerciseTags> { ExerciseTags.ExerciseByReps };
        Description = new DescriptionViewModel();
    }

    public ExerciseViewModel(Common.Models.Exercise exercise)
    {
        LoadExercise(exercise);
    }

    private void LoadExercise(Common.Models.Exercise exercise)
    {
        Id = exercise.Id;
        Tags = [.. ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue)];
        Name = exercise.Name;
        Muscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.ConvertFromStringToList(exercise.MusclesString));
        Description = DescriptionViewModel.ConvertFromJson(exercise.Description);
        CodeNum = exercise.CodeNum;
        OnPropertyChanged(nameof(IsBase));
    }

    public virtual ExerciseDto GetExercise()
    {
        return new ExerciseDto()
        {
            Id = Id,
            Description = JsonConvert.SerializeObject(Description?.Model),
            Name = Name,
            MusclesString = MusclesConverter.ConvertFromListToString(Muscles.ToList()),
            TagsValue = ExerciseExtensions.ConvertTagListToInt(Tags),
            CodeNum = CodeNum,
        };
    }

    #region Properties
    public bool IsBase => CodeNum != 0;
    public int Id { get; set; }
    public int LoadId
    {
        get => _id; 
        set
        {
            _id = value;
            var exercise = App.Database.GetExerciseItem(_id);
            LoadExercise(exercise);
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public List<ExerciseTags> Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            OnPropertyChanged();
        }
    }

    public DescriptionViewModel Description
    {
        get => _descriptionItem;
        set
        {
            _descriptionItem = value;
            OnPropertyChanged();
        }
    }

    public int CodeNum
    {
        get => codeNum;
        set
        {
            codeNum = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<MuscleViewModel> Muscles
    {
        get => muscles;
        set
        {
            muscles = value;
            OnPropertyChanged();
        }
    }
    #endregion
}