using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class BlogViewModel : BaseViewModel
{
    private int id;
    private int guid;
    private string title;
    private string text;
    private DateTime dateTime;
    public BlogViewModel()
    {
    }

    public BlogViewModel(BlogDto item)
    {
        id = item.Id;
        Title = item.Title;
        Text = item.Content ??string.Empty;
        DateTime = item.Published;
        guid = item.Guid;
    }

    public int Id
    {
        get { return id; }
    }

    public int Guid
    {
        get { return guid; }
    }

    public bool IsNew
    {
        get { return string.IsNullOrEmpty(Text); }
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
            OnPropertyChanged(nameof(IsNew));
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
            var lightHead = """
                <head>
                <style>
                    body { background-color:#f0f0f0;color: #1b1b1b !important; }
                </style>
                </head>
                """;
            var darkHead = """
                <head>
                <style>
                    body { background-color:#1b1b1b;color: #FFFFFF; !important; }
                </style>
                </head>
                """;
            Text = Text.Replace("style=\"color:rgb(0, 0, 0);\"", string.Empty);
            var textColorString = App.Current.RequestedTheme == AppTheme.Light ? lightHead : darkHead;
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