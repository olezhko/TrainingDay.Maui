using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class MakeTrainingAlarmPage : ContentPage
{
	public MakeTrainingAlarmPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as MakeTrainingAlarmPageViewModel;

        if (vm.Alarm != null)
        {
            DaysOfWeekSelection.Days = vm.Alarm.Days;

            if (vm.Alarm.AlarmItem.Id == 0)
            {
                Titlelabel.Text = AppResources.CreateNewString;
                RemoveToolbarItem.IsVisible = false;
            }
            else
            {
                Titlelabel.Text = AppResources.Editing;
            }

            if (vm.TrainingItems != null && vm.Alarm.TrainingId != 0)
            {
                TrainingsPicker.SelectedIndex = vm.TrainingItems.IndexOf(vm.TrainingItems.First(a => a.Id == vm.Alarm.TrainingId));
            }
        }
    }
}