﻿using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AppCenter.Analytics;
using System.Collections.ObjectModel;
using System.Windows.Input;
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
    private bool isLongPressPopupOpened;
    private ObservableCollection<Grouping<string, TrainingViewModel>> itemsGrouped;

    public TrainingItemsBasePageViewModel()
    {
        ItemsGrouped = new ObservableCollection<Grouping<string, TrainingViewModel>>();
        AddNewTrainingCommand = new Command(AddNewTraining);
        ItemSelectedCommand = new Command<Frame>(TrainingSelected);
        SubscribeMessages();
    }

    public void LoadItems(bool isOverride = false)
    {
        var trainingsItems = App.Database.GetTrainingItems();

        var existCount = ItemsGrouped.Count;
        if (isOverride && ItemsGrouped.Any() && existCount == trainingsItems.Count())
        {
            return;
        }

        ItemsGrouped.Clear();

        IsGrouped = trainingsItems.Any();
        OnPropertyChanged(nameof(IsGrouped));

        if (!IsGrouped)
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
            tempGroup.Expanded = tempGroup.Expanded;
            ItemsGrouped.Add(tempGroup);
        }
    }

    private void FillGroupedTraining(Training training, IEnumerable<TrainingUnion> unions, IEnumerable<LastTraining> lastTrainings,
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
                    Expanded = trainingUnion.IsExpanded,
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
            {
                item.LastImplementedDateTime = lastTraining.Time.ToString(Settings.GetLanguage());
            }

            Grouping<string, TrainingViewModel> defaultGroup = tempGroups.FirstOrDefault(gp => gp.Key == AppResources.GroupingDefaultName);
            if (defaultGroup == null)
            {
                var gr = new Grouping<string, TrainingViewModel>(AppResources.GroupingDefaultName, new List<TrainingViewModel>())
                {
                    Expanded = Settings.IsExpandedMainGroup,
                };
                gr.Add(item);
                tempGroups.Insert(0, gr);
            }
            else
            {
                defaultGroup.Add(item);
            }
        }
    }

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<IncomingTrainingAddedMessage>(this, (r, args) =>
        {
            LoadItems();
            LoggingService.TrackEvent($"TrainingItemsBase Added Incoming Training");
        });
    }

    private void UnsubscribeMessages()
    {
        WeakReferenceMessenger.Default.Unregister<IncomingTrainingAddedMessage>(this);
    }

    private async void AddNewTraining()
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} AddNewTraining Button Clicked");
        await Shell.Current.GoToAsync(nameof(PreparedTrainingsPage));
    }

    private async void DeleteSelectedTraining(Frame viewCell)
    {
        try
        {
            LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} DeleteSelectedTraining Clicked");
            var item = (TrainingViewModel)viewCell.BindingContext;
            var result = await MessageManager.DisplayAlert(AppResources.DeleteTraining, item.Title, AppResources.OkString, AppResources.CancelString);
            if (result)
            {
                LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} DeleteSelectedTraining Clicked Approved");
                App.Database.DeleteTrainingItem(item.Id);
                item.DeleteTrainingsItemsFromBase();

                var group = ItemsGrouped.FirstOrDefault(gr => gr.Contains(item));
                group.Remove(item);
                if (group.Count == 0)
                {
                    ItemsGrouped.Remove(group);
                    var groups = App.Database.GetTrainingsGroups();
                    var gr = groups.FirstOrDefault(a => a.Name == group.Key);
                    if (gr != null)
                    {
                        App.Database.DeleteTrainingGroup(gr.Id);
                    }
                }

                IsGrouped = ItemsGrouped.Count > 0;
                OnPropertyChanged(nameof(IsGrouped));
                OnPropertyChanged(nameof(ItemsGrouped));
                await Toast.Make(AppResources.DeletedString).Show();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private async void TrainingSelected(Frame viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} TrainingSelected Clicked");
        var item = (TrainingViewModel)viewCell.BindingContext;
        await Shell.Current.GoToAsync($"{nameof(TrainingExercisesPage)}?{nameof(TrainingExercisesPageViewModel.ItemId)}={item.Id}");
    }

    private void DuplicateSelectedTraining(Frame viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} DuplicateSelectedTraining Clicked");
        var training = (TrainingViewModel)viewCell.BindingContext;
        int id = App.Database.SaveTrainingItem(new Training()
        {
            Title = training.Title + AppResources.CopiedString,
        });

        // save every exercise
        int order = 0;
        var exercises = App.Database.GetTrainingExerciseItemByTrainingId(training.Id);
        foreach (var item in exercises)
        {
            // save order numbers
            App.Database.SaveTrainingExerciseItem(new TrainingExerciseComm()
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

    private void AddToGroup(Frame viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} AddToGroup started");

        var item = (TrainingViewModel)viewCell.BindingContext;

        Navigation.PushAsync(new TrainingsSetGroupPage()
        {
            TrainingMoveToGroup = item,
        });
    }

    private void DeleteFromGroup(Frame viewCell)
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} Delete From Group");

        var item = (TrainingViewModel)viewCell.BindingContext;

        if (item.GroupName == null || item.GroupName.Id == 0)
        {
            MessageManager.DisplayAlert(AppResources.Denied, AppResources.GroupingTrainingNotInGroup, AppResources.OkString);
        }
        else
        {
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
    }

    private static void ToggleExpandGroup(object item)
    {
        var group = item as Grouping<string, TrainingViewModel>;
        group.Expanded = !group.Expanded;

        var groups = App.Database.GetTrainingsGroups();
        if (group.Key == AppResources.GroupingDefaultName)
        {
            Settings.IsExpandedMainGroup = group.Expanded;
            return;
        }

        // save to DB
        var gr = groups.First(a => a.Name == group.Key);
        gr.IsExpanded = group.Expanded;
        App.Database.SaveTrainingGroup(gr);
    }

    private async void LongPressed(Frame sender)
    {
        if (isLongPressPopupOpened)
        {
            return;
        }

        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} LongPressed started");

        var item = (TrainingViewModel)sender.BindingContext;

        isLongPressPopupOpened = true;
        var action = await MessageManager.DisplayActionSheet(AppResources.ChoseAction, AppResources.CancelString, AppResources.RemoveString, AppResources.Duplicate, item.GroupName == null ? AppResources.TrainingToGroupString : AppResources.TrainingUnGroupString.Fill(item.GroupName.Name));
        isLongPressPopupOpened = false;
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} LongPressed finished with {action}");
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
    public bool IsGrouped { get; set; }

    public ObservableCollection<Grouping<string, TrainingViewModel>> ItemsGrouped { get => itemsGrouped; set => SetProperty(ref itemsGrouped, value); }

    public INavigation Navigation { get; set; }

    public ICommand AddNewTrainingCommand { get; set; }

    public ICommand ItemSelectedCommand { get; set; }

    public Command<object> ToggleExpandGroupCommand => new Command<object>(ToggleExpandGroup);

    public ICommand LongPressedEffectCommand => new Command<Frame>(LongPressed);
    #endregion
}