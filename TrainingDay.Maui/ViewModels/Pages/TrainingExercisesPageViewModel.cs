using Microsoft.AppCenter.Analytics;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Views;
using TrainingDay.Maui.Services;
using CommunityToolkit.Maui.Alerts;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.ViewModels.Pages;

sealed class TrainingExercisesPageViewModel : BaseViewModel
{
    private bool isSelectAllState = true;
    readonly ObservableCollection<TrainingExerciseViewModel> selectedItems = new ObservableCollection<TrainingExerciseViewModel>();

    public ObservableCollection<TrainingViewModel> TrainingItems { get; set; } = new ObservableCollection<TrainingViewModel>();

    public string ExerciseActionString { get; set; } = Resources.Strings.AppResources.TrainingString;

    public bool IsExercisesCheckBoxVisible { get; set; }

    public ExerciseCheckBoxAction CurrentAction { get; set; }

    public int TappedExerciseIndex { get; set; } = -1;

    public INavigation Navigation { get; set; }

    public TrainingViewModel Training { get; set; }

    public ICommand DeleteExerciseCommand => new Command<TrainingExerciseViewModel>(DeleteExercise);

    public ICommand MakeNotifyCommand => new Command(MakeNotify);

    public ICommand MakeTrainingCommand => new Command(MakeTraining);

    public ICommand ItemTappedCommand => new Command<object>(TrainingExerciseTapped);

    public ICommand SaveChangesCommand { get; set; }

    public ICommand ShowTrainingSettingsPageCommand => new Command(ShowTrainingSettingsPage);

    public ICommand SelectAllCommand => new Command(SelectAllItems);

    public ICommand AddExercisesCommand => new Command(AddExercises);

    public ICommand ShareTrainingCommand => new Command(ShareTraining);

    public ICommand SetSuperSetCommand => new Command(InitSuperSetMode);

    public ICommand StartCopyExerciseCommand => new Command(StartCopyExercise);

    public ICommand ExercisesCheckedChangedCommand => new Command<TrainingExerciseViewModel>(CollectCheckedExercises);

    public ICommand CancelActionCommand => new Command(() => StopAction());

    public ICommand StartMoveExerciseCommand => new Command(StartMoveExercises);

    public ICommand StartActionCommand => new Command(StartAction);

    public ICommand AcceptTrainingForMoveOrCopyCommand => new Command(AcceptTrainingForMoveOrCopy);

    public TrainingViewModel SelectedTrainingForCopyOrMove { get; set; }

    public ICommand CreateTrainingFromSelectedExercisesCommand => new Command(CreateTrainingFromSelectedExercises);

    public TrainingExercisesPageViewModel()
    {
        SaveChangesCommand = new Command(SaveChanges);
        Training = new TrainingViewModel();
    }

    public void Load(TrainingViewModel trVm)
    {
        if (trVm == null)
        {
            return;
        }

        Training.Title = trVm.Title;
        OnPropertyChanged(nameof(Training.Title));

        Training.Id = trVm.Id;
        Training.Exercises.Clear();
        foreach (var item in trVm.Exercises)
        {
            Training.AddExercise(item);
        }

        OnPropertyChanged(nameof(Training.Exercises));
    }

    public void StartSelectExercises()
    {
        CurrentAction = ExerciseCheckBoxAction.Select;
        OnPropertyChanged(nameof(CurrentAction));
        PrepareAction(Resources.Strings.AppResources.SaveTrainingString);
    }

    private async void AddExercises()
    {
        var vm = new ExerciseListPageViewModel() { Navigation = Navigation };
        vm.ExistedExercises = Training.Exercises;
        vm.ExercisesSelectFinished += async (sender, args) =>
        {
            AddSelectedExercises(args.Select(item => new TrainingExerciseViewModel(item.GetExercise(), new TrainingExerciseComm())));
            Analytics.TrackEvent($"{GetType().Name}: AddExercises finished");
            await Navigation.PopModalAsync(false);
        };

        await Navigation.PushModalAsync(new NavigationPage(new ExerciseListPage(vm)));
        Analytics.TrackEvent($"{GetType().Name}: AddExercises started");
    }

    private void AddSelectedExercises(IEnumerable<TrainingExerciseViewModel> obj)
    {
        obj.ForEach(a => a.IsSelected = false);
        foreach (var exerciseItem in obj)
        {
            exerciseItem.TrainingId = Training.Id;
            exerciseItem.OrderNumber = Training.Exercises.Count;
            App.Database.SaveTrainingExerciseItem(exerciseItem.GetTrainingExerciseComm());
            Training.AddExercise(exerciseItem);
            OnPropertyChanged(nameof(Training.Exercises));
        }

        Analytics.TrackEvent($"{GetType().Name}: AddExercises finished");
    }

    private async void DeleteExercise(TrainingExerciseViewModel sender)
    {
        Analytics.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} started");
        if (CurrentAction == ExerciseCheckBoxAction.SuperSet)
        {
            if (sender.SuperSetId != 0)
            {
                var result = await MessageManager.DisplayAlert(Resources.Strings.AppResources.DeleteExerciseFromSuperSetQuestion, string.Empty, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString);
                if (result)
                {
                    Analytics.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} finished");

                    var id = sender.SuperSetId;
                    sender.SuperSetId = 0;
                    sender.SuperSetNum = 0;
                    CheckSuperSetExist(id);
                    App.Database.SaveTrainingExerciseItem(sender.GetTrainingExerciseComm());
                }
            }
        }
        else
        {
            var result = await MessageManager.DisplayAlert(Resources.Strings.AppResources.DeleteExercise, sender.ExerciseItemName, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString);
            if (result)
            {
                Analytics.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} finished");
                Training.DeleteExercise(sender);
                if (sender.SuperSetId != 0)
                    CheckSuperSetExist(sender.SuperSetId);

                App.Database.DeleteTrainingExerciseItem(sender.TrainingExerciseId);
            }
        }
    }

    private async void MakeNotify()
    {
        MakeTrainingAlarmPageViewModel vm = new MakeTrainingAlarmPageViewModel
        {
            Alarm = new AlarmViewModel
            {
                TrainingId = Training.Id,
            }
        };

        MakeTrainingAlarmPage page = new MakeTrainingAlarmPage() { BindingContext = vm };
        await Navigation.PushModalAsync(page, true);
    }

    private async void TrainingExerciseTapped(object item)
    {
        var selectedExercise = item as TrainingExerciseViewModel;
        TappedExerciseIndex = Training.Exercises.IndexOf(selectedExercise);
        if (CurrentAction != ExerciseCheckBoxAction.None) // when we in action, tapped equals changing selected
        {
            if (CurrentAction == ExerciseCheckBoxAction.SuperSet)
            {
                if (selectedExercise.SuperSetId == 0) //!IsExerciseInSuperSet
                {
                    selectedExercise.IsSelected = !selectedExercise.IsSelected;
                }
            }

            return;
        }

        TrainingExerciseItemPage page = new TrainingExerciseItemPage();
        selectedExercise.IsNotFinished = false;// --> for Time start button, to hide button
        page.LoadExercise(selectedExercise);
        await Navigation.PushAsync(page);
    }

    private void CheckSuperSetExist(int supersetId)
    {
        var list = new List<TrainingExerciseViewModel>();
        foreach (var item in Training.Exercises)
        {
            if (item.SuperSetId == supersetId)
            {
                list.Add(item);
            }
        }

        if (list.Count == 1)
        {
            var item = list.First();
            item.SuperSetId = 0;
            item.SuperSetNum = 0;
            App.Database.SaveTrainingExerciseItem(item.GetTrainingExerciseComm());
            App.Database.DeleteSuperSetItem(supersetId);
        }
    }

    private async void MakeTraining()
    {
        if (Training.Exercises.Count == 0)
        {
            await Toast.Make(AppResources.NoExercisesNeedAddNewString, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
            return;
        }

        foreach (var item in Training.Exercises)
        {
            item.IsNotFinished = true;
        }

        if (Settings.IsShowAdvicesOnImplementing)
        {
            await MessageManager.DisplayAlert(Resources.Strings.AppResources.AdviceString, Resources.Strings.AppResources.AdviceBeforeTrainingMessage, Resources.Strings.AppResources.OkString);
        }

        Analytics.TrackEvent($"{GetType().Name}: Training Implementing started");
        await Navigation.PushAsync(new TrainingImplementPage() { TrainingItem = Training, Title = Training.Title });
    }

    /// <summary>
    /// clear all trainingExercises communication with selected training id
    /// </summary>
    private void ClearTrExUnused()
    {
        var trExercises = Training.Exercises;
        var trainingExerciseItems = App.Database.GetTrainingExerciseItems(); // get all tr-exercises comm

        // delete all exercises, that user delete by "cross" button
        foreach (var trainingExerciseItem in trainingExerciseItems)
        {
            if (trainingExerciseItem.TrainingId == Training.Id && trExercises.All(model => trainingExerciseItem.Id != model.TrainingExerciseId))
            {
                App.Database.DeleteTrainingExerciseItem(trainingExerciseItem.Id);
            }
        }

        var superSets = App.Database.GetSuperSetItems();
        foreach (var superSet in superSets)
        {
            if (superSet.TrainingId == Training.Id)
            {
                bool res = false;
                foreach (var trainingExerciseViewModel in trExercises)
                {
                    if (trainingExerciseViewModel.SuperSetId == superSet.Id) //if training have exercises with this superset id
                    {
                        res = true;
                    }
                }

                if (!res)
                {
                    App.Database.DeleteSuperSetItem(superSet.Id);
                }
            }
        }
    }

    private void SaveChanges()
    {
        ClearTrExUnused();

        SaveTraining();
    }

    private void SaveTraining()
    {
        var id = App.Database.SaveTrainingItem(new Training() { Id = Training.Id, Title = Training.Title });

        int order = 0;
        foreach (var item in Training.Exercises)
        {
            if (!item.IsSelected && CurrentAction == ExerciseCheckBoxAction.Select)
            {
                continue;
            }

            var exId = App.Database.SaveExerciseItem(item.GetExercise());
            order++;
            App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm()
            {
                ExerciseId = exId,
                TrainingId = id,
                OrderNumber = order,
                Id = item.TrainingExerciseId,
                SuperSetId = item.SuperSetId,
                WeightAndRepsString = ExerciseManager.ConvertJson(item.Tags, item),
            });
        }

        Toast.Make(Resources.Strings.AppResources.SavedString).Show();
    }

    private void InitSuperSetMode()
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsCheckBoxVisible = item.SuperSetId == 0;
        });

        CurrentAction = ExerciseCheckBoxAction.SuperSet;
        OnPropertyChanged(nameof(CurrentAction));
        PrepareAction(Resources.Strings.AppResources.CreateSuperSetString);
    }

    private void CollectCheckedExercises(TrainingExerciseViewModel item)
    {
        if (CurrentAction == ExerciseCheckBoxAction.Select)
        {
            return;
        }

        if (item.IsSelected)
        {
            if (selectedItems.All(sel => sel.Id != item.Id))
            {
                selectedItems.Add(item.Clone());
            }
        }
        else
        {
            if (selectedItems.Count == 0)
            {
                return;
            }

            var items = selectedItems.FirstOrDefault(selected => selected.TrainingExerciseId == item.TrainingExerciseId);
            if (items != null)
            {
                selectedItems.Remove(items);
            }
        }
    }

    private void CreateSuperSet()
    {
        Analytics.TrackEvent($"{GetType().Name}: CreateSuperSet finished");
        var id = App.Database.SaveSuperSetItem(new SuperSet()
        {
            Count = selectedItems.Count,
            TrainingId = Training.Id,
        });

        var superSetNum = Training.GetNewSuperSetNum();
        foreach (var trainingExerciseViewModel in Training.Exercises)
        {
            if (trainingExerciseViewModel.IsSelected)
            {
                trainingExerciseViewModel.SuperSetId = id;
                trainingExerciseViewModel.SuperSetNum = superSetNum;
                trainingExerciseViewModel.IsSelected = false;
                App.Database.SaveTrainingExerciseItem(trainingExerciseViewModel.GetTrainingExerciseComm());
            }
        }
    }

    private void StartMoveExercises()
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsCheckBoxVisible = true;
        });

        CurrentAction = ExerciseCheckBoxAction.Move;
        OnPropertyChanged(nameof(CurrentAction));
        PrepareAction(Resources.Strings.AppResources.MoveString);
    }

    private void StartCopyExercise()
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsCheckBoxVisible = true;
        });

        CurrentAction = ExerciseCheckBoxAction.Copy;
        OnPropertyChanged(nameof(CurrentAction));
        PrepareAction(Resources.Strings.AppResources.CopyString);
    }

    private void PrepareAction(string action)
    {
        IsExercisesCheckBoxVisible = true;
        OnPropertyChanged(nameof(IsExercisesCheckBoxVisible));

        ExerciseActionString = action;
        OnPropertyChanged(nameof(ExerciseActionString));
    }

    private void StopAction(bool result = false)
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsCheckBoxVisible = false;
        });

        Analytics.TrackEvent($"{GetType().Name}: StopAction {CurrentAction} Started");
        ExerciseActionString = Resources.Strings.AppResources.TrainingString;
        OnPropertyChanged(nameof(ExerciseActionString));

        if (result)
        {
            switch (CurrentAction)
            {
                case ExerciseCheckBoxAction.SuperSet:
                    Toast.Make(Resources.Strings.AppResources.SuperSetCreatedMessage).Show();
                    break;
                case ExerciseCheckBoxAction.Move:
                    Toast.Make(Resources.Strings.AppResources.MoveExercisesFinishedMessage).Show();
                    break;
                case ExerciseCheckBoxAction.Copy:
                    Toast.Make(Resources.Strings.AppResources.CopyExercisesFinishedMessage).Show();
                    break;
                case ExerciseCheckBoxAction.Select:
                    //DependencyService.Get<IMessage>().ShortAlert("Select Items");
                    break;
            }
        }
        else
        {
            Training.Exercises.ForEach(item => item.IsSelected = false);
            if (CurrentAction == ExerciseCheckBoxAction.Select)
            {
                Navigation.PopAsync(false);
                Navigation.PopAsync();
                return;
            }
        }

        selectedItems.Clear();
        CurrentAction = ExerciseCheckBoxAction.None;
        OnPropertyChanged(nameof(CurrentAction));

        IsExercisesCheckBoxVisible = false;
        OnPropertyChanged(nameof(IsExercisesCheckBoxVisible));
    }

    private void StartAction()
    {
        Analytics.TrackEvent($"{GetType().Name}: StartAction {CurrentAction} Started");
        if (CurrentAction == ExerciseCheckBoxAction.Select)
        {
            SaveTraining();
            StopAction(true);
            Navigation.PopAsync(false);
            Navigation.PopAsync();
        }
        else if (CurrentAction == ExerciseCheckBoxAction.Copy || CurrentAction == ExerciseCheckBoxAction.Move)
        {
            ReFillTrainingToCopyOrMove();
            TrainingExercisesMoveOrCopy page = new TrainingExercisesMoveOrCopy();
            page.BindingContext = this;
            Navigation.PushAsync(page);
        }
        else
        {
            if (selectedItems.Count > 1)
            {
                CreateSuperSet();
                StopAction(true);
            }
            else
            {
                MessageManager.DisplayAlert(Resources.Strings.AppResources.Denied, Resources.Strings.AppResources.SupersetInvalidMessage, Resources.Strings.AppResources.OkString);
            }
        }
    }

    private void ReFillTrainingToCopyOrMove()
    {
        var trainingsItems = App.Database.GetTrainingItems(); // get list of trainings
        TrainingItems.Clear();
        if (trainingsItems != null && trainingsItems.Any())
        {
            foreach (var training in trainingsItems)
            {
                if (training.Id != Training.Id)
                {
                    TrainingItems.Add(new TrainingViewModel(training)
                    {
                        Title = training.Title,
                    });
                }
            }
        }

        OnPropertyChanged(nameof(TrainingItems));
    }

    private void AcceptTrainingForMoveOrCopy()
    {
        Navigation.PopAsync();

        if (SelectedTrainingForCopyOrMove != null && SelectedTrainingForCopyOrMove.Id != 0)
        {
            while (selectedItems.Count != 0)
            {
                var model = selectedItems[0];
                var ex1 = model.GetTrainingExerciseComm();
                if (CurrentAction == ExerciseCheckBoxAction.Move)
                {
                    App.Database.DeleteTrainingExerciseItem(ex1.Id);
                    Training.DeleteExercise(model.TrainingExerciseId);
                    if (model.SuperSetId != 0)
                    {
                        CheckSuperSetExist(model.SuperSetId);
                    }
                }

                ex1.SuperSetId = 0;
                ex1.TrainingId = SelectedTrainingForCopyOrMove.Id;
                ex1.Id = 0;
                ex1.OrderNumber = -1;
                App.Database.SaveTrainingExerciseItem(ex1);
                selectedItems.Remove(model);
            }

            SelectedTrainingForCopyOrMove = null;
        }

        StopAction(true);
        Analytics.TrackEvent($"{GetType().Name}: AcceptTrainingForMoveOrCopy {CurrentAction} Finished");
    }

    private async void CreateTrainingFromSelectedExercises()
    {
        var result = await MessageManager.DisplayPromptAsync(Resources.Strings.AppResources.CreateNewString, Resources.Strings.AppResources.EnterTrainingName, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString, Resources.Strings.AppResources.NameString);
        if (result.IsNotNullOrEmpty())
        {
            await Navigation.PopAsync();

            var id = App.Database.SaveTrainingItem(new Training()
            {
                Title = result,
            });

            while (selectedItems.Count != 0)
            {
                var model = selectedItems[0];
                var ex1 = model.GetTrainingExerciseComm();
                if (CurrentAction == ExerciseCheckBoxAction.Move)
                {
                    App.Database.DeleteTrainingExerciseItem(ex1.Id);
                    Training.DeleteExercise(model.TrainingExerciseId);
                    if (model.SuperSetId != 0)
                    {
                        CheckSuperSetExist(model.SuperSetId);
                    }
                }

                ex1.SuperSetId = 0;
                ex1.TrainingId = id;
                ex1.Id = 0;
                ex1.OrderNumber = -1;
                App.Database.SaveTrainingExerciseItem(ex1);
                selectedItems.Remove(model);
            }

            StopAction(true);
            Analytics.TrackEvent($"{GetType().Name}: CreateTrainingFromSelectedExercises");
        }
    }

    private async void ShareTraining()
    {
        Analytics.TrackEvent($"{GetType().Name}: ShareTraining");

        var fn = $"{Resources.Strings.AppResources.TrainingString}_{Training.Title}.bin";
        var filename = Path.Combine(FileSystem.CacheDirectory, fn);

        Training.SaveToFile(filename);
        await Share.RequestAsync(new ShareFileRequest()
        {
            Title = Resources.Strings.AppResources.ShareTrainingString,
            File = new ShareFile(filename, "application/trday"),
        });
    }

    private async void ShowTrainingSettingsPage()
    {
        Analytics.TrackEvent($"{GetType().Name}: ShowTrainingSettingsPage");
        var page = new TrainingSettingsPage();
        page.ActionSelected += Page_ActionSelected;
        await Navigation.PushAsync(page);
    }

    private void Page_ActionSelected(object sender, TrainingSettingsPage.TrainingSettingsActions e)
    {
        Analytics.TrackEvent($"{GetType().Name}: ShowTrainingSettingsPage finished with {e}");
        switch (e)
        {
            case TrainingSettingsPage.TrainingSettingsActions.AddAlarm:
                MakeNotify();
                break;
            case TrainingSettingsPage.TrainingSettingsActions.ShareTraining:
                ShareTraining();
                break;
            case TrainingSettingsPage.TrainingSettingsActions.SuperSetAction:
                InitSuperSetMode();
                break;
            case TrainingSettingsPage.TrainingSettingsActions.MoveExercises:
                StartMoveExercises();
                break;
            case TrainingSettingsPage.TrainingSettingsActions.CopyExercises:
                StartCopyExercise();
                break;
        }
    }

    private void SelectAllItems()
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsSelected = isSelectAllState;
            selectedItems.Add(item.Clone());
        });

        isSelectAllState = !isSelectAllState;
    }

    private void SaveNewExerciseOrder()
    {
        int order = 0;
        foreach (var item in Training.Exercises)
        {
            var exId = App.Database.SaveExerciseItem(item.GetExercise());
            order++;
            App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm()
            {
                ExerciseId = exId,
                TrainingId = Training.Id,
                OrderNumber = order,
                Id = item.TrainingExerciseId,
                SuperSetId = item.SuperSetId,
                WeightAndRepsString = ExerciseManager.ConvertJson(item.Tags, item),
            });
            Debug.WriteLine($"Exercise {order} {item.ExerciseItemName}");
        }
    }

    private void DragSuperSet(int supersetId, int newIndex)
    {
        var needToDrag = Training.Exercises.Where(item => item.SuperSetId == supersetId).ToList();
        foreach (var model in needToDrag)
        {
            Training.Exercises.Remove(model);
        }

        if (newIndex > Training.Exercises.Count)
        {
            newIndex = Training.Exercises.Count;
        }

        foreach (var model in needToDrag)
        {
            Training.Exercises.Insert(newIndex, model);
            newIndex++;
        }

        SaveNewExerciseOrder();
    }

    private int FixIndexIfInsertInSuperSet(int insertAtIndex)
    {
        int maxCount = Training.Exercises.Count;
        int superSetId = Training.Exercises[insertAtIndex].SuperSetId;
        if (superSetId != 0)
        {
            while (insertAtIndex < Training.Exercises.Count - 1 && maxCount-- > 0)
            {
                if (Training.Exercises[insertAtIndex].SuperSetId == superSetId && insertAtIndex - 1 >= 0 && Training.Exercises[insertAtIndex - 1].SuperSetId == superSetId)
                {
                    insertAtIndex++;
                }
            }
        }

        return insertAtIndex;
    }

    #region Drag & Drop
    public ICommand ItemDragged => new Command<TrainingExerciseViewModel>(OnItemDragged);
    public ICommand ItemDraggedOver => new Command<TrainingExerciseViewModel>(OnItemDraggedOver);
    public ICommand ItemDragLeave => new Command<TrainingExerciseViewModel>(OnItemDragLeave);
    public ICommand ItemDropped => new Command<TrainingExerciseViewModel>(i => OnItemDropped(i));
    private void OnItemDragged(TrainingExerciseViewModel item)
    {
        Debug.WriteLine($"OnItemDragged: {item?.ExerciseItemName}");
        Training.Exercises.ForEach(i => i.IsBeingDragged = item == i);
    }

    private void OnItemDraggedOver(TrainingExerciseViewModel item)
    {
        Debug.WriteLine($"OnItemDraggedOver: {item?.ExerciseItemName}");
        var itemBeingDragged = Training.Exercises.FirstOrDefault(i => i.IsBeingDragged);
        Training.Exercises.ForEach(i => i.IsBeingDraggedOver = item == i && item != itemBeingDragged);
    }

    private void OnItemDragLeave(TrainingExerciseViewModel item)
    {
        Debug.WriteLine($"OnItemDragLeave: {item?.ExerciseItemName}");
        Training.Exercises.ForEach(i => i.IsBeingDraggedOver = false);
    }

    private async Task OnItemDropped(TrainingExerciseViewModel item)
    {
        var itemToMove = Training.Exercises.First(i => i.IsBeingDragged);
        var oldIndex = Training.Exercises.IndexOf(itemToMove);
        var itemToInsertBefore = item;

        if (itemToMove == null || itemToInsertBefore == null || itemToMove == itemToInsertBefore)
            return;

        var insertAtIndex = Training.Exercises.IndexOf(itemToInsertBefore);
        Training.Exercises.Move(oldIndex, insertAtIndex);
        itemToMove.IsBeingDragged = false;
        itemToInsertBefore.IsBeingDraggedOver = false;
        SaveNewExerciseOrder();
        Debug.WriteLine($"OnItemDropped: [{itemToMove?.ExerciseItemName}] => [{itemToInsertBefore?.ExerciseItemName}], target index = [{insertAtIndex}]");
    }

    #endregion
}

public enum ExerciseCheckBoxAction
{
    None,
    SuperSet,
    Move,
    Copy,
    Select,
}