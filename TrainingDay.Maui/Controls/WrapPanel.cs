using Microsoft.Maui.Controls.Compatibility;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TrainingDay.Common;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Controls;

public class WrapPanel : Layout<View>
{
    public event EventHandler<object> ItemTapped;
    private readonly Button _addButton;
    private readonly EnumBindablePicker<MusclesEnum> _picker = new EnumBindablePicker<MusclesEnum>();
    private bool _subscribed;
    private event EventHandler<NotifyCollectionChangedEventArgs> _collectionChanged;

    public WrapPanel()
    {
        _picker.IsVisible = false;
        _picker.WidthRequest = 1;
        _picker.HeightRequest = 1;
        _picker.Items.RemoveAt(_picker.Items.Count - 1);
        SubscribeEvents();

        _addButton = new Button()
        {
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Gray,
            TextColor = Colors.Orange,
            Padding = new Thickness(20, 0),
            CornerRadius = 20,
        };
        _addButton.Clicked += AddButton_Tapped;
    }

    private void SubscribeEvents()
    {
        if (!_subscribed)
        {
            _subscribed = true;
            _collectionChanged += OnCollectionChanged;
            _picker.SelectedIndexChanged += Picker_SelectedIndexChanged;
            _picker.Unfocused += Picker_Unfocused;
        }
    }

    #region Collection
    private static void ItemsSource_OnPropertyChanged(BindableObject bindable, IEnumerable oldvalue, IEnumerable newvalue)
    {
        if (oldvalue != null)
        {
            var co = bindable as WrapPanel;
            var coll = (INotifyCollectionChanged)oldvalue;
            coll.CollectionChanged -= ItemsSource_OnItemChanged(co);
            co?.RemoveItems();
        }

        if (newvalue != null)
        {
            var co = bindable as WrapPanel;
            var coll = newvalue as INotifyCollectionChanged;
            coll.CollectionChanged += ItemsSource_OnItemChanged(co);
            co?.AddItems(co.ItemsSource);
        }
    }

    private static NotifyCollectionChangedEventHandler ItemsSource_OnItemChanged(WrapPanel co)
    {
        return (sender, args) =>
        {
            co._collectionChanged?.Invoke(null, args);
        };
    }

    private void RemoveItems()
    {
        Children.Clear();
        if (IsEditableItems.HasValue && IsEditableItems.Value)
        {
            Children.Add(_addButton);
            Children.Add(_picker);
        }
    }

    private void AddItems(IEnumerable coll)
    {
        if (coll is ObservableCollection<MuscleViewModel> a)
        {
            if (a.Count == 0)
            {
                return;
            }
        }

        foreach (object item in coll)
        {
            var child = ItemTemplate.CreateContent() as View;
            if (child == null)
                return;

            var tap = new TapGestureRecognizer();
            tap.Tapped += Tap_Tapped;
            child.GestureRecognizers.Add(tap);

            child.BindingContext = item;
            Children.Add(child);
        }
    }

    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
    {
        RemoveItems();
        if (ItemsSource != null)
        {
            AddItems(ItemsSource);
        }
    }
    #endregion

    #region DrawItems
    /// <summary>
    /// This is called when the spacing or orientation properties are changed - it forces
    /// the control to go back through a layout pass.
    /// </summary>
    private void OnSizeChanged()
    {
        ForceLayout();
    }

    /// <summary>
    /// This method is called during the measure pass of a layout cycle to get the desired size of an element.
    /// </summary>
    /// <param name="widthConstraint">The available width for the element to use.</param>
    /// <param name="heightConstraint">The available height for the element to use.</param>
    protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
    {
        //_addButton.WidthRequest = widthConstraint;
        if (WidthRequest > 0)
            widthConstraint = Math.Min(widthConstraint, WidthRequest);
        if (HeightRequest > 0)
            heightConstraint = Math.Min(heightConstraint, HeightRequest);

        double internalWidth = double.IsPositiveInfinity(widthConstraint) ? double.PositiveInfinity : Math.Max(0, widthConstraint);
        double internalHeight = double.IsPositiveInfinity(heightConstraint) ? double.PositiveInfinity : Math.Max(0, heightConstraint);

        return Orientation == StackOrientation.Vertical
            ? DoVerticalMeasure(internalWidth, internalHeight)
                : DoHorizontalMeasure(internalWidth, internalHeight);
    }

    /// <summary>
    /// Does the vertical measure.
    /// </summary>
    /// <returns>The vertical measure.</returns>
    /// <param name="widthConstraint">Width constraint.</param>
    /// <param name="heightConstraint">Height constraint.</param>
    private SizeRequest DoVerticalMeasure(double widthConstraint, double heightConstraint)
    {
        int columnCount = 1;

        double width = 0;
        double height = 0;
        double minWidth = 0;
        double minHeight = 0;
        double heightUsed = 0;

        foreach (var item in Children)
        {
            var size = item.Measure(widthConstraint, heightConstraint);
            width = Math.Max(width, size.Request.Width);

            var newHeight = height + size.Request.Height + Spacing;
            if (newHeight > heightConstraint)
            {
                columnCount++;
                heightUsed = Math.Max(height, heightUsed);
                height = size.Request.Height;
            }
            else
                height = newHeight;

            minHeight = Math.Max(minHeight, size.Minimum.Height);
            minWidth = Math.Max(minWidth, size.Minimum.Width);
        }

        if (columnCount > 1)
        {
            height = Math.Max(height, heightUsed);
            width *= columnCount;  // take max width
        }

        return new SizeRequest(new Size(width, height), new Size(minWidth, minHeight));
    }

    /// <summary>
    /// Does the horizontal measure.
    /// </summary>
    /// <returns>The horizontal measure.</returns>
    /// <param name="widthConstraint">Width constraint.</param>
    /// <param name="heightConstraint">Height constraint.</param>
    private SizeRequest DoHorizontalMeasure(double widthConstraint, double heightConstraint)
    {
        int rowCount = 1;

        double width = 0;
        double height = 0;
        double minWidth = 0;
        double minHeight = 0;
        double widthUsed = 0;
        foreach (var item in Children.Where(c => c.IsVisible))
        {
            var size = item.Measure(widthConstraint, heightConstraint);
            height = Math.Max(height, size.Request.Height);

            var newWidth = width + size.Request.Width;
            if (newWidth > widthConstraint)
            {
                rowCount++;
                widthUsed = Math.Max(width, widthUsed);
                width = size.Request.Width;
            }
            else
            {
                width = newWidth + Spacing;
            }

            minHeight = Math.Max(minHeight, size.Minimum.Height);
            minWidth = Math.Max(minWidth, size.Minimum.Width);
        }

        if (rowCount > 1)
        {
            width = Math.Max(width, widthUsed);
            height = (height + Spacing) * rowCount - Spacing;
        }

        return new SizeRequest(new Size(width, height), new Size(minWidth, minHeight));
    }

    /// <summary>
    /// Positions and sizes the children of a Layout.
    /// </summary>
    /// <param name="x">A value representing the x coordinate of the child region bounding box.</param>
    /// <param name="y">A value representing the y coordinate of the child region bounding box.</param>
    /// <param name="width">A value representing the width of the child region bounding box.</param>
    /// <param name="height">A value representing the height of the child region bounding box.</param>
    protected override void LayoutChildren(double x, double y, double width, double height)
    {
        if (Orientation == StackOrientation.Vertical)
        {
            double colWidth = 0;
            double yPos = y, xPos = x;

            foreach (var child in Children.Where(c => c.IsVisible))
            {
                var request = child.Measure(width, height);

                double childWidth = request.Request.Width;
                double childHeight = request.Request.Height;
                colWidth = Math.Max(colWidth, childWidth);

                if (yPos + childHeight > height)
                {
                    yPos = y;
                    xPos += colWidth + Spacing;
                    colWidth = 0;
                }
                    
                var region = new Rect(xPos, yPos, childWidth, childHeight);
                LayoutChildIntoBoundingRegion(child, region);
                yPos += region.Height + Spacing;
            }
        }
        else
        {
            double rowHeight = 0, offset = 0;
            double yPos = y, xPos = x;
            var itemsInRow = new List<View>();
            foreach (var child in Children.Where(c => c.IsVisible))
            {
                var request = child.Measure(width, height);

                double childWidth = request.Request.Width;
                double childHeight = request.Request.Height;
                rowHeight = Math.Max(rowHeight, childHeight);
                if (xPos + childWidth > width)
                {
                    if (IsCenterItems)
                    {
                        offset = (width - xPos) / 2;
                        foreach (var view in itemsInRow)
                        {
                            LayoutChildIntoBoundingRegion(view, new Rect(view.X + offset, view.Y, view.Width, view.Height));
                        }
                    }

                    itemsInRow.Clear();

                    xPos = x;
                    yPos += rowHeight + Spacing;
                    rowHeight = 0;
                }

                itemsInRow.Add(child);

                var region = new Rect(xPos, yPos, childWidth, childHeight);
                LayoutChildIntoBoundingRegion(child, region);
                xPos += childWidth + Spacing;
            }

            if (IsCenterItems)
            {
                offset = (width - xPos) / 2;
                foreach (var view in itemsInRow)
                {
                    LayoutChildIntoBoundingRegion(view, new Rect(view.X + offset, view.Y, view.Width, view.Height));
                }
            }
        }
    }

    #endregion

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_picker.SelectedIndex >= 0)
        {
            var newMuscle = new MuscleViewModel((MusclesEnum)_picker.SelectedIndex);

            var items = (ObservableCollection<MuscleViewModel>)(ItemsSource as IList);
            if (!items.Any(a => a.Id == newMuscle.Id))
            {
                items.Add(newMuscle);
            }
        }
    }

    private void Picker_Unfocused(object sender, FocusEventArgs e)
    {
        _picker.IsVisible = false;
    }

    private void AddButton_Tapped(object sender, EventArgs e)
    {
        _picker.IsVisible = true;
        _picker.Focus();
    }

    private void Tap_Tapped(object sender, EventArgs e)
    {
        if (IsEditableItems.HasValue && IsEditableItems.Value)
        {
            try
            {
                var elem = (View)sender;
                var index = Children.IndexOf(elem);
                Children.Remove(elem);
                (ItemsSource as IList).RemoveAt(index - 2);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        ItemTapped?.Invoke(sender, e);
    }

    private void OnIsEditableItemsChanged()
    {
        if (IsEditableItems.HasValue)
        {
            _addButton.IsVisible = IsEditableItems.Value;
            _picker.IsVisible = IsEditableItems.Value;
        }
    }

    #region Dep
    public static BindableProperty IsEditableItemsProperty = BindableProperty.Create(nameof(IsEditableItems), typeof(bool?), typeof(WrapPanel), false, propertyChanged: (bindable, oldvalue, newvalue) => ((WrapPanel)bindable).OnIsEditableItemsChanged(), defaultBindingMode: BindingMode.TwoWay);
    public bool? IsEditableItems
    {
        get { return (bool?)GetValue(IsEditableItemsProperty); }
        set { SetValue(IsEditableItemsProperty, value); }
    }

    public static BindableProperty IsCenterItemsProperty = BindableProperty.Create(nameof(IsCenterItems), typeof(bool), typeof(WrapPanel), false);
    public bool IsCenterItems
    {
        get { return (bool)GetValue(IsCenterItemsProperty); }
        set { SetValue(IsCenterItemsProperty, value); }
    }

    public static BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(WrapPanel), StackOrientation.Vertical, propertyChanged: (bindable, oldvalue, newvalue) => ((WrapPanel)bindable).OnSizeChanged(), defaultBindingMode: BindingMode.TwoWay);
    /// <summary>
    /// Orientation (Horizontal or Vertical)
    /// </summary>
    public StackOrientation Orientation
    {
        get { return (StackOrientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    public static BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double), typeof(WrapPanel), 2.0, propertyChanged: (bindable, oldvalue, newvalue) => ((WrapPanel)bindable).OnSizeChanged(), defaultBindingMode: BindingMode.TwoWay);
    public double Spacing
    {
        get { return (double)GetValue(SpacingProperty); }
        set { SetValue(SpacingProperty, value); }
    }

    public static BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(WrapPanel), null, propertyChanged: (bindable, oldvalue, newvalue) => ((WrapPanel)bindable).OnSizeChanged(), defaultBindingMode: BindingMode.TwoWay);
    public DataTemplate ItemTemplate
    {
        get { return (DataTemplate)GetValue(ItemTemplateProperty); }
        set { SetValue(ItemTemplateProperty, value); }
    }

    public static BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(WrapPanel), null, propertyChanged: (bindable, oldvalue, newvalue) => ItemsSource_OnPropertyChanged(bindable, (IEnumerable)oldvalue, (IEnumerable)newvalue), defaultBindingMode: BindingMode.TwoWay);
    public IEnumerable ItemsSource
    {
        get { return (IEnumerable)GetValue(ItemsSourceProperty); }
        set { SetValue(ItemsSourceProperty, value); }
    }

    public string AddContent
    {
        get => _addButton.Text;
        set => _addButton.Text = value;
    }
    #endregion
}