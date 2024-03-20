using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class StatisticsPage : ContentPage
{
    private StatisticsViewModel viewModel;

    public StatisticsPage()
    {
        InitializeComponent();
        viewModel = BindingContext as StatisticsViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.LoadData();
    }
}