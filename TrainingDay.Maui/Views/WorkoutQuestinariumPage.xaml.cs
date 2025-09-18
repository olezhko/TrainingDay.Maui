using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class WorkoutQuestinariumPage : ContentPage
{
    WorkoutQuestinariumPageViewModel viewModel;

    public WorkoutQuestinariumPage()
	{
		InitializeComponent();
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
        {
            var dataService = Handler.MauiContext.Services.GetRequiredService<IDataService>();
            var workoutService = Handler.MauiContext.Services.GetRequiredService<WorkoutService>();
            viewModel = new WorkoutQuestinariumPageViewModel(dataService, workoutService);
            BindingContext = viewModel;
            await viewModel.LoadSteps();
        }
    }
}
