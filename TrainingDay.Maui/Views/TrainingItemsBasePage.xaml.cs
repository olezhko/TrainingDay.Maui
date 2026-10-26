using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class TrainingItemsBasePage : ContentPage
{
    private readonly TrainingItemsBasePageViewModel _vm;

    public TrainingItemsBasePage()
    {
        InitializeComponent();
        BindingContext = _vm = new TrainingItemsBasePageViewModel();

        NavigationPage.SetBackButtonTitle(this, AppResources.TrainingsBaseString);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadItemsAsync(true);

        if (!_vm.ItemsGrouped.Any())
        {
            ToolTipTapHoldTraining.Hide();
        }
    }
}
