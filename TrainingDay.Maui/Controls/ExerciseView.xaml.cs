using Microsoft.Maui.Layouts;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class ExerciseView : ContentView
{
    private Picker dataPicker;
    private PickerMode mode;

    public ICommand DeleteRequestCommand => new Command<WeightAndRepsViewModel>(DeleteRequestWeightAndReps);

    public ObservableCollection<YoutubeVideoItem> VideoItems { get; set; } = new ObservableCollection<YoutubeVideoItem>();

    public TrainingExerciseViewModel CurrentExercise
    {
        get
        {
            var ex = (TrainingExerciseViewModel)BindingContext;
            return ex;
        }
    }


    public event EventHandler<ImageSource> ImageTappedEvent;

    public static readonly BindableProperty IsNameVisibleProperty = BindableProperty.Create(nameof(IsNameVisible), typeof(bool), typeof(ExerciseView), true);

    public bool IsNameVisible
    {
        get => (bool)GetValue(IsNameVisibleProperty);
        set => SetValue(IsNameVisibleProperty, value);
    }

    public ExerciseView()
    {
        InitializeComponent();
        dataPicker = new Picker();
        dataPicker.ItemsSource = Enumerable.Range(0, 60).Select(min => min.ToString("D2")).ToList();
        dataPicker.SelectedIndexChanged += DataPickerOnSelectedIndexChanged;
        dataPicker.IsVisible = false;
        dataPicker.Unfocused += DataPicker_Unfocused;
        dataPicker.TitleColor = Colors.Orange;
        BindingContextChanged += ExerciseView_BindingContextChanged;
        MainGrid.Children.Add(dataPicker);
    }

    private void ExerciseView_BindingContextChanged(object sender, EventArgs e)
    {
        //LoadVideoItems();
    }

    private void ImageTapped(object sender, EventArgs e)
    {
        ImageTappedEvent?.Invoke(this, ImageControl.Source);
    }

    private void AddWeightAndRepsItem_Clicked(object sender, EventArgs e)
    {
        var item = (TrainingExerciseViewModel)BindingContext;
        if (item.WeightAndRepsItems.Count == 0)
        {
            item.WeightAndRepsItems.Add(new WeightAndRepsViewModel(0, 15));
        }
        else
        {
            var last = item.WeightAndRepsItems.Last();
            item.WeightAndRepsItems.Add(new WeightAndRepsViewModel(last.Weight, last.Repetitions));
        }
    }

    private void DeleteRequestWeightAndReps(WeightAndRepsViewModel sender)
    {
        var item = (TrainingExerciseViewModel)BindingContext;
        item.WeightAndRepsItems.Remove(sender);
    }

    private void StartCalculateTime_Clicked(object sender, EventArgs e)
    {
        var item = (TrainingExerciseViewModel)BindingContext;
        if (item.IsTimeCalculating)
        {
            item.IsTimeCalculating = false; // stop calculating time
            return;
        }

        item.StartCalculateDateTime = DateTime.Now;
        item.IsTimeCalculating = true;
    }

    private void DataPickerOnSelectedIndexChanged(object sender, EventArgs e)
    {
        if (!dataPicker.IsVisible)
        {
            return;
        }

        var item = (TrainingExerciseViewModel)BindingContext;
        switch (mode)
        {
            case PickerMode.Hour:
                item.TimeHours = dataPicker.SelectedIndex;
                break;
            case PickerMode.Minute:
                item.TimeMinutes = dataPicker.SelectedIndex;
                break;
            case PickerMode.Second:
                item.TimeSeconds = dataPicker.SelectedIndex;
                break;
        }
    }

    private void DataPicker_Unfocused(object sender, FocusEventArgs e)
    {
        dataPicker.IsVisible = false;
    }

    private void DatePicker(string title, PickerMode newMode)
    {
        dataPicker.Title = title;
        dataPicker.SelectedIndex = -1;
        dataPicker.IsVisible = true;
        dataPicker.Focus();
        mode = newMode;
    }

    private void HourGestureRecognizer_OnTapped(object sender, EventArgs e)
    {
        DatePicker(AppResources.Hours, PickerMode.Hour);
    }

    private void SecondGestureRecognizer_OnTapped(object sender, EventArgs e)
    {
        DatePicker(AppResources.Seconds, PickerMode.Second);
    }

    private void MinuteGestureRecognizer_OnTapped(object sender, EventArgs e)
    {
        DatePicker(AppResources.Minutes, PickerMode.Minute);
    }

    public enum PickerMode
    {
        None,
        Hour,
        Minute,
        Second,
    }

    private async void Description_Click(object sender, EventArgs e)
    {
        DesctiptionLabel.BackgroundColor = Colors.Green;
        //VideoLabel.BackgroundColor = Colors.DarkGray;

        VideoCollectionView.IsVisible = false;
        DescriptionGrid.IsVisible = true;
        VideoActivityIndicatorGrid.IsVisible = false;
    }

    private async void Video_Click(object sender, EventArgs e)
    {
        DesctiptionLabel.BackgroundColor = Colors.DarkGray;
        //VideoLabel.BackgroundColor = Colors.Green;
        DescriptionGrid.IsVisible = false;

        VideoActivityIndicatorGrid.IsVisible = true;
        VideoActivityIndicator.IsRunning = true;

        //await LoadVideoItems();

        VideoActivityIndicator.IsRunning = false;
        VideoCollectionView.IsVisible = true;
        VideoActivityIndicatorGrid.IsVisible = false;
    }

    public async Task LoadVideoItems()
    {
        try
        {
            VideoItems.Clear();
            if (CurrentExercise != null)
            {
                var items = await SiteService.GetVideosFromServer(CurrentExercise.ExerciseItemName);
                foreach (var item in items)
                {
                    VideoItems.Add(new YoutubeVideoItem()
                    {
                        VideoAuthor = item.VideoAuthor,
                        VideoTitle = item.VideoTitle,
                        VideoUrl = item.VideoUrl,
                    });
                }
            }

            VideoCollectionView.ItemsSource = VideoItems;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    
}