namespace TrainingDay.Maui.Controls;

public class StepBar : ContentView
{
    #region Bindable properties
    public static readonly BindableProperty StepCountProperty =
        BindableProperty.Create(
            nameof(StepCount),
            typeof(int),
            typeof(StepBar),
            1,
            propertyChanged: (_, __, ___) => ((StepBar)_).BuildUI());

    public int StepCount
    {
        get => (int)GetValue(StepCountProperty);
        set => SetValue(StepCountProperty, value);
    }

    public static readonly BindableProperty CurrentStepProperty =
        BindableProperty.Create(
            nameof(CurrentStep),
            typeof(int),
            typeof(StepBar),
            1,
            propertyChanged: (_, __, ___) => ((StepBar)_).UpdateColors());

    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }

    public static readonly BindableProperty FinishedColorProperty =
        BindableProperty.Create(
            nameof(FinishedColor),
            typeof(Brush),
            typeof(StepBar),
            Brush.Purple,
            propertyChanged: (_, __, ___) => ((StepBar)_).UpdateColors());

    public Brush FinishedColor
    {
        get => (Brush)GetValue(FinishedColorProperty);
        set => SetValue(FinishedColorProperty, value);
    }

    public static readonly BindableProperty UnfinishedColorProperty =
        BindableProperty.Create(
            nameof(UnfinishedColor),
            typeof(Brush),
            typeof(StepBar),
            Brush.LightGray,
            propertyChanged: (_, __, ___) => ((StepBar)_).UpdateColors());

    public Brush UnfinishedColor
    {
        get => (Brush)GetValue(UnfinishedColorProperty);
        set => SetValue(UnfinishedColorProperty, value);
    }
    #endregion

    readonly Grid _grid = new()   // ⬅ main layout now a Grid
    {
        RowDefinitions = { new RowDefinition(GridLength.Auto) },
        ColumnSpacing = 0
    };

    public StepBar()
    {
        Content = _grid;
        BuildUI();
        SizeChanged += (_, __) => UpdateColors(); // ensures lines recolor after resizing
    }

    void BuildUI()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();

        if (StepCount < 1) StepCount = 1;

        /*  Each step uses TWO grid columns:
         *  [circle] [line] [circle] [line] … [circle]
         *  Circle → Width = Auto
         *  Line   → Width = *  (equally splits remaining space)
         */
        for (int i = 1; i <= StepCount; i++)
        {
            // Circle column
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var circle = CreateCircle(i);
            _grid.Add(circle, _grid.ColumnDefinitions.Count - 1, 0);

            // Add a star-width line after every circle except the last
            if (i != StepCount)
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

                var line = new BoxView
                {
                    HeightRequest = 2,
                    VerticalOptions = LayoutOptions.Center
                };
                // Tag so UpdateColors knows it’s a line
                line.BindingContext = "line";

                _grid.Add(line, _grid.ColumnDefinitions.Count - 1, 0);
            }
        }

        UpdateColors();
    }

    View CreateCircle(int number)
    {
        var label = new Label
        {
            Text = number.ToString(),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };

        var frame = new Frame
        {
            Content = label,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = 0,
            HasShadow = false
        };

        // Tag the index for easy lookup in UpdateColors()
        frame.BindingContext = number;

        return frame;
    }

    void UpdateColors()
    {
        int stepIndex = 0;
        foreach (View child in _grid.Children)
        {
            switch (child.BindingContext)
            {
                case int circleNumber:
                    stepIndex++;
                    bool finished = circleNumber <= CurrentStep;
                    ((Frame)child).Background = finished ? FinishedColor : UnfinishedColor;
                    break;

                case "line":
                    // finished if the *previous* circle is ≤ CurrentStep
                    bool lineFinished = stepIndex < CurrentStep;
                    ((BoxView)child).Background = lineFinished ? FinishedColor : UnfinishedColor;
                    break;
            }
        }
    }
}