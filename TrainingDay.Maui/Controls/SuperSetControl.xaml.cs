using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;
using static TrainingDay.Maui.Controls.ExerciseView;

namespace TrainingDay.Maui.Controls;

public partial class SuperSetControl : ContentView
{
    private Picker dataPicker;
    private PickerMode mode;

    public ICommand DeleteRequestCommand => new Command<WeightAndRepsViewModel>(DeleteRequestWeightAndReps);
    public ICommand AddRequestCommand => new Command(AddRequestWeightAndReps);

    public SuperSetControl()
	{
		InitializeComponent();
        dataPicker = new Picker();
        dataPicker.ItemsSource = Enumerable.Range(0, 60).Select(min => min.ToString("D2")).ToList();
        dataPicker.SelectedIndexChanged += DataPickerOnSelectedIndexChanged;
        dataPicker.IsVisible = false;
        dataPicker.Unfocused += DataPicker_Unfocused;
        dataPicker.TitleColor = Colors.Orange;
        mainGrid.Children.Add(dataPicker);
    }

    public static readonly BindableProperty SuperSetItemsProperty =
        BindableProperty.Create("SuperSetItems", typeof(ObservableCollection<TrainingExerciseViewModel>), typeof(SuperSetControl), null, propertyChanged: SourcePropertyChanged);

    public ObservableCollection<TrainingExerciseViewModel> SuperSetItems
    {
        get { return (ObservableCollection<TrainingExerciseViewModel>)GetValue(SuperSetItemsProperty); }
        set { SetValue(SuperSetItemsProperty, value); }
    }

    public static readonly BindableProperty CurrentItemProperty =
        BindableProperty.Create("CurrentItem", typeof(TrainingExerciseViewModel), typeof(SuperSetControl), null);

    public TrainingExerciseViewModel CurrentItem
    {
        get { return (TrainingExerciseViewModel)GetValue(CurrentItemProperty); }
        set { SetValue(CurrentItemProperty, value); }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var context = (sender as Grid).BindingContext as TrainingExerciseViewModel;
        CurrentItem = context;
    }

    private static void SourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = bindable as SuperSetControl;
        var items = (ObservableCollection<TrainingExerciseViewModel>)newValue;
        control.SourceChanged(items);
    }

    private void SourceChanged(ObservableCollection<TrainingExerciseViewModel> newValue)
    {
        CurrentItem = newValue.First();
    }

    private void DeleteRequestWeightAndReps(WeightAndRepsViewModel sender)
    {
        var item = CurrentItem;
        item.WeightAndRepsItems.Remove(sender);
    }

    private void AddRequestWeightAndReps()
    {
        var item = CurrentItem;
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

    private void StartCalculateTime_Clicked(object sender, EventArgs e)
    {
        var item = CurrentItem;
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

        var item = CurrentItem;
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
}