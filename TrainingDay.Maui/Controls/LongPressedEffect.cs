using Microsoft.Maui.Controls.Platform;
using System.Windows.Input;

namespace TrainingDay.Maui.Controls;

public class LongPressedEffect : RoutingEffect
{
    public static readonly BindableProperty ClickCommandProperty = BindableProperty.CreateAttached("ClickCommand", typeof(ICommand), typeof(LongPressedEffect), null);

    public static ICommand GetClickCommand(BindableObject view)
    {
        return (ICommand)view.GetValue(ClickCommandProperty);
    }

    public static void SetClickCommand(BindableObject view, ICommand value)
    {
        view.SetValue(ClickCommandProperty, value);
    }

    public static readonly BindableProperty CommandProperty = BindableProperty.CreateAttached("Command", typeof(ICommand), typeof(LongPressedEffect), null);

    public static ICommand GetCommand(BindableObject view)
    {
        return (ICommand)view.GetValue(CommandProperty);
    }

    public static void SetCommand(BindableObject view, ICommand value)
    {
        view.SetValue(CommandProperty, value);
    }

    public static readonly BindableProperty CommandParameterProperty = BindableProperty.CreateAttached("CommandParameter", typeof(object), typeof(LongPressedEffect), null);

    public static object GetCommandParameter(BindableObject view)
    {
        return view.GetValue(CommandParameterProperty);
    }

    public static void SetCommandParameter(BindableObject view, object value)
    {
        view.SetValue(CommandParameterProperty, value);
    }
}

public class LongPressedPlatformEffect : PlatformEffect
{
    private bool _attached;

    public static void Initialize() { }

    public LongPressedPlatformEffect()
    {
    }

    protected override void OnAttached()
    {
        //because an effect can be detached immediately after attached (happens in listview), only attach the handler one time.
        if (!_attached)
        {
#if ANDROID
            if (Control != null)
            {
                Control.LongClickable = true;
                Control.LongClick += Control_LongClick;
                Control.Click += Control_Click;
            }
            else
            {
                Container.LongClickable = true;
                Container.LongClick += Control_LongClick;
                Container.Click += Control_Click;
            }
#endif
            _attached = true;
        }
    }

    private void Control_Click(object sender, EventArgs e)
    {
        Console.WriteLine("Invoking click command");
        var command = LongPressedEffect.GetClickCommand(Element);
        command?.Execute(LongPressedEffect.GetCommandParameter(Element));
    }

#if ANDROID
    private void Control_LongClick(object sender, Android.Views.View.LongClickEventArgs e)
    {
        Console.WriteLine("Invoking long click command");
        var command = LongPressedEffect.GetCommand(Element);
        command?.Execute(LongPressedEffect.GetCommandParameter(Element));
    }
#endif

    protected override void OnDetached()
    {
        if (_attached)
        {
#if ANDROID
            if (Control != null)
            {
                Control.LongClickable = true;
                Control.LongClick -= Control_LongClick;
                Control.Click -= Control_Click;
            }
            else
            {
                Container.LongClickable = true;
                Container.LongClick -= Control_LongClick;
                Container.Click -= Control_Click;
            }
#endif
            _attached = false;
        }
    }
}
//#elif __IOS__
//internal class LongPressedPlatformEffect : PlatformEffect
//{
//        private bool _attached;
//        private readonly UILongPressGestureRecognizer _longPressRecognizer;
//        private readonly UITapGestureRecognizer gestureRecognizer;
//        public LongPressedPlatformEffect()
//        {
//            _longPressRecognizer = new UILongPressGestureRecognizer(HandleLongClick);
//            gestureRecognizer = new UITapGestureRecognizer(HandleTapClick);
//        }

//        /// <summary>
//        /// Apply the handler
//        /// </summary>
//        protected override void OnAttached()
//        {
//            //because an effect can be detached immediately after attached (happens in listview), only attach the handler one time
//            if (!_attached)
//            {
//                Container.AddGestureRecognizer(_longPressRecognizer);
//                Container.AddGestureRecognizer(gestureRecognizer);
//                _attached = true;
//            }
//        }
//        /// <summary>
//        /// Invoke the command if there is one
//        /// </summary>
//        private void HandleTapClick()
//        {
//            var command = LongPressedEffect.GetClickCommand(Element);
//            command?.Execute(LongPressedEffect.GetCommandParameter(Element));
//        }
//        /// <summary>
//        /// Invoke the command if there is one
//        /// </summary>
//        private void HandleLongClick()
//        {
//            var command = LongPressedEffect.GetCommand(Element);
//            command?.Execute(LongPressedEffect.GetCommandParameter(Element));
//        }

//        /// <summary>
//        /// Clean the event handler on detach
//        /// </summary>
//        protected override void OnDetached()
//        {
//            if (_attached)
//            {
//                Container.RemoveGestureRecognizer(_longPressRecognizer);
//                Container.RemoveGestureRecognizer(gestureRecognizer);
//                _attached = false;
//            }
//        }
//}
