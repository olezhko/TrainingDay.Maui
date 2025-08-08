using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using UIKit;

namespace TrainingDay.Maui.Platforms.iOS;

internal class CustomShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
    {
        return new MyOtherTabBarAppearanceTracker();
    }

    public class MyOtherTabBarAppearanceTracker : ShellTabBarAppearanceTracker, IShellTabBarAppearanceTracker
    {
        public override void UpdateLayout(UITabBarController controller)
        {
            base.UpdateLayout(controller);
        }

        void IShellTabBarAppearanceTracker.SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);
            UITabBar tabBar = controller.TabBar;
            tabBar.BackgroundImage = new UIImage();
            tabBar.ClipsToBounds = true;
        }
    }
}
