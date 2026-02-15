using CommunityToolkit.Maui.Alerts;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class HistoryTrainingPageViewModel : BaseViewModel
{
    private IOrderedEnumerable<LastTrainingEntity> baseItems;
    private static List<Tuple<string, int>> DaysAndTextLimits = new List<Tuple<string, int>>();

    public HistoryTrainingPageViewModel()
    {
        LastTrainings = new ObservableCollection<LastTrainingViewModelList>();
        ItemSelectedCommand = new Command<LastTrainingViewModel>(SelectionChanged);

        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.WeekString, 7));
        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.OneMounthString, 31));
        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.ThreeMounthString, 91));
        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.HalfYearString, 182));
        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.YearString, 365));
        DaysAndTextLimits.Add(new Tuple<string, int>(AppResources.MoreThanYearString, -1));
    }

    public ObservableCollection<LastTrainingViewModelList> LastTrainings { get; set; }

    public INavigation Navigation { get; set; }

    public LastTrainingViewModel SelectedTraining { get; set; }

    public ICommand ItemSelectedCommand { get; set; }

    public ICommand StartAgainCommand => new Command(StartAgainTraining);

    public ICommand RemoveLastTrainingCommand => new Command(RemoveLastTraining);

    public ICommand RemainingItemsThresholdReachedCommand => new Command(RemainingItemsThresholdReached);

    private int _itemTreshold;

    public int ItemTreshold
    {
        get => _itemTreshold;
        set => SetProperty(ref _itemTreshold, value);
    }

    public void LoadItems(int page = 1)
    {
        if (page == 1)
        {
            ItemTreshold = 1;
            LastTrainings.Clear();
            baseItems = App.Database.GetLastTrainingItems().OrderByDescending(item => item.Time);
        }

        List<LastTrainingViewModelList> tempLastTrainings = new List<LastTrainingViewModelList>();
        var newItems = baseItems.Skip((page - 1) * 10).Take(10).ToList();
        foreach (var lastTraining in newItems)
        {
            try
            {
                var newItem = new LastTrainingViewModel()
                {
                    ElapsedTime = lastTraining.ElapsedTime,
                    ImplementDateTime = lastTraining.Time,
                    Title = lastTraining.Title,
                    Id = lastTraining.Id,
                    TrainingId = lastTraining.TrainingId,
                };

                PutItemToListByDate(tempLastTrainings, newItem);
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        foreach (var tempLastTraining in tempLastTrainings)
        {
            LastTrainings.Add(tempLastTraining);
        }
        OnPropertyChanged(nameof(LastTrainings));
    }

    private void SelectionChanged(LastTrainingViewModel selected)
    {
        SelectedTraining = selected;
        var trainingExerciseItems = App.Database.GetLastTrainingExerciseItems().Where(item => item.LastTrainingId == SelectedTraining.Id);
        foreach (var trainingExercise in trainingExerciseItems)
        {
            var ex = new TrainingExerciseViewModel()
            {
                Name = trainingExercise.ExerciseName,
                Muscles = new ObservableCollection<MuscleViewModel>(
                    MusclesExtensions.ConvertFromStringToList(trainingExercise.MusclesString)),
                OrderNumber = trainingExercise.OrderNumber,
                Description = DescriptionViewModel.ConvertFromJson(trainingExercise.Description),
                TrainingExerciseId = trainingExercise.Id,
                SuperSetId = trainingExercise.SuperSetId,
                CodeNum = trainingExercise.CodeNum,
            };

            ex.Tags = ExerciseExtensions.ConvertTagIntToList(trainingExercise.TagsValue).ToList();
            ExerciseManager.ConvertJsonBack(ex, trainingExercise.WeightAndRepsString);
            SelectedTraining.Items.Add(ex);
        }

        HistoryTrainingExercisesPage page = new HistoryTrainingExercisesPage() { BindingContext = this };
        Navigation.PushAsync(page);
    }

    private void PutItemToListByDate(List<LastTrainingViewModelList> tempLastTrainings, LastTrainingViewModel newItem)
    {
        foreach (var daysAndTextLimit in DaysAndTextLimits)
        {
            var result = AddLastTraining(newItem, daysAndTextLimit.Item1, daysAndTextLimit.Item2, tempLastTrainings);
            if (result)
            {
                break;
            }
        }
    }

    private bool AddLastTraining(LastTrainingViewModel newItem, string daysSectorString, int daysSector, List<LastTrainingViewModelList> tempLastTrainings)
    {
        bool addResult = false;
        if (daysSector == -1 || DateTime.Now - newItem.ImplementDateTime < TimeSpan.FromDays(daysSector))
        {
            var index = tempLastTrainings.FirstOrDefault(a => a.Heading == daysSectorString);
            if (index != null)
            {
                index.Add(newItem);
                addResult = true;
            }
            else
            {
                tempLastTrainings.Add(new LastTrainingViewModelList { Heading = daysSectorString });
                tempLastTrainings.Last().Add(newItem);
                addResult = true;
            }
        }

        return addResult;
    }

    private void StartAgainTraining()
    {
        int trId = SelectedTraining.TrainingId;
        TrainingViewModel training = new TrainingViewModel();
        if (trId == 0)
        {
            training.Title = SelectedTraining.Title;
            foreach (var trainingExerciseViewModel in SelectedTraining.Items)
            {
                training.AddExercise(trainingExerciseViewModel);
            }
        }
        else
        {
            training = new TrainingViewModel(App.Database.GetTrainingItem(trId));
            PrepareTrainingViewModel(training);
        }

        Navigation.PushAsync(new TrainingImplementPage() { TrainingItem = training, Title = training.Title });
    }

    private void PrepareTrainingViewModel(TrainingViewModel vm)
    {
        var trainingExerciseItems = App.Database.GetTrainingExerciseItems();
        var exerciseItems = App.Database.GetExerciseItems();
        var trainingExercises = trainingExerciseItems.Where(ex => ex.TrainingId == vm.Id);
        var unOrderedItems = trainingExercises.Where(a => a.OrderNumber < 0);

        trainingExercises = trainingExercises.OrderBy(a => a.OrderNumber).Where(a => a.OrderNumber >= 0).ToList();
        int index = 0;
        foreach (var trainingExercise in trainingExercises)
        {
            var exercise = exerciseItems.First(ex => ex.Id == trainingExercise.ExerciseId);
            var trEx = new TrainingExerciseViewModel(exercise, trainingExercise)
            {
                TrainingExerciseId = trainingExercise.Id,
            };
            index++;

            vm.AddExercise(trEx);
        }

        foreach (var trainingExercise in unOrderedItems)
        {
            if (trainingExercise.OrderNumber == -1)
            {
                trainingExercise.OrderNumber = index;
                App.Database.SaveTrainingExerciseItem(trainingExercise);
            }

            var exercise = exerciseItems.First(ex => ex.Id == trainingExercise.ExerciseId);
            var trEx = new TrainingExerciseViewModel(exercise, trainingExercise)
            {
                TrainingExerciseId = trainingExercise.Id,
            };
            index++;
            vm.AddExercise(trEx);
        }
    }

    private async void RemoveLastTraining()
    {
        var result = await MessageManager.DisplayAlert(AppResources.DeleteTraining, SelectedTraining.Title, AppResources.YesString, AppResources.CancelString);
        if (result)
        {
            App.Database.DeleteLastTraining(SelectedTraining.Id);
            foreach (var trainingExerciseViewModel in SelectedTraining.Items)
            {
                App.Database.DeleteLastTrainingExercise(trainingExerciseViewModel.TrainingExerciseId);
            }

            if (LastTrainings.Select(lastTraining => lastTraining.Remove(SelectedTraining)).Any(res => res))
            {
                return;
            }

            await Toast.Make(AppResources.DeletedString).Show();
            await Shell.Current.GoToAsync("..");
        }
    }

    private void RemainingItemsThresholdReached()
    {
        LoadItems(++ItemTreshold);
    }
}

public class LastTrainingViewModelList : ObservableCollection<LastTrainingViewModel>
{
    public string Heading { get; set; }
}