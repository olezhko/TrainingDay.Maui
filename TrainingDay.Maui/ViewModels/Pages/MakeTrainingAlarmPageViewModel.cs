using CommunityToolkit.Maui.Alerts;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

[QueryProperty(nameof(TrainingId), nameof(TrainingId))]

public class MakeTrainingAlarmPageViewModel : BaseViewModel
{
    public ObservableCollection<TrainingViewModel> TrainingItems { get; set; } = new ObservableCollection<TrainingViewModel>();

    private TrainingViewModel _selTrainingViewModel;
    public TrainingViewModel SelectedTrainingItem
    {
        get => _selTrainingViewModel;
        set
        {
            _selTrainingViewModel = value;
            OnPropertyChanged();
        }
    }

    private AlarmViewModel alarm;
    public AlarmViewModel Alarm
    {
        get => alarm;
        set
        {
            alarm = value;
            OnPropertyChanged();
        }
    }

    public int TrainingId
    {
        get => Alarm.TrainingId;
        set
        {
            if (Alarm == null)
            {
                Alarm = new AlarmViewModel();
            }

            if (Alarm.TrainingId != value)
            {
                Alarm.TrainingId = value;
                OnPropertyChanged();
            }
        }
    }

    public MakeTrainingAlarmPageViewModel()
    {
        LoadTrainingItems();
    }

    public ICommand CloseCommand => new Command(Close);

    private void Close()
    {
        Shell.Current.Navigation.PopModalAsync();
    }

    public ICommand SaveAlarmCommand => new Command(SaveAlarm);
    private async void SaveAlarm()
    {
        if (!ValidateFields())
        {
            await Toast.Make(Resources.Strings.AppResources.NotAllFieldsEntered).Show();
            return;
        }

        Alarm.IsActive = true;
        //Set alarm and add to our list of alarms
        Alarm.AlarmItem.TrainingId = SelectedTrainingItem.Id;
        var newItem = new Models.Database.Alarm()
        {
            Id = Alarm.AlarmItem.Id,
            Days = Alarm.Days.Value,
            Name = Alarm.Name,
            TimeOffset = Alarm.GetDateTimeOffsetFromTimeSpan(Alarm.Time),
            IsActive = Alarm.IsActive,
            TrainingId = SelectedTrainingItem.Id,
            ServerId = Alarm.AlarmItem.ServerId,
        };

        var id = App.Database.SaveAlarmItem(newItem);
        Alarm.AlarmItem.Id = id;
        await Toast.Make(Resources.Strings.AppResources.SavedString).Show();
        App.SyncAlarms();
        await Shell.Current.GoToAsync("..");
    }

    private bool ValidateFields()
    {
        bool validation = DaysOfWeek.GetHasADayBeenSelected(Alarm.Days);

        if (string.IsNullOrWhiteSpace(Alarm.Name) || SelectedTrainingItem == null)
        {
            validation = false;
        }

        return validation;
    }

    private void LoadTrainingItems()
    {
        var items = App.Database.GetTrainingItems();
        foreach (var training in items)
        {
            TrainingItems.Add(new TrainingViewModel(training));
        }

        OnPropertyChanged(nameof(TrainingItems));
    }

    public ICommand DeleteAlarmCommand => new Command(DeleteAlarm);
    private async void DeleteAlarm()
    {
        var result = await MessageManager.DisplayAlert(AppResources.DeleteAlarmQuestion, string.Empty, AppResources.OkString, AppResources.CancelString);
        if (result)
        {
            App.Database.DeleteAlarmItem(Alarm.AlarmItem.Id);
            await Toast.Make(Resources.Strings.AppResources.DeletedString).Show();
            App.SyncAlarms();
            await Shell.Current.GoToAsync("..");
        }
    }
}