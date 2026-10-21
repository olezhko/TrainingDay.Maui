using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace TrainingDay.Maui.ViewModels;

public class ImplementTrainingExerciseViewModel : TrainingExerciseViewModel
{
    private bool notFinished = true;
    private bool skipped;
    private bool isTimeCalculating;
    private string superSetLetter = string.Empty;
    private ObservableCollection<WeightAndRepsViewModel> trackedSets;

    public ImplementTrainingExerciseViewModel()
    {
        PropertyChanged += OnOwnPropertyChanged;
        TrackSets(WeightAndRepsItems);
    }

    /// <summary>
    /// Raised when one of the exercise's sets is marked as finished.
    /// </summary>
    public event EventHandler<WeightAndRepsViewModel> SetFinished;

    public bool IsNotFinished
    {
        get => notFinished;
        set
        {
            notFinished = value;
            OnPropertyChanged();
        }
    }

    public bool IsSkipped
    {
        get => skipped;
        set
        {
            skipped = value;
            OnPropertyChanged();
        }
    }

    public DateTime StartCalculateDateTime { get; set; }

    public bool IsTimeCalculating
    {
        get => isTimeCalculating;
        set
        {
            isTimeCalculating = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Position label inside a superset (A, B, C...).
    /// </summary>
    public string SuperSetLetter
    {
        get => superSetLetter;
        set
        {
            superSetLetter = value;
            OnPropertyChanged();
        }
    }

    public int SetsCount => WeightAndRepsItems?.Count ?? 0;

    public int FinishedSetsCount => WeightAndRepsItems?.Count(item => item.IsFinished) ?? 0;

    public bool HasSets => SetsCount > 0;

    public bool HasUnfinishedSets => FinishedSetsCount < SetsCount;

    public bool AreAllSetsFinished => HasSets && !HasUnfinishedSets;

    public string SetsProgress => $"{FinishedSetsCount}/{SetsCount}";

    /// <summary>
    /// 1-based number of the first unfinished set, or 0 when every set is finished.
    /// </summary>
    public int FirstUnfinishedSetNumber
    {
        get
        {
            if (WeightAndRepsItems == null)
                return 0;

            for (int i = 0; i < WeightAndRepsItems.Count; i++)
            {
                if (!WeightAndRepsItems[i].IsFinished)
                    return i + 1;
            }

            return 0;
        }
    }

    public ICommand AddSetCommand => new Command(() =>
    {
        var lastItem = WeightAndRepsItems.LastOrDefault() ?? new WeightAndRepsViewModel(0,15);
        WeightAndRepsItems.Add(new WeightAndRepsViewModel(lastItem.Weight, lastItem.Repetitions));
        IsNotFinished = true;
    });

    public ICommand RemoveSetCommand => new Command(() =>
    {
        if (WeightAndRepsItems.Count == 0)
            return;

        WeightAndRepsItems.RemoveAt(WeightAndRepsItems.Count - 1);
    });

    public ICommand StartCalculatingTimeCommand => new Command(StartCalculateTime);
    private void StartCalculateTime()
    {
        if (IsTimeCalculating)
        {
            IsTimeCalculating = false;
            return;
        }

        StartCalculateDateTime = DateTime.Now;
        IsTimeCalculating = true;
    }

    private void OnOwnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WeightAndRepsItems))
        {
            TrackSets(WeightAndRepsItems);
            NotifySetsProgressChanged();
        }
    }

    private void TrackSets(ObservableCollection<WeightAndRepsViewModel> sets)
    {
        if (trackedSets != null)
        {
            trackedSets.CollectionChanged -= OnSetsCollectionChanged;
            foreach (var set in trackedSets)
            {
                set.PropertyChanged -= OnSetPropertyChanged;
            }
        }

        trackedSets = sets;
        if (sets == null)
            return;

        sets.CollectionChanged += OnSetsCollectionChanged;
        foreach (var set in sets)
        {
            set.PropertyChanged += OnSetPropertyChanged;
        }
    }

    private void OnSetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            TrackSets(trackedSets);
        }
        else
        {
            if (e.OldItems != null)
            {
                foreach (WeightAndRepsViewModel set in e.OldItems)
                {
                    set.PropertyChanged -= OnSetPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (WeightAndRepsViewModel set in e.NewItems)
                {
                    set.PropertyChanged += OnSetPropertyChanged;
                }
            }
        }

        NotifySetsProgressChanged();
    }

    private void OnSetPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WeightAndRepsViewModel.IsFinished))
            return;

        NotifySetsProgressChanged();

        var set = (WeightAndRepsViewModel)sender;
        if (set.IsFinished)
        {
            SetFinished?.Invoke(this, set);
        }
    }

    private void NotifySetsProgressChanged()
    {
        OnPropertyChanged(nameof(SetsCount));
        OnPropertyChanged(nameof(FinishedSetsCount));
        OnPropertyChanged(nameof(HasSets));
        OnPropertyChanged(nameof(HasUnfinishedSets));
        OnPropertyChanged(nameof(AreAllSetsFinished));
        OnPropertyChanged(nameof(SetsProgress));
        OnPropertyChanged(nameof(FirstUnfinishedSetNumber));
    }
}
