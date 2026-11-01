namespace TrainingDay.Maui.Controls;

public class TooltipControlContainer : Grid
{
    private sealed record HiddenChildState(double Opacity, bool InputTransparent);

    private List<ToolTipControl> nextToolTipControls = new List<ToolTipControl>();
    private readonly Dictionary<VisualElement, HiddenChildState> hiddenChildren = new Dictionary<VisualElement, HiddenChildState>();
    private int currentToolTip;

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

            if (nextToolTipControls.Count != 0) // if this one not first tool tip
            {
                _ = childToolTipControl.Hide(false); // hide previous tooltip
            }

            if (!childToolTipControl.NeverShow)
            {
                childToolTipControl.Closed += ChildToolTipControl_Closed;
                nextToolTipControls.Add(childToolTipControl);
            }

            if (nextToolTipControls.Count == 0)
            {
                BackgroundColor = Colors.Transparent;
                VisibleAllChild(true);
            }
            else
            {
                SetChildVisible(nextToolTipControls.First().AttachedControl, true);
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
            if (child is VisualElement visualElement && visualElement.GetType() != typeof(ToolTipControl))
            {
                SetChildVisible(visualElement, isVisible);
            }
        }
    }

    /// <summary>
    /// Hides a child behind the tooltip with Opacity + InputTransparent, never IsVisible:
    /// setting IsVisible from code would drop the child's own IsVisible binding or style trigger.
    /// </summary>
    internal void SetChildVisible(VisualElement? child, bool isVisible)
    {
        if (child == null)
        {
            return;
        }

        if (isVisible)
        {
            if (hiddenChildren.Remove(child, out var state))
            {
                child.Opacity = state.Opacity;
                child.InputTransparent = state.InputTransparent;
            }
        }
        else if (!hiddenChildren.ContainsKey(child))
        {
            hiddenChildren[child] = new HiddenChildState(child.Opacity, child.InputTransparent);
            child.Opacity = 0;
            child.InputTransparent = true;
        }
    }
}
