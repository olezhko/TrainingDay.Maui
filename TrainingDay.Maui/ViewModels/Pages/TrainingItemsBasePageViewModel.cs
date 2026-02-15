using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using TrainingDay.Maui.Controls;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class TrainingItemsBasePageViewModel : BaseViewModel
{
    private readonly IDataService dataService;
    private bool isLongPressPopupOpened;
    private ObservableCollection<Grouping<string, TrainingViewModel>> itemsGrouped;

    public TrainingItemsBasePageViewModel(IDataService dataService)
    {
        this.dataService = dataService;
        ItemsGrouped = new ObservableCollection<Grouping<string, TrainingViewModel>>();
        AddNewTrainingCommand = new DoOnceCommand(AddNewTraining);
        ItemSelectedCommand = new DoOnceCommand<Border>(TrainingSelected);
        SubscribeMessages();
    }

    private async Task UpdateFirebaseToken()
    {
        #if IOS
        await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
        Settings.Token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        await dataService.SendFirebaseTokenAsync(Settings.Token, CultureInfo.CurrentCulture.Name, TimeZoneInfo.Local.BaseUtcOffset.ToString());
        await dataService.PostActionAsync(Settings.Token, Common.Communication.MobileActions.Enter);
        #endif
    }

    public void LoadItems(bool isOverride = false)
    {
        Shell.Current.Dispatcher.Dispatch(async () =>
        {
            await LoggingService.TrackEvent("Application start");
            await UpdateFirebaseToken();
        });
        var trainingsItems = App.Database.GetTrainingItems();

        var existCount = ItemsGrouped.Count;
        if (isOverride && ItemsGrouped.Any() && existCount == trainingsItems.Count())
        {
            return;
        }

        ItemsGrouped.Clear();
        SelectedTrainings.Clear();

        if (!trainingsItems.Any())
            return;

        List<Grouping<string, TrainingViewModel>> tempGroups = new List<Grouping<string, TrainingViewModel>>();
        var trainingsGroups = App.Database.GetTrainingsGroups();
        var lastTrainings = App.Database.GetLastTrainingItems();
        foreach (var training in trainingsItems)
        {
            FillGroupedTraining(training, trainingsGroups, lastTrainings, tempGroups);
        }

        foreach (var tempGroup in tempGroups)
        {
            ItemsGrouped.Add(tempGroup);
        }

        SelectWorkout(tempGroups);
    }

    private void SelectWorkout(List<Grouping<string, TrainingViewModel>> tempGroups)
    {
        try
        {
            SelectedTrainings.Clear();
            var defaultGroup = tempGroups.FirstOrDefault(gp => gp.Key == AppResources.GroupingDefaultName);
            if (defaultGroup is null)
            {
                defaultGroup = tempGroups.FirstOrDefault();
            }

            defaultGroup.IsSelected = true;
            
            foreach (var item in defaultGroup)
            {
                SelectedTrainings.Add(item);
            }

            OnPropertyChanged(nameof(SelectedTrainings));
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    private void FillGroupedTraining(TrainingEntity training, IEnumerable<TrainingUnionEntity> unions, IEnumerable<LastTrainingEntity> lastTrainings,
        IList<Grouping<string, TrainingViewModel>> tempGroups)
    {
        var lastTraining = lastTrainings
            .Where(item => item.TrainingId == training.Id)
            .OrderByDescending(item => item.Time)
            .FirstOrDefault();

        var trainingUnion = unions.FirstOrDefault(union => new TrainingUnionViewModel(union).TrainingIDs.Contains(training.Id));
        if (trainingUnion != null) // union exist
        {
            var group = tempGroups.FirstOrDefault(item => item.Id == trainingUnion.Id);
            if (group != null)
            {
                var item = new TrainingViewModel(training);
                if (lastTraining != null)
                {
                    item.LastImplementedDateTime = lastTraining.Time.ToString(Settings.GetLanguage());
                }

                item.GroupName = group.First().GroupName;
                group.Add(item);
            }
            else
            {
                tempGroups.Add(new Grouping<string, TrainingViewModel>(trainingUnion.Name, new List<TrainingViewModel>())
                {
                    Id = trainingUnion.Id,
                });
                var item = new TrainingViewModel(training);
                if (lastTraining != null)
                {
                    item.LastImplementedDateTime = lastTraining.Time.ToString(Settings.GetLanguage());
                }

                item.GroupName = new TrainingUnionViewModel(trainingUnion);
                tempGroups.Last().Add(item);
            }
        }
        else
        {
            var item = new TrainingViewModel(training);
            if (lastTraining != null)
                item.LastImplementedDateTime = lastTraining.Time.ToString(Settings.GetLanguage());

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

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<IncomingTrainingAddedMessage>(this, async (r, args) =>
        {
            LoadItems();
            LoggingService.TrackEvent($"TrainingItemsBasePageViewModel: Added Incoming Training");
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

    private async void DeleteSelectedTraining(Border viewCell)
    {
        try
        {
            LoggingService.TrackEvent($"{GetType().Name}: DeleteSelectedTraining Clicked");
            var item = (TrainingViewModel)viewCell.BindingContext;
            var result = await MessageManager.DisplayAlert(AppResources.DeleteTraining, item.Title, AppResources.OkString, AppResources.CancelString);
            if (result)
            {
                LoggingService.TrackEvent($"{GetType().Name}: DeleteSelectedTraining Clicked Approved");
                
                item.DeleteTrainingsItemsFromBase();

                var group = ItemsGrouped.FirstOrDefault(gr => gr.Contains(item));
                group.Remove(item);

                if (group.Count == 0)
                {
                    ItemsGrouped.Remove(group);
                    var groups = App.Database.GetTrainingsGroups();
                    var gr = groups.FirstOrDefault(a => a.Name == group.Key);
                    if (gr != null)
                        App.Database.DeleteTrainingGroup(gr.Id);
                }

                OnPropertyChanged(nameof(ItemsGrouped));
                SelectWorkout(ItemsGrouped.ToList());
                await Toast.Make(AppResources.DeletedString).Show();
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    private async Task TrainingSelected(Border viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name}: TrainingSelected Clicked");
        var item = (TrainingViewModel)viewCell.BindingContext;
        await Shell.Current.GoToAsync($"{nameof(TrainingExercisesPage)}?{nameof(TrainingExercisesPageViewModel.ItemId)}={item.Id}");
    }

    private void DuplicateSelectedTraining(Border viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name}: DuplicateSelectedTraining Clicked");
        var training = (TrainingViewModel)viewCell.BindingContext;
        int id = App.Database.SaveTrainingItem(new TrainingEntity()
        {
            Title = training.Title + AppResources.CopiedString,
        });

        // save every exercise
        int order = 0;
        var exercises = App.Database.GetTrainingExerciseItemByTrainingId(training.Id);
        foreach (var item in exercises)
        {
            // save order numbers
            App.Database.SaveTrainingExerciseItem(new TrainingExerciseEntity()
            {
                ExerciseId = item.ExerciseId,
                TrainingId = id,
                OrderNumber = order,
                SuperSetId = item.SuperSetId,
                WeightAndRepsString = ExerciseManager.ConvertJson(item.Tags, item),
            });
            order++;
        }

        Toast.Make(AppResources.SavedString).Show();

        LoadItems();
    }

    #region grouping
    private async Task AddToGroup(Border viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name}: AddToGroup started");

        var item = (TrainingViewModel)viewCell.BindingContext;

        var result = await Shell.Current.ShowPopupAsync(
            PopupBuilders.BuildWorkoutGroupsPopup(async accept =>
                {
                    await GroupSelected(item, accept.Id);
                }, async () => await CreateNewGroup(item)
            )
        );
    }

    private async Task GroupSelected(TrainingViewModel TrainingMoveToGroup, int id)
    {
        try
        {
            //  если тренировка уже в группе и группа такая же, как и выбрали
            if (TrainingMoveToGroup.GroupName != null && id == TrainingMoveToGroup.GroupName.Id)
            {
                await Shell.Current.ClosePopupAsync();
                return;
            }

            var unions = App.Database.GetTrainingsGroups();
            // если тренировка в группе и группа не "Основные"
            if (TrainingMoveToGroup.GroupName != null && TrainingMoveToGroup.GroupName.Id != 0)
            {
                var unionToEdit = unions.First(u => u.Id == TrainingMoveToGroup.GroupName.Id);
                var vm = new TrainingUnionViewModel(unionToEdit);
                vm.TrainingIDs.Remove(TrainingMoveToGroup.Id);
                if (vm.TrainingIDs.Count != 0)
                    App.Database.SaveTrainingGroup(vm.Model);
                else
                    App.Database.DeleteTrainingGroup(vm.Id);
            }

            if (id != 0)
            {
                var union = unions.FirstOrDefault(un => un.Id == id);
                if (union != null)
                {
                    var viewModel = new TrainingUnionViewModel(union);
                    viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);
                    TrainingMoveToGroup.GroupName = viewModel;
                    App.Database.SaveTrainingGroup(viewModel.Model);
                }
            }

            LoggingService.TrackEvent($"{GetType().Name}: AddToGroup finsihed");
            await Toast.Make(AppResources.SavedString).Show();
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        await Shell.Current.ClosePopupAsync();
    }

    private async Task CreateNewGroup(TrainingViewModel TrainingMoveToGroup)
    {
        LoggingService.TrackEvent($"{GetType().Name}: AddToGroup with new group STARTED");
        string result = await Shell.Current.DisplayPromptAsync(string.Empty, AppResources.GroupingEnterNameofGroup, AppResources.OkString, AppResources.CancelString, placeholder: AppResources.NameString);
        if (result != null)
        {
            try
            {
                var name = result;
                if (!string.IsNullOrEmpty(name))
                {
                    var unions = App.Database.GetTrainingsGroups();
                    DeleteFromGroup(TrainingMoveToGroup, [.. unions]);
                    var union = unions.FirstOrDefault(un => un.Name == name);
                    if (union != null) // если группа с таким именем уже существует
                    {
                        var viewModel = new TrainingUnionViewModel(union);
                        if (!viewModel.TrainingIDs.Contains(TrainingMoveToGroup.Id))
                        {
                            viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);// добавляем в список тренировок у группы выбранную тренировку
                            TrainingMoveToGroup.GroupName = viewModel;
                            App.Database.SaveTrainingGroup(viewModel.Model);
                        }
                    }
                    else
                    {
                        var viewModel = new TrainingUnionViewModel();
                        viewModel.Name = name;
                        viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);
                        TrainingMoveToGroup.GroupName = viewModel;
                        App.Database.SaveTrainingGroup(viewModel.Model);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        LoggingService.TrackEvent($"{GetType().Name}: AddToGroup with new group FINISHED");

        await Toast.Make(AppResources.SavedString).Show();

        await Shell.Current.ClosePopupAsync();
    }

    private void DeleteFromGroup(TrainingViewModel TrainingMoveToGroup, List<TrainingUnionEntity> unions)
    {
        try
        {
            if (TrainingMoveToGroup.GroupName != null && TrainingMoveToGroup.GroupName.Id != 0)
            {
                var unionToEdit = new TrainingUnionViewModel(unions.First(u => u.Id == TrainingMoveToGroup.GroupName.Id));
                unionToEdit.TrainingIDs.Remove(TrainingMoveToGroup.Id);
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

    private void DeleteFromGroup(Border viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name}: Delete From Group");

        var item = (TrainingViewModel)viewCell.BindingContext;

        if (item.GroupName == null || item.GroupName.Id == 0)
        {
            MessageManager.DisplayAlert(AppResources.Denied, AppResources.GroupingTrainingNotInGroup, AppResources.OkString);
            return;
        }

        var union = App.Database.GetTrainingGroup(item.GroupName.Id);
        var viewModel = new TrainingUnionViewModel(union);
        viewModel.TrainingIDs.Remove(item.Id);

        if (viewModel.TrainingIDs.Count == 0)
        {
            App.Database.DeleteTrainingGroup(viewModel.Model.Id);
        }
        else
        {
            App.Database.SaveTrainingGroup(viewModel.Model);
        }

        item.GroupName = null;
        LoadItems();

        Toast.Make(AppResources.SavedString).Show();
    }

    private void ActivateGroup(object item)
    {
        var group = item as Grouping<string, TrainingViewModel>;
        SelectedTrainings.Clear();
        ItemsGrouped.ForEach(item => item.IsSelected = false);
        foreach (var training in group)
        {
            SelectedTrainings.Add(training);
        }
        group.IsSelected = true;
    }

    #endregion
    
    private async void LongPressed(Border sender)
    {
        if (isLongPressPopupOpened)
        {
            return;
        }

        LoggingService.TrackEvent($"{GetType().Name}: LongPressed started");

        var item = (TrainingViewModel)sender.BindingContext;

        isLongPressPopupOpened = true;
        var action = await MessageManager.DisplayActionSheet(AppResources.ChoseAction, AppResources.CancelString, AppResources.RemoveString, AppResources.Duplicate, item.GroupName == null ? AppResources.TrainingToGroupString : AppResources.TrainingUnGroupString.Fill(item.GroupName.Name));
        isLongPressPopupOpened = false;
        LoggingService.TrackEvent($"{GetType().Name}: LongPressed finished with {action}");
        if (action == AppResources.CancelString)
        {
            return;
        }

        if (action == AppResources.RemoveString)
        {
            DeleteSelectedTraining(sender);
            return;
        }

        if (action == AppResources.Duplicate)
        {
            DuplicateSelectedTraining(sender);
            return;
        }

        if (action == AppResources.TrainingToGroupString)
        {
            AddToGroup(sender);
            return;
        }

        if (item.GroupName != null && action == AppResources.TrainingUnGroupString.Fill(item.GroupName.Name))
        {
            DeleteFromGroup(sender);
            return;
        }
    }

    #region Properties

    public ObservableCollection<Grouping<string, TrainingViewModel>> ItemsGrouped { get => itemsGrouped; set => SetProperty(ref itemsGrouped, value); }

    public ObservableCollection<TrainingViewModel> SelectedTrainings { get; set; } = new ObservableCollection<TrainingViewModel>();

    public ICommand AddNewTrainingCommand { get; set; }

    public ICommand ItemSelectedCommand { get; set; }

    public Command<object> SelectGroupCommand => new Command<object>(ActivateGroup);

    public ICommand LongPressedEffectCommand => new Command<Border>(LongPressed);
    #endregion
}