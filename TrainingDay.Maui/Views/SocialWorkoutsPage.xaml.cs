using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class SocialWorkoutsPage : ContentPage
{
    public SocialWorkoutsPage(ISocialWorkoutsService service)
    {
        InitializeComponent();
        BindingContext = new SocialWorkoutsPageViewModel(service);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as SocialWorkoutsPageViewModel;
        vm.OnAppearing();
    }
}
