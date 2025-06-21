using System.ComponentModel;
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
            var service = Handler.MauiContext.Services.GetRequiredService<DataService>();
            viewModel = new WorkoutQuestinariumPageViewModel(service);
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            BindingContext = viewModel;
            await viewModel.LoadSteps();
        }
    }

    CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    private async void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkoutQuestinariumPageViewModel.IsBusy))
            return;

        if (viewModel.IsBusy)
            await ActivityBorder.StartInfiniteRotate(CancellationTokenSource.Token);
        else
            CancellationTokenSource.Cancel();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.PropertyChanged -= ViewModelPropertyChanged;
    }
}
