using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class WorkoutQuestinariumPage : ContentPage
{
	public WorkoutQuestinariumPage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var viewModel = BindingContext as WorkoutQuestinariumPageViewModel;
        await viewModel.LoadSteps();
    }
}
