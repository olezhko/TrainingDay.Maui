using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingsPage : ContentPage
{
    private readonly TrainingsPageViewModel _vm;

    public TrainingsPage()
    {
        InitializeComponent();
        BindingContext = _vm = new TrainingsPageViewModel();

        NavigationPage.SetBackButtonTitle(this, AppResources.TrainingsBaseString);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadItemsAsync();

        if (!_vm.ItemsGrouped.Any())
        {
            ToolTipTapHoldTraining.Hide();
        }
    }
}
