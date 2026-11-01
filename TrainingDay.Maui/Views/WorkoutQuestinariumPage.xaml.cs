using System.ComponentModel;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class WorkoutQuestinariumPage : ContentPage
{
    WorkoutQuestinariumPageViewModel? viewModel;

    public WorkoutQuestinariumPage()
	{
		InitializeComponent();
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null && viewModel is null)
        {
            var workoutQuestinariumService = Handler.MauiContext!.Services.GetRequiredService<IWorkoutQuestinariumService>();
            viewModel = new WorkoutQuestinariumPageViewModel(workoutQuestinariumService);
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            BindingContext = viewModel;
            await viewModel.LoadSteps();
        }
    }

    /// <summary>Android hardware back goes to the previous step instead of leaving the wizard.</summary>
    protected override bool OnBackButtonPressed()
    {
        viewModel?.BackOrCancelCommand.Execute(null);
        return true;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkoutQuestinariumPageViewModel.CurrentStep))
        {
            return;
        }

        StepHeader.Opacity = 0;
        VariantsList.Opacity = 0;
        await Task.WhenAll(StepHeader.FadeToAsync(1, 200, Easing.CubicOut), VariantsList.FadeToAsync(1, 250, Easing.CubicOut));
    }
}
