using System.ComponentModel;
using Microsoft.Maui.Controls;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Controls;

public enum ToolTipPosition
{
    Bottom,
    Right,
    Left,
    Start,
    Center,
    TopLeft,
    TopCenter,
    TopRight
}

public partial class ToolTipControl : ContentView
{
    private const double CenterSideMargin = 50;
    private const double MinimumWidth = 120;

    private View innerView;

    public ToolTipControl()
    {
        InitializeComponent();
        IsVisible = !NeverShow;
        PropertyChanged += ToolTipControl_PropertyChanged;
    }

    public async Task Show()
    {
        if (!NeverShow)
        {
            (Parent as TooltipControlContainer)?.SetChildVisible(AttachedControl, true);
            IsVisible = true;
            SafeCalculatePosition();
            await frame.FadeToAsync(1, 500);
        }
    }

    public async Task Hide(bool isRaise = true)
    {
        await frame.FadeToAsync(0, 500);
        if (isRaise)
        {
            OnClosed();
        }

        IsVisible = false;
    }

    public event EventHandler Closed;

    private void OnClosed()
    {
        Closed?.Invoke(this, null);
    }

    private void ToolTipControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (IsLayoutProperty(e.PropertyName))
        {
            SafeCalculatePosition();
        }
    }

    private static bool IsLayoutProperty(string? propertyName)
    {
        return propertyName == "Width" || propertyName == "Height" || propertyName == "WidthRequest" || propertyName == "HeightRequest"
               || propertyName == "Y" || propertyName == "X" || propertyName == "EmptyView";
    }

    protected override void OnParentChanging(ParentChangingEventArgs args)
    {
        base.OnParentChanging(args);
        if (args.OldParent is VisualElement oldParent)
        {
            oldParent.SizeChanged -= Parent_SizeChanged;
        }

        if (args.NewParent is VisualElement newParent)
        {
            newParent.SizeChanged += Parent_SizeChanged;
        }
    }

    private void Parent_SizeChanged(object? sender, EventArgs e)
    {
        SafeCalculatePosition();
    }

    private void SafeCalculatePosition()
    {
        try
        {
            CalculatePosition();
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
    }

    private async void Skip_Tapped(object sender, EventArgs e)
    {
        await Hide();
    }

    private async void NeverShow_Tapped(object sender, EventArgs e)
    {
        await Hide();
        NeverShow = true;
    }

    /// <summary>
    /// Places the tooltip relative to <see cref="AttachedControl"/>, in the parent's coordinates.
    /// Uses TranslationX/Y (no layout pass, so Auto rows don't grow) and keeps the tooltip inside the parent.
    /// </summary>
    private void CalculatePosition()
    {
        if (innerView == null || Parent is not VisualElement parent || Width <= 0 || Height <= 0 || innerView.Width < 0)
        {
            return;
        }

        var controlPosition = GetPositionIn(innerView, parent);
        if (controlPosition == null)
        {
            return;
        }

        // the parent's content area, in the parent's coordinates
        var padding = (parent as Layout)?.Padding ?? Thickness.Zero;
        var area = new Rect(padding.Left, padding.Top,
            parent.Width - padding.HorizontalThickness, parent.Height - padding.VerticalThickness);
        if (area.Width <= 0 || area.Height <= 0)
        {
            return;
        }

        var control = new Rect(controlPosition.Value, new Size(innerView.Width, innerView.Height));
        double spacing = Spacing;
        double maxWidth = area.Width - 2 * spacing;
        double x;
        double y;

        switch (Position)
        {
            case ToolTipPosition.Bottom:
                x = control.Left;
                y = control.Bottom + spacing;
                break;
            case ToolTipPosition.Right:
                maxWidth = area.Right - control.Right - 2 * spacing;
                x = control.Right + spacing;
                y = control.Center.Y - Height / 2;
                break;
            case ToolTipPosition.Left:
                maxWidth = control.Left - area.Left - 2 * spacing;
                x = control.Left - spacing - Width;
                y = control.Center.Y - Height / 2;
                break;
            case ToolTipPosition.TopLeft:
                x = area.Left + spacing;
                y = control.Top - spacing - Height;
                break;
            case ToolTipPosition.TopCenter:
                x = area.Center.X - Width / 2;
                y = control.Top - spacing - Height;
                break;
            case ToolTipPosition.TopRight:
                x = area.Right - spacing - Width;
                y = control.Top - spacing - Height;
                break;
            case ToolTipPosition.Start:
                x = control.Left;
                y = control.Top;
                break;
            default: // Center of the parent
                maxWidth = area.Width - 2 * CenterSideMargin;
                x = area.Center.X - Width / 2;
                y = area.Center.Y - Height / 2;
                break;
        }

        // only the width is constrained; the height follows the wrapped text, so this converges after one re-measure
        var maximumWidth = Math.Max(MinimumWidth, maxWidth);
        if (Math.Abs(MaximumWidthRequest - maximumWidth) > 0.5)
        {
            MaximumWidthRequest = maximumWidth;
        }

        x = Math.Clamp(x, area.Left + spacing, Math.Max(area.Left + spacing, area.Right - spacing - Width));
        y = Math.Clamp(y, area.Top + spacing, Math.Max(area.Top + spacing, area.Bottom - spacing - Height));

        // X/Y is where the grid laid the tooltip out (its cell origin); translate from there
        TranslationX = x - X;
        TranslationY = y - Y;
    }

    /// <summary>
    /// Position of <paramref name="element"/> in the coordinates of <paramref name="ancestor"/>; null when it is not a descendant.
    /// </summary>
    private static Point? GetPositionIn(VisualElement element, Element ancestor)
    {
        double x = 0;
        double y = 0;
        Element? current = element;
        while (current is VisualElement visual && current != ancestor)
        {
            x += visual.X + visual.TranslationX;
            y += visual.Y + visual.TranslationY;
            current = visual.Parent;
        }

        return current == ancestor ? new Point(x, y) : null;
    }

    private static void LinkToLinksPreviewControl(ToolTipControl control, View view)
    {
        if (view == null)
            return;

        control.innerView = view;
        view.PropertyChanged += (sender, e) =>
        {
            if (IsLayoutProperty(e.PropertyName))
            {
                control.SafeCalculatePosition();
            }
        };
    }

    private static void NeverShowPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if ((bool)newvalue)
        {
            (bindable as ToolTipControl).IsVisible = false;
        }
    }

    #region Properties
    public static readonly BindableProperty FrameBackgroundProperty =
        BindableProperty.Create("FrameBackground", typeof(Color), typeof(ToolTipControl), Colors.Gray);
    public Color FrameBackground
    {
        get { return (Color)GetValue(FrameBackgroundProperty); }
        set { SetValue(FrameBackgroundProperty, value); }
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create("Title", typeof(string), typeof(ToolTipControl), null);
    public string Title
    {
        get { return (string)GetValue(TitleProperty); }
        set { SetValue(TitleProperty, value); }
    }

    public static readonly BindableProperty EmptyViewProperty =
        BindableProperty.Create(nameof(EmptyView), typeof(object), typeof(ToolTipControl), null, propertyChanged: EmptyViewPropertyChanged);

    private static void EmptyViewPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (newvalue is not null && bindable is ToolTipControl)
        {
            ((ToolTipControl)bindable).StackLayout.Children.Insert(0, (IView)newvalue);
            Grid.SetColumn((BindableObject)newvalue, 0);
        }
    }

    public object EmptyView
    {
        get => GetValue(EmptyViewProperty);
        set => SetValue(EmptyViewProperty, value);
    }

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create("Text", typeof(string), typeof(ToolTipControl), null);
    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public static readonly BindableProperty SkipTextProperty =
        BindableProperty.Create("SkipText", typeof(string), typeof(ToolTipControl), "Skip");
    public string SkipText
    {
        get { return (string)GetValue(SkipTextProperty); }
        set { SetValue(SkipTextProperty, value); }
    }

    public static readonly BindableProperty NeverShowTextProperty =
        BindableProperty.Create("NeverShowText", typeof(string), typeof(ToolTipControl), "Never Show");
    public string NeverShowText
    {
        get { return (string)GetValue(NeverShowTextProperty); }
        set { SetValue(NeverShowTextProperty, value); }
    }

    public static readonly BindableProperty SpacingProperty =
        BindableProperty.Create("Spacing", typeof(int), typeof(ToolTipControl), 10);
    public int Spacing
    {
        get { return (int)GetValue(SpacingProperty); }
        set { SetValue(SpacingProperty, value); }
    }

    [TypeConverter(typeof(ReferenceTypeConverter))]
    public View AttachedControl
    {
        get => innerView;
        set => LinkToLinksPreviewControl(this, value);
    }

    public static readonly BindableProperty NeverShowProperty =
        BindableProperty.Create("NeverShow", typeof(bool), typeof(ToolTipControl), false, propertyChanged: NeverShowPropertyChanged);

    public bool NeverShow
    {
        get { return (bool)GetValue(NeverShowProperty); }
        set { SetValue(NeverShowProperty, value); }
    }

    public static readonly BindableProperty PositionProperty =
        BindableProperty.Create("Position", typeof(ToolTipPosition), typeof(ToolTipControl), ToolTipPosition.Left);
    public ToolTipPosition Position
    {
        get { return (ToolTipPosition)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }

    #endregion
}