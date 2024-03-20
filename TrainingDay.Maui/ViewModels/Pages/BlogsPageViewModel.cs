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

    public BlogsPageViewModel()
    {
        BlogsCollection = new ObservableCollection<BlogViewModel>();
    }

    public bool IsLoadingItems { get; set; }

    public int Page { get; set; } = 1;

    public INavigation Navigation { get; set; }

    public ObservableCollection<BlogViewModel> BlogsCollection { get; set; }

    public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(LoadItems));

    public ICommand OpenBlogCommand => openBlogCommand ?? (openBlogCommand = new Command<BlogViewModel>(OpenBlog));

    public ICommand NewBlogLoadCommand => newBlogLoadCommand ?? (newBlogLoadCommand = new Command(NewBlogLoad));

    public async void LoadItems()
    {
        Page = 1;
        OnPropertyChanged(nameof(Page));
        if (BlogsCollection.Count != 0)
        {
            BlogsCollection.Clear();
        }

        IsLoadingItems = true;
        OnPropertyChanged(nameof(IsLoadingItems));

        var res = await SiteService.GetBlogsFromServer(Settings.GetLanguage().TwoLetterISOLanguageName, Page);
        if (res != null)
        {
            BlogsCollection = new ObservableCollection<BlogViewModel>(res.Select(item => new BlogViewModel(item)).OrderByDescending(item => item.DateTime));
            OnPropertyChanged(nameof(BlogsCollection));

            IsLoadingItems = false;
            OnPropertyChanged(nameof(IsLoadingItems));
        }
    }

    private async void OpenBlog(BlogViewModel obj)
    {
        await Navigation.PushAsync(new BlogItemPage() { BindingContext = obj });
    }

    private async void NewBlogLoad()
    {
        if (IsBusy || IsLoadingItems)
        {
            return;
        }

        IsBusy = true;
        Page++;
        OnPropertyChanged(nameof(Page));
        var res = await SiteService.GetBlogsFromServer(Settings.GetLanguage().TwoLetterISOLanguageName, Page);
        if (res != null)
        {
            foreach (var item in res)
            {
                BlogsCollection.Add(new BlogViewModel(item));
            }

            OnPropertyChanged(nameof(BlogsCollection));
        }
        IsBusy = false;
    }
}