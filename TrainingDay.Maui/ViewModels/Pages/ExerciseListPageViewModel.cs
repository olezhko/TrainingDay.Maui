using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class ExerciseListPageViewModel : BaseViewModel, IQueryAttributable
{
    private List<int> selectedIndexes = new List<int>();
    public FilterModel Filter { get; set; }
    public ObservableCollection<ExerciseListItemViewModel> BaseItems { get; set; } // все упражнения из базы
    public ObservableCollection<ExerciseListItemViewModel> Items { get; set; } // элементы, отображенные на экране в списке
    public ObservableCollection<TrainingExerciseViewModel> ExistedExercises { get; set; }

    public ICommand ViewFilterWindowCommand { get; protected set; }

    public ICommand ChoseExercisesCommand { get; protected set; }

    public ICommand CancelCommand { get; protected set; }

    public ExerciseListPageViewModel()
    {
        Filter = new FilterModel();
        ChoseExercisesCommand = new Command(ChoseExercises);
        ViewFilterWindowCommand = new Command(ViewFilterWindow);
        CancelCommand = new Command(Cancel);
        Items = new ObservableCollection<ExerciseListItemViewModel>();
        BaseItems = new ObservableCollection<ExerciseListItemViewModel>();
        UpdateItems();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("ExistedExercises"))
        {
            ExistedExercises = query["ExistedExercises"] as ObservableCollection<TrainingExerciseViewModel>;
        }
        else
        {
            ExistedExercises = [];
        }
    }

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<FilterAcceptedForExercisesMessage>(this, (r, m) =>
        {
            Filter = m.Filter;
            UpdateItems();

            UnsubscribeMessages();
            LoggingService.TrackEvent($"{GetType().Name}: FilterAcceptedForExercisesMessage");
        });
    }

    private void UnsubscribeMessages() => WeakReferenceMessenger.Default.Unregister<FilterAcceptedForExercisesMessage>(this);

    private async void Cancel() => await Shell.Current.GoToAsync("..");

    /// <summary>
    /// Если элементы были выбраны путем поиска, а потом сбросили поиск, то оставить их выбранными
    /// </summary>
    /// <returns></returns>
    private List<ExerciseListItemViewModel> GetSelectedItems()
    {
        IEnumerable<int> ids = BaseItems.Where(a => a.IsSelected)
            .Union(Items.Where(a => a.IsSelected))
            .Select(x => x.Id)
            .Union(selectedIndexes)
            .Distinct();

        
        List<ExerciseListItemViewModel> resultItems = new List<ExerciseListItemViewModel>();

        foreach (var id in ids)
        {
            var item = BaseItems.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                resultItems.Add(item);
            }
            else
            {
                item = Items.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    resultItems.Add(item);
                }
            }
        }

        return resultItems;
    }

    public void UpdateItems() => LoadItemsAsync();

    private async void LoadItemsAsync()
    {
        IsBusy = true;
        var res = await Task.Run(LoadItems);
        Items = res;

        if (!BaseItems.Any())
        {
            BaseItems = new ObservableCollection<ExerciseListItemViewModel>(res);
        }
        
        OnPropertyChanged(nameof(Items));
        IsBusy = false;
    }

    private ObservableCollection<ExerciseListItemViewModel> LoadItems()
    {
        var newItems = new ObservableCollection<ExerciseListItemViewModel>();
        var baseItems = App.Database.GetExerciseItems();
        try
        {
            FillSelectedIndexes();

            foreach (var exercise in baseItems)
            {
                var newItem = new ExerciseListItemViewModel(exercise);
                newItem.IsExerciseExistsInWorkout = ExistedExercises != null && ExistedExercises.Any(x => x.ExerciseId == exercise.Id);
                newItem.IsSelected = selectedIndexes.Contains(newItem.Id);
                bool? byname = null, byFilter = null, atHome = null, barbell = null, dumbbell = null, byLevel = null;

                var tags = ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue);

                if (Filter.IsNoEquipmentFilter)
                {
                    atHome = tags.Contains(ExerciseTags.CanDoAtHome);
                }

                if (Filter.IsBarbellExists)
                {
                    barbell = tags.Contains(ExerciseTags.BarbellExist);
                }

                if (Filter.IsDumbbellExists)
                {
                    dumbbell = tags.Contains(ExerciseTags.DumbbellExist);
                }

                if (!string.IsNullOrEmpty(Filter.NameFilter))
                {
                    byname = newItem.Name.Contains(Filter.NameFilter, StringComparison.OrdinalIgnoreCase);
                }

                if (Filter.CurrentMuscles.Count != 0)
                {
                    byFilter = newItem.Muscles.Select(i => (MusclesEnum)i.Id).Intersect(Filter.CurrentMuscles).Count() != 0;
                }

                if (Filter.DifficultyLevel != 0)
                {
                    byLevel = (int)newItem.DifficultType == Filter.DifficultyLevel;
                }

                if (byname.HasValue && !byname.Value 
                    || byFilter.HasValue && !byFilter.Value 
                    || atHome.HasValue && !atHome.Value
                    || barbell.HasValue && !barbell.Value
                    || dumbbell.HasValue && !dumbbell.Value
                    || byLevel.HasValue && !byLevel.Value)
                {
                    continue;
                }

                newItems.Add(newItem);
            }

            return newItems;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            LoggingService.TrackError(e);
            try
            {
                foreach (var exercise in baseItems)
                {
                    newItems.Add(new ExerciseListItemViewModel(exercise));
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                LoggingService.TrackError(e);
            }

            return newItems;
        }
    }

    private async void ViewFilterWindow()
    {
        SubscribeMessages();
        Dictionary<string, object> param = new Dictionary<string, object> { { "Filter", Filter } };
        await Shell.Current.GoToAsync(nameof(FilterPage), param);
    }

    private async void ChoseExercises()
    {
        WeakReferenceMessenger.Default.Send(new ExercisesSelectFinishedMessage(GetSelectedItems()));
        await Shell.Current.GoToAsync("..");
    }

    private void FillSelectedIndexes()
    {
        foreach (var trainingExerciseViewModel in Items)
        {
            if (trainingExerciseViewModel.IsSelected && !selectedIndexes.Contains(trainingExerciseViewModel.Id))
            {
                selectedIndexes.Add(trainingExerciseViewModel.Id);
            }

            if (!trainingExerciseViewModel.IsSelected && selectedIndexes.Contains(trainingExerciseViewModel.Id))
            {
                selectedIndexes.Remove(trainingExerciseViewModel.Id);
            }
        }
    }
}