using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class BlogsPage : ContentPage
{
	public BlogsPage(IDataService service)
	{
		InitializeComponent();
        BindingContext = new BlogsPageViewModel(service);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as BlogsPageViewModel;
        vm.LoadItems();
    }
}