using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Controls
{
    public static class PopupBuilders
    {
        internal static View BuildAdvicePopup(string text)
        {
            var titleLabel = new Label
            {
                Text = AppResources.AdviceString,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };

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
                CornerRadius = 20,
                Style = App.Current.Resources["ActionButton"] as Style,
            };
            okButton.Clicked += async (s, e) =>
            {
                await Shell.Current.ClosePopupAsync();
            };

            return new VerticalStackLayout
            {
                Spacing = 15,
                Padding = new Thickness(20),
                Children = { titleLabel, messageLabel, okButton },
            };
        }

        internal static View BuildWorkoutGroupsPopup(
            Action<TrainingUnionEntity> acceptGroup,
            Action createNewGroup)
        {
            var backgroundColor = App.Current.RequestedTheme == AppTheme.Light
                ? App.Current.Resources["ContentPageBackgroundColorLight"] as Color
                : App.Current.Resources["ContentPageBackgroundColor"] as Color;

            var titleLabel = new Label
            {
                Text = AppResources.TrainingToGroupString,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
            };

            var descriptionLabel = new Label
            {
                Text = AppResources.WorkoutGroupDescriptionString,
                FontSize = 14,
                HorizontalTextAlignment = TextAlignment.Center,
            };

            var cancelButton = new Button
            {
                Text = AppResources.CancelString,
                FontSize = 15,
                CornerRadius = 20,
                Style = App.Current.Resources["SecondActionButton"] as Style,
            };
            cancelButton.Clicked += async (s, e) =>
            {
                await Shell.Current.ClosePopupAsync();
            };

            var newButton = new Button
            {
                Text = AppResources.CreateNewString,
                FontSize = 15,
                CornerRadius = 20,
            };
            newButton.Clicked += (s, e) =>
            {
                createNewGroup();
            };

            var verticalStack = new VerticalStackLayout
            {
                Spacing = 15,
                Padding = new Thickness(20),
                BackgroundColor = backgroundColor,
                Children =
                {
                    titleLabel,
                    descriptionLabel,
                    newButton,
                    cancelButton,
                }
            };

            var trainingsGroups = new ObservableCollection<TrainingUnionEntity>(App.Database.GetTrainingsGroups());
            if (!trainingsGroups.Any())
            {
                return verticalStack;
            }

            Picker selectPicker = new()
            {
                Title = AppResources.SelectGroup,
                ItemsSource = trainingsGroups,
                FontSize = 14,
                ItemDisplayBinding = new Binding { Path = "Name" },
                HorizontalOptions = LayoutOptions.Fill,
            };

            var acceptPickerButton = new Button
            {
                Text = AppResources.SelectString,
                FontSize = 15,
                CornerRadius = 20,
                TextColor = Colors.White,
                Style = App.Current.Resources["ActionButton"] as Style,
                IsEnabled = false,
            };

            acceptPickerButton.Clicked += (s, e) =>
            {
                var selectedGroup = selectPicker.SelectedItem as TrainingUnionEntity;
                acceptGroup(selectedGroup);
            };
            selectPicker.SelectedIndexChanged += (_, __) =>
            {
                acceptPickerButton.IsEnabled = selectPicker.SelectedItem != null;
            };

            var pickerBorder = new Border
            {
                Stroke = App.Current.RequestedTheme != AppTheme.Light ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#919191"),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(10, 5),
                Content = selectPicker,
            };

            verticalStack.Children.Insert(2, pickerBorder);
            verticalStack.Children.Insert(3, acceptPickerButton);

            return verticalStack;
        }
    }
}
