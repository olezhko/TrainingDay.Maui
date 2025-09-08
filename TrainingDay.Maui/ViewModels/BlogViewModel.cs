using TrainingDay.Common.Communication;

namespace TrainingDay.Maui.ViewModels;

public class BlogViewModel : BaseViewModel
{
    private string guid;
    private string title;
    private string text;
    private DateTime dateTime;

    public BlogViewModel()
    {
    }

    public BlogViewModel(BlogResponse item)
    {
        Title = item.Title;
        Text = item.Content;
        DateTime = item.Published;
        guid = item.Guid;
    }

    public string Guid
    {
        get { return guid; }
    }

    public string Title
    {
        get => title;
        set
        {
            title = value;
            OnPropertyChanged();
        }
    }

    public string Text
    {
        get => text;
        set
        {
            text = value;
            OnPropertyChanged();
        }
    }

    public DateTime DateTime
    {
        get => dateTime;
        set
        {
            dateTime = value;
            OnPropertyChanged();
        }
    }

    public WebViewSource WebViewDataText
    {
        get
        {
            var textColorString = App.Current.RequestedTheme == AppTheme.Light ? 
                "<head><style>body { background-color:#f0f0f0;color: #1b1b1b;}</style></head>" : 
                "<head><style>body { background-color:#1b1b1b;color: #FFFFFF;}</style></head>";
            var htmlSource = new HtmlWebViewSource
            {
                Html = $@"
<html>{textColorString}
    <body>
    <h3>{Title}</h3>
    {Text}
    </body>
</html>",
            };
            return htmlSource;
        }
    }
}