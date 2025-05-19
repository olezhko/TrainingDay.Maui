using System.Collections;
using System.Collections.Specialized;
using Microsoft.Maui.ApplicationModel;

namespace TrainingDay.Maui.Controls;

public class StepProgressBar : Grid
{
    Button _lastStepSelected;

    #region Dep

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(StepProgressBar), null, propertyChanged: ItemsSource_OnPropertyChanged);

    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(StepProgressBar), null);

    public DataTemplate ItemTemplate
    {
        get { return (DataTemplate)GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); }
    }

    public static readonly BindableProperty UnselectedIndicatorProperty = BindableProperty.Create(nameof(DeselectElementImage), typeof(string), typeof(StepProgressBar), string.Empty, BindingMode.OneWay);
    public string DeselectElementImage
    {
        get { return (string)this.GetValue(UnselectedIndicatorProperty); }
        set { this.SetValue(UnselectedIndicatorProperty, value); }
    }

    public static readonly BindableProperty StepSelectedProperty = BindableProperty.Create(nameof(StepSelected), typeof(int), typeof(StepProgressBar), 0, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty StepColorProperty = BindableProperty.Create(nameof(StepColor), typeof(Color), typeof(StepProgressBar), Colors.Black, defaultBindingMode: BindingMode.TwoWay);
    public static readonly BindableProperty SteppedColorProperty = BindableProperty.Create(nameof(SteppedColor), typeof(Color), typeof(StepProgressBar), Colors.Black, defaultBindingMode: BindingMode.TwoWay, propertyChanged: StepColorPropertyChanged);
    public static readonly BindableProperty StepCanTouchProperty = BindableProperty.Create(nameof(StepCanTouch), typeof(bool), typeof(StepProgressBar), true);

    public Color StepColor
    {
        get { return (Color)GetValue(StepColorProperty); }
        set { SetValue(StepColorProperty, value); }
    }

    public Color SteppedColor
    {
        get { return (Color)GetValue(SteppedColorProperty); }
        set { SetValue(SteppedColorProperty, value); }
    }

    public int StepSelected
    {
        get { return (int)GetValue(StepSelectedProperty); }
        set { SetValue(StepSelectedProperty, value); }
    }

    public bool StepCanTouch
    {
        get { return (bool)GetValue(StepCanTouchProperty); }
        set { SetValue(StepCanTouchProperty, value); }
    }


    #endregion

    public StepProgressBar()
    {
        CollectionChanged += OnCollectionChanged;
        HorizontalOptions = LayoutOptions.Fill;
        AddStyles();
        RowDefinitions = new RowDefinitionCollection
        {
            new RowDefinition() { Height = GridLength.Auto },
            new RowDefinition() { Height = GridLength.Star },
        };

        RowSpacing = 10;

        headersStackLayout = new HorizontalStackLayout();
        headersStackLayout.VerticalOptions = LayoutOptions.Start;
        headersStackLayout.HorizontalOptions = LayoutOptions.Fill;
        headersStackLayout.Padding = new Thickness(0);
        headersStackLayout.Spacing = 2;

        var headerScroll = new ScrollView();
        headerScroll.Orientation = ScrollOrientation.Horizontal;
        headerScroll.HorizontalOptions = LayoutOptions.Fill;
        headerScroll.Content = headersStackLayout;
        headerScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Always;

        Children.Add(headerScroll);
        Grid.SetRow(headerScroll, 0);
    }

    #region Draw
    private HorizontalStackLayout headersStackLayout;
    private View element;
    private void SetTemplateElement()
    {
        element = ItemTemplate.CreateContent() as View;
        Children.Add(element);
        Grid.SetRow(element, 1);
    }

    private void AddStyles()
    {
        var unselectedCircleStyle = new Style(typeof(Button))
        {
            Setters =
                {
                    new Setter { Property = Button.BackgroundColorProperty, Value = Colors.Transparent },
                    new Setter { Property = Button.BorderColorProperty, Value = StepColor },
                    new Setter { Property = Button.TextColorProperty, Value = App.Current.RequestedTheme == AppTheme.Light? Colors.Black : Colors.White },
                    new Setter { Property = Button.BorderWidthProperty, Value = 0.5 },
                    new Setter { Property = HeightRequestProperty, Value = 45 },
                    new Setter { Property = WidthRequestProperty, Value = 45 },
                    new Setter { Property = PaddingProperty, Value = new Thickness(0) },
                },
        };

        var selectedCircleStyle = new Style(typeof(Button))
        {
            Setters =
                {
                    new Setter { Property = Button.BackgroundColorProperty, Value = SteppedColor },
                    new Setter { Property = Button.FontAttributesProperty, Value = FontAttributes.Bold },
                    new Setter { Property = Button.TextColorProperty, Value = App.Current.RequestedTheme == AppTheme.Light? Colors.Black : Colors.White },
                    new Setter { Property = Button.BorderColorProperty, Value = StepColor },
                    new Setter { Property = Button.BorderWidthProperty, Value = 0.5 },
                    new Setter { Property = HeightRequestProperty, Value = 45 },
                    new Setter { Property = WidthRequestProperty, Value = 45 },
                    new Setter { Property = PaddingProperty, Value = new Thickness(0) },
                },
        };

        Resources = new ResourceDictionary
        {
            { "unselectedCircleStyle", unselectedCircleStyle },
            { "selectedCircleStyle", selectedCircleStyle }
        };
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == ItemsSourceProperty.PropertyName)
        {
            var list = ItemsSource as IList;
            for (int i = 0; i < list.Count; i++)
            {
                AddStepButton(i + 1);

                if (i < list.Count - 1)
                {
                    AddSeparatorLine();
                }
            }
        }

        //if (propertyName == StepSelectedProperty.PropertyName)
        //{
        //    var children = headersStackLayout.Children.FirstOrDefault(p => (!string.IsNullOrEmpty(p.ClassId) && Convert.ToInt32(p.ClassId) == StepSelected));
        //    if (children != null) SelectElement(children as Button);
        //}

        if (propertyName == StepColorProperty.PropertyName)
        {
            AddStyles();
        }

		if (propertyName == WidthProperty.PropertyName)
		{
			RecalculateSeparatorLineWidth();
		}
	}

    private void AddItems(IList argsNewItems)
    {
        var count = (ItemsSource as IList).Count;

        if (count - argsNewItems.Count != 0)
        {
            AddSeparatorLine();
        }

        for (int i = 0; i < argsNewItems.Count; i++)
        {
            AddStepButton(i + count);

            if (i < argsNewItems.Count - 1)
            {
                AddSeparatorLine();
            }
        }
    }

    private void AddStepButton(int index)
    {
        var button = new Button()
        {
            Text = $"{index}",
            AutomationId = $"{index}",
            Style = Resources["unselectedCircleStyle"] as Style,
            CornerRadius = 20,
        };

        button.Clicked += Handle_Clicked;

        headersStackLayout.Children.Add(button);
		RecalculateSeparatorLineWidth();
	}

    private void AddSeparatorLine()
    {
        var separatorLine = new BoxView()
        {
            BackgroundColor = Colors.Silver,
            HeightRequest = 1,
            WidthRequest = 10,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            Margin = new Thickness(0),
        };
        headersStackLayout.Children.Add(separatorLine);

        RecalculateSeparatorLineWidth();
	}

    private void RecalculateSeparatorLineWidth()
    {
        List<BoxView> lines = headersStackLayout.Children.Where(item => item.GetType() == typeof(BoxView)).Select(item => (BoxView)item).ToList();
        var newWidth = (Width - (lines.Count + 1) * (45 + 2)) / lines.Count;
        if(newWidth < 15)
            newWidth = 15;

        lines.ForEach(item => item.WidthRequest = newWidth - 2);
	}

    #endregion

    private void Handle_Clicked(object sender, System.EventArgs e)
    {
        if (StepCanTouch)
            SelectElement(sender as Button);
    }

    private void SelectElement(Button elementSelected)
    {
        if (_lastStepSelected != null)
        {
            _lastStepSelected.Style = Resources["unselectedCircleStyle"] as Style;
        }

        elementSelected.Style = Resources["selectedCircleStyle"] as Style;

        var index = Convert.ToInt32(elementSelected.Text) - 1;
        var list = ItemsSource as IList;

        StepSelected = index;
        if (StepSelected >= 0)
        {
            element.BindingContext = list[StepSelected];
        }

        _lastStepSelected = elementSelected;
    }

    public void SelectElement(int index)
    {
        if (headersStackLayout.Children.FirstOrDefault(p => (Convert.ToInt32(p.AutomationId) == index + 1)) is Button children)
        {
            SelectElement(children);
        }
    }

    public void DeselectElement()
    {
        if (headersStackLayout.Children.FirstOrDefault(p => (Convert.ToInt32(p.AutomationId) == StepSelected + 1)) is Button children)
        {
            children.BackgroundColor = Colors.Green;
        }
    }

    public void SkipElement(bool isSkip = true)
    {
        var children = headersStackLayout.Children.FirstOrDefault(p => (Convert.ToInt32(p.AutomationId) == StepSelected + 1)) as Button;
        if (children != null)
        {
            children.BackgroundColor = isSkip ? Colors.DimGray : Colors.Transparent;
        }
    }

    public void NextElement(int index)
    {
        SelectElement(index);
    }

    private static void ItemsSource_OnPropertyChanged(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (newvalue != null)
        {
            if (bindable is StepProgressBar co)
            {
                co.SetTemplateElement();
                co.SelectElement(co.StepSelected);
            }

            var coll = newvalue as INotifyCollectionChanged;
            coll.CollectionChanged += ItemsSource_OnItemChanged;
        }

        if (oldvalue != null)
        {
            var coll = (INotifyCollectionChanged)oldvalue;
            coll.CollectionChanged -= ItemsSource_OnItemChanged;
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems != null) AddItems(args.NewItems);
        //if (args.OldItems != null) RemoveItems(args.OldItems);
    }

    private static event EventHandler<NotifyCollectionChangedEventArgs> CollectionChanged;
    private static void ItemsSource_OnItemChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(null, e);
    }

    private static void StepColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        StepProgressBar control = (StepProgressBar)bindable;
        var style = control.Resources["selectedCircleStyle"] as Style;
        var setter = style.Setters.First(item => item.Property.PropertyName == BackgroundColorProperty.PropertyName);
        setter.Value = newValue;
    }
}