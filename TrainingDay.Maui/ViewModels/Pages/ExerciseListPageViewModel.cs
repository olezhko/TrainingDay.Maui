using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;
using static TrainingDay.Maui.Models.Messages.ExerciseChangedMessage;

namespace TrainingDay.Maui.ViewModels.Pages;

public class ExerciseListPageViewModel : BaseViewModel, IQueryAttributable
{
    private HashSet<int> existedExerciseIds = [];
    private int selectedCount;

    public FilterModel Filter { get; set; }
    public ObservableCollection<ExerciseListItemViewModel> BaseItems { get; set; } // all exercises from the database, loaded once
    public ObservableCollection<ExerciseListItemViewModel> Items { get; set; } // filtered subset of BaseItems shown in the list

    public ICommand ViewFilterWindowCommand { get; protected set; }

    public ICommand ChoseExercisesCommand { get; protected set; }

    public ICommand CancelCommand { get; protected set; }

    public ICommand ToggleSelectionCommand { get; protected set; }

    public ICommand OpenExerciseCommand { get; protected set; }

    public ICommand ResetFilterCommand { get; protected set; }

    public ICommand AddNewExerciseCommand { get; protected set; }

    public int SelectedCount
    {
        get => selectedCount;
        private set
        {
            if (SetProperty(ref selectedCount, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                OnPropertyChanged(nameof(SelectButtonText));
            }
        }
    }

    public bool HasSelection => SelectedCount > 0;

    public string SelectButtonText => HasSelection ? $"{AppResources.SelectString} ({SelectedCount})" : AppResources.SelectString;

    public bool IsFilterActive => Filter.Muscles?.Count > 0 || Filter.IsNoEquipmentFilter || Filter.IsBarbellExists || Filter.IsDumbbellExists || Filter.DifficultyLevel != 0;

    public bool IsAnyFilterApplied => IsFilterActive || !string.IsNullOrWhiteSpace(Filter.NameFilter);

    public ExerciseListPageViewModel()
    {
        Filter = new FilterModel();
        ChoseExercisesCommand = new AsyncRelayCommand(ChoseExercises);
        ViewFilterWindowCommand = new AsyncRelayCommand(ViewFilterWindow);
        CancelCommand = new AsyncRelayCommand(Cancel);
        ToggleSelectionCommand = new RelayCommand<ExerciseListItemViewModel>(ToggleSelection);
        OpenExerciseCommand = new AsyncRelayCommand<ExerciseListItemViewModel>(OpenExercise);
        ResetFilterCommand = new RelayCommand(ResetFilter);
        AddNewExerciseCommand = new AsyncRelayCommand(AddNewExercise);
        Items = [];
        BaseItems = [];
        SubscribeExerciseChangedMessage();
        LoadItemsAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var existed = query.TryGetValue("ExistedExercises", out var value) ? value as ObservableCollection<TrainingExerciseViewModel> : null;
        existedExerciseIds = existed?.Select(x => x.ExerciseId).ToHashSet() ?? [];

        // items may already be loaded when the query arrives
        foreach (var item in BaseItems)
        {
            item.IsExerciseExistsInWorkout = existedExerciseIds.Contains(item.Id);
        }
    }

    public void UpdateItems() => ApplyFilter();

    // ExerciseItemPage sends the change while this page is hidden, so the subscription lives as long as the view model
    private void SubscribeExerciseChangedMessage()
    {
        WeakReferenceMessenger.Default.Register<ExerciseChangedMessage>(this, (r, m) =>
        {
            OnExerciseChanged(m);
            LoggingService.TrackEvent($"{GetType().Name}: ExerciseChangedMessage");
        });
    }

    private void SubscribeFilterMessage()
    {
        WeakReferenceMessenger.Default.Unregister<FilterAcceptedForExercisesMessage>(this);
        WeakReferenceMessenger.Default.Register<FilterAcceptedForExercisesMessage>(this, (r, m) =>
        {
            m.Filter.NameFilter = Filter.NameFilter;
            Filter = m.Filter;
            OnPropertyChanged(nameof(Filter));
            ApplyFilter();

            WeakReferenceMessenger.Default.Unregister<FilterAcceptedForExercisesMessage>(this);
            LoggingService.TrackEvent($"{GetType().Name}: FilterAcceptedForExercisesMessage");
        });
    }

    private async Task Cancel() => await Shell.Current.GoToAsync("..");

    private List<ExerciseListItemViewModel> GetSelectedItems() => [.. BaseItems.Where(item => item.IsSelected)];

    private async void LoadItemsAsync()
    {
        IsBusy = true;
        try
        {
            var entities = await Task.Run(() => App.Database.GetExerciseItems().ToList());
            BaseItems = new ObservableCollection<ExerciseListItemViewModel>(entities.Select(CreateItem).OrderByDescending(x => x.IsFavourite));
            ApplyFilter();
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ExerciseListItemViewModel CreateItem(ExerciseEntity entity)
    {
        var item = new ExerciseListItemViewModel(entity)
        {
            IsExerciseExistsInWorkout = existedExerciseIds.Contains(entity.Id),
        };
        item.PropertyChanged += Item_PropertyChanged;
        return item;
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExerciseListItemViewModel.IsSelected))
        {
            SelectedCount = BaseItems.Count(item => item.IsSelected);
        }
    }

    private void ApplyFilter()
    {
        var nameFilter = Filter.NameFilter?.Trim();
        var hasMuscleFilter = Filter.Muscles?.Count > 0;
        bool checkTags = Filter.IsNoEquipmentFilter || Filter.IsBarbellExists || Filter.IsDumbbellExists;

        Items = new ObservableCollection<ExerciseListItemViewModel>(BaseItems.Where(item => IsMatch(item, nameFilter, hasMuscleFilter, checkTags)));
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(IsFilterActive));
        OnPropertyChanged(nameof(IsAnyFilterApplied));
    }

    private bool IsMatch(ExerciseListItemViewModel item, string? nameFilter, bool hasMuscleFilter, bool checkTags)
    {
        if (!string.IsNullOrEmpty(nameFilter) && !item.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (checkTags)
        {
            var tags = item.Tags ?? [];
            if ((Filter.IsNoEquipmentFilter && !tags.Contains(ExerciseTags.CanDoAtHome))
                || (Filter.IsBarbellExists && !tags.Contains(ExerciseTags.BarbellExist))
                || (Filter.IsDumbbellExists && !tags.Contains(ExerciseTags.DumbbellExist)))
            {
                return false;
            }
        }

        if (Filter.DifficultyLevel != 0 && (int)item.DifficultType != Filter.DifficultyLevel)
        {
            return false;
        }

        if (hasMuscleFilter && !item.Muscles.Any(m => Filter.Muscles!.Contains((MusclesEnum)m.Id)))
        {
            return false;
        }

        return true;
    }

    private void OnExerciseChanged(ExerciseChangedMessage message)
    {
        var existing = BaseItems.FirstOrDefault(item => item.Id == message.Sender.Id);
        int index = existing != null ? BaseItems.IndexOf(existing) : -1;
        if (existing != null)
        {
            existing.PropertyChanged -= Item_PropertyChanged;
            BaseItems.Remove(existing);
        }

        if (message.Action != ExerciseAction.Removed)
        {
            var item = CreateItem(message.Sender);
            item.IsSelected = message.Action == ExerciseAction.Added || existing?.IsSelected == true; // a just-created exercise is most likely wanted
            BaseItems.Insert(Math.Max(index, 0), item); // a new exercise goes on top so it is visible
        }

        SelectedCount = BaseItems.Count(item => item.IsSelected);
        ApplyFilter();
    }

    private void ToggleSelection(ExerciseListItemViewModel? item)
    {
        if (item != null)
        {
            item.IsSelected = !item.IsSelected;
        }
    }

    private async Task OpenExercise(ExerciseListItemViewModel? item)
    {
        if (item == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(ExerciseItemPage)}?{nameof(ExerciseViewModel.LoadId)}={item.Id}");
    }

    private async Task AddNewExercise() => await Shell.Current.GoToAsync(nameof(ExerciseItemPage));

    private void ResetFilter()
    {
        Filter = new FilterModel();
        OnPropertyChanged(nameof(Filter));
        ApplyFilter();
    }

    private async Task ViewFilterWindow()
    {
        SubscribeFilterMessage();
        Dictionary<string, object> param = new Dictionary<string, object> { { "Filter", Filter } };
        await Shell.Current.GoToAsync(nameof(FilterPage), param);
    }

    private async Task ChoseExercises()
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
}
