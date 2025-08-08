using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

[QueryProperty(nameof(ItemId), nameof(ItemId))]
public sealed class TrainingExercisesPageViewModel : BaseViewModel
{
    private int itemId;

    public ObservableCollection<TrainingViewModel> TrainingItems { get; set; } = new ObservableCollection<TrainingViewModel>();

    public string ExerciseActionString { get; set; } = Resources.Strings.AppResources.TrainingString;

    public bool IsExercisesCheckBoxVisible { get; set; }

    public ExerciseCheckBoxAction CurrentAction { get; set; }

    public int TappedExerciseIndex { get; set; } = -1;

    public TrainingViewModel Training { get; set; }

    public ICommand DeleteExerciseCommand => new Command<TrainingExerciseViewModel>(DeleteExercise);

    public ICommand MakeTrainingCommand => new Command(MakeTraining);

    public ICommand ItemTappedCommand => new Command<object>(TrainingExerciseTapped);

    public ICommand ShowTrainingSettingsPageCommand => new Command(ShowTrainingSettingsPage);

    public ICommand AddExercisesCommand => new Command(AddExercises);

    public ICommand CancelActionCommand => new Command(() => StopAction());

    public ICommand StartMoveExerciseCommand => new Command(StartMoveExercises);

    public ICommand StartActionCommand => new Command(StartAction);

    public ICommand AcceptTrainingForMoveOrCopyCommand => new Command(AcceptTrainingForMoveOrCopy);

	public TrainingViewModel? SelectedTrainingForCopyOrMove { get; set; } = null;

    public ICommand CreateTrainingFromSelectedExercisesCommand => new Command(CreateTrainingFromSelectedExercises);

    public int ItemId
    {
        get => itemId;
        set
        {
            if (itemId != value)
            {
                itemId = value;
                LoadItemId(value);
                OnPropertyChanged();
            }
        }
    }

    public TrainingExercisesPageViewModel()
    {
        Training = new TrainingViewModel();
    }

    #region Load
    private void LoadItemId(int id)
    {
        TrainingViewModel trVm = new TrainingViewModel(App.Database.GetTrainingItem(id));
        PrepareTrainingViewModel(trVm);
        Load(trVm);
    }

    private static void PrepareTrainingViewModel(TrainingViewModel vm)
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
    #endregion

    private async void AddExercises()
    {
        WeakReferenceMessenger.Default.Unregister<ExercisesSelectFinishedMessage>(this);
        WeakReferenceMessenger.Default.Register<ExercisesSelectFinishedMessage>(this, (r, m) =>
        {
            AddSelectedExercises(m.Selected.Select(item => TrainingExerciseViewModel.Create(item.GetExercise())));
            WeakReferenceMessenger.Default.Unregister<ExercisesSelectFinishedMessage>(this);
            LoggingService.TrackEvent($"{GetType().Name}: AddExercises finished");
        });

        Dictionary<string, object> param = new Dictionary<string, object> { { "ExistedExercises", Training.Exercises } };

        await Shell.Current.GoToAsync(nameof(ExerciseListPage), param);
        LoggingService.TrackEvent($"{GetType().Name}: AddExercises started");
    }

    private void AddSelectedExercises(IEnumerable<TrainingExerciseViewModel> args)
    {
        args.ForEach(a => a.IsSelected = false);
        var count = Training.Exercises.Count - 1;
        foreach (var exerciseItem in args)
        {
            exerciseItem.TrainingId = Training.Id;
            exerciseItem.OrderNumber = count;
            var id = App.Database.SaveTrainingExerciseItem(exerciseItem.GetTrainingExerciseComm());
            exerciseItem.TrainingExerciseId = id;
            Training.AddExercise(exerciseItem);
            OnPropertyChanged(nameof(Training.Exercises));
            count += 1;
        }

        LoggingService.TrackEvent($"{GetType().Name}: AddExercises finished");
    }

    private async void DeleteExercise(TrainingExerciseViewModel sender)
    {
        LoggingService.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} started");
        if (CurrentAction == ExerciseCheckBoxAction.SuperSet)
        {
            if (sender.SuperSetId != 0)
            {
                var result = await MessageManager.DisplayAlert(Resources.Strings.AppResources.DeleteExerciseFromSuperSetQuestion, string.Empty, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString);
                if (result)
                {
                    LoggingService.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} finished");

                    var id = sender.SuperSetId;
                    sender.SuperSetId = 0;
                    sender.SuperSetNum = 0;
                    sender.IsCheckBoxVisible = true;
                    CheckSuperSetExist(id);
                    App.Database.SaveTrainingExerciseItem(sender.GetTrainingExerciseComm());
                }
            }
        }
        else
        {
            var result = await MessageManager.DisplayAlert(Resources.Strings.AppResources.DeleteExercise, sender.Name, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString);
            if (result)
            {
                LoggingService.TrackEvent($"{GetType().Name}: DeleteExercise {CurrentAction} finished");
                Training.DeleteExercise(sender);
                if (sender.SuperSetId != 0)
                    CheckSuperSetExist(sender.SuperSetId);

                App.Database.DeleteTrainingExerciseItem(sender.TrainingExerciseId);
            }
        }
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

        selectedExercise.IsNotFinished = false;// --> for Time start button, to hide button

        Dictionary<string, object> param = new Dictionary<string, object> { { "Item", selectedExercise } };
        await Shell.Current.GoToAsync(nameof(TrainingExerciseItemPage), param);
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
            item.IsCheckBoxVisible = true;
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

        LoggingService.TrackEvent($"{GetType().Name}: Training Implementing started");

        Dictionary<string, object> param = new Dictionary<string, object> { { "TrainingItem", Training } };
        await Shell.Current.GoToAsync(nameof(TrainingImplementPage), param);
    }

    private void InitSuperSetMode()
    {
        Training.Exercises.ForEach(item =>
        {
            item.IsCheckBoxVisible = item.SuperSetId == 0;
        });

        CurrentAction = ExerciseCheckBoxAction.SuperSet;
        OnPropertyChanged(nameof(CurrentAction));
        MessageManager.DisplayAlert(AppResources.AdviceString,
                "Нажмите на кружочек справа от упражнения для тех, какие хотите добавить в суперсет. Или крестик, если хотите удалить из других суперсетов.", AppResources.OkString);
        PrepareAction(Resources.Strings.AppResources.CreateSuperSetString);
    }

    private async Task CreateSuperSet()
    {
        var countSelected = Training.Exercises.Count(item => item.IsSelected);
        if (countSelected < 2)
        {
            await MessageManager.DisplayAlert(Resources.Strings.AppResources.Denied, Resources.Strings.AppResources.SupersetInvalidMessage, Resources.Strings.AppResources.OkString);
        }

        LoggingService.TrackEvent($"{GetType().Name}: CreateSuperSet finished");
        var id = App.Database.SaveSuperSetItem(new SuperSetDto()
        {
            Count = countSelected,
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

        LoggingService.TrackEvent($"{GetType().Name}: StopAction {CurrentAction} Started");
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
            }
        }

        Training.Exercises.ForEach(item => item.IsSelected = false);
        
        CurrentAction = ExerciseCheckBoxAction.None;
        OnPropertyChanged(nameof(CurrentAction));

        IsExercisesCheckBoxVisible = false;
        OnPropertyChanged(nameof(IsExercisesCheckBoxVisible));
    }

    private async void StartAction()
    {
        LoggingService.TrackEvent($"{GetType().Name}: StartAction {CurrentAction} Started");
        if (CurrentAction == ExerciseCheckBoxAction.Copy || CurrentAction == ExerciseCheckBoxAction.Move)
        {
            UpdateTrainingListToMoveOrCopy();

            Dictionary<string, object> param = new Dictionary<string, object> { { "Context", this } };
            await Shell.Current.GoToAsync(nameof(TrainingExercisesMoveOrCopy), param);
        }
        else
        {
            await CreateSuperSet();
            StopAction(true);
        }
    }

    private void UpdateTrainingListToMoveOrCopy()
    {
        var trainingsItems = App.Database.GetTrainingItems();
        if (trainingsItems != null && trainingsItems.Any())
        {
            TrainingItems = trainingsItems.Where(item => item.Id != Training.Id)
                .Select(item => new TrainingViewModel(item)
                {
                    Title = item.Title,
                })
                .ToObservableCollection();
        }

        OnPropertyChanged(nameof(TrainingItems));
    }

    private async void AcceptTrainingForMoveOrCopy()
    {
        await Shell.Current.GoToAsync("..");

        var selectedItems = Training.Exercises.Where(item => item.IsSelected).Select(item => item.Clone()).ToList();
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
        LoggingService.TrackEvent($"{GetType().Name}: AcceptTrainingForMoveOrCopy {CurrentAction} Finished");
    }

    private async void CreateTrainingFromSelectedExercises()
    {
        var result = await MessageManager.DisplayPromptAsync(Resources.Strings.AppResources.CreateNewString, Resources.Strings.AppResources.EnterTrainingName, Resources.Strings.AppResources.OkString, Resources.Strings.AppResources.CancelString, Resources.Strings.AppResources.NameString);
        if (result.IsNotNullOrEmpty())
        {
            await Shell.Current.GoToAsync("..");

            var id = App.Database.SaveTrainingItem(new TrainingDto()
            {
                Title = result,
            });

            var selectedItems = Training.Exercises.Where(item => item.IsSelected).Select(item => item.Clone()).ToList();
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
            LoggingService.TrackEvent($"{GetType().Name}: CreateTrainingFromSelectedExercises");
        }
    }

    private async void ShareTraining()
    {
        LoggingService.TrackEvent($"{GetType().Name}: ShareTraining");

        string filename = Path.Combine(FileSystem.CacheDirectory, $"{AppResources.TrainingString}_{Training.Title}.trday");

        Training.SaveToFile(filename);

        await Share.RequestAsync(new ShareFileRequest()
        {
            Title = AppResources.ShareTrainingString,
            File = new ShareFile(filename, "application/trday"),
        });
    }

    private async void ShowTrainingSettingsPage()
    {
        LoggingService.TrackEvent($"{GetType().Name}: ShowTrainingSettingsPage");

        var action = await Shell.Current.DisplayActionSheet(AppResources.ChoseAction, AppResources.CancelString, null, 
            AppResources.ShareTrainingString, 
            AppResources.SuperSetControl, 
            AppResources.MoveString, 
            AppResources.CopyString);

        LoggingService.TrackEvent($"{GetType().Name}: ShowTrainingSettingsPage finished with {action}");
        if(action == AppResources.ShareTrainingString)
        {
            ShareTraining();
        }
        else if (action == AppResources.SuperSetControl)
        {
            InitSuperSetMode();
        }
        else if (action == AppResources.MoveString)
        {
            StartMoveExercises();
        }
        else if (action == AppResources.CopyString)
        {
            StartCopyExercise();
        }
    }

    private void SaveNewExerciseOrder()
    {
        int order = 0;
        foreach (var item in Training.Exercises)
        {
            order++;
            App.Database.SaveTrainingExerciseItem(new TrainingExerciseDto()
            {
                ExerciseId = item.ExerciseId,
                TrainingId = Training.Id,
                OrderNumber = order,
                Id = item.TrainingExerciseId,
                SuperSetId = item.SuperSetId,
                WeightAndRepsString = ExerciseManager.ConvertJson(item.Tags, item),
            });
        }
    }

    #region Drag & Drop
    public ICommand ItemDragged => new Command<TrainingExerciseViewModel>(OnItemDragged);
    public ICommand ItemDraggedOver => new Command<TrainingExerciseViewModel>(OnItemDraggedOver);
    public ICommand ItemDragLeave => new Command<TrainingExerciseViewModel>(OnItemDragLeave);
    public ICommand ItemDropped => new Command<TrainingExerciseViewModel>(i => OnItemDropped(i));
    private void OnItemDragged(TrainingExerciseViewModel item)
    {
        Training.Exercises.ForEach(i => i.IsBeingDragged = item == i);
    }

    private void OnItemDraggedOver(TrainingExerciseViewModel item)
    {
        var itemBeingDragged = Training.Exercises.FirstOrDefault(i => i.IsBeingDragged);
        Training.Exercises.ForEach(i => i.IsBeingDraggedOver = item == i && item != itemBeingDragged);
    }

    private void OnItemDragLeave(TrainingExerciseViewModel item)
    {
        Training.Exercises.ForEach(i => i.IsBeingDraggedOver = false);
    }

    private void OnItemDropped(TrainingExerciseViewModel item)
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
    }

    #endregion
}

public enum ExerciseCheckBoxAction
{
    None,
    SuperSet,
    Move,
    Copy,
}