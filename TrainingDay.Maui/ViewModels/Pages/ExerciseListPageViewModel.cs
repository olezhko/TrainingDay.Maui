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
    private List<int> selectedIndexes = [];
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
        
        var resultItems = new List<ExerciseListItemViewModel>();

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

        try
        {
            FillSelectedIndexes();

            var baseItems = App.Database.GetExerciseItems();
            var nameFilter = Filter.NameFilter?.Trim();
            var hasMuscleFilter = Filter.CurrentMuscles?.Count > 0;
            var existedExerciseIds = ExistedExercises?.Select(x => x.ExerciseId).ToHashSet() ?? new HashSet<int>();
            bool checkTags = Filter.IsNoEquipmentFilter || Filter.IsBarbellExists || Filter.IsDumbbellExists;

            foreach (var exercise in baseItems)
            {
                bool match = true;

                if (!string.IsNullOrEmpty(nameFilter))
                {
                    match &= exercise.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase);
                }

                List<ExerciseTags>? tags = null;
                if (checkTags)
                    tags = [.. ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue)];

                if (Filter.IsNoEquipmentFilter)
                    match &= tags!.Contains(ExerciseTags.CanDoAtHome);

                if (Filter.IsBarbellExists)
                    match &= tags!.Contains(ExerciseTags.BarbellExist);

                if (Filter.IsDumbbellExists)
                    match &= tags!.Contains(ExerciseTags.DumbbellExist);

                if (Filter.DifficultyLevel != 0)
                    match &= (int)exercise.DifficultType == Filter.DifficultyLevel;

                var newItem = new ExerciseListItemViewModel(exercise)
                {
                    IsExerciseExistsInWorkout = existedExerciseIds.Contains(exercise.Id),
                    IsSelected = selectedIndexes.Contains(exercise.Id)
                };

                if (hasMuscleFilter)
                {
                    var exerciseMuscles = newItem.Muscles.Select(m => (MusclesEnum)m.Id);
                    match &= exerciseMuscles.Any(m => Filter.CurrentMuscles!.Contains(m));
                }

                if (!match)
                    continue;

                newItems.Add(newItem);
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        return newItems;
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

        int pageCount = Shell.Current.Navigation.NavigationStack.Count;
        if (pageCount > 1 && Shell.Current.Navigation.NavigationStack[pageCount - 2] is PreparedTrainingsPage)
        {
            await Shell.Current.GoToAsync("//workouts");
            return;
        }

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