using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class DaysOfWeekSelection : ContentView
{
    public DaysOfWeekSelection()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Days))
        {
            Monday.IsSelected = Days.Monday;
            Thursday.IsSelected = Days.Thursday;
            Tuesday.IsSelected = Days.Tuesday;
            Wednesday.IsSelected = Days.Wednesday;
            Friday.IsSelected = Days.Friday;
            Saturday.IsSelected = Days.Saturday;
            Sunday.IsSelected = Days.Sunday;
        }
    }

    public static readonly BindableProperty DaysProperty =
        BindableProperty.Create("Days", typeof(DaysOfWeek), typeof(DaysOfWeekSelection), new DaysOfWeek());

    public DaysOfWeek Days
    {
        get { return (DaysOfWeek)GetValue(DaysProperty); }
        set { SetValue(DaysProperty, value); }
    }

    private void Monday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Monday = !days.Monday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Tuesday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Tuesday = !days.Tuesday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Wednesday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Wednesday = !days.Wednesday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Thursday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Thursday = !days.Thursday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Friday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Friday = !days.Friday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Saturday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Saturday = !days.Saturday;
        Days = new DaysOfWeek(days.AllDays);
    }

    private void Sunday_OnClicked(object sender, EventArgs e)
    {
        var days = Days;
        days.Sunday = !days.Sunday;
        Days = new DaysOfWeek(days.AllDays);
    }
}