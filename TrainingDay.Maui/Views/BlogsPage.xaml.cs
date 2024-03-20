using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class BlogsPage : ContentPage
{
	public BlogsPage()
	{
		InitializeComponent();
        BindingContext = new BlogsPageViewModel() { Navigation = Navigation };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as BlogsPageViewModel;
        vm.LoadItems();
    }
}