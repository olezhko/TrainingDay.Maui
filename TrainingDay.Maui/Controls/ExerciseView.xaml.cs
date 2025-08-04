using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class ExerciseView : ContentView
{
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

    public ExerciseView()
    {
        InitializeComponent();
        BindingContextChanged += ExerciseView_BindingContextChanged;
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
            //VideoItems.Clear();
            //if (CurrentExercise != null)
            //{
            //    var items = await SiteService.GetVideosFromServer(CurrentExercise.ExerciseItemName);
            //    foreach (var item in items)
            //    {
            //        VideoItems.Add(new YoutubeVideoItem()
            //        {
            //            VideoAuthor = item.VideoAuthor,
            //            VideoTitle = item.VideoTitle,
            //            VideoUrl = item.VideoUrl,
            //        });
            //    }
            //}

            //VideoCollectionView.ItemsSource = VideoItems;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}