﻿using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Common.Communication;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages;

public class BlogsPageViewModel : BaseViewModel
{
    private Command newBlogLoadCommand;
    private Command refreshCommand;
    private Command openBlogCommand;
    int page = 0;
    IDataService dataService;


	public BlogsPageViewModel(IDataService dataService)
    {
        this.dataService = dataService;
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

        var res = await dataService.GetBlogsAsync(Page);
        if (res != null)
        {
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
        //await Shell.Current.GoToAsync($"{nameof(BlogItemPage)}?{nameof(BlogItemPage.ItemId)}={obj.}");
    }

    private async void NewBlogsRequest()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        Page++;

        var res = await dataService.GetBlogsAsync(Page);
        if (res != null)
        {
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

    public ObservableCollection<BlogViewModel> BlogsCollection { get; set; }

    public ICommand RefreshCommand => refreshCommand ?? (refreshCommand = new Command(LoadItems));

    public ICommand OpenBlogCommand => openBlogCommand ?? (openBlogCommand = new Command<BlogViewModel>(OpenBlog));

    public ICommand NewBlogLoadCommand => newBlogLoadCommand ?? (newBlogLoadCommand = new Command(NewBlogsRequest));

    #endregion
}