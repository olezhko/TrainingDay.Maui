using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class TrainingAlarmListPageViewModel : BaseViewModel
{
    public TrainingAlarmListPageViewModel()
    {
    }

    public INavigation Navigation { get; set; }

    public ObservableCollection<AlarmViewModel> Alarms { get; set; } = new ObservableCollection<AlarmViewModel>();

    public void LoadItems()
    {
        Alarms.Clear();
        var itemsfromBase = App.Database.GetAlarmItems();
        foreach (var alarm in itemsfromBase)
        {
            var newItem = new AlarmViewModel(alarm);
            newItem.PropertyChanged += NewItemOnPropertyChanged;
            Alarms.Add(newItem);
        }
    }

    private void NewItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AlarmViewModel.IsActive) && sender is AlarmViewModel item)
        {
            App.SyncAlarms();
        }
    }

    public ICommand NewAlarmCommand
    {
        get
        {
            return new Command(() =>
            {
                try
                {
                    MakeTrainingAlarmPageViewModel vm = new MakeTrainingAlarmPageViewModel();
                    MakeTrainingAlarmPage page = new MakeTrainingAlarmPage() { BindingContext = vm };
                    vm.Alarm = new AlarmViewModel();
                    Navigation.PushModalAsync(page);
                }
                catch (Exception e)
                {

                }
            });
        }
    }

    public ICommand ItemTappedCommand
    {
        get
        {
            return new Command((obj) =>
            {
                MakeTrainingAlarmPage page = new MakeTrainingAlarmPage();
                MakeTrainingAlarmPageViewModel vm = new MakeTrainingAlarmPageViewModel();

                var alarm = obj as AlarmViewModel;
                vm.Alarm = alarm;
                vm.Alarm.TrainingId = alarm.AlarmItem.TrainingId;
                vm.SelectedTrainingItem = new TrainingViewModel(App.Database.GetTrainingItem(alarm.AlarmItem.TrainingId));
                page.BindingContext = vm;//SelectedTrainingItem null
                Navigation.PushModalAsync(page, true);
            });
        }
    }
}