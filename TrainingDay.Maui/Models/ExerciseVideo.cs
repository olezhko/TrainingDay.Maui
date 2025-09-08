using System.Text;

namespace TrainingDay.Maui.Models;

public class ExerciseVideo
{
    public required string VideoUrl { get; set; }

    public WebViewSource WebViewData
    {
        get
        {
            var width = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - 10;
            var htmlSource = new HtmlWebViewSource();
            htmlSource.Html = $@"<html><body>
                  <iframe width=""{width}"" height=""{width}"" src=""{ConvertUrl()}"" frameborder=""0"" allow=""accelerometer; autoplay; encrypted - media; gyroscope; picture -in-picture"" allowfullscreen ></iframe>
                     </body></html>";
            return htmlSource;
        }
    }

    private string ConvertUrl()
    {
        var sb = new StringBuilder();
        sb.Append(@"https://www.youtube.com/embed/");
        sb.Append(VideoUrl.Replace("http://www.youtube.com/watch?v=", string.Empty).Replace("https://www.youtube.com/watch?v=", string.Empty));
        return sb.ToString();
    }
}