using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.ViewModels.Pages;
using static TrainingDay.Maui.Models.Messages.ExerciseChangedMessage;

namespace TrainingDay.Maui.Views;

public partial class ExerciseListPage : ContentPage
{
    private bool newExerciseAdded;
    private ExerciseListPageViewModel _viewModel;

    public ExerciseListPage()
    {
        InitializeComponent();
        AdMob.AdUnitId = DeviceInfo.Platform == DevicePlatform.Android ? ConstantKeys.ExercisesAndroidAds : ConstantKeys.ExercisesiOSAds;
        _viewModel = new ExerciseListPageViewModel();
        BindingContext = _viewModel;
        NavigationPage.SetBackButtonTitle(this, AppResources.ExercisesString);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Items.Count == 0 || SearchBar.Text.IsNotNullOrEmpty() || _viewModel.Filter.CurrentMuscles.Count != 0)
        {
            _viewModel.UpdateItems();
        }

        if (newExerciseAdded)
        {
            newExerciseAdded = false;
            ExercisesListView.ScrollTo(_viewModel.Items.Count - 1);
        }

        ExercisesListView.SelectedItem = null;
        SubscribeMessages();
    }

    private void SubscribeMessages()
    {
        UnsubscribeMessages();
        WeakReferenceMessenger.Default.Register<ExerciseChangedMessage>(this, (r, args) =>
        {
            if (args.Action == ExerciseAction.Added)
            {
                newExerciseAdded = true;
                _viewModel.Items.Add(new ExerciseListItemViewModel(args.Sender));
                _viewModel.BaseItems.Add(new ExerciseListItemViewModel(args.Sender));
            }

            try
            {
                var delete = _viewModel.Items.First(item => item.Id == args.Sender.Id);
                var index = _viewModel.Items.IndexOf(delete);
                _viewModel.Items.Remove(delete);
                if (args.Action == ExerciseAction.Changed)
                {
                    _viewModel.Items.Insert(index, new ExerciseListItemViewModel(args.Sender));
                }
            }
            catch (Exception exception)
            {
                Crashes.TrackError(exception);
            }

            UnsubscribeMessages();
            Analytics.TrackEvent($"{GetType().Name}: ExerciseChangedMessage");
        });
    }

    private void UnsubscribeMessages()
    {
        WeakReferenceMessenger.Default.Unregister<ExerciseChangedMessage>(this);
    }

    private async void AddExercisesButton_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ExerciseItemPage));

    private void SearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.UpdateItems();
    }

    private async void ListView_OnItemTapped(object sender, TappedEventArgs e)
    {
        ExerciseListItemViewModel? selected = ((Frame)sender).BindingContext as ExerciseListItemViewModel;

        await Shell.Current.GoToAsync($"{nameof(ExerciseItemPage)}?{nameof(ExerciseViewModel.LoadId)}={selected.Id}");
    }
}