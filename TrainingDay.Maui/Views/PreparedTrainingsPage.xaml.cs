using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(StartCustom), StartCustomQueryKey)]
public partial class PreparedTrainingsPage : ContentPage
{
    /// <summary>
    /// Query key to open the page and immediately start the "create custom training" flow.
    /// </summary>
    public const string StartCustomQueryKey = "StartCustom";

    public PreparedTrainingsPage()
    {
        InitializeComponent();
        viewModel = BindingContext as PreparedTrainingsPageViewModel;
        NavigationPage.SetBackButtonTitle(this, AppResources.AddTrainingString);
    }

    public bool StartCustom { get; set; }

    private PreparedTrainingsPageViewModel viewModel;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.FillTrainings();

        if (StartCustom)
        {
            StartCustom = false;
            viewModel.CreateNewTrainingCommand.Execute(null);
        }
    }
}
