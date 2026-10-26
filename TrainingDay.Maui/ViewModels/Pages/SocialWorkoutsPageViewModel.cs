using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class SocialWorkoutsPageViewModel : BaseViewModel
{
    private const int PageSize = 10;

    private readonly ISocialWorkoutsService socialWorkoutsService;
    private bool feedOpenedTracked;
    private int page = 1;

    public SocialWorkoutsPageViewModel(ISocialWorkoutsService socialWorkoutsService)
    {
        this.socialWorkoutsService = socialWorkoutsService;
        Feed = new ObservableCollection<SocialWorkoutViewModel>();
        RefreshCommand = new AsyncRelayCommand(Refresh);
        RemainingItemsThresholdReachedCommand = new AsyncRelayCommand(LoadNextPage);
        ItemSelectedCommand = new AsyncRelayCommand<SocialWorkoutViewModel>(OpenDetail);
        LikeCommand = new AsyncRelayCommand<SocialWorkoutViewModel>(item => item?.ToggleLikeAsync(socialWorkoutsService) ?? Task.CompletedTask);
        GoToLoginCommand = new AsyncRelayCommand(() => Shell.Current.GoToAsync(nameof(LoginPage)));
    }

    public ObservableCollection<SocialWorkoutViewModel> Feed { get; }

    private bool isOffline;
    public bool IsOffline
    {
        get => isOffline;
        set => SetProperty(ref isOffline, value);
    }

    private bool isRefreshing;
    public bool IsRefreshing
    {
        get => isRefreshing;
        set => SetProperty(ref isRefreshing, value);
    }

    private bool isLoadingMore;
    public bool IsLoadingMore
    {
        get => isLoadingMore;
        set => SetProperty(ref isLoadingMore, value);
    }

    private bool isInitialLoading;
    /// <summary>
    /// True only while the feed is empty and the first page is loading — shows a spinner instead of the "no workouts" text.
    /// </summary>
    public bool IsInitialLoading
    {
        get => isInitialLoading;
        set => SetProperty(ref isInitialLoading, value);
    }

    public ICommand RefreshCommand { get; }

    public ICommand RemainingItemsThresholdReachedCommand { get; }

    public ICommand ItemSelectedCommand { get; }

    public ICommand LikeCommand { get; }

    public ICommand GoToLoginCommand { get; }

    public async void OnAppearing()
    {
        if (!feedOpenedTracked)
        {
            feedOpenedTracked = true;
        }

        LoggingService.TrackEvent("Feed Opened");
        if (Feed.Count == 0)
        {
            await Refresh();
        }
    }

    private async Task Refresh()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        IsInitialLoading = Feed.Count == 0;
        page = 1;

        try
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                var items = await socialWorkoutsService.GetFeedAsync(page, PageSize);
                var localExercises = App.Database.GetExerciseItems().ToList();

                Feed.Clear();
                foreach (var item in items)
                {
                    Feed.Add(new SocialWorkoutViewModel(item, localExercises));
                }

                IsOffline = false;
                App.Database.SaveSocialWorkoutCacheItems(items.Select(SocialWorkoutCacheEntity.FromDto));
            }
            else
            {
                LoadFromCache();
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
            LoadFromCache();
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
            IsInitialLoading = false;
        }
    }

    private void LoadFromCache()
    {
        Feed.Clear();
        var localExercises = App.Database.GetExerciseItems().ToList();
        var cached = App.Database.GetSocialWorkoutCacheItems();
        foreach (var item in cached)
        {
            Feed.Add(new SocialWorkoutViewModel(item.ToDto(), localExercises));
        }

        IsOffline = true;
    }

    private async Task LoadNextPage()
    {
        if (IsBusy || IsOffline)
        {
            return;
        }

        IsBusy = true;
        IsLoadingMore = true;
        try
        {
            var nextPage = page + 1;
            var items = await socialWorkoutsService.GetFeedAsync(nextPage, PageSize);
            if (items.Count > 0)
            {
                page = nextPage;
                var localExercises = App.Database.GetExerciseItems().ToList();
                foreach (var item in items)
                {
                    Feed.Add(new SocialWorkoutViewModel(item, localExercises));
                }
            }
        }
        catch (Exception ex)
        {
            LoggingService.TrackError(ex);
        }
        finally
        {
            IsBusy = false;
            IsLoadingMore = false;
        }
    }

    private async Task OpenDetail(SocialWorkoutViewModel selected)
    {
        if (selected == null)
        {
            return;
        }

        LoggingService.TrackEvent("Workout Viewed");

        Dictionary<string, object> param = new Dictionary<string, object> { { "Context", selected } };
        await Shell.Current.GoToAsync(nameof(SocialWorkoutDetailPage), param);
    }
}
