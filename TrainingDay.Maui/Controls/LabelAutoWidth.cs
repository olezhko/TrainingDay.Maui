namespace TrainingDay.Maui.Controls;

public static class LabelAutoWidth
{
    public static readonly BindableProperty EnableAutoWidthProperty =
        BindableProperty.CreateAttached(
            "EnableAutoWidth",
            typeof(bool),
            typeof(LabelAutoWidth),
            false,
            propertyChanged: OnEnableAutoWidthChanged);

    public static bool GetEnableAutoWidth(BindableObject view)
        => (bool)view.GetValue(EnableAutoWidthProperty);

    public static void SetEnableAutoWidth(BindableObject view, bool value)
        => view.SetValue(EnableAutoWidthProperty, value);


    private static void OnEnableAutoWidthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Label label)
            return;

        // Enable
        if ((bool)newValue)
        {
            label.PropertyChanged += Label_PropertyChanged;
            UpdateWidth(label);
        }
        else // Disable
        {
            label.PropertyChanged -= Label_PropertyChanged;
        }
    }


    private static void Label_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == Label.TextProperty.PropertyName ||
            e.PropertyName == Label.FontSizeProperty.PropertyName ||
            e.PropertyName == Label.FontFamilyProperty.PropertyName)
        {
            UpdateWidth(sender as Label);
        }
    }


    private static void UpdateWidth(Label? label)
    {
        if (label == null || string.IsNullOrEmpty(label.Text))
            return;

        var size = label.Measure(double.PositiveInfinity, double.PositiveInfinity);

        if (size.Width > 0)
            label.WidthRequest = size.Width;
    }
}