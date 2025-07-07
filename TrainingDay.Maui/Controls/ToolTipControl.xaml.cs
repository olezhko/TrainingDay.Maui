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
            AttachedControl.IsVisible = true;
            IsVisible = true;
            await frame.FadeTo(1, 500);
        }
    }

    public async Task Hide(bool isRaise = true)
    {
        await frame.FadeTo(0, 500);
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
        try
        {
            if (e.PropertyName == "Width" || e.PropertyName == "Height" || e.PropertyName == "WidthRequest" || e.PropertyName == "HeightRequest"
                || e.PropertyName == "Y" || e.PropertyName == "X" || e.PropertyName == "EmptyView")
            {
                CalculatePosition(innerView);
            }
        }
        catch (Exception)
        {
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

    private void CalculatePosition(View control)
    {
        if (control == null || Width == -1 || Height == -1)
        {
            return;
        }

        switch (Position)
        {
            case ToolTipPosition.Bottom:
                Margin = new Thickness(control.X, control.Y, 0, 0);
                break;
            case ToolTipPosition.Right:
                Margin = new Thickness(control.X, control.Y, 0, 0);
                break;
            case ToolTipPosition.Left:
                Margin = new Thickness(
                    Spacing,
                    control.Y + (control.Height / 2.0) + (Height / 2.0) > ParentHeight ? ParentHeight - Height - Spacing : control.Y + (control.Height / 2.0) - (Height / 2.0) - Spacing,
                    0, 0);
                if (Margin.Left + Width > control.X - Spacing)
                {
                    WidthRequest = control.X - Spacing - Margin.Left;
                }

                break;
            case ToolTipPosition.TopLeft:
                Margin = new Thickness(0, control.Y - Spacing - Height - control.Height, 0, 0);
                break;
            case ToolTipPosition.TopRight:
                Margin = new Thickness(ParentWidth - ParentWidth / 2, control.Y - Spacing - Height - control.Height, 0, 0);
                break;
            case ToolTipPosition.TopCenter:
                Margin = new Thickness(ParentWidth / 2 - Width / 2, control.Y - Spacing - Height - control.Height, ParentWidth / 2 - Width / 2, 0);
                break;
            case ToolTipPosition.Start:
                Margin = new Thickness(control.X, control.Y, 0, 0);
                break;
            case ToolTipPosition.Center:
                Margin = new Thickness(50, ParentHeight / 2 - control.Height / 2, 50, 0);
                break;
        }
    }

    private static void LinkToLinksPreviewControl(ToolTipControl control, View view)
    {
        if (view == null)
            return;

        control.innerView = view;
        System.ComponentModel.PropertyChangedEventHandler properyChanged = (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        {
            if (e.PropertyName == "Width" || e.PropertyName == "Height" || e.PropertyName == "WidthRequest" || e.PropertyName == "HeightRequest"
                || e.PropertyName == "Y" || e.PropertyName == "X" || e.PropertyName == "EmptyView")
            {
                try
                {
                    control.CalculatePosition((View)sender);
                }
                catch (Exception)
                {
                    LoggingService.TrackError(new Exception($"Error calculating position for ToolTipControl. {sender.GetType().Name}")); 
                }
            }
        };
        view.PropertyChanged += properyChanged;
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
        if (newvalue != null)
        {
            (bindable as ToolTipControl).StackLayout.Children.Insert(0, (ContentView)newvalue);
            Grid.SetColumn((ContentView)newvalue, 0);
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

    public static readonly BindableProperty ParentHeightProperty =
        BindableProperty.Create("ParentHeight", typeof(double), typeof(ToolTipControl), 0.0);
    public double ParentHeight
    {
        get { return (double)GetValue(ParentHeightProperty); }
        set { SetValue(ParentHeightProperty, value); }
    }

    public static readonly BindableProperty ParentWidthProperty =
        BindableProperty.Create("ParentWidth", typeof(double), typeof(ToolTipControl), 0.0);
    public double ParentWidth
    {
        get { return (double)GetValue(ParentWidthProperty); }
        set { SetValue(ParentWidthProperty, value); }
    }

    #endregion
}