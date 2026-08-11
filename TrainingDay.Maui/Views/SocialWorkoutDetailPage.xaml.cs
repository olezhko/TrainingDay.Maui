using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(Context), "Context")]
public partial class SocialWorkoutDetailPage : ContentPage
{
    private SocialWorkoutViewModel workout;
    private ISocialWorkoutsService socialWorkoutsService;

    public SocialWorkoutDetailPage()
    {
        InitializeComponent();
    }

    public SocialWorkoutViewModel Context
    {
        get => (BindingContext as SocialWorkoutDetailViewModel)?.Workout;
        set
        {
            workout = value;
            TryCreateViewModel();
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler != null)
        {
            socialWorkoutsService = Handler.MauiContext.Services.GetRequiredService<ISocialWorkoutsService>();
            TryCreateViewModel();
        }
    }

    private void TryCreateViewModel()
    {
        if (workout != null && socialWorkoutsService != null)
        {
            BindingContext = new SocialWorkoutDetailViewModel(workout, socialWorkoutsService);
        }
    }
}
