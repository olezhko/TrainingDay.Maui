using System.Text;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class DaysOfWeekView : ContentView
{
    StringBuilder _sb;

    private static string[] days = new[]
    {
            AppResources.DayTextMonday, AppResources.DayTextThusday, AppResources.DayTextWensdey,
            AppResources.DayTextThursday, AppResources.DayTextFriday,   AppResources.DayTextSaturday, AppResources.DayTextSunday
        };
    public DaysOfWeekView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext == null) return;
        var alarm = ((AlarmViewModel)BindingContext);
        IsEnabled = alarm.IsActive;

        var daysOfWeek = alarm.Days;
        _sb = new StringBuilder();

        bool isEveryDay = alarm.Days.AllDays.All(X => X == true);

        if (isEveryDay)
        {
            DaysLabel.Text = AppResources.DayTextEveryDay;
            return;
        }

        for (int i = 0; i < daysOfWeek.AllDays.Length; i++)
        {
            var hasDay = daysOfWeek.AllDays[i];
            if (hasDay)
            {
                if (i > 0 && !string.IsNullOrWhiteSpace(_sb.ToString()))
                {
                    _sb.Append(", ");
                }
                _sb.Append(days[i]);
            }
        }

        DaysLabel.Text = _sb.ToString();
    }
}