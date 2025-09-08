using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Views;

[QueryProperty(nameof(Context), "Context")]
public partial class BlogItemPage : ContentPage
{
	public BlogItemPage()
	{
		InitializeComponent();
	}

    public BlogViewModel Context
    {
        get => BindingContext as BlogViewModel;
        set => BindingContext = value;
    }
}