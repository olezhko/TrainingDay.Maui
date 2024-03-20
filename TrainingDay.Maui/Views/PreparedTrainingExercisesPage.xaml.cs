using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class PreparedTrainingExercisesPage : ContentPage
{
	public PreparedTrainingExercisesPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var vm = BindingContext as TrainingExercisesPageViewModel;
        vm.StartSelectExercises();
        MessageManager.DisplayAlert(AppResources.AdviceString, AppResources.MarkTheRequiredExercisesAndPressFormat.Fill(AppResources.SelectString), AppResources.OkString);
        ItemsListView.SelectedItem = null;
    }
}