using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class HistoryTrainingPage : ContentPage
{
    public HistoryTrainingPage()
    {
        InitializeComponent();
        NavigationPage.SetBackButtonTitle(this, AppResources.HistoryTrainings);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as HistoryTrainingPageViewModel;
        vm.Navigation = Navigation;
        vm.LoadItems();
    }
}