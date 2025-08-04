using TrainingDay.Maui.Models;
using TrainingDay.Maui.Resources.Strings;
using static TrainingDay.Maui.Controls.ExerciseView;

namespace TrainingDay.Maui.Controls;

public partial class TimerPicker : ContentView
{
    private Picker dataPicker;
    private PickerMode mode;
    private TimeSpan value;
    public TimerPicker()
	{
		InitializeComponent();
        dataPicker = new Picker();
        dataPicker.ItemsSource = Enumerable.Range(0, 60).Select(min => min.ToString("D2")).ToList();
        dataPicker.SelectedIndexChanged += DataPickerOnSelectedIndexChanged;
        dataPicker.IsVisible = false;
        dataPicker.Unfocused += DataPicker_Unfocused;
        dataPicker.TitleColor = Colors.Orange;
        dataPicker.WidthRequest = 1;
        dataPicker.HeightRequest = 1;
        Container.Children.Add(dataPicker);

        BindingContext = this;
    }

    private void HourGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
    {
        DatePicker(AppResources.Hours, PickerMode.Hour);
    }

    private void SecondsGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
    {
        DatePicker(AppResources.Seconds, PickerMode.Second);
    }

    private void MinutesGestureRecognizer_OnTapped(object sender, TappedEventArgs e)
    {
        DatePicker(AppResources.Minutes, PickerMode.Minute);
    }

    private void DatePicker(string title, PickerMode newMode)
    {
        dataPicker.Unfocus();
        dataPicker.Title = title;
        dataPicker.SelectedIndex = -1;
        dataPicker.IsVisible = true;
        dataPicker.Focus();
        mode = newMode;
    }

    private void DataPickerOnSelectedIndexChanged(object sender, EventArgs e)
    {
        if (IsTimerDisabled)
        {
            return;
        }

        if (!dataPicker.IsVisible)
        {
            return;
        }

        switch (mode)
        {
            case PickerMode.Hour:
                value = new TimeSpan(dataPicker.SelectedIndex, value.Minutes, value.Seconds);
                break;
            case PickerMode.Minute:
                value = new TimeSpan(value.Hours, dataPicker.SelectedIndex,  value.Seconds);
                break;
            case PickerMode.Second:
                value = new TimeSpan(value.Hours, value.Minutes, dataPicker.SelectedIndex);
                break;
        }

        Value = value;
        dataPicker.IsVisible = false;
    }

    private void DataPicker_Unfocused(object sender, FocusEventArgs e)
    {
        dataPicker.IsVisible = false;
    }

    public TimeSpan Value
    {
        get { return (TimeSpan)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public TimePickerValueType ValueType
    {
        get { return (TimePickerValueType)GetValue(ValueTypeProperty); }
        set { SetValue(ValueTypeProperty, value); }
    }

    public bool IsTimerDisabled
    {
        get { return (bool)GetValue(IsTimerDisabledProperty); }
        set { SetValue(IsTimerDisabledProperty, value); }
    }

    public Color TextColor
    {
        get { return (Color)GetValue(TextColorProperty); }
        set { SetValue(TextColorProperty, value); }
    }

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(nameof(TextColor), typeof(Color), typeof(TimerPicker), Colors.White, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty IsTimerDisabledProperty = BindableProperty.Create(nameof(IsTimerDisabled), typeof(bool), typeof(TimerPicker), false, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(TimeSpan), typeof(TimerPicker), TimeSpan.FromMinutes(2), defaultBindingMode: BindingMode.TwoWay, propertyChanged: ValuePropertyChanged);
    public static readonly BindableProperty ValueTypeProperty = BindableProperty.Create(nameof(ValueType), typeof(TimePickerValueType), typeof(TimerPicker), TimePickerValueType.MinSec, defaultBindingMode: BindingMode.TwoWay, propertyChanged: ValueTypePropertyChanged);

    private static void ValueTypePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = bindable as TimerPicker;

        var newType = (TimePickerValueType)newValue;
        if (newType == TimePickerValueType.MinSec)
            control.HourPart.IsVisible = false;
    }

    private static void ValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = bindable as TimerPicker;
        var time = (TimeSpan)newValue;
        control.HourPicker.Text = time.Hours.ToString("D2");
        control.MinutesPicker.Text = time.Minutes.ToString("D2");
        control.SecondsPicker.Text = time.Seconds.ToString("D2");
    }

    public enum TimePickerValueType
    {
        OnlySec,
        MinSec,
        FullTime,
    }
}