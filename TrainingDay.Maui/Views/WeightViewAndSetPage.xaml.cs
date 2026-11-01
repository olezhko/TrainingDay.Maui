using System.Text;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.ViewModels.Pages;

namespace TrainingDay.Maui.Views;

public partial class WeightViewAndSetPage : ContentPage
{
    private WeightViewAndSetPageViewModel vm;
    public WeightViewAndSetPage()
    {
        InitializeComponent();
        vm = new WeightViewAndSetPageViewModel();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        vm?.OnAppearing();
    }

    private async void ShowInfo_Click(object sender, EventArgs e)
    {
        try
        {
            double coef = -1;
            var waist = vm.BodyControlItems.FirstOrDefault(item => item.Type == WeightType.Waist);
            var hips = vm.BodyControlItems.FirstOrDefault(item => item.Type == WeightType.Hip);
            if (waist != null && hips != null && hips.CurrentValue > 0)
            {
                coef = waist.CurrentValue / hips.CurrentValue;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(AppResources.WaistHipMessage.Replace("_", "\n"), coef));
            var result = await MessageManager.DisplayAlert(AppResources.WaistHipTitle, sb.ToString(), AppResources.YesString, AppResources.CancelString);
            if (result)
            {
                await Browser.OpenAsync("https://tday-app.blogspot.com/2024/12/blog-post_19.html", BrowserLaunchMode.SystemPreferred);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }
}