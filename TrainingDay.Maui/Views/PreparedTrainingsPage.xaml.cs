using System.ComponentModel;
using System.Runtime.CompilerServices;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class PreparedTrainingsPage : ContentPage
{
    public PreparedTrainingsPage()
    {
        InitializeComponent();
        viewModel = BindingContext as PreparedTrainingsPageViewModel;
        NavigationPage.SetBackButtonTitle(this, AppResources.AddTrainingString);
    }

    private PreparedTrainingsPageViewModel viewModel;
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.FillTrainings();
    }
}