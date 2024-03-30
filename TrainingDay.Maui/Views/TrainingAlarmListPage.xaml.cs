using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingAlarmListPage : ContentPage
{
    private TrainingAlarmListPageViewModel vm;
    public TrainingAlarmListPage()
    {
        InitializeComponent();
        vm = BindingContext as TrainingAlarmListPageViewModel;
        vm.Navigation = Navigation;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        vm.LoadItems();
    }
}