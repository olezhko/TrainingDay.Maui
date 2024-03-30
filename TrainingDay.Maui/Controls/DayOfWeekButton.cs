namespace TrainingDay.Maui.Controls;

public class DayOfWeekButton : Button
{
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create("IsSelected", typeof(bool), typeof(DayOfWeekButton), false, BindingMode.TwoWay, propertyChanged: IsSelectedPropertyChanged);

    private static void IsSelectedPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        var picker = (DayOfWeekButton)bindable;
        picker.BackgroundColor = ((bool)newvalue) ? Colors.Orange : Colors.DimGray;
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}