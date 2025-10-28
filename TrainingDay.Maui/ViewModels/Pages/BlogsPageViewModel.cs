using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;

public class BlogsPageViewModel : BaseViewModel
{
    private Command openBlogCommand;
    IDataService dataService;

	public BlogsPageViewModel(IDataService dataService)
    {
        this.dataService = dataService;
		BlogsCollection = new ObservableCollection<BlogViewModel>();
    }

    public async void LoadItems()
    {
        if (IsBusy)
        {
            return;
        }

        if (BlogsCollection.Count != 0)
        {
            BlogsCollection.Clear();
        }

        IsBusy = true;

        var dbBlogs = App.Database.GetBlogItems()
            .OrderByDescending(item => item.Published)
            .Select(item => new BlogViewModel(item))
            .ToList();

        var lastDate = dbBlogs.FirstOrDefault()?.DateTime;
        try
        {
            var newBlogs = await dataService.GetBlogsAsync(lastDate);

            if (newBlogs != null)
            {
                foreach (var item in newBlogs)
                {
                    var newBlog = new BlogDto()
                    {
                        Guid = item.Guid,
                        Content = item.Content,
                        Published = item.Published,
                        Title = item.Title,
                    };
                    var id = App.Database.SaveItem(newBlog);
                    newBlog.Id = id;

                    dbBlogs.Add(new BlogViewModel(newBlog));
                }
            }
        }
        catch
        {
        }

        BlogsCollection = new ObservableCollection<BlogViewModel>(dbBlogs.OrderByDescending(item => item.DateTime));
        OnPropertyChanged(nameof(BlogsCollection));

        IsBusy = false;
    }

    private async void OpenBlog(BlogViewModel sender)
    {
        var blog = await dataService.GetBlogAsync(sender.Guid);
        var update = new BlogDto()
        {
            Id = sender.Id,
            Guid = sender.Guid,
            Content = blog.Content,
            Published = blog.Published,
            Title = blog.Title,
        };
        App.Database.SaveItem(update, sender.Id);

        Dictionary<string, object> param = new Dictionary<string, object> { { "Context", new BlogViewModel(update) } };
        await Shell.Current.GoToAsync(nameof(BlogItemPage), param);
    }

    #region Properties

    public ObservableCollection<BlogViewModel> BlogsCollection { get; set; }

    public ICommand OpenBlogCommand => openBlogCommand ?? (openBlogCommand = new Command<BlogViewModel>(OpenBlog));

    #endregion
}