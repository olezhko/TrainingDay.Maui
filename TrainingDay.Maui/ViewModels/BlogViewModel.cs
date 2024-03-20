using TrainingDay.Common;

namespace TrainingDay.Maui.ViewModels;

public class BlogViewModel : BaseViewModel
{
    private string title;
    private string shortText;
    private string text;
    private DateTime dateTime;

    public BlogViewModel()
    {
    }

    public BlogViewModel(MobileBlog item)
    {
        Title = item.Title;
        Text = item.Text;
        ShortText = item.ShortText;
        DateTime = item.DateTime;
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

    public string ShortText
    {
        get => shortText;
        set
        {
            shortText = value;
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
            var body = Text != null ? Text.Replace("img src=\"/img", $"img src=\"{Consts.Site}/img") : Text;
            var textColorString = App.Current.RequestedTheme == AppTheme.Light ? "<head><style>body { background-color:#f0f0f0;color: #1b1b1b;}</style></head>" : "<head><style>body { background-color:#1b1b1b;color: #FFFFFF;}</style></head>";
            var htmlSource = new HtmlWebViewSource
            {
                Html = $@"<html>{textColorString}<body>{body}</body></html>",
            };
            return htmlSource;
        }
    }
}