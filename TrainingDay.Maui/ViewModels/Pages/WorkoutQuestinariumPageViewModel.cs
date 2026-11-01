using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Questions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages
{
	public class WorkoutQuestinariumPageViewModel : BaseViewModel
	{
		private WorkoutQuestinariumStepViewModel? currentStep;
        private int stepCount;
        private readonly IWorkoutQuestinariumService _workoutQuestinariumService;

        public WorkoutQuestinariumStepViewModel? CurrentStep
		{
			get => currentStep;
			set
            {
                if (SetProperty(ref currentStep, value))
                {
                    OnPropertyChanged(nameof(CurrentStepIndex));
                    OnPropertyChanged(nameof(StepCounterText));
                    OnPropertyChanged(nameof(IsFirstStep));
                    OnPropertyChanged(nameof(IsLastStep));
                }
            }
		}

        public int StepCount
        {
            get => stepCount;
            set
            {
                if (SetProperty(ref stepCount, value))
                {
                    OnPropertyChanged(nameof(StepCounterText));
                }
            }
        }

        /// <summary>
        /// Zero-based index of the current wizard step.
        /// </summary>
        public int CurrentStepIndex => CurrentStep?.Index ?? 0;
        public string StepCounterText => string.Format(AppResources.StepOfFormat, CurrentStepIndex + 1, StepCount);
        public bool IsFirstStep => CurrentStep?.Previous is null;
        public bool IsLastStep => CurrentStep?.Next is null;

        public ICommand BackOrCancelCommand { get; set; }
        public ICommand NextOrFinishCommand { get; set; }

        public WorkoutQuestinariumPageViewModel(IWorkoutQuestinariumService workoutQuestinariumService)
		{
            _workoutQuestinariumService = workoutQuestinariumService;
            BackOrCancelCommand = new AsyncRelayCommand(Back);
            NextOrFinishCommand = new AsyncRelayCommand(Next);
		}

		public async Task LoadSteps()
		{
            var steps = await ResourceExtension.LoadResourceAsync<WorkoutQuestinariumStep>("questions", Settings.GetLanguage().TwoLetterISOLanguageName);
            WorkoutQuestinariumStepViewModel? first = null;
            WorkoutQuestinariumStepViewModel? previous = null;
            int index = 0;
            foreach (var step in steps.Where(item => item.Answers?.Any() == true))
            {
                var newStep = new WorkoutQuestinariumStepViewModel()
                {
                    Title = step.Title,
                    Number = step.Number,
                    Index = index,
                    IsMultiple = step.IsMultiple,
                    Instruction = step.Instruction,
                    Variants = step.Answers.Select(item => new WorkoutQuestinariumVariantViewModel()
                    {
                        Title = item.Answer,
                        Option = item.Option,
                        IsMultiple = step.IsMultiple,
                    }).ToObservableCollection(),
                };

                newStep.Variants[0].IsChecked = true;

                first ??= newStep;
                InsertAfter(previous, newStep);
                index++;
                previous = newStep;
            }

            StepCount = index;
            CurrentStep = first;
        }

        private static void InsertAfter(WorkoutQuestinariumStepViewModel? current, WorkoutQuestinariumStepViewModel newStep)
        {
            newStep.Previous = current;
            current?.Next = newStep;
        }

        public async Task Next()
		{
            if (CurrentStep is null || !CurrentStep.HasSelection)
            {
                return;
            }

            if (CurrentStep.Next is not null)
			{
				CurrentStep = CurrentStep.Next;
				return;
            }

			await CreateWorkout();
        }

        private async Task CreateWorkout()
        {
            var options = new Dictionary<WorkoutQuestion, IReadOnlyCollection<string>>();
            for (var step = CurrentStep; step is not null; step = step.Previous)
            {
                options[(WorkoutQuestion)step.Number] = step.Variants.Where(item => item.IsChecked).Select(item => item.Option).ToList();
            }

            var nameOfWorkout = await Shell.Current.DisplayPromptAsync(AppResources.WorkoutAlmostCreated, AppResources.EnterTrainingName,
                AppResources.OkString, AppResources.CancelString, AppResources.NameString, maxLength: 50, keyboard: Keyboard.Text);

            // Cancelled: stay on the last step so the answers are not lost.
            if (nameOfWorkout is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(nameOfWorkout))
            {
                nameOfWorkout = AppResources.NameString;
            }

            int trainingId = 0;
            IsBusy = true;
            try
            {
                trainingId = await _workoutQuestinariumService.CreateWorkoutAsync(nameOfWorkout.Trim(), WorkoutQuestinariumAnswers.Parse(options), CancellationToken.None);
            }
            catch (Exception e)
            {
                LoggingService.TrackError(e);
            }
            finally
            {
                IsBusy = false;
            }

            if (trainingId == 0)
            {
                await MessageManager.DisplayAlert(AppResources.LetsBuildYourWorkout, AppResources.GenericErrorString, AppResources.OkString);
                return;
            }

            await Shell.Current.GoToAsync($"//workouts/{nameof(TrainingExercisesPage)}?{nameof(TrainingExercisesPageViewModel.ItemId)}={trainingId}");
        }

        public async Task Back()
		{
            if (CurrentStep?.Previous is not null)
            {
                CurrentStep = CurrentStep.Previous;
                return;
            }

            await Shell.Current.GoToAsync("..");
        }
    }
}
