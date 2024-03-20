using Newtonsoft.Json;
using System.Collections.ObjectModel;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class ExerciseViewModel : BaseViewModel
{
    public int Id { get; set; }
    private string _name;
    public string ExerciseItemName
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    private string _imageUrl;
    public string ExerciseImageUrl
    {
        get => _imageUrl;
        set
        {
            _imageUrl = value;
            OnPropertyChanged();
        }
    }

    private List<TrainingDay.Common.ExerciseTags> _tags;
    public List<TrainingDay.Common.ExerciseTags> Tags
    {
        get => _tags;
        set
        {
            _tags = value;
            OnPropertyChanged();
        }
    }

    private DescriptionViewModel _descriptionItem;
    public DescriptionViewModel Description
    {
        get => _descriptionItem;
        set
        {
            _descriptionItem = value;
            OnPropertyChanged();
        }
    }

    private int codeNum;
    public int CodeNum
    {
        get => codeNum;
        set
        {
            codeNum = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<MuscleViewModel> muscles;
    public ObservableCollection<MuscleViewModel> Muscles
    {
        get => muscles;
        set
        {
            muscles = value;
            OnPropertyChanged();
        }
    }
    public ExerciseViewModel()
    {
        Muscles = new ObservableCollection<MuscleViewModel>();
        Tags = new List<TrainingDay.Common.ExerciseTags> { TrainingDay.Common.ExerciseTags.ExerciseByReps };
        Description = new DescriptionViewModel();
    }

    public ExerciseViewModel(Exercise exercise)
    {
        Tags = TrainingDay.Common.ExerciseTools.ConvertFromIntToTagList(exercise.TagsValue);
        ExerciseItemName = exercise.ExerciseItemName;
        Id = exercise.Id;
        ExerciseImageUrl = exercise.ExerciseImageUrl;
        Muscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.ConvertFromStringToList(exercise.MusclesString));
        Description = DescriptionViewModel.ConvertFromJson(exercise.Description);
        CodeNum = exercise.CodeNum;
    }

    public virtual Exercise GetExercise()
    {
        return new Exercise()
        {
            Id = Id,
            Description = JsonConvert.SerializeObject(Description?.Model),
            ExerciseImageUrl = ExerciseImageUrl,
            ExerciseItemName = ExerciseItemName,
            MusclesString = MusclesConverter.ConvertFromListToString(Muscles.ToList()),
            TagsValue = TrainingDay.Common.ExerciseTools.ConvertTagListToInt(Tags),
            CodeNum = CodeNum,
        };
    }

    public bool IsBase => CodeNum != 0;
}