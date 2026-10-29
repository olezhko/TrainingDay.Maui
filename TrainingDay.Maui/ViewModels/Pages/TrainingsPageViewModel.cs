using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Controls.PopUp;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Mappers;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class TrainingsPageViewModel : BaseViewModel
{
    private static readonly TimeSpan UndoDuration = TimeSpan.FromSeconds(5);
    private const int SummaryMusclesCount = 2;

    private readonly HashSet<int> pendingDeleteIds = [];
    private ObservableCollection<Grouping<string, TrainingViewModel>> itemsGrouped;
    private bool isLoaded;
    private string weeklySummary = string.Empty;
    private bool hasUnfinishedTraining;
    private string unfinishedTrainingTitle = string.Empty;
    private string unfinishedTrainingElapsed = string.Empty;

    public TrainingsPageViewModel()
    {
        ItemsGrouped = new ObservableCollection<Grouping<string, TrainingViewModel>>();
        ItemsGrouped.CollectionChanged += (_, _) => UpdateViewState();
        SelectedTrainings.CollectionChanged += (_, _) => UpdateViewState();

        AddNewTrainingCommand = new AsyncRelayCommand(AddNewTraining);
        CreateOwnTrainingCommand = new AsyncRelayCommand(CreateOwnTraining);
        AiPlanCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(nameof(WorkoutQuestinariumPage)));
        ShowHistoryCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(nameof(HistoryTrainingPage)));
        ItemSelectedCommand = new AsyncRelayCommand<TrainingViewModel>(TrainingSelected);
        StartTrainingCommand = new AsyncRelayCommand<TrainingViewModel>(StartTraining);
        MoreActionsCommand = new AsyncRelayCommand<TrainingViewModel>(ShowTrainingActions);
        LongPressedEffectCommand = new AsyncRelayCommand<TrainingViewModel>(LongPressed);
        DeleteTrainingCommand = new AsyncRelayCommand<TrainingViewModel>(DeleteTrainingWithUndo, AsyncRelayCommandOptions.AllowConcurrentExecutions);
        DuplicateTrainingCommand = new RelayCommand<TrainingViewModel>(DuplicateSelectedTraining);
        ActivateGroupCommand = new RelayCommand<Grouping<string, TrainingViewModel>>(ActivateGroup);
        GroupLongPressedCommand = new AsyncRelayCommand<Grouping<string, TrainingViewModel>>(ShowGroupActions);
        CreateGroupCommand = new AsyncRelayCommand(CreateEmptyGroup);
        ResumeUnfinishedCommand = new AsyncRelayCommand(ResumeUnfinishedTraining);
        DiscardUnfinishedCommand = new AsyncRelayCommand(DiscardUnfinishedTraining);
        SubscribeMessages();
    }

    #region Load
    private sealed record TrainingsPageData(
        List<Grouping<string, TrainingViewModel>> Groups,
        int WorkoutsThisWeek,
        TrainingSerialize? UnfinishedTraining);

    public async Task LoadItemsAsync()
    {
        TrainingsPageData data;
        try
        {
            var excludedIds = pendingDeleteIds.ToHashSet();
            data = await Task.Run(() => ReadPageData(excludedIds));
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
            return;
        }

        WeeklySummary = AppResources.WorkoutsThisWeekFormat.Fill(data.WorkoutsThisWeek);
        ApplyUnfinishedTraining(data.UnfinishedTraining);

        // always rebuild: cards show per-training data (last performed, next up) that changes without the count changing
        var selectedGroupId = ItemsGrouped.FirstOrDefault(g => g.IsSelected)?.Id;
        var groupToKeep = data.Groups.FirstOrDefault(g => g.Id == selectedGroupId);
        if (groupToKeep != null)
        {
            data.Groups.ForEach(g => g.IsSelected = false);
            groupToKeep.IsSelected = true;
        }

        ItemsGrouped.Clear();
        SelectedTrainings.Clear();

        foreach (var group in data.Groups)
        {
            ItemsGrouped.Add(group);
        }

        SelectGroupAndRefillWorkouts(data.Groups);
        MarkLoaded();
    }

    /// <summary>
    /// All DB/file reads of the page in one place, so they can run off the UI thread.
    /// </summary>
    private static TrainingsPageData ReadPageData(HashSet<int> excludedIds)
    {
        var trainingsItems = App.Database.GetTrainingItems().Where(t => !excludedIds.Contains(t.Id)).ToList();
        var trainingsGroups = App.Database.GetTrainingsGroups().ToList();
        var lastTrainings = App.Database.GetLastTrainingItems().ToList();

        var lastTimeByTraining = lastTrainings
            .GroupBy(item => item.TrainingId)
            .ToDictionary(g => g.Key, g => g.Max(item => item.Time));

        var exercisesById = App.Database.GetExerciseItems().ToDictionary(e => e.Id);
        var exercisesByTraining = App.Database.GetTrainingExerciseItems()
            .GroupBy(item => item.TrainingId)
            .ToDictionary(g => g.Key, g => g.Select(item => exercisesById.GetValueOrDefault(item.ExerciseId)).ToList());

        var now = DateTime.Now;
        var culture = Settings.GetLanguage();
        var tempGroups = new List<Grouping<string, TrainingViewModel>>();
        foreach (var training in trainingsItems)
        {
            var item = new TrainingViewModel(training);
            if (lastTimeByTraining.TryGetValue(training.Id, out var lastTime))
            {
                item.LastImplementedDateTime = lastTime.ToRelativeDateString(now, culture);
            }

            item.Summary = BuildSummary(exercisesByTraining.GetValueOrDefault(training.Id));
            FillGroupedTraining(item, trainingsGroups, tempGroups);
        }

        // groups created from the "+" chip have no workouts yet but must stay visible
        foreach (var union in trainingsGroups.Where(u => new TrainingUnion(u).TrainingIDs.Count == 0))
        {
            if (tempGroups.All(g => g.Id != union.Id))
            {
                tempGroups.Add(new Grouping<string, TrainingViewModel>(union.Name, []) { Id = union.Id });
            }
        }

        foreach (var group in tempGroups)
        {
            MarkNextUp(group, lastTimeByTraining);
        }

        var weekStart = now.Date.AddDays(-(((int)now.DayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek + 7) % 7));
        var workoutsThisWeek = lastTrainings.Count(item => item.Time >= weekStart);

        return new TrainingsPageData(tempGroups, workoutsThisWeek, ReadUnfinishedTraining());
    }

    private static string BuildSummary(List<ExerciseEntity?>? exercises)
    {
        if (exercises == null || exercises.Count == 0)
        {
            return AppResources.ExercisesShortCountFormat.Fill(0);
        }

        var topMuscles = exercises
            .Where(e => e != null)
            .SelectMany(e => MuscleViewModelExtensions.ConvertFromStringToList(e!.MusclesString))
            .GroupBy(m => m.Id)
            .OrderByDescending(g => g.Count())
            .Take(SummaryMusclesCount)
            .Select(g => g.First().Name)
            .ToList();

        var count = AppResources.ExercisesShortCountFormat.Fill(exercises.Count);
        return topMuscles.Count == 0 ? count : $"{count} · {string.Join(", ", topMuscles)}";
    }

    /// <summary>
    /// In a group with several workouts, the one not done for the longest time (never done first) is the next one.
    /// </summary>
    private static void MarkNextUp(Grouping<string, TrainingViewModel> group, Dictionary<int, DateTime> lastTimeByTraining)
    {
        if (group.Count < 2 || !group.Any(t => lastTimeByTraining.ContainsKey(t.Id)))
        {
            return;
        }

        var next = group.MinBy(t => lastTimeByTraining.GetValueOrDefault(t.Id, DateTime.MinValue));
        if (next != null)
        {
            next.IsNextUp = true;
        }
    }

    private void MarkLoaded()
    {
        isLoaded = true;
        UpdateViewState();
    }

    private void SelectGroupAndRefillWorkouts(List<Grouping<string, TrainingViewModel>> tempGroups)
    {
        try
        {
            SelectedTrainings.Clear();
            var selectedGroup = tempGroups.FirstOrDefault(gp => gp.IsSelected == true);
            if (selectedGroup == null)
            {
                selectedGroup = tempGroups.FirstOrDefault(gp => gp.Key == AppResources.GroupingDefaultName);
                selectedGroup ??= tempGroups.FirstOrDefault();
            }

            if (selectedGroup != null)
            {
                selectedGroup.IsSelected = true;

                foreach (var item in selectedGroup)
                {
                    SelectedTrainings.Add(item);
                }
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
        finally
        {
            OnPropertyChanged(nameof(SelectedTrainings));
        }
    }

    private static void FillGroupedTraining(TrainingViewModel item,
        IEnumerable<TrainingUnionEntity> unions,
        List<Grouping<string, TrainingViewModel>> tempGroups)
    {
        var trainingUnion = unions.FirstOrDefault(union => new TrainingUnion(union).TrainingIDs.Contains(item.Id));
        if (trainingUnion != null) // union exist
        {
            var group = tempGroups.FirstOrDefault(gr => gr.Id == trainingUnion.Id);
            if (group != null)
            {
                item.Group = group.First().Group;
                group.Add(item);
            }
            else
            {
                tempGroups.Add(new Grouping<string, TrainingViewModel>(trainingUnion.Name, new List<TrainingViewModel>())
                {
                    Id = trainingUnion.Id,
                });

                item.Group = new TrainingUnion(trainingUnion);
                tempGroups.Last().Add(item);
            }
        }
        else
        {
            Grouping<string, TrainingViewModel> defaultGroup = tempGroups.FirstOrDefault(gp => gp.Key == AppResources.GroupingDefaultName);
            if (defaultGroup == null)
            {
                var gr = new Grouping<string, TrainingViewModel>(AppResources.GroupingDefaultName, new List<TrainingViewModel>())
                {
                    item
                };
                gr.IsSelected = true;
                tempGroups.Insert(0, gr);
            }
            else
                defaultGroup.Add(item);
        }
    }

    private void UpdateViewState()
    {
        OnPropertyChanged(nameof(HasNoTrainings));
        OnPropertyChanged(nameof(IsSelectedGroupEmpty));
    }
    #endregion

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<IncomingTrainingAddedMessage>(this, (r, args) =>
        {
            MainThread.BeginInvokeOnMainThread(async () => await LoadItemsAsync());
            LoggingService.TrackEvent($"TrainingsPageViewModel: Added Incoming Training");
        });
    }

    private void UnsubscribeMessages()
    {
        WeakReferenceMessenger.Default.Unregister<IncomingTrainingAddedMessage>(this);
    }

    private async Task AddNewTraining()
    {
        LoggingService.TrackEvent($"{GetType().Name}: AddNewTraining Button Clicked");
        await Shell.Current.GoToAsync(nameof(PreparedTrainingsPage));
    }

    private async Task CreateOwnTraining()
    {
        LoggingService.TrackEvent($"{GetType().Name}: CreateOwnTraining Button Clicked");
        await Shell.Current.GoToAsync($"{nameof(PreparedTrainingsPage)}?{PreparedTrainingsPage.StartCustomQueryKey}=true");
    }

    private async Task TrainingSelected(TrainingViewModel? item)
    {
        if (item == null)
            return;

        LoggingService.TrackEvent($"{GetType().Name}: TrainingSelected Clicked");
        await Shell.Current.GoToAsync($"{nameof(TrainingExercisesPage)}?{nameof(TrainingExercisesPageViewModel.ItemId)}={item.Id}");
    }

    private async Task StartTraining(TrainingViewModel? item)
    {
        if (item == null)
            return;

        LoggingService.TrackEvent($"{GetType().Name}: StartTraining from card");
        var training = await Task.Run(() => TrainingViewModel.LoadWithExercises(item.Id));
        if (training.Exercises.Count == 0)
        {
            await Toast.Make(AppResources.NoExercisesNeedAddNewString, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
            return;
        }

        if (Settings.IsShowAdvicesOnImplementing)
        {
            await Shell.Current.ShowPopupAsync(
                PopupBuilders.BuildAdvicePopup(AppResources.AdviceBeforeTrainingMessage),
                PopupBuilders.DefaultPopupOptions);
        }

        Dictionary<string, object> param = new Dictionary<string, object> { { "TrainingItem", training } };
        await Shell.Current.GoToAsync(nameof(TrainingImplementPage), param);
    }

    #region Delete with undo
    private async Task DeleteTrainingWithUndo(TrainingViewModel? item)
    {
        if (item == null)
            return;

        var group = ItemsGrouped.FirstOrDefault(gr => gr.Contains(item));
        if (group == null)
            return;

        LoggingService.TrackEvent($"{GetType().Name}: DeleteSelectedTraining Clicked");

        var groupIndex = ItemsGrouped.IndexOf(group);
        var itemIndex = group.IndexOf(item);
        var wasSelected = group.IsSelected;

        pendingDeleteIds.Add(item.Id);
        group.Remove(item);
        var groupRemoved = group.Count == 0 && group.Id == 0;
        if (groupRemoved)
        {
            ItemsGrouped.Remove(group);
        }

        if (wasSelected)
        {
            SelectedTrainings.Remove(item);
        }

        EnsureGroupSelected();
        UpdateViewState();

        var undoRequested = new TaskCompletionSource<bool>();
        var snackbar = Snackbar.Make(
            AppResources.TrainingDeletedFormat.Fill(item.Title),
            () => undoRequested.TrySetResult(true),
            AppResources.UndoString,
            UndoDuration);
        await snackbar.Show();

        var completed = await Task.WhenAny(undoRequested.Task, Task.Delay(UndoDuration + TimeSpan.FromMilliseconds(500)));
        pendingDeleteIds.Remove(item.Id);

        if (completed == undoRequested.Task)
        {
            LoggingService.TrackEvent($"{GetType().Name}: DeleteSelectedTraining Undone");
            RestoreDeletedTraining(item, group, groupIndex, itemIndex, groupRemoved);
            return;
        }

        CommitDeleteTraining(item);
    }

    private void RestoreDeletedTraining(TrainingViewModel item, Grouping<string, TrainingViewModel> group, int groupIndex, int itemIndex, bool groupRemoved)
    {
        // the page was reloaded meanwhile, the group objects are stale
        if (!groupRemoved && !ItemsGrouped.Contains(group))
        {
            _ = LoadItemsAsync();
            return;
        }

        if (groupRemoved)
        {
            ItemsGrouped.Insert(Math.Min(groupIndex, ItemsGrouped.Count), group);
        }

        group.Insert(Math.Min(itemIndex, group.Count), item);
        if (group.IsSelected)
        {
            RefillSelectedTrainings(group);
        }

        UpdateViewState();
    }

    private void CommitDeleteTraining(TrainingViewModel item)
    {
        try
        {
            LoggingService.TrackEvent($"{GetType().Name}: DeleteSelectedTraining Approved");
            item.DeleteTrainingsItemsFromBase();

            if (item.Group != null && item.Group.Id != 0)
            {
                var union = App.Database.GetTrainingsGroups().FirstOrDefault(u => u.Id == item.Group.Id);
                if (union != null)
                {
                    var viewModel = new TrainingUnion(union);
                    viewModel.TrainingIDs.Remove(item.Id);
                    if (viewModel.TrainingIDs.Count == 0)
                    {
                        App.Database.DeleteTrainingGroup(viewModel.Id);
                        var emptyGroup = ItemsGrouped.FirstOrDefault(g => g.Id == viewModel.Id && g.Count == 0);
                        if (emptyGroup != null)
                        {
                            ItemsGrouped.Remove(emptyGroup);
                            EnsureGroupSelected();
                        }
                    }
                    else
                    {
                        App.Database.SaveTrainingGroup(viewModel.Model);
                    }
                }
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }
    #endregion

    private void DuplicateSelectedTraining(TrainingViewModel? training)
    {
        if (training == null)
            return;

        LoggingService.TrackEvent($"{GetType().Name}: DuplicateSelectedTraining Clicked");
        int id = App.Database.SaveTrainingItem(new TrainingEntity()
        {
            Title = training.Title + AppResources.CopiedString,
        });

        // save every exercise
        int order = 0;
        var exercises = App.Database.GetTrainingExercisesByTrainingId(training.Id)
            .Select(item => new TrainingExerciseViewModel(item.Exercise, item.TrainingExercise));
        foreach (var item in exercises)
        {
            // save order numbers
            App.Database.SaveTrainingExerciseItem(TrainingExerciseMapper.ToNewEntity(item, id, order));
            order++;
        }

        Toast.Make(AppResources.SavedString).Show();

        _ = LoadItemsAsync();
    }

    #region grouping
    private async Task AddToGroupAsync(TrainingViewModel item)
    {
        LoggingService.TrackEvent($"{GetType().Name}: AddToGroup started");

        var trainingsGroups = await Task.Run(() => App.Database.GetTrainingsGroups().ToList());
        await Shell.Current.ShowPopupAsync(
            PopupBuilders.BuildWorkoutGroupsPopup(
                trainingsGroups,
                accept => GroupSelected(item, accept.Id),
                () => CreateNewGroup(item)),
            PopupBuilders.DefaultPopupOptions);
    }

    private async Task GroupSelected(TrainingViewModel TrainingMoveToGroup, int id)
    {
        try
        {
            //  если тренировка уже в группе и группа такая же, как и выбрали
            if (TrainingMoveToGroup.Group != null && id == TrainingMoveToGroup.Group.Id)
            {
                await Shell.Current.ClosePopupAsync();
                return;
            }

            var unions = App.Database.GetTrainingsGroups();
            // если тренировка в группе и группа не "Основные"
            if (TrainingMoveToGroup.Group != null && TrainingMoveToGroup.Group.Id != 0)
            {
                var unionToEdit = unions.First(u => u.Id == TrainingMoveToGroup.Group.Id);
                var vm = new TrainingUnion(unionToEdit);
                vm.TrainingIDs.Remove(TrainingMoveToGroup.Id);
                if (vm.TrainingIDs.Count != 0)
                    App.Database.SaveTrainingGroup(vm.Model);
                else
                    App.Database.DeleteTrainingGroup(vm.Id);
            }

            RemoveFromCurrentGroup(TrainingMoveToGroup);
            TrainingMoveToGroup.Group = null;

            var union = id != 0 ? unions.FirstOrDefault(un => un.Id == id) : null;
            if (union != null)
            {
                var viewModel = new TrainingUnion(union);
                viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);
                TrainingMoveToGroup.Group = viewModel;
                App.Database.SaveTrainingGroup(viewModel.Model);

                var targetGroup = ItemsGrouped.FirstOrDefault(g => g.Id == union.Id);
                if (targetGroup == null)
                {
                    targetGroup = new Grouping<string, TrainingViewModel>(union.Name, []) { Id = union.Id };
                    ItemsGrouped.Add(targetGroup);
                }

                AddToGroupUi(targetGroup, TrainingMoveToGroup);
            }
            else
            {
                AddToGroupUi(GetOrCreateDefaultGroup(), TrainingMoveToGroup);
            }

            EnsureGroupSelected();
            LoggingService.TrackEvent($"{GetType().Name}: AddToGroup finsihed");
            await Toast.Make(AppResources.SavedString).Show();
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        await Shell.Current.ClosePopupAsync();
    }

    private async Task CreateNewGroup(TrainingViewModel trainingItem)
    {
        LoggingService.TrackEvent($"{GetType().Name}: AddToGroup with new group STARTED");
        string result = await Shell.Current.DisplayPromptAsync(string.Empty, AppResources.GroupingEnterNameofGroup, AppResources.OkString, AppResources.CancelString, placeholder: AppResources.NameString);
        if (result != null && !string.IsNullOrEmpty(result))
        {
            try
            {
                var unions = App.Database.GetTrainingsGroups();

                DeleteFromGroupDatabase(trainingItem, [.. unions]);
                RemoveFromCurrentGroup(trainingItem);

                var union = unions.FirstOrDefault(un => un.Name == result);
                if (union != null) // если группа с таким именем уже существует
                {
                    var viewModel = new TrainingUnion(union);
                    if (!viewModel.TrainingIDs.Contains(trainingItem.Id))
                    {
                        viewModel.TrainingIDs.Add(trainingItem.Id);// добавляем в список тренировок у группы выбранную тренировку
                        App.Database.SaveTrainingGroup(viewModel.Model);
                    }
                    trainingItem.Group = viewModel;

                    var existingGroup = ItemsGrouped.FirstOrDefault(g => g.Id == union.Id);
                    if (existingGroup != null)
                        AddToGroupUi(existingGroup, trainingItem);
                }
                else
                {
                    var viewModel = new TrainingUnion(new TrainingUnionEntity());
                    viewModel.Name = result;
                    viewModel.TrainingIDs.Add(trainingItem.Id);
                    viewModel.Id = App.Database.SaveTrainingGroup(viewModel.Model);
                    trainingItem.Group = viewModel;

                    ItemsGrouped.Add(new Grouping<string, TrainingViewModel>(result, [trainingItem])
                    {
                        Id = viewModel.Id,
                    });
                }

                EnsureGroupSelected();
                LoggingService.TrackEvent($"{GetType().Name}: AddToGroup with new group FINISHED");
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
            finally
            {
                await Toast.Make(AppResources.SavedString).Show();
            }
        }

        await Shell.Current.ClosePopupAsync();
    }

    private async Task CreateEmptyGroup()
    {
        LoggingService.TrackEvent($"{GetType().Name}: Create empty group STARTED");
        string result = await Shell.Current.DisplayPromptAsync(AppResources.NewGroupString, AppResources.GroupingEnterNameofGroup, AppResources.OkString, AppResources.CancelString, placeholder: AppResources.NameString);
        if (string.IsNullOrWhiteSpace(result))
            return;

        try
        {
            var existing = ItemsGrouped.FirstOrDefault(g => g.Key == result);
            if (existing == null)
            {
                var viewModel = new TrainingUnion(new TrainingUnionEntity()) { Name = result };
                viewModel.Id = App.Database.SaveTrainingGroup(viewModel.Model);
                existing = new Grouping<string, TrainingViewModel>(result, []) { Id = viewModel.Id };
                ItemsGrouped.Add(existing);
            }

            ActivateGroup(existing);
            LoggingService.TrackEvent($"{GetType().Name}: Create empty group FINISHED");
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async Task ShowGroupActions(Grouping<string, TrainingViewModel>? group)
    {
        // the default group is virtual (not stored in DB), it can't be renamed or deleted
        if (group == null || group.Id == 0)
            return;

        PerformLongPressHaptic();
        var action = await MessageManager.DisplayActionSheet(group.Key, AppResources.CancelString,
            new ActionSheetOption<GroupAction>(GroupAction.Rename, AppResources.RenameString),
            new ActionSheetOption<GroupAction>(GroupAction.Delete, AppResources.DeleteGroupString));

        switch (action)
        {
            case GroupAction.Rename:
                await RenameGroup(group);
                break;
            case GroupAction.Delete:
                await DeleteGroup(group);
                break;
        }
    }

    private enum GroupAction
    {
        Rename,
        Delete,
    }

    private async Task RenameGroup(Grouping<string, TrainingViewModel> group)
    {
        string result = await Shell.Current.DisplayPromptAsync(AppResources.RenameString, AppResources.GroupingEnterNameofGroup, AppResources.OkString, AppResources.CancelString, placeholder: AppResources.NameString, initialValue: group.Key);
        if (string.IsNullOrWhiteSpace(result) || result == group.Key)
            return;

        try
        {
            var union = App.Database.GetTrainingGroup(group.Id);
            union.Name = result;
            App.Database.SaveTrainingGroup(union);

            group.Key = result;
            foreach (var training in group.Where(t => t.Group != null))
            {
                training.Group!.Name = result;
            }

            LoggingService.TrackEvent($"{GetType().Name}: Group renamed");
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async Task DeleteGroup(Grouping<string, TrainingViewModel> group)
    {
        var confirmed = await MessageManager.DisplayAlert(AppResources.DeleteGroupString,
            AppResources.DeleteGroupQuestionFormat.Fill(group.Key, AppResources.GroupingDefaultName),
            AppResources.RemoveString, AppResources.CancelString);
        if (!confirmed)
            return;

        try
        {
            App.Database.DeleteTrainingGroup(group.Id);

            var trainings = group.ToList();
            ItemsGrouped.Remove(group);
            if (trainings.Count > 0)
            {
                var defaultGroup = GetOrCreateDefaultGroup();
                foreach (var training in trainings)
                {
                    training.Group = null;
                    defaultGroup.Add(training);
                }
            }

            if (group.IsSelected)
            {
                group.IsSelected = false;
                SelectedTrainings.Clear();
            }

            EnsureGroupSelected();
            UpdateViewState();
            LoggingService.TrackEvent($"{GetType().Name}: Group deleted");
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private void DeleteFromGroupDatabase(TrainingViewModel trainingMoveToGroup, List<TrainingUnionEntity> unions)
    {
        if (trainingMoveToGroup.Group == null)
            return;

        try
        {
            var unionToEdit = new TrainingUnion(unions.First(u => u.Id == trainingMoveToGroup.Group.Id));
            unionToEdit.TrainingIDs.Remove(trainingMoveToGroup.Id);

            if (trainingMoveToGroup.Group != null && trainingMoveToGroup.Group.Id != 0)
            {
                if (unionToEdit.TrainingIDs.Count != 0)
                    App.Database.SaveTrainingGroup(unionToEdit.Model);
                else
                    App.Database.DeleteTrainingGroup(unionToEdit.Id);
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    private void DeleteFromGroup(TrainingViewModel item)
    {
        LoggingService.TrackEvent($"{GetType().Name}: Delete From Group");

        var union = App.Database.GetTrainingGroup(item.Group.Id);
        var viewModel = new TrainingUnion(union);
        viewModel.TrainingIDs.Remove(item.Id);

        if (viewModel.TrainingIDs.Count == 0)
        {
            App.Database.DeleteTrainingGroup(item.Group.Id);
        }
        else
        {
            App.Database.SaveTrainingGroup(viewModel.Model);
        }

        RemoveFromCurrentGroup(item);
        item.Group = null;

        AddToGroupUi(GetOrCreateDefaultGroup(), item);

        EnsureGroupSelected();

        Toast.Make(AppResources.SavedString).Show();
    }

    private Grouping<string, TrainingViewModel> GetOrCreateDefaultGroup()
    {
        var defaultGroup = ItemsGrouped.FirstOrDefault(g => g.Key == AppResources.GroupingDefaultName);
        if (defaultGroup == null)
        {
            defaultGroup = new Grouping<string, TrainingViewModel>(AppResources.GroupingDefaultName, new List<TrainingViewModel>());
            ItemsGrouped.Insert(0, defaultGroup);
        }

        return defaultGroup;
    }

    private void ActivateGroup(Grouping<string, TrainingViewModel>? group)
    {
        if (group == null)
            return;

        ItemsGrouped.ForEach(item => item.IsSelected = false);
        group.IsSelected = true;
        RefillSelectedTrainings(group);
    }

    private void RefillSelectedTrainings(Grouping<string, TrainingViewModel> group)
    {
        SelectedTrainings.Clear();
        foreach (var training in group)
        {
            SelectedTrainings.Add(training);
        }
    }

    private void RemoveFromCurrentGroup(TrainingViewModel item)
    {
        var currentGroup = ItemsGrouped.FirstOrDefault(g => g.Contains(item));
        if (currentGroup == null)
            return;

        currentGroup.Remove(item);
        if (currentGroup.IsSelected)
            SelectedTrainings.Remove(item);

        if (currentGroup.Count == 0)
            ItemsGrouped.Remove(currentGroup);
    }

    private void AddToGroupUi(Grouping<string, TrainingViewModel> group, TrainingViewModel item)
    {
        group.Add(item);
        if (group.IsSelected)
            SelectedTrainings.Add(item);
        UpdateViewState();
    }

    private void EnsureGroupSelected()
    {
        if (ItemsGrouped.Any(g => g.IsSelected))
            return;

        SelectGroupAndRefillWorkouts(ItemsGrouped.ToList());
    }

    #endregion

    #region Unfinished training
    private static string UnfinishedTrainingFileName => Path.Combine(FileSystem.CacheDirectory, ConstantKeys.NotFinishedTrainingName);

    private static TrainingSerialize? ReadUnfinishedTraining()
    {
        try
        {
            if (Settings.IsTrainingNotFinished && File.Exists(UnfinishedTrainingFileName))
            {
                return DataManageViewModel.LoadFromFile(UnfinishedTrainingFileName);
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        return null;
    }

    private void ApplyUnfinishedTraining(TrainingSerialize? training)
    {
        HasUnfinishedTraining = training != null;
        if (training == null)
            return;

        UnfinishedTrainingTitle = AppResources.UnfinishedTrainingFormat.Fill(training.Title);
        UnfinishedTrainingElapsed = string.IsNullOrEmpty(Settings.IsTrainingNotFinishedTime)
            ? string.Empty
            : AppResources.ElapsedTimeFormat.Fill(Settings.IsTrainingNotFinishedTime);
    }

    private async Task ResumeUnfinishedTraining()
    {
        var trainingSerialize = await Task.Run(ReadUnfinishedTraining);
        HasUnfinishedTraining = false;
        if (trainingSerialize == null)
            return;

        Settings.IsTrainingNotFinished = false;

        TrainingViewModel training = new TrainingViewModel();
        training.Title = trainingSerialize.Title;
        training.Id = trainingSerialize.Id;
        foreach (var trainingExerciseSerialize in trainingSerialize.Items)
        {
            try
            {
                training.AddExercise(TrainingExerciseMapper.FromSerialize(trainingExerciseSerialize));
            }
            catch (Exception e)
            {
                LoggingService.TrackError(e);
            }
        }

        if (!TimeSpan.TryParse(Settings.IsTrainingNotFinishedTime, out var startTime))
        {
            startTime = TimeSpan.Zero;
        }

        LoggingService.TrackEvent($"{GetType().Name}: Unfinished training resumed");
        Dictionary<string, object> param = new Dictionary<string, object> { { "TrainingItem", training }, { "StartTime", startTime } };
        await Shell.Current.GoToAsync(nameof(TrainingImplementPage), param);
    }

    private async Task DiscardUnfinishedTraining()
    {
        var confirmed = await MessageManager.DisplayAlert(AppResources.DiscardUnfinishedTrainingQuestion, UnfinishedTrainingTitle,
            AppResources.DiscardString, AppResources.CancelString);
        if (!confirmed)
            return;

        Settings.IsTrainingNotFinished = false;
        HasUnfinishedTraining = false;
        try
        {
            File.Delete(UnfinishedTrainingFileName);
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        LoggingService.TrackEvent($"{GetType().Name}: Unfinished training discarded");
    }
    #endregion

    private async Task LongPressed(TrainingViewModel? item)
    {
        if (item == null)
            return;

        PerformLongPressHaptic();
        await ShowTrainingActions(item);
    }

    private async Task ShowTrainingActions(TrainingViewModel? item)
    {
        if (item == null)
            return;

        LoggingService.TrackEvent($"{GetType().Name}: LongPressed started");

        var groupOption = item.Group == null
            ? new ActionSheetOption<TrainingAction>(TrainingAction.AddToGroup, AppResources.TrainingToGroupString)
            : new ActionSheetOption<TrainingAction>(TrainingAction.RemoveFromGroup, AppResources.TrainingUnGroupString.Fill(item.Group.Name));

        var action = await MessageManager.DisplayActionSheet(AppResources.ChoseAction, AppResources.CancelString,
            new ActionSheetOption<TrainingAction>(TrainingAction.Remove, AppResources.RemoveString),
            new ActionSheetOption<TrainingAction>(TrainingAction.Duplicate, AppResources.Duplicate),
            groupOption);
        LoggingService.TrackEvent($"{GetType().Name}: LongPressed finished with {action}");

        switch (action)
        {
            case TrainingAction.Remove:
                await DeleteTrainingWithUndo(item);
                break;
            case TrainingAction.Duplicate:
                DuplicateSelectedTraining(item);
                break;
            case TrainingAction.AddToGroup:
                await AddToGroupAsync(item);
                break;
            case TrainingAction.RemoveFromGroup:
                DeleteFromGroup(item);
                break;
        }
    }

    private enum TrainingAction
    {
        Remove,
        Duplicate,
        AddToGroup,
        RemoveFromGroup,
    }

    private static void PerformLongPressHaptic()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    #region Properties

    public ObservableCollection<Grouping<string, TrainingViewModel>> ItemsGrouped { get => itemsGrouped; set => SetProperty(ref itemsGrouped, value); }

    public ObservableCollection<TrainingViewModel> SelectedTrainings { get; set; } = new ObservableCollection<TrainingViewModel>();

    public bool HasNoTrainings => isLoaded && ItemsGrouped.All(g => g.Count == 0);

    public bool IsSelectedGroupEmpty => isLoaded && !HasNoTrainings && SelectedTrainings.Count == 0;

    public string WeeklySummary { get => weeklySummary; set => SetProperty(ref weeklySummary, value); }

    public bool HasUnfinishedTraining { get => hasUnfinishedTraining; set => SetProperty(ref hasUnfinishedTraining, value); }

    public string UnfinishedTrainingTitle { get => unfinishedTrainingTitle; set => SetProperty(ref unfinishedTrainingTitle, value); }

    public string UnfinishedTrainingElapsed { get => unfinishedTrainingElapsed; set => SetProperty(ref unfinishedTrainingElapsed, value); }

    public ICommand AddNewTrainingCommand { get; }

    public ICommand CreateOwnTrainingCommand { get; }

    public ICommand AiPlanCommand { get; }

    public ICommand ShowHistoryCommand { get; }

    public ICommand ItemSelectedCommand { get; }

    public ICommand StartTrainingCommand { get; }

    public ICommand MoreActionsCommand { get; }

    public ICommand LongPressedEffectCommand { get; }

    public ICommand DeleteTrainingCommand { get; }

    public ICommand DuplicateTrainingCommand { get; }

    public ICommand ActivateGroupCommand { get; }

    public ICommand GroupLongPressedCommand { get; }

    public ICommand CreateGroupCommand { get; }

    public ICommand ResumeUnfinishedCommand { get; }

    public ICommand DiscardUnfinishedCommand { get; }
    #endregion
}
