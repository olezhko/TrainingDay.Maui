using CommunityToolkit.Maui.Alerts;
using Microsoft.AppCenter.Crashes;
using System.Collections.ObjectModel;
using System.Text;
using TrainingDay.Common;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;
using LanguageResource = TrainingDay.Maui.Resources.Strings.AppResources;

namespace TrainingDay.Maui.Views;

public partial class ExerciseItemPage : ContentPage
{
    public event EventHandler<ExerciseChangedEventArgs> ExerciseChanged;

    public ExerciseItemPage()
    {
        InitializeComponent();
        FillMuscleLookup();
        ExerciseByRepsCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.RepsString}";
        ExerciseByDistanceCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.DistanceString}";
        ExerciseByRepsAndWeightCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.RepsAndWeightString}";
        ExerciseByTimeCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.TimeString}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ExerciseViewModel item = BindingContext as ExerciseViewModel;

        if (item.Id == 0)
        {
            TitleLabel.Text = LanguageResource.CreateNewString;
            DeleteExerciseToolbarItem.IsVisible = false;
        }
        else
        {
            TitleLabel.Text = item.ExerciseItemName;
            NameLabel.IsVisible = false;
        }
    }

    private async void Save_clicked(object sender, EventArgs e)
    {
        try
        {
            ExerciseViewModel ex = BindingContext as ExerciseViewModel;
            ex.Tags.Clear();
            if (ExerciseByDistanceCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByDistance);

            if (ExerciseByRepsAndWeightCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByRepsAndWeight);

            if (ExerciseByTimeCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByTime);

            if (ExerciseByRepsCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByReps);

            if (ex.ExerciseItemName.IsNotNullOrEmpty())
            {
                var exercise = ex.GetExercise();
                var action = ex.Id == 0 ? ExerciseChangedEventArgs.ExerciseAction.Added : ExerciseChangedEventArgs.ExerciseAction.Changed;
                var id = App.Database.SaveExerciseItem(exercise);
                exercise.Id = id;

                ExerciseChanged?.Invoke(this, new ExerciseChangedEventArgs(exercise, action));

                await Toast.Make(LanguageResource.SavedString).Show();
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }

    private async void Remove_clicked(object sender, EventArgs e)
    {
        ExerciseViewModel ex = BindingContext as ExerciseViewModel;
        if (ex.Id != 0)
        {
            var trainingIdsWithThisExercise = App.Database.GetTrainingExerciseItems()
                .Where(comm => comm.ExerciseId == ex.Id)
                .Select(item => item.TrainingId)
                .Distinct();

            var trainings = App.Database.GetTrainingItems().Where(item => trainingIdsWithThisExercise.Contains(item.Id)).Select(training => training.Title);
            StringBuilder questionBuilder = new StringBuilder();
            if (trainings.Count() != 0)
            {
                questionBuilder.Append("\n");
                var trainingNames = string.Join(", ", trainings);
                questionBuilder.Append(LanguageResource.ExerciseInTrainings + "\n" + trainingNames);
            }

            var result = await MessageManager.DisplayAlert(LanguageResource.DeleteExercise, questionBuilder.ToString(), LanguageResource.OkString, LanguageResource.CancelString);
            if (result)
            {
                App.Database.DeleteExerciseItem(ex.Id);
                App.Database.DeleteTrainingExerciseItemByExerciseId(ex.Id);
                await Toast.Make(LanguageResource.DeletedString).Show();
                ExerciseChanged?.Invoke(this, new ExerciseChangedEventArgs(ex.GetExercise(), ExerciseChangedEventArgs.ExerciseAction.Removed));

                await Navigation.PopAsync();
            }
        }
    }

    private void ShowMusclesLookup_Click(object sender, EventArgs e)
    {
        AcceptMuscleLookup();
        MuscleSelectorView.IsVisible = true;
    }

    private void FillMuscleLookup()
    {
        List<MuscleLookupItem> lookupItems = new List<MuscleLookupItem>();
        foreach (MusclesEnum value in Enum.GetValues(typeof(MusclesEnum)))
        {
            var muscle = new MuscleViewModel(value);
            lookupItems.Add(new MuscleLookupItem()
            {
                Muscle = muscle,
            });
        }

        MuscleSelectorList.ItemsSource = lookupItems;
    }

    private void AcceptMuscleLookup()
    {
        ExerciseViewModel item = BindingContext as ExerciseViewModel;

        foreach (MuscleLookupItem muscle in MuscleSelectorList.ItemsSource)
        {
            muscle.IsChecked = item.Muscles.Any(model => model.Id == muscle.Muscle.Id);
        }
    }

    private void MuscleSelectorViewCancel_OnClicked(object sender, EventArgs e)
    {
        MuscleSelectorView.IsVisible = false;
    }

    private void MuscleSelectorViewApprove_OnClicked(object sender, EventArgs e)
    {
        MuscleSelectorView.IsVisible = false;
        var selected = (MuscleSelectorList.ItemsSource as List<MuscleLookupItem>)
            .Where(item => item.IsChecked).Select(item => item.Muscle);
        ExerciseViewModel exercise = BindingContext as ExerciseViewModel;
        exercise.Muscles = new ObservableCollection<MuscleViewModel>(selected);
    }
}