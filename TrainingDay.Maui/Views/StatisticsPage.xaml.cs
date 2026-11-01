using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class StatisticsPage : ContentPage
{
    private readonly StatisticsViewModel viewModel;

    public StatisticsPage()
    {
        InitializeComponent();
        viewModel = (StatisticsViewModel)BindingContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.LoadDataAsync();
    }
}
