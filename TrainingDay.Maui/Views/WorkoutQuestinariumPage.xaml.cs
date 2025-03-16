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
        AdMob.AdUnitId = DeviceInfo.Platform == DevicePlatform.Android ? ConstantKeys.WorkoutAndroidAds : ConstantKeys.WorkoutiOSAds;
    }

    protected override async void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is not null)
        {
            var service = Handler.MauiContext.Services.GetRequiredService<ChatGptService>();
            viewModel = new WorkoutQuestinariumPageViewModel(service);
            BindingContext = viewModel;
            await viewModel.LoadSteps();
        }
    }
}
