using CommunityToolkit.Maui.Alerts;
using System.Collections.ObjectModel;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Views;

public partial class TrainingsSetGroupPage : ContentPage
{
    public TrainingsSetGroupPage()
    {
        InitializeComponent();
        var trainingsGroups = new ObservableCollection<TrainingUnion>(App.Database.GetTrainingsGroups());
        if (!trainingsGroups.Any())
        {
            GroupContainer.IsVisible = false;
        }
        GroupsPicker.ItemsSource = trainingsGroups;
    }

    public TrainingViewModel TrainingMoveToGroup { get; set; }

    private async void ShowNewGroupWnd_Click(object sender, EventArgs e)
    {
        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} AddToGroup with new group STARTED");
        string result = await DisplayPromptAsync(string.Empty, AppResources.GroupingEnterNameofGroup, AppResources.OkString, AppResources.CancelString, placeholder: AppResources.NameString);
        if (result != null)
        {
            try
            {
                var name = result;
                if (!string.IsNullOrEmpty(name))
                {
                    var unions = App.Database.GetTrainingsGroups();
                    DeleteGroup(new List<TrainingUnion>(unions));
                    var union = unions.FirstOrDefault(un => un.Name == name);
                    if (union != null) // если группа с таким именем уже существует
                    {
                        var viewModel = new TrainingUnionViewModel(union);
                        if (!viewModel.TrainingIDs.Contains(TrainingMoveToGroup.Id))
                        {
                            viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);// добавляем в список тренировок у группы выбранную тренировку
                            TrainingMoveToGroup.GroupName = viewModel;
                            App.Database.SaveTrainingGroup(viewModel.Model);
                        }
                    }
                    else
                    {
                        var viewModel = new TrainingUnionViewModel();//новая группа
                        viewModel.Name = name;
                        viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);
                        TrainingMoveToGroup.GroupName = viewModel;
                        App.Database.SaveTrainingGroup(viewModel.Model);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.TrackError(ex);
            }
        }

        LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} AddToGroup with new group FINISHED");

        await Toast.Make(AppResources.SavedString).Show();

        await Navigation.PopAsync();
    }

    private void DeleteGroup(List<TrainingUnion> unions)
    {
        try
        {
            if (TrainingMoveToGroup.GroupName != null && TrainingMoveToGroup.GroupName.Id != 0)
            {
                var unionToEdit = new TrainingUnionViewModel(unions.First(u => u.Id == TrainingMoveToGroup.GroupName.Id));
                unionToEdit.TrainingIDs.Remove(TrainingMoveToGroup.Id);
                if (unionToEdit.TrainingIDs.Count != 0)
                    App.Database.SaveTrainingGroup(unionToEdit.Model);
                else
                    App.Database.DeleteTrainingGroup(unionToEdit.Id);
            }
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }
    }

    private void GroupSelected(int id)
    {
        try
        {
            //  если тренировка уже в группе и группа такая же, как и выбрали
            if (TrainingMoveToGroup.GroupName != null && id == TrainingMoveToGroup.GroupName.Id)
            {
                Navigation.PopAsync();
                return;
            }

            var unions = App.Database.GetTrainingsGroups();
            // если тренировка в группе и группа не "Основные"
            if (TrainingMoveToGroup.GroupName != null && TrainingMoveToGroup.GroupName.Id != 0)
            {
                var unionToEdit = unions.First(u => u.Id == TrainingMoveToGroup.GroupName.Id);
                var vm = new TrainingUnionViewModel(unionToEdit);
                vm.TrainingIDs.Remove(TrainingMoveToGroup.Id);
                if (vm.TrainingIDs.Count != 0)
                    App.Database.SaveTrainingGroup(vm.Model);
                else
                    App.Database.DeleteTrainingGroup(vm.Id);
            }

            if (id != 0)
            {
                var union = unions.FirstOrDefault(un => un.Id == id);
                if (union != null)
                {
                    var viewModel = new TrainingUnionViewModel(union);
                    viewModel.TrainingIDs.Add(TrainingMoveToGroup.Id);
                    TrainingMoveToGroup.GroupName = viewModel;
                    App.Database.SaveTrainingGroup(viewModel.Model);
                }
            }

            LoggingService.TrackEvent($"{GetType().Name.Replace("PageViewModel", string.Empty)} AddToGroup finsihed");
            Toast.Make(AppResources.SavedString).Show();
        }
        catch (Exception e)
        {
            LoggingService.TrackError(e);
        }

        Navigation.PopAsync();
    }

    private void GroupsPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedGroupToTraining = GroupsPicker.SelectedItem as TrainingUnion;
        GroupSelected(selectedGroupToTraining.Id);
    }
}