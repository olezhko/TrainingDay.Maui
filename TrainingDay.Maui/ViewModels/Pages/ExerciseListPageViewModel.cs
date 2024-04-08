using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Messages;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class ExerciseListPageViewModel : BaseViewModel, IQueryAttributable
{
    List<int> selectedIndexes = new List<int>();
    public FilterModel Filter { get; set; }
    public ObservableCollection<ExerciseListItemViewModel> BaseItems { get; set; } // все упражнения из базы
    public ObservableCollection<ExerciseListItemViewModel> Items { get; set; } // элементы, отображенные на экране в списке
    public ObservableCollection<TrainingExerciseViewModel> ExistedExercises { get; set; }

    public ICommand ViewFilterWindowCommand { get; protected set; }

    public ICommand ChoseExercisesCommand { get; protected set; }

    public ICommand CancelCommand { get; protected set; }

    public INavigation Navigation { get; set; }

    public event EventHandler<List<ExerciseListItemViewModel>> ExercisesSelectFinished;

    public ExerciseListPageViewModel()
    {
        Filter = new FilterModel();
        ChoseExercisesCommand = new Command(ChoseExercises);
        ViewFilterWindowCommand = new Command(ViewFilterWindow);
        CancelCommand = new Command(Cancel);
        Items = new ObservableCollection<ExerciseListItemViewModel>();
        BaseItems = new ObservableCollection<ExerciseListItemViewModel>();
        UpdateItems();

        WeakReferenceMessenger.Default.Register<FilterAcceptedForExercisesMessage>(this, (r, m) =>
        {
            Filter = m.Filter;
            UpdateItems();
            Analytics.TrackEvent($"{GetType().Name}: FilterAcceptedForExercisesMessage");
        });
    }

    private async void Cancel()
    {
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Если элементы были выбраны путем поиска, а потом сбросили поиск, то оставить их выбранными
    /// </summary>
    /// <returns></returns>
    private List<ExerciseListItemViewModel> GetSelectedItems()
    {
        var resultItems = new List<ExerciseListItemViewModel>();
        resultItems.AddRange(BaseItems.Where(a => a.IsSelected).Select(x => x));
        resultItems.AddRange(Items.Where(a => a.IsSelected).Select(x => x));

        var selectedIds = resultItems.GroupBy(item => item.Id).Select(item => item.Key);
        resultItems = BaseItems.Where(item => selectedIds.Contains(item.Id)).ToList();

        foreach (var selectedIndex in selectedIndexes)
        {
            if (resultItems.All(item => item.Id != selectedIndex))
            {
                resultItems.Add(new ExerciseListItemViewModel(App.Database.GetExerciseItem(selectedIndex)));
            }
        }

        return resultItems;
    }

    public void UpdateItems()
    {
        LoadItemsAsync();
    }

    public ObservableCollection<ExerciseListItemViewModel> LoadItems()
    {
        var newItems = new ObservableCollection<ExerciseListItemViewModel>();
        var baseItems = App.Database.GetExerciseItems();
        try
        {
            FillSelectedIndexes();

            foreach (var exercise in baseItems)
            {
                var newItem = new ExerciseListItemViewModel(exercise);
                newItem.IsExerciseExistsInWorkout = ExistedExercises != null && ExistedExercises.Any(x => x.ExerciseId == exercise.Id);
                newItem.IsSelected = selectedIndexes.Contains(newItem.Id);
                bool? byname = null, byFilter = null, atHome = null;

                var tags = ExerciseTools.ConvertFromIntToTagList(exercise.TagsValue);

                if (Filter.IsNoEquipmentFilter)
                {
                    atHome = tags.Contains(ExerciseTags.CanDoAtHome);
                }

                if (!string.IsNullOrEmpty(Filter.NameFilter))
                {
                    byname = newItem.ExerciseItemName.Contains(Filter.NameFilter, StringComparison.OrdinalIgnoreCase);
                }

                if (Filter.CurrentMuscles.Count != 0)
                {
                    byFilter = newItem.Muscles.Select(i => (MusclesEnum)i.Id).Intersect(Filter.CurrentMuscles).Count() != 0;
                }

                if (byname.HasValue && !byname.Value || byFilter.HasValue && !byFilter.Value || atHome.HasValue && !atHome.Value)
                {
                    continue;
                }

                newItems.Add(newItem);
            }

            return newItems;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Crashes.TrackError(e);
            try
            {
                foreach (var exercise in baseItems)
                {
                    newItems.Add(new ExerciseListItemViewModel(exercise));
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Crashes.TrackError(e);
            }

            return newItems;
        }
    }

    private async void ViewFilterWindow()
    {
        Dictionary<string, object> param = new Dictionary<string, object> { { "Filter", Filter } };
        await Shell.Current.GoToAsync(nameof(FilterPage), param);
    }

    private async void ChoseExercises()
    {
        await Shell.Current.GoToAsync("..");
        WeakReferenceMessenger.Default.Send(new ExercisesSelectFinishedMessage(GetSelectedItems()));
    }

    private async void LoadItemsAsync()
    {
        var res = await Task.Run(() => LoadItems());
        Items = res;
        BaseItems = new ObservableCollection<ExerciseListItemViewModel>(res);
        OnPropertyChanged(nameof(Items));
    }

    private void FillSelectedIndexes()
    {
        foreach (var trainingExerciseViewModel in Items)
        {
            if (trainingExerciseViewModel.IsSelected && !selectedIndexes.Contains(trainingExerciseViewModel.Id))
            {
                selectedIndexes.Add(trainingExerciseViewModel.Id);
            }

            if (!trainingExerciseViewModel.IsSelected && selectedIndexes.Contains(trainingExerciseViewModel.Id))
            {
                selectedIndexes.Remove(trainingExerciseViewModel.Id);
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ExistedExercises = query["ExistedExercises"] as ObservableCollection<TrainingExerciseViewModel>;
    }
}