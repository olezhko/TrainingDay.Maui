using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Controls.PopUp
{
    public static class PopupBuilders
    {
        // Single-message popup with an OK button; same look as the other popups built here
        internal static View BuildAdvicePopup(string text)
        {
            var titleLabel = new Label
            {
                Text = AppResources.AdviceString,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            SemanticProperties.SetHeadingLevel(titleLabel, SemanticHeadingLevel.Level1);

            var messageLabel = new Label
            {
                Text = text,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Start,
            };

            var okButton = new Button
            {
                Text = AppResources.OkString,
                FontSize = 15,
                HeightRequest = 44,
                Style = App.Current.Resources["ActionButton"] as Style,
            };

            // Closing twice throws, so ignore taps after the first one
            bool isClosing = false;
            okButton.Clicked += async (s, e) =>
            {
                if (isClosing)
                    return;

                isClosing = true;
                await Shell.Current.ClosePopupAsync();
            };

            return new VerticalStackLayout
            {
                Spacing = 12,
                Padding = new Thickness(20),
                WidthRequest = 320,
                Style = App.Current.Resources["PopupLayout"] as Style,
                Children =
                {
                    titleLabel,
                    // Long advice texts scroll instead of pushing the OK button off screen
                    new ScrollView { MaximumHeightRequest = 340, Content = messageLabel },
                    okButton,
                },
            };
        }

        // Shape shared by the popups built here, matches MoveOrCopyExercisesPopup
        internal static PopupOptions DefaultPopupOptions => new()
        {
            Shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(20),
                StrokeThickness = 0,
            },
        };

        // Destination picker for "add training to group": one tap on an existing group, or create a new one.
        // Layout mirrors MoveOrCopyExercisesPopup so both group/move pickers look and behave the same.
        internal static View BuildWorkoutGroupsPopup(
            IReadOnlyList<TrainingUnionEntity> trainingsGroups,
            Func<TrainingUnionEntity, Task> acceptGroup,
            Func<Task> createNewGroup)
        {
            // Every action closes the popup, so ignore taps that arrive while one is already running
            bool isBusy = false;
            async Task RunOnce(Func<Task> action)
            {
                if (isBusy)
                    return;

                isBusy = true;
                try
                {
                    await action();
                }
                finally
                {
                    isBusy = false;
                }
            }

            var titleLabel = new Label
            {
                Text = AppResources.TrainingToGroupString,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            SemanticProperties.SetHeadingLevel(titleLabel, SemanticHeadingLevel.Level1);

            var descriptionLabel = new Label
            {
                Text = AppResources.WorkoutGroupDescriptionString,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
            };
            descriptionLabel.SetAppThemeColor(Label.TextColorProperty,
                App.Current.Resources["Gray500"] as Color, App.Current.Resources["Gray300"] as Color);

            var newButton = new Button
            {
                Text = AppResources.CreateNewString,
                FontSize = 15,
                HeightRequest = 44,
                Style = App.Current.Resources["ActionButton"] as Style,
            };
            newButton.Clicked += async (s, e) => await RunOnce(createNewGroup);

            var cancelButton = new Button
            {
                Text = AppResources.CancelString,
                FontSize = 15,
                HeightRequest = 44,
                Style = App.Current.Resources["SecondActionButton"] as Style,
            };
            cancelButton.Clicked += async (s, e) => await RunOnce(() => Shell.Current.ClosePopupAsync());

            var verticalStack = new VerticalStackLayout
            {
                Spacing = 12,
                Padding = new Thickness(20),
                WidthRequest = 320,
                Style = App.Current.Resources["PopupLayout"] as Style,
                Children = { titleLabel, descriptionLabel },
            };

            if (trainingsGroups.Count != 0)
            {
                var groupsList = new VerticalStackLayout { Spacing = 8 };
                foreach (var group in trainingsGroups)
                {
                    groupsList.Children.Add(BuildGroupRow(group, () => RunOnce(() => acceptGroup(group))));
                }

                verticalStack.Children.Add(new ScrollView
                {
                    MaximumHeightRequest = 340,
                    Content = groupsList,
                });
                verticalStack.Children.Add(BuildOrSeparator());
            }

            verticalStack.Children.Add(newButton);
            verticalStack.Children.Add(cancelButton);

            return verticalStack;
        }

        private static Border BuildGroupRow(TrainingUnionEntity group, Func<Task> onTapped)
        {
            var row = new Border
            {
                Style = App.Current.Resources["BorderCellStyle"] as Style,
                Padding = new Thickness(14, 0),
                MinimumHeightRequest = 50,
                Content = new Label
                {
                    Text = group.Name,
                    TextTransform = TextTransform.Uppercase,
                    FontSize = 16,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    MaxLines = 2,
                    LineBreakMode = LineBreakMode.TailTruncation,
                },
            };
            SemanticProperties.SetDescription(row, group.Name);

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) => await onTapped();
            row.GestureRecognizers.Add(tap);

            return row;
        }

        private static Grid BuildOrSeparator()
        {
            var lineLight = App.Current.Resources["Gray200"] as Color;
            var lineDark = App.Current.Resources["Gray500"] as Color;

            var leftLine = new BoxView { HeightRequest = 1, VerticalOptions = LayoutOptions.Center };
            leftLine.SetAppThemeColor(BoxView.ColorProperty, lineLight, lineDark);

            var rightLine = new BoxView { HeightRequest = 1, VerticalOptions = LayoutOptions.Center };
            rightLine.SetAppThemeColor(BoxView.ColorProperty, lineLight, lineDark);

            var orLabel = new Label
            {
                Text = AppResources.Or,
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center,
            };
            orLabel.SetAppThemeColor(Label.TextColorProperty,
                App.Current.Resources["Gray500"] as Color, App.Current.Resources["Gray300"] as Color);

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 10,
            };
            grid.Add(leftLine, 0);
            grid.Add(orLabel, 1);
            grid.Add(rightLine, 2);

            return grid;
        }
    }
}
