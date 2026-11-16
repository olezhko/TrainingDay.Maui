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

        // Holds the single set of handlers for one label so they can be unsubscribed again.
        private static readonly BindableProperty MaterialLabelStateProperty =
            BindableProperty.CreateAttached("MaterialLabelState", typeof(MaterialLabelState), typeof(Label), null);

        private static void OnIsMaterialBehaviorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not Label label)
            {
                return;
            }

            if (label.GetValue(MaterialLabelStateProperty) is MaterialLabelState oldState)
            {
                oldState.Dispose();
                label.ClearValue(MaterialLabelStateProperty);
            }

            if (!(bool)newValue)
            {
                label.IsVisible = false;
                return;
            }

            var state = new MaterialLabelState(label);
            label.SetValue(MaterialLabelStateProperty, state);
            if (label.IsLoaded)
            {
                state.AttachToControl();
            }
        }

        private static async void ChangeActivate(Label label, bool isCollapse, bool isVisible = true)
        {
            label.IsVisible = isVisible;
            bool _isCollapsed = label.VerticalOptions == LayoutOptions.Start;
            if (_isCollapsed && isCollapse || !_isCollapsed && !isCollapse)
            {
                return;
            }

            if (isCollapse)
            {
                label.VerticalOptions = LayoutOptions.Start;
                await Task.WhenAll(label.ScaleXToAsync(0.6, 250), label.ScaleYToAsync(0.6, 250));
                label.Opacity = 1;
#if IOS
                label.Margin = new Thickness(5, -10, 5, 0);
#endif

#if ANDROID
                label.Margin = new Thickness(5, -5, 5, 0);
#endif
            }
            else
            {
                label.VerticalOptions = LayoutOptions.Center;
                await Task.WhenAll(label.ScaleXToAsync(1, 250), label.ScaleYToAsync(1, 250));
                label.Opacity = 0.5;
                label.Margin = new Thickness(5, 0, 5, 0);
            }
        }

        private static void ControlFocusChanged(FocusEventArgs args, Label label, bool isEmpty)
        {
            if (!isEmpty)
            {
                return;
            }

            ChangeActivate(label, args.IsFocused);
        }

        /// <summary>
        /// Per-label subscriptions. Label Loaded/Unloaded are subscribed once for the lifetime of the
        /// behavior; the sibling input control is attached on Loaded and detached on Unloaded, so
        /// re-loading the page (or toggling the attached property) never stacks handlers.
        /// </summary>
        private sealed class MaterialLabelState : IDisposable
        {
            private readonly Label label;
            private InputView? inputView;
            private Picker? picker;

            public MaterialLabelState(Label label)
            {
                this.label = label;
                label.Loaded += Label_Loaded;
                label.Unloaded += Label_Unloaded;
            }

            public void AttachToControl()
            {
                DetachFromControl();

                if (label.Parent is not Grid parent)
                {
                    return;
                }

                switch (GetMaterialControlType(label))
                {
                    case MaterialControlTypes.Editor:
                    case MaterialControlTypes.Entry:
                        inputView = parent.Children.OfType<InputView>().FirstOrDefault();
                        if (inputView is null)
                        {
                            return;
                        }

                        if (inputView.Text.IsNotNullOrEmpty())
                        {
                            ChangeActivate(label, true);
                        }

                        inputView.Focused += InputView_FocusChanged;
                        inputView.Unfocused += InputView_FocusChanged;
                        inputView.TextChanged += InputView_TextChanged;
                        break;
                    case MaterialControlTypes.Picker:
                        picker = parent.Children.OfType<Picker>().FirstOrDefault();
                        if (picker is null)
                        {
                            return;
                        }

                        if (picker.SelectedIndex != -1)
                        {
                            ChangeActivate(label, true);
                        }
                        else
                        {
                            ChangeActivate(label, false, isVisible: false);
                        }

                        picker.Focused += Picker_FocusChanged;
                        picker.Unfocused += Picker_FocusChanged;
                        picker.SelectedIndexChanged += Picker_SelectedIndexChanged;
                        break;
                    case MaterialControlTypes.None:
                        ChangeActivate(label, true);
                        break;
                }
            }

            public void Dispose()
            {
                label.Loaded -= Label_Loaded;
                label.Unloaded -= Label_Unloaded;
                DetachFromControl();
            }

            private void DetachFromControl()
            {
                if (inputView != null)
                {
                    inputView.Focused -= InputView_FocusChanged;
                    inputView.Unfocused -= InputView_FocusChanged;
                    inputView.TextChanged -= InputView_TextChanged;
                    inputView = null;
                }

                if (picker != null)
                {
                    picker.Focused -= Picker_FocusChanged;
                    picker.Unfocused -= Picker_FocusChanged;
                    picker.SelectedIndexChanged -= Picker_SelectedIndexChanged;
                    picker = null;
                }
            }

            private void Label_Loaded(object? sender, EventArgs e) => AttachToControl();

            private void Label_Unloaded(object? sender, EventArgs e) => DetachFromControl();

            private void InputView_FocusChanged(object? sender, FocusEventArgs e) =>
                ControlFocusChanged(e, label, string.IsNullOrEmpty(inputView?.Text));

            private void InputView_TextChanged(object? sender, TextChangedEventArgs e) =>
                ChangeActivate(label, e.NewTextValue.IsNotNullOrEmpty());

            private void Picker_FocusChanged(object? sender, FocusEventArgs e) =>
                ControlFocusChanged(e, label, picker?.SelectedIndex == -1);

            private void Picker_SelectedIndexChanged(object? sender, EventArgs e) =>
                ChangeActivate(label, picker?.SelectedIndex != -1);
        }
    }

    public enum MaterialControlTypes
    {
        Entry,
        Picker,
        Editor,

        None
    }
}
