using System.Collections;
using System.Collections.Specialized;

namespace TrainingDay.Maui.Controls;

/// <summary>
/// Row of numbered step circles joined by connector lines, optionally with the selected item's content below.
/// Steps come from <see cref="ItemsSource"/> (each item rendered with <see cref="ItemTemplate"/>) or, without items, from <see cref="StepCount"/>.
/// Lines stretch to fill the width; when the steps don't fit, the row scrolls horizontally.
/// </summary>
public class StepProgressBar : Grid
{
    private enum StepState
    {
        Pending,
        Finished,
        Skipped,
    }

    private const double MinLineWidth = 15;
    private const double HeaderSpacing = 2;

    private readonly HorizontalStackLayout headersStackLayout;
    private readonly List<Button> stepButtons = [];
    private readonly List<BoxView> separatorLines = [];
    private readonly List<StepState> stepStates = [];
    private View? element;

    #region Bindable properties

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(StepProgressBar), null, propertyChanged: ItemsSource_OnPropertyChanged);
    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(StepProgressBar), null);
    public static readonly BindableProperty StepCountProperty = BindableProperty.Create(nameof(StepCount), typeof(int), typeof(StepProgressBar), 0, propertyChanged: (b, _, _) => ((StepProgressBar)b).RebuildSteps());
    public static readonly BindableProperty StepSelectedProperty = BindableProperty.Create(nameof(StepSelected), typeof(int), typeof(StepProgressBar), 0, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (b, _, _) => ((StepProgressBar)b).OnStepSelectedChanged());
    public static readonly BindableProperty CompletePreviousStepsProperty = BindableProperty.Create(nameof(CompletePreviousSteps), typeof(bool), typeof(StepProgressBar), false, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty StepCanTouchProperty = BindableProperty.Create(nameof(StepCanTouch), typeof(bool), typeof(StepProgressBar), true);

    public static readonly BindableProperty StepColorProperty = BindableProperty.Create(nameof(StepColor), typeof(Color), typeof(StepProgressBar), Colors.Black, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty SteppedColorProperty = BindableProperty.Create(nameof(SteppedColor), typeof(Color), typeof(StepProgressBar), Colors.Black, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty FinishedColorProperty = BindableProperty.Create(nameof(FinishedColor), typeof(Color), typeof(StepProgressBar), Colors.Green, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty SkippedColorProperty = BindableProperty.Create(nameof(SkippedColor), typeof(Color), typeof(StepProgressBar), Colors.DimGray, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty UnfinishedColorProperty = BindableProperty.Create(nameof(UnfinishedColor), typeof(Color), typeof(StepProgressBar), Colors.Transparent, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty StepTextColorProperty = BindableProperty.Create(nameof(StepTextColor), typeof(Color), typeof(StepProgressBar), null, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(StepProgressBar), Colors.Silver, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty FinishedLineColorProperty = BindableProperty.Create(nameof(FinishedLineColor), typeof(Color), typeof(StepProgressBar), Colors.Silver, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty LineThicknessProperty = BindableProperty.Create(nameof(LineThickness), typeof(double), typeof(StepProgressBar), 1.0, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty StepSizeProperty = BindableProperty.Create(nameof(StepSize), typeof(double), typeof(StepProgressBar), 45.0, propertyChanged: OnAppearanceChanged);

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Content shown under the steps, bound to the selected item.
    /// </summary>
    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// Number of steps when no <see cref="ItemsSource"/> is set.
    /// </summary>
    public int StepCount
    {
        get => (int)GetValue(StepCountProperty);
        set => SetValue(StepCountProperty, value);
    }

    /// <summary>
    /// Zero-based index of the current step.
    /// </summary>
    public int StepSelected
    {
        get => (int)GetValue(StepSelectedProperty);
        set => SetValue(StepSelectedProperty, value);
    }

    /// <summary>
    /// Wizard mode: every step before the current one is drawn as finished.
    /// </summary>
    public bool CompletePreviousSteps
    {
        get => (bool)GetValue(CompletePreviousStepsProperty);
        set => SetValue(CompletePreviousStepsProperty, value);
    }

    public bool StepCanTouch
    {
        get => (bool)GetValue(StepCanTouchProperty);
        set => SetValue(StepCanTouchProperty, value);
    }

    /// <summary>
    /// Circle border colour.
    /// </summary>
    public Color StepColor
    {
        get => (Color)GetValue(StepColorProperty);
        set => SetValue(StepColorProperty, value);
    }

    /// <summary>
    /// Fill of the current step.
    /// </summary>
    public Color SteppedColor
    {
        get => (Color)GetValue(SteppedColorProperty);
        set => SetValue(SteppedColorProperty, value);
    }

    public Color FinishedColor
    {
        get => (Color)GetValue(FinishedColorProperty);
        set => SetValue(FinishedColorProperty, value);
    }

    public Color SkippedColor
    {
        get => (Color)GetValue(SkippedColorProperty);
        set => SetValue(SkippedColorProperty, value);
    }

    public Color UnfinishedColor
    {
        get => (Color)GetValue(UnfinishedColorProperty);
        set => SetValue(UnfinishedColorProperty, value);
    }

    /// <summary>
    /// Number colour; follows the app theme (black / white) when not set.
    /// </summary>
    public Color StepTextColor
    {
        get => (Color)GetValue(StepTextColorProperty);
        set => SetValue(StepTextColorProperty, value);
    }

    public Color LineColor
    {
        get => (Color)GetValue(LineColorProperty);
        set => SetValue(LineColorProperty, value);
    }

    /// <summary>
    /// Colour of a line whose both ends are finished (or finished and current).
    /// </summary>
    public Color FinishedLineColor
    {
        get => (Color)GetValue(FinishedLineColorProperty);
        set => SetValue(FinishedLineColorProperty, value);
    }

    public double LineThickness
    {
        get => (double)GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    public double StepSize
    {
        get => (double)GetValue(StepSizeProperty);
        set => SetValue(StepSizeProperty, value);
    }

    #endregion

    public event EventHandler<StepProgressBarCurrentItemChangedEventArgs>? CurrentItemChanged;

    public StepProgressBar()
    {
        HorizontalOptions = LayoutOptions.Fill;
        RowDefinitions = new RowDefinitionCollection
        {
            new RowDefinition() { Height = GridLength.Auto },
            new RowDefinition() { Height = GridLength.Star },
        };
        RowSpacing = 10;

        headersStackLayout = new HorizontalStackLayout
        {
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Fill,
            Padding = new Thickness(0),
            Spacing = HeaderSpacing
        };

        var headerScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalOptions = LayoutOptions.Fill,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
            Content = headersStackLayout,
        };

        Children.Add(headerScroll);
        Grid.SetRow(headerScroll, 0);

        SizeChanged += (_, _) => RecalculateSeparatorLineWidth();
    }

    private IList? Items => ItemsSource as IList;

    private int StepTotal => Items?.Count ?? Math.Max(StepCount, 0);

    #region Public API

    public void SelectElement(int index)
    {
        if (index < 0 || index >= StepTotal)
            return;

        StepSelected = index;
        ApplyCurrentItem();

        if (element?.BindingContext is { } currentItem)
            CurrentItemChanged?.Invoke(this, new StepProgressBarCurrentItemChangedEventArgs(currentItem));
    }

    public void NextElement(int index) => SelectElement(index);

    /// <summary>
    /// Marks the current step as finished.
    /// </summary>
    public void DeselectElement() => SetState(StepSelected, StepState.Finished);

    /// <summary>
    /// Marks the current step as skipped, or back to pending.
    /// </summary>
    public void SkipElement(bool isSkip = true) => SetState(StepSelected, isSkip ? StepState.Skipped : StepState.Pending);

    #endregion

    #region Build

    private void RebuildSteps()
    {
        headersStackLayout.Children.Clear();
        stepButtons.Clear();
        separatorLines.Clear();

        var count = StepTotal;
        while (stepStates.Count < count)
            stepStates.Add(StepState.Pending);
        if (stepStates.Count > count)
            stepStates.RemoveRange(count, stepStates.Count - count);

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                var line = new BoxView { VerticalOptions = LayoutOptions.Center, WidthRequest = MinLineWidth };
                separatorLines.Add(line);
                headersStackLayout.Children.Add(line);
            }

            var button = new Button
            {
                Text = $"{i + 1}",
                AutomationId = $"{i + 1}",
                Padding = new Thickness(0),
                BorderWidth = 0.5,
            };
            var stepIndex = i;
            button.Clicked += (_, _) =>
            {
                if (StepCanTouch)
                    SelectElement(stepIndex);
            };
            stepButtons.Add(button);
            headersStackLayout.Children.Add(button);
        }

        UpdateVisuals();
        RecalculateSeparatorLineWidth();
    }

    private void UpdateVisuals()
    {
        var size = StepSize;
        for (int i = 0; i < stepButtons.Count; i++)
        {
            var button = stepButtons[i];
            var state = GetEffectiveState(i);
            var isCurrent = i == StepSelected;

            button.WidthRequest = button.HeightRequest = size;
            button.CornerRadius = (int)(size / 2);
            button.BorderColor = StepColor;
            button.FontAttributes = isCurrent || CompletePreviousSteps ? FontAttributes.Bold : FontAttributes.None;
            button.BackgroundColor = state switch
            {
                StepState.Finished => FinishedColor,
                StepState.Skipped => SkippedColor,
                _ => isCurrent ? SteppedColor : UnfinishedColor,
            };

            if (StepTextColor is { } textColor)
                button.TextColor = textColor;
            else
                button.SetAppThemeColor(Button.TextColorProperty, Colors.Black, Colors.White);
        }

        for (int i = 0; i < separatorLines.Count; i++)
        {
            // Line i joins step i and step i + 1
            var line = separatorLines[i];
            var isFinished = GetEffectiveState(i) == StepState.Finished
                && (GetEffectiveState(i + 1) == StepState.Finished || i + 1 == StepSelected);

            line.HeightRequest = LineThickness;
            line.Color = isFinished ? FinishedLineColor : LineColor;
        }
    }

    private StepState GetEffectiveState(int index)
    {
        if (CompletePreviousSteps && index < StepSelected)
            return StepState.Finished;

        return index < stepStates.Count ? stepStates[index] : StepState.Pending;
    }

    private void SetState(int index, StepState state)
    {
        if (index < 0 || index >= stepStates.Count)
            return;

        stepStates[index] = state;
        UpdateVisuals();
    }

    private void RecalculateSeparatorLineWidth()
    {
        if (separatorLines.Count == 0 || Width <= 0)
            return;

        // Stretch the lines to fill the row; below the minimum the row scrolls instead
        var gaps = (stepButtons.Count + separatorLines.Count - 1) * HeaderSpacing;
        var newWidth = (Width - stepButtons.Count * StepSize - gaps) / separatorLines.Count;
        newWidth = Math.Max(MinLineWidth, Math.Floor(newWidth));

        separatorLines.ForEach(item => item.WidthRequest = newWidth);
    }

    private void SetTemplateElement()
    {
        if (element != null || ItemTemplate == null)
            return;

        element = ItemTemplate.CreateContent() as View;
        if (element != null)
        {
            Children.Add(element);
            Grid.SetRow(element, 1);
        }
    }

    private void ApplyCurrentItem()
    {
        if (element != null && Items is { } list && StepSelected >= 0 && StepSelected < list.Count)
            element.BindingContext = list[StepSelected];
    }

    #endregion

    private void OnStepSelectedChanged()
    {
        ApplyCurrentItem();
        UpdateVisuals();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Step states are positional; a reset or removal can't be mapped, so start those over
        if (e.Action is NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Remove)
            stepStates.Clear();

        RebuildSteps();
    }

    private static void ItemsSource_OnPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        var control = (StepProgressBar)bindable;

        if (oldvalue is INotifyCollectionChanged oldCollection)
            oldCollection.CollectionChanged -= control.OnItemsCollectionChanged;

        if (newvalue is INotifyCollectionChanged newCollection)
            newCollection.CollectionChanged += control.OnItemsCollectionChanged;

        control.stepStates.Clear();
        control.RebuildSteps();

        if (newvalue != null)
        {
            control.SetTemplateElement();
            control.SelectElement(control.StepSelected);
        }
    }

    private static void OnAppearanceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (StepProgressBar)bindable;
        control.UpdateVisuals();
        control.RecalculateSeparatorLineWidth();
    }
}

public class StepProgressBarCurrentItemChangedEventArgs : EventArgs
{
    public StepProgressBarCurrentItemChangedEventArgs(object currentItem)
    {
        CurrentItem = currentItem;
    }
    public object CurrentItem { get; }
}
