using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Text;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;
using static TrainingDay.Maui.Models.Messages.ExerciseChangedMessage;
using LanguageResource = TrainingDay.Maui.Resources.Strings.AppResources;

namespace TrainingDay.Maui.Views;

public partial class ExerciseItemPage : ContentPage
{
    public ExerciseItemPage()
    {
        InitializeComponent();
        FillMuscleLookup();
        ExerciseByRepsCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.RepsString}";
        ExerciseByRepsAndWeightCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.RepsAndWeightString}";
        ExerciseByTimeCheckBox.Content = $"{LanguageResource.ExerciseForTypeString}{LanguageResource.TimeString}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ExerciseViewModel item = BindingContext as ExerciseViewModel;

        if (item.LoadId == 0)
        {
            TitleLabel.Text = LanguageResource.CreateNewString;
            DeleteExerciseToolbarItem.IsVisible = false;
        }
        else
        {
            TitleLabel.Text = item.Name;
            NameLabel.IsVisible = false;
        }
    }

    private async void Save_clicked(object sender, EventArgs e)
    {
        try
        {
            ExerciseViewModel ex = BindingContext as ExerciseViewModel;
            ex.Tags.Clear();

            if (ExerciseByRepsAndWeightCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByRepsAndWeight);

            if (ExerciseByTimeCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByTime);

            if (ExerciseByRepsCheckBox.IsChecked)
                ex.Tags.Add(ExerciseTags.ExerciseByReps);

            if (ex.Name.IsNotNullOrEmpty())
            {
                var exercise = ex.GetExercise();
                exercise.Id = ex.LoadId;
                var action = ex.LoadId == 0 ? ExerciseAction.Added : ExerciseAction.Changed;
                var id = App.Database.SaveExerciseItem(exercise);
                exercise.Id = id;

                if (ex.ImageData != null && ex.ImageData.Any())
                {
                    App.Database.SaveImage(new Models.Database.ImageDto()
                    {
                        Data = ex.ImageData,
                        Url = $"new_{exercise.Id}"
                    });
                }

                WeakReferenceMessenger.Default.Send(new ExerciseChangedMessage(exercise, action));

                await Toast.Make(LanguageResource.SavedString).Show();
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async void Remove_clicked(object sender, EventArgs e)
    {
        ExerciseViewModel ex = BindingContext as ExerciseViewModel;
        if (ex.LoadId != 0)
        {
            var trainingIdsWithThisExercise = App.Database.GetTrainingExerciseItems()
                .Where(comm => comm.ExerciseId == ex.LoadId)
                .Select(item => item.TrainingId)
                .Distinct();

            var trainings = App.Database.GetTrainingItems().Where(item => trainingIdsWithThisExercise.Contains(item.Id)).Select(training => training.Title);
            StringBuilder questionBuilder = new ();
            if (trainings.Any())
            {
                questionBuilder.Append("\n");
                var trainingNames = string.Join(", ", trainings);
                questionBuilder.Append(LanguageResource.ExerciseInTrainings + "\n" + trainingNames);
            }

            var result = await MessageManager.DisplayAlert(LanguageResource.DeleteExercise, questionBuilder.ToString(), LanguageResource.OkString, LanguageResource.CancelString);
            if (result)
            {
                App.Database.DeleteExerciseItem(ex.LoadId);
                App.Database.DeleteTrainingExerciseItemByExerciseId(ex.LoadId);
                await Toast.Make(LanguageResource.DeletedString).Show();

                WeakReferenceMessenger.Default.Send(new ExerciseChangedMessage(ex.GetExercise(), ExerciseAction.Removed));

                await Shell.Current.GoToAsync("..");
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
        var lookupItems = new List<MuscleCheckItem>();
        foreach (MusclesEnum value in Enum.GetValues<MusclesEnum>())
        {
            var muscle = new MuscleViewModel(value);
            lookupItems.Add(new MuscleCheckItem()
            {
                Muscle = value,
                Text = muscle.Name,
            });
        }

        MuscleSelectorList.ItemsSource = lookupItems;
    }

    private void AcceptMuscleLookup()
    {
        ExerciseViewModel item = BindingContext as ExerciseViewModel;

        foreach (MuscleCheckItem muscle in MuscleSelectorList.ItemsSource)
        {
            muscle.IsChecked = item.Muscles.Any(model => model.Id == (int)muscle.Muscle);
        }
    }

    private void MuscleSelectorViewCancel_OnClicked(object sender, EventArgs e)
    {
        MuscleSelectorView.IsVisible = false;
    }

    private void MuscleSelectorViewApprove_OnClicked(object sender, EventArgs e)
    {
        MuscleSelectorView.IsVisible = false;
        var selected = (MuscleSelectorList.ItemsSource as List<MuscleCheckItem>)
            .Where(item => item.IsChecked)
            .Select(item => new MuscleViewModel(item.Muscle));

        ExerciseViewModel exercise = BindingContext as ExerciseViewModel;
        exercise.Muscles = new ObservableCollection<MuscleViewModel>(selected);
    }

    private void RatingView_RatingChanged(object sender, CommunityToolkit.Maui.Core.RatingChangedEventArgs e)
    {
        SetDifficultyLevel((int)Math.Round(e.Rating));
    }

    private void SetDifficultyLevel(int newValue)
    {
        ExerciseViewModel exercise = BindingContext as ExerciseViewModel;
        exercise.DifficultType = (DifficultTypes)newValue;
        DifficultyLevelLabel.Text = $"{AppResources.Difficulty}: {GetDifficultyLevelLabel(newValue)}";
    }

    private string GetDifficultyLevelLabel(object value)
    {
        if (value is int difficultyLevel)
        {
            return difficultyLevel switch
            {
                1 => AppResources.DifficultyEasy,
                2 => AppResources.DifficultyMedium,
                3 => AppResources.DifficultyHard,
                _ => AppResources.DifficultyAny
            };
        }
        return AppResources.DifficultyAny;
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var action = await Shell.Current.DisplayActionSheetAsync(
           "Add Image",
           LanguageResource.CancelString,
           null,
           "Gallery",
           "Photo");

        FileResult? result = null;
        if (action == AppResources.CancelString)
            return;

        var isGranted = await RequestPermissionsAsync();
        if (!isGranted)
        {
            return;
        }
        if (action == "Gallery")
        {
            result = await FilePicker.PickAsync(new PickOptions()
            {
                FileTypes = FilePickerFileType.Images,
                PickerTitle = "Please select an image"
            });
        }

        if (action == "Photo")
        {
            result = await MediaPicker.CapturePhotoAsync();
        }

        if (result == null)
            return;

        await using var stream = await result.OpenReadAsync();
        using MemoryStream ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var imageData = ms.ToArray();
        imageData = ResizeImageService.ResizeImage(imageData, 600, 600, false);
        ExerciseViewModel exercise = BindingContext as ExerciseViewModel;
        exercise.ImageData = imageData;

        ExerciseImage.Behaviors.Clear();
        ExerciseImage.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
    }

    private async Task<bool> RequestPermissionsAsync()
    {
        PermissionStatus statusCamera = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (statusCamera != PermissionStatus.Granted)
        {
            statusCamera = await Permissions.RequestAsync<Permissions.Camera>();
        }

        PermissionStatus statusStorageRead = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (statusStorageRead != PermissionStatus.Granted)
        {
            statusStorageRead = await Permissions.RequestAsync<Permissions.StorageRead>();
        }

        return statusCamera == PermissionStatus.Granted && statusStorageRead == PermissionStatus.Granted;
    }
}