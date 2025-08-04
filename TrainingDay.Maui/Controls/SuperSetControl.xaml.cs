using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class SuperSetControl : ContentView
{

    public SuperSetControl()
	{
		InitializeComponent();
    }

    #region Properties

    public static readonly BindableProperty SuperSetItemsProperty =
        BindableProperty.Create("SuperSetItems", typeof(ObservableCollection<TrainingExerciseViewModel>), typeof(SuperSetControl), null, propertyChanged: SourcePropertyChanged);

    public ObservableCollection<TrainingExerciseViewModel> SuperSetItems
    {
        get { return (ObservableCollection<TrainingExerciseViewModel>)GetValue(SuperSetItemsProperty); }
        set { SetValue(SuperSetItemsProperty, value); }
    }

    public static readonly BindableProperty CurrentItemProperty =
        BindableProperty.Create("CurrentItem", typeof(TrainingExerciseViewModel), typeof(SuperSetControl), propertyChanged: CurrentItemPropertyChanged);

    public TrainingExerciseViewModel CurrentItem
    {
        get { return (TrainingExerciseViewModel)GetValue(CurrentItemProperty); }
        set { SetValue(CurrentItemProperty, value); }
    }

    public static readonly BindableProperty IsNextAvailableProperty =
        BindableProperty.Create("IsNextAvailable", typeof(bool), typeof(SuperSetControl), false);

    public static readonly BindableProperty IsPrevAvailableProperty =
        BindableProperty.Create("IsPrevAvailable", typeof(bool), typeof(SuperSetControl), false);

    public bool IsNextAvailable
    {
        get { return (bool)GetValue(IsNextAvailableProperty); }
        set { SetValue(IsNextAvailableProperty, value); }
    }

    public bool IsPrevAvailable
    {
        get { return (bool)GetValue(IsPrevAvailableProperty); }
        set { SetValue(IsPrevAvailableProperty, value); }
    }

    public ICommand PreviousCommand => new Command(MovePrev);
    public ICommand NextCommand => new Command(MoveNext);
    public ICommand DeleteRequestCommand => new Command<WeightAndRepsViewModel>(DeleteRequestWeightAndReps);
    public ICommand AddRequestCommand => new Command(AddRequestWeightAndReps);
    #endregion

    private int currentIndex = 0;
    private void MoveNext()
    {
        var index = currentIndex;
        Move(index + 1);
    }

    private void MovePrev()
    {
        var index = currentIndex;
        Move(index - 1);
    }

    private void Move(int index)
    {
        currentIndex = index;
        CurrentItem = SuperSetItems[index];
        UpdateMoveProps(index);
    }

    private void UpdateMoveProps(int index)
    {
        IsNextAvailable = true;
        IsPrevAvailable = true;

        if (index <= 0)
            IsPrevAvailable = false;

        if (index >= SuperSetItems.Count - 1)
            IsNextAvailable = false;
    }

    private static void SourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = bindable as SuperSetControl;
        var items = (ObservableCollection<TrainingExerciseViewModel>)newValue;
        control.SourceChanged(items);
    }

    private void SourceChanged(ObservableCollection<TrainingExerciseViewModel> newValue)
    {
        Move(0);
    }

    private static void CurrentItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = bindable as SuperSetControl;
        var index = control.SuperSetItems.IndexOf(control.CurrentItem);
        control.currentIndex = index;
        control.UpdateMoveProps(index);
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
            item.IsTimeCalculating = false;
            return;
        }

        item.StartCalculateDateTime = DateTime.Now;
        item.IsTimeCalculating = true;
    }
}