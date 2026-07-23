using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
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
    private DifficultTypes difficultType;
    private ObservableCollection<ExerciseVideo> videoItems;
    private bool isFavourite;

    public ExerciseViewModel()
    {
        Muscles = new ObservableCollection<MuscleViewModel>();
        Tags = new List<ExerciseTags> { ExerciseTags.ExerciseByReps };
        VideoItems = new ObservableCollection<ExerciseVideo>();
        Description = new DescriptionViewModel();
        ToggleFavouriteCommand = new RelayCommand(ToggleFavourite);
    }

    public ExerciseViewModel(ExerciseEntity exercise)
    {
        LoadExercise(exercise);
        ToggleFavouriteCommand = new RelayCommand(ToggleFavourite);
    }

    protected virtual void ToggleFavourite() => IsFavourite = !IsFavourite;

    private void LoadExercise(ExerciseEntity exercise)
    {
        Id = exercise.Id;
        Tags = [.. ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue)];
        Name = exercise.Name;
        Muscles = new ObservableCollection<MuscleViewModel>(MusclesExtensions.ConvertFromStringToList(exercise.MusclesString));
        Description = DescriptionViewModel.ConvertFromJson(exercise.Description);
        CodeNum = exercise.CodeNum;
        DifficultType = exercise.DifficultType;
        IsFavourite = exercise.IsFavourite;
        OnPropertyChanged(nameof(IsBase));
    }

    public virtual ExerciseEntity GetExercise()
    {
        return new ExerciseEntity()
        {
            Id = Id,
            Description = JsonSerializer.Serialize(Description?.Model),
            Name = Name,
            MusclesString = MusclesExtensions.ConvertFromListToString(Muscles.ToList()),
            TagsValue = ExerciseExtensions.ConvertTagListToInt(Tags),
            CodeNum = CodeNum,
            DifficultType = DifficultType,
            IsFavourite = IsFavourite
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
            OnPropertyChanged();
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


    public DescriptionViewModel Description { get => _descriptionItem; set => SetProperty(ref _descriptionItem, value); }

    public int CodeNum { get => codeNum; set => SetProperty(ref codeNum, value); }

    public ObservableCollection<MuscleViewModel> Muscles { get => muscles; set => SetProperty(ref muscles, value); }

    public DifficultTypes DifficultType { get => difficultType; set => SetProperty(ref difficultType, value); }

    public bool IsFavourite { get => isFavourite; set => SetProperty(ref isFavourite, value); }

    public ICommand ToggleFavouriteCommand { get; }

    public ObservableCollection<ExerciseVideo> VideoItems { get => videoItems; set => SetProperty(ref videoItems, value); }
    public byte[] ImageData { get; internal set; }

    #endregion
}
