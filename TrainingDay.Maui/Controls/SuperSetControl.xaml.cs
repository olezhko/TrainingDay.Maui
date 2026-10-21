using System.Collections.ObjectModel;
using System.ComponentModel;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class SuperSetControl : ContentView
{
    private static readonly TimeSpan AutoAdvanceDelay = TimeSpan.FromMilliseconds(350);

    private ObservableCollection<ImplementTrainingExerciseViewModel> trackedItems;
    private string statusMessage = string.Empty;
    private string roundText = string.Empty;
    private IReadOnlyList<ImplementTrainingExerciseViewModel> itemsBefore = [];
    private IReadOnlyList<ImplementTrainingExerciseViewModel> itemsAfter = [];

    public SuperSetControl()
    {
        InitializeComponent();
        Loaded += (_, _) => TrackItems(SuperSetItems);
        Unloaded += (_, _) => TrackItems(null);
    }

    #region Properties

    public static readonly BindableProperty SuperSetItemsProperty =
        BindableProperty.Create("SuperSetItems", typeof(ObservableCollection<ImplementTrainingExerciseViewModel>), typeof(SuperSetControl), null, propertyChanged: SourcePropertyChanged);

    public ObservableCollection<ImplementTrainingExerciseViewModel> SuperSetItems
    {
        get { return (ObservableCollection<ImplementTrainingExerciseViewModel>)GetValue(SuperSetItemsProperty); }
        set { SetValue(SuperSetItemsProperty, value); }
    }

    public static readonly BindableProperty CurrentItemProperty =
        BindableProperty.Create("CurrentItem", typeof(ImplementTrainingExerciseViewModel), typeof(SuperSetControl), propertyChanged: CurrentItemPropertyChanged);

    public ImplementTrainingExerciseViewModel CurrentItem
    {
        get { return (ImplementTrainingExerciseViewModel)GetValue(CurrentItemProperty); }
        set { SetValue(CurrentItemProperty, value); }
    }

    public static readonly BindableProperty AutoAdvanceProperty =
        BindableProperty.Create("AutoAdvance", typeof(bool), typeof(SuperSetControl), true);

    /// <summary>
    /// When a set is finished, switch to the next superset exercise that still has unfinished sets.
    /// </summary>
    public bool AutoAdvance
    {
        get { return (bool)GetValue(AutoAdvanceProperty); }
        set { SetValue(AutoAdvanceProperty, value); }
    }

    public bool IsSuperSet => SuperSetItems?.Count > 1;

    public string HeaderText => string.Format(AppResources.SuperSetExercisesCount, SuperSetItems?.Count ?? 0);

    public string RoundText
    {
        get => roundText;
        private set
        {
            roundText = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<ImplementTrainingExerciseViewModel> ItemsBefore
    {
        get => itemsBefore;
        private set
        {
            itemsBefore = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<ImplementTrainingExerciseViewModel> ItemsAfter
    {
        get => itemsAfter;
        private set
        {
            itemsAfter = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            statusMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    #endregion

    private static void SourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (SuperSetControl)bindable;
        control.SourceChanged((ObservableCollection<ImplementTrainingExerciseViewModel>)newValue);
    }

    private void SourceChanged(ObservableCollection<ImplementTrainingExerciseViewModel> items)
    {
        TrackItems(items);

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].SuperSetLetter = ((char)('A' + i)).ToString();
            }
        }

        OnPropertyChanged(nameof(IsSuperSet));
        OnPropertyChanged(nameof(HeaderText));
        StatusMessage = string.Empty;
        CurrentItem = items?.FirstOrDefault();
        UpdatePinnedItems();
        UpdateRoundText();
    }

    private static void CurrentItemPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((SuperSetControl)bindable).UpdatePinnedItems();
    }

    private void TrackItems(ObservableCollection<ImplementTrainingExerciseViewModel> items)
    {
        if (trackedItems != null)
        {
            foreach (var item in trackedItems)
            {
                item.SetFinished -= OnSetFinished;
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        trackedItems = items;
        if (items == null)
            return;

        foreach (var item in items)
        {
            item.SetFinished += OnSetFinished;
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImplementTrainingExerciseViewModel.FinishedSetsCount))
        {
            UpdateRoundText();
        }
    }

    private void OnPinnedExerciseTapped(object sender, TappedEventArgs e)
    {
        if (sender is BindableObject { BindingContext: ImplementTrainingExerciseViewModel item })
        {
            StatusMessage = string.Empty;
            Select(item);
        }
    }

    private void OnSetFinished(object sender, WeightAndRepsViewModel set)
    {
        var items = SuperSetItems;
        var current = CurrentItem;
        if (!AutoAdvance || !IsSuperSet || !ReferenceEquals(sender, current))
            return;

        var currentIndex = items.IndexOf(current);
        var next = FindNextWithUnfinishedSets(items, currentIndex);
        if (next == null)
        {
            StatusMessage = AppResources.SuperSetAllSetsDone;
            return;
        }

        var nextIndex = items.IndexOf(next);
        var format = nextIndex <= currentIndex ? AppResources.SuperSetRoundDone : AppResources.SuperSetNextUp;
        var message = string.Format(format, next.SuperSetLetter, next.FirstUnfinishedSetNumber);

        // Short delay so the user sees the set turn green before the card switches.
        Dispatcher.DispatchDelayed(AutoAdvanceDelay, () =>
        {
            if (!set.IsFinished || !ReferenceEquals(CurrentItem, current))
                return;

            StatusMessage = message;
            Select(next);
        });
    }

    private static ImplementTrainingExerciseViewModel FindNextWithUnfinishedSets(IList<ImplementTrainingExerciseViewModel> items, int currentIndex)
    {
        for (int offset = 1; offset <= items.Count; offset++)
        {
            var candidate = items[(currentIndex + offset) % items.Count];
            if (candidate.HasUnfinishedSets)
                return candidate;
        }

        return null;
    }

    private void Select(ImplementTrainingExerciseViewModel item)
    {
        if (ReferenceEquals(item, CurrentItem))
            return;

        CurrentItem = item;

        ActiveCard.Opacity = 0;
        _ = ActiveScroll.ScrollToAsync(0, 0, false);
        _ = ActiveCard.FadeToAsync(1, 200);
    }

    private void UpdatePinnedItems()
    {
        var items = SuperSetItems;
        var index = items == null || CurrentItem == null ? -1 : items.IndexOf(CurrentItem);
        if (index < 0)
        {
            ItemsBefore = [];
            ItemsAfter = [];
            return;
        }

        ItemsBefore = items.Take(index).ToList();
        ItemsAfter = items.Skip(index + 1).ToList();
    }

    private void UpdateRoundText()
    {
        var withSets = SuperSetItems?.Where(item => item.HasSets).ToList() ?? [];
        if (withSets.Count == 0)
        {
            RoundText = string.Empty;
            return;
        }

        var rounds = withSets.Max(item => item.SetsCount);
        var round = Math.Min(withSets.Min(item => item.FinishedSetsCount) + 1, rounds);
        RoundText = string.Format(AppResources.SuperSetRound, round, rounds);
    }
}
