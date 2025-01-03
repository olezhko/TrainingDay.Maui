using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class BlogsPageViewModel : BaseViewModel
{
    private Command newBlogLoadCommand;
    private Command refreshCommand;
    private Command openBlogCommand;
    private string? nextPageToken = null;
    int page = 0;

    public BlogsPageViewModel()
    {
        BlogsCollection = new ObservableCollection<BlogViewModel>();
    }

    public async void LoadItems()
    {
        Page = 0;
        if (IsBusy)
        {
            return;
        }

        if (BlogsCollection.Count != 0)
        {
            BlogsCollection.Clear();
        }

        IsBusy = true;

        var res = await SiteService.GetBlogsAsync();
        if (res != null)
        {
            nextPageToken = res.NextPageToken;

            BlogsCollection = new ObservableCollection<BlogViewModel>(res.Items.Where(CheckLanguage).Select(item => new BlogViewModel(item)).OrderByDescending(item => item.DateTime));
            OnPropertyChanged(nameof(BlogsCollection));

            IsBusy = false;
        }
    }

    private bool CheckLanguage(BlogResponse response)
    {
        if (response.Labels is not null)
        {
            if (Settings.GetLanguage().TwoLetterISOLanguageName.ToLower() is "en" or "de")
                return response.Labels.Contains("en");

            if (Settings.GetLanguage().TwoLetterISOLanguageName.ToLower() is "ru")
                return response.Labels.Contains("ru");
        }

        return true;
    }

    private async void OpenBlog(BlogViewModel obj)
    {
        await Navigation.PushAsync(new BlogItemPage() { BindingContext = obj });
    }

    private async void NewBlogsRequest()
    {
        if (IsBusy || nextPageToken is null)
        {
            return;
        }

        IsBusy = true;
        Page++;

        var res = await SiteService.GetBlogsAsync(nextPageToken);
        if (res != null)
        {
            nextPageToken = res.NextPageToken;
            foreach (var item in res.Items)
            {
                BlogsCollection.Add(new BlogViewModel(item));
            }

            OnPropertyChanged(nameof(BlogsCollection));
        }
        IsBusy = false;
    }

    #region Properties

    public int Page { get => page; set => SetProperty(ref page, value); }

    public INavigation Navigation { get; set; }

    public ObservableCollection<BlogViewModel> BlogsCollection { get; set; }

    public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(LoadItems));

    public ICommand OpenBlogCommand => openBlogCommand ?? (openBlogCommand = new Command<BlogViewModel>(OpenBlog));

    public ICommand NewBlogLoadCommand => newBlogLoadCommand ?? (newBlogLoadCommand = new Command(NewBlogsRequest));

    #endregion
}