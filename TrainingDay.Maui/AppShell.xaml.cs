using TrainingDay.Maui.Views;

namespace TrainingDay.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(TrainingExercisesPage), typeof(TrainingExercisesPage));
            Routing.RegisterRoute(nameof(TrainingItemsBasePage), typeof(TrainingItemsBasePage));
            Routing.RegisterRoute(nameof(TrainingExerciseItemPage), typeof(TrainingExerciseItemPage));
            Routing.RegisterRoute(nameof(TrainingImplementPage), typeof(TrainingImplementPage));
            Routing.RegisterRoute(nameof(TrainingExercisesMoveOrCopy), typeof(TrainingExercisesMoveOrCopy));
            Routing.RegisterRoute(nameof(HistoryTrainingPage), typeof(HistoryTrainingPage));
            Routing.RegisterRoute(nameof(PreparedTrainingsPage), typeof(PreparedTrainingsPage));

            Routing.RegisterRoute(nameof(WeightViewAndSetPage), typeof(WeightViewAndSetPage));
            Routing.RegisterRoute(nameof(ExerciseListPage), typeof(ExerciseListPage));
            Routing.RegisterRoute(nameof(ExerciseItemPage), typeof(ExerciseItemPage));
            Routing.RegisterRoute(nameof(FilterPage), typeof(FilterPage));

            Routing.RegisterRoute(nameof(BlogsPage), typeof(BlogsPage));

            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(StatisticsPage), typeof(StatisticsPage));
        }
    }
}
