using System.Windows.Input;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public partial class RepsAndWeightControl : ContentView
{
	public RepsAndWeightControl()
	{
		InitializeComponent();
	}

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(WeightAndRepsViewModel), typeof(RepsAndWeightControl), new WeightAndRepsViewModel(5, 15), defaultBindingMode: BindingMode.TwoWay);
    public WeightAndRepsViewModel Value
    {
        get { return (WeightAndRepsViewModel)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    public static readonly BindableProperty IsOnlyRepsProperty = BindableProperty.Create(nameof(IsOnlyReps), typeof(bool), typeof(RepsAndWeightControl), false, defaultBindingMode: BindingMode.TwoWay, propertyChanged: PropertyChanged);

    private static void PropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if ((bool)newvalue)
        {
            (bindable as RepsAndWeightControl).MainGrid.ColumnDefinitions[2].Width = new GridLength(0);
        }
    }

    public bool IsOnlyReps
    {
        get { return (bool)GetValue(IsOnlyRepsProperty); }
        set { SetValue(IsOnlyRepsProperty, value); }
    }

    private void DeleteButton_Clicked(object sender, EventArgs e)
    {
        OnDeleteRequire();
    }

    public void OnDeleteRequire()
    {
        DeleteRequestCommand?.Execute(Value);
    }

    public static readonly BindableProperty DeleteRequestCommandProperty =
        BindableProperty.Create("DeleteRequestCommand", typeof(Command), typeof(RepsAndWeightControl), null);

    public ICommand DeleteRequestCommand
    {
        get { return (ICommand)GetValue(DeleteRequestCommandProperty); }
        set { SetValue(DeleteRequestCommandProperty, value); }
    }
}