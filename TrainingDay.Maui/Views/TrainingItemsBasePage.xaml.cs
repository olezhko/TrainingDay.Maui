using Microsoft.AppCenter.Crashes;
using System.Collections.ObjectModel;
using TrainingDay.Common;
using TrainingDay.Maui.ViewModels.Pages;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.ViewModels;
using TrainingDay.Maui.Extensions;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;

namespace TrainingDay.Maui.Views;

public partial class TrainingItemsBasePage : ContentPage
{
    private TrainingItemsBasePageViewModel _vm;

    public TrainingItemsBasePage()
    {
        InitializeComponent();
        BindingContext = new TrainingItemsBasePageViewModel()
        {
            Navigation = Navigation,
        };

        AdMob.AdUnitId = DeviceInfo.Platform == DevicePlatform.Android ? ConstantKeys.WorkoutsAndroidAds : ConstantKeys.WorkoutsiOSAds;
        NavigationPage.SetBackButtonTitle(this, AppResources.TrainingsBaseString);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm = BindingContext as TrainingItemsBasePageViewModel;
        _vm.LoadItems();
        IsStartNotFinishedTraining();
        if (!_vm.IsGrouped)
        {
            ToolTipTapHoldTraining.Hide();
        }
    }

    private async void IsStartNotFinishedTraining()
    {
        var filename = Path.Combine(FileSystem.CacheDirectory, ConstantKeys.NotFinishedTrainingName);

        if (Settings.IsTrainingNotFinished && File.Exists(filename))
        {
            Settings.IsTrainingNotFinished = false;

            var trainingSerialize = TrainingSerialize.LoadFromFile(filename);
            if (trainingSerialize != null)
            {
                var result = await MessageManager.DisplayAlert(AppResources.ContinueLastTrainingQuestion, trainingSerialize.Title, AppResources.YesString, AppResources.NoString);
                if (result)
                {
                    TrainingViewModel training = new TrainingViewModel();
                    training.Title = trainingSerialize.Title;
                    training.Id = trainingSerialize.Id;
                    foreach (var trainingExerciseSerialize in trainingSerialize.Items)
                    {
                        try
                        {
                            var item = new TrainingExerciseViewModel()
                            {
                                TrainingExerciseId = trainingExerciseSerialize.TrainingExerciseId,
                                ExerciseId = trainingExerciseSerialize.ExerciseId,
                                TrainingId = trainingExerciseSerialize.TrainingId,
                                IsNotFinished = trainingExerciseSerialize.IsNotFinished,
                                Muscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.ConvertFromStringToList(trainingExerciseSerialize.Muscles)),
                                OrderNumber = trainingExerciseSerialize.OrderNumber,
                                ExerciseItemName = trainingExerciseSerialize.ExerciseItemName,
                                SuperSetId = trainingExerciseSerialize.SuperSetId,
                                SuperSetNum = trainingExerciseSerialize.SuperSetNum,
                                Tags = ExerciseTools.ConvertFromIntToTagList(trainingExerciseSerialize.TagsValue),
                                Description = DescriptionViewModel.ConvertFromJson(trainingExerciseSerialize.Description),
                                CodeNum = trainingExerciseSerialize.CodeNum,
                            };

                            ExerciseManager.ConvertJsonBack(item, trainingExerciseSerialize.WeightAndRepsString);
                            training.AddExercise(item);
                        }
                        catch (Exception e)
                        {
                            Crashes.TrackError(e);
                        }
                    }

                    await Navigation.PushAsync(new TrainingImplementPage()
                    {
                        TrainingItem = training,
                        Title = training.Title,
                        StartTime = TimeSpan.Parse(Settings.IsTrainingNotFinishedTime),
                    });
                }
            }
        }
    }

    private async void ShowHistory_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HistoryTrainingPage));
    }

    private async void ShowAlarms_Click(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TrainingAlarmListPage));
    }
}