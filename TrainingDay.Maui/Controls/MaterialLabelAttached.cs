using TrainingDay.Maui.Extensions;

namespace TrainingDay.Maui.Controls
{
    public class MaterialLabelAttached
    {
        public static readonly BindableProperty IsMaterialBehaviorProperty =
            BindableProperty.CreateAttached("IsMaterialBehavior", typeof(bool), typeof(Label), false, propertyChanged: OnIsMaterialBehaviorPropertyChanged);
        public static bool GetIsMaterialBehavior(BindableObject view)
        {
            return (bool)view.GetValue(IsMaterialBehaviorProperty);
        }

        public static void SetIsMaterialBehavior(BindableObject view, bool value)
        {
            view.SetValue(IsMaterialBehaviorProperty, value);
        }

        public static readonly BindableProperty MaterialControlTypeProperty =
            BindableProperty.CreateAttached("MaterialControlType", typeof(MaterialControlTypes), typeof(Label), MaterialControlTypes.Entry);

        public static MaterialControlTypes GetMaterialControlType(BindableObject view)
        {
            return (MaterialControlTypes)view.GetValue(MaterialControlTypeProperty);
        }

        public static void SetMaterialControlType(BindableObject view, MaterialControlTypes value)
        {
            view.SetValue(MaterialControlTypeProperty, value);
        }

        private static void OnIsMaterialBehaviorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable == null)
            {
                return;
            }

            var label = (Label)bindable;
            label.Loaded += (sender, args) => Label_Loaded(sender, bindable);
            if (!(bool)newValue)
            {
                label.IsVisible = false;
                return;
            }
        }

        private static void ChangeActivate(Label label, bool isCollapse)
        {
            bool _isCollapsed = label.VerticalOptions == LayoutOptions.Start;
            if (_isCollapsed && isCollapse || !_isCollapsed && !isCollapse)
            {
                return;
            }

            Console.WriteLine("MaterialLabelAttached: Change state to: " + isCollapse);
            if (isCollapse)
            {
                label.VerticalOptions = LayoutOptions.Start;
                label.ScaleXTo(0.6, 250);
                label.ScaleYTo(0.6, 250);
            }
            else
            {
                label.VerticalOptions = LayoutOptions.Center;
                label.ScaleXTo(1, 250);
                label.ScaleYTo(1, 250);
            }
        }
        
        private static void Label_Loaded(object? sender, BindableObject bindable)
        {
            var label = (Label)sender;
            var parent = label.Parent as Grid;
            
            var type = GetMaterialControlType(bindable);
            switch (type)
            {
                case MaterialControlTypes.Editor:
                case MaterialControlTypes.Entry:
                    {
                        var entry = parent.Children.FirstOrDefault(item => (item as InputView) != null) as InputView;
                        if (entry.Text.IsNotNullOrEmpty())
                        {
                            ChangeActivate(label, true);
                        }

                        entry.Focused += (sender, args) =>
                        {
                            ControlFocusChanged(sender, args, label, () => string.IsNullOrEmpty(entry.Text));
                        };
                        entry.Unfocused += (sender, args) =>
                        {
                            ControlFocusChanged(sender, args, label, () => string.IsNullOrEmpty(entry.Text));
                        };
                        entry.TextChanged += (sender, args) =>
                        {
                            ChangeActivate(label, args.NewTextValue.IsNotNullOrEmpty());
                        };
                    }
                    break;
                case MaterialControlTypes.Picker:
                    {
                        var picker = parent.Children.FirstOrDefault(item => (item as Picker) != null) as Picker;
                        if (picker.SelectedIndex != -1)
                        {
                            ChangeActivate(label, true);
                        }

                        picker.Focused += (sender, args) =>
                        {
                            ControlFocusChanged(sender, args, label, () => picker.SelectedIndex == -1);
                        };
                        picker.Unfocused += (sender, args) =>
                        {
                            ControlFocusChanged(sender, args, label, () => picker.SelectedIndex == -1);
                        };

                        picker.SelectedIndexChanged += (sender, args) =>
                        {
                            ChangeActivate(label, picker.SelectedIndex != -1);
                        };
                    }
                    break;
            }
        }

        private static void ControlFocusChanged(object? sender, FocusEventArgs args, Label label, Func<bool> checkEmptyAction)
        {
            if (args.IsFocused && checkEmptyAction.Invoke())
            {
                ChangeActivate(label, true);
                return;
            }

            if (!args.IsFocused && checkEmptyAction.Invoke())
            {
                ChangeActivate(label, false);
                return;
            }
        }
    }

    public enum MaterialControlTypes
    {
        Entry,
        Picker,
        Editor
    }
}
