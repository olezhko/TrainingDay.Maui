using System.Collections.ObjectModel;
using Microsoft.Maui.Controls.Shapes;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;

namespace TrainingDay.Maui.Controls
{
    public static class PopupBuilders
    {
        internal static View BuildWorkoutGroupsPopup(
            Action<TrainingUnionEntity> acceptGroup,
            Action createNewGroup)
        {
            var newButton = new Button
            {
                Text = AppResources.CreateNewString,
                FontSize = 15,
                CornerRadius = 20,
                BackgroundColor = App.Current.RequestedTheme == AppTheme.Light ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"),
                TextColor = App.Current.RequestedTheme != AppTheme.Light ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000")
            };
            newButton.Clicked += (s, e) =>
            {
                createNewGroup();
            };
            
            var verticalStack = new VerticalStackLayout 
            {
                Spacing = 15,
                Padding = new Thickness(20),
                Children =
                {
                    newButton
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
                FontSize = 16,
                ItemDisplayBinding = new Binding()
                {
                    Path = "Name"
                },
                HorizontalOptions = LayoutOptions.Fill,
                TextColor = App.Current.RequestedTheme == AppTheme.Light ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000")
            };

            var acceptPickerButton = new Button()
            {
                Text = AppResources.SelectString,
                FontSize = 15,
                CornerRadius = 20,
                TextColor = Colors.White,
                Style = App.Current.Resources["ActionButton"] as Style,
                IsEnabled = false,
            };

            acceptPickerButton.Clicked += (s,e) =>
            {
                var selectedGroup = selectPicker.SelectedItem as TrainingUnionEntity;
                acceptGroup(selectedGroup);
            };
            selectPicker.SelectedIndexChanged += (_, __) =>
            {
                acceptPickerButton.IsEnabled = selectPicker.SelectedItem != null;
            };

            var headImage = new Image()
            {
                Source = "workouts.png",
                HeightRequest = 20,
                WidthRequest = 20,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.Start
            };

            var dropImage = new Image()
            {
                Source = "arrow_left.png",
                HeightRequest = 20,
                WidthRequest = 20,
                BackgroundColor = Colors.Transparent,
                HorizontalOptions = LayoutOptions.End,
                Rotation = 90,
            };

            var horLayout = new Grid()
            {
                ColumnDefinitions = new ColumnDefinitionCollection(new()
                {
                    Width = 20
                }, 
                new (), 
                new ()
                {
                    Width = 30
                }),
                ColumnSpacing = 10,
                Children =
                {
                    headImage,
                    dropImage,
                    selectPicker
                },
            };

            Grid.SetColumn(dropImage, 2);
            Grid.SetColumn(selectPicker, 1);

            var pickerBorder = new Border()
            {
                Stroke = App.Current.RequestedTheme == AppTheme.Light ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#000000"),
                StrokeShape = new RoundRectangle()
                {
                    CornerRadius = 20
                },
                Padding = new Thickness(10,5),
                Content = horLayout
            };

            verticalStack.Children.Insert(0, pickerBorder);
            verticalStack.Children.Insert(1, acceptPickerButton);
            verticalStack.Children.Insert(2, new Border()
            {
                WidthRequest = 50,
                HeightRequest = 2,
                Stroke = Colors.Gray
            });

            return verticalStack;
        }
    }
}
