using TrainingDay.Maui.Views;

namespace TrainingDay.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(TrainingItemsBasePage), typeof(TrainingItemsBasePage));
            Routing.RegisterRoute(nameof(WeightViewAndSetPage), typeof(WeightViewAndSetPage));
            Routing.RegisterRoute(nameof(TrainingAlarmListPage), typeof(TrainingAlarmListPage));
            Routing.RegisterRoute(nameof(BlogsPage), typeof(BlogsPage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(StatisticsPage), typeof(StatisticsPage));
            Routing.RegisterRoute(nameof(HistoryTrainingPage), typeof(HistoryTrainingPage));
        }
    }
}
