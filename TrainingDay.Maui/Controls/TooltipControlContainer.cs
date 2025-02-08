using Microsoft.Maui.Graphics;

namespace TrainingDay.Maui.Controls;

public class TooltipControlContainer : Grid
{
    private List<ToolTipControl> nextToolTipControls = new List<ToolTipControl>();
    private int currentToolTip;
    private View lastAttachedView;

    public TooltipControlContainer()
    {
        BackgroundColor = Color.FromArgb("#CC000000");
    }

    protected override void OnChildAdded(Element child)
    {
        base.OnChildAdded(child);
        if (child is ToolTipControl childToolTipControl)
        {
            VisibleAllChild(false);

            if (nextToolTipControls.Count != 0)
            {
                childToolTipControl.Hide(false);
            }

            if (!childToolTipControl.NeverShow)
            {
                childToolTipControl.Closed += ChildToolTipControl_Closed;
                nextToolTipControls.Add(childToolTipControl);
                if (lastAttachedView != null)
                {
                    lastAttachedView.IsVisible = false;
                }

                lastAttachedView = childToolTipControl.AttachedControl;
                lastAttachedView.IsVisible = true;
            }

            if (nextToolTipControls.Count == 0)
            {
                BackgroundColor = Colors.Transparent;
                VisibleAllChild(true);
            }
        }
    }

    private void ChildToolTipControl_Closed(object sender, System.EventArgs e)
    {
        if (currentToolTip != -1)
        {
            var nextIndex = currentToolTip + 1;
            if (nextIndex < nextToolTipControls.Count)
            {
                VisibleAllChild(false);
                currentToolTip = nextIndex;
                nextToolTipControls[nextIndex]?.Show();
            }
            else
            {
                BackgroundColor = Colors.Transparent;
                VisibleAllChild(true);
            }
        }
    }

    private void VisibleAllChild(bool isVisible)
    {
        foreach (Element child in Children)
        {
            if (child is VisualElement visualElement)
            {
                if (child.GetType() != typeof(ToolTipControl) && !GetIgnoreVisible(child))
                {
                    child.SetValue(VisualElement.IsVisibleProperty, isVisible ? true : false);
                }
            }
        }
    }

    public static readonly BindableProperty IgnoreVisibleProperty =
        BindableProperty.CreateAttached("IgnoreVisible", typeof(bool), typeof(TooltipControlContainer), false);

    public static bool GetIgnoreVisible(BindableObject view)
    {
        return (bool)view.GetValue(IgnoreVisibleProperty);
    }

    public static void SetIgnoreVisible(BindableObject view, bool value)
    {
        view.SetValue(IgnoreVisibleProperty, value);
    }
}