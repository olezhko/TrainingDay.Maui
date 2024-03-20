using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class AlarmListCell : ContentView
{
    AlarmViewModel _alarm;
    public AlarmListCell()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext == null)
            return;

        _alarm = (AlarmViewModel)BindingContext;

        if (string.IsNullOrWhiteSpace(_alarm.Name))
        {
            NameLabel.IsVisible = false;
        }
        else
        {
            NameLabel.Text = _alarm.Name;
            NameLabel.IsVisible = true;
            TrainingNameLabel.Text = App.Database.GetTrainingItem(_alarm.AlarmItem.TrainingId).Title;
        }

        StartSpan.Text = _alarm.Time.ToString(@"hh\:mm");
    }

    private void ActiveSwitch_OnToggled(object sender, ToggledEventArgs e)
    {
        if (_alarm != null)
            App.Database.SaveAlarmItem(_alarm.AlarmItem);
    }
}