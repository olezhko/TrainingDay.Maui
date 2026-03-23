using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Questions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages
{
	public class WorkoutQuestinariumPageViewModel : BaseViewModel
	{
		private WorkoutQuestinariumStepViewModel currentStep;
        private readonly IDataService _dataService;
        private readonly IWorkoutService _workoutService;

        public WorkoutQuestinariumStepViewModel CurrentStep
		{
			get => currentStep;
			set => SetProperty(ref currentStep, value);
		}

        public int CurrentStepIndex { get; set; } = 1;
        public ICommand BackOrCancelCommand { get; set; }
        public ICommand NextOrFinishCommand { get; set; }

        public WorkoutQuestinariumPageViewModel(IDataService dataService, IWorkoutService workoutService)
		{
            _dataService = dataService;
            _workoutService = workoutService;
            BackOrCancelCommand = new Command(Back);
            NextOrFinishCommand = new Command(Next);
		}

		public async Task LoadSteps()
		{
            var steps = await ResourceExtension.LoadResource<WorkoutQuestinariumStep>("questions", 
                Settings.GetLanguage().TwoLetterISOLanguageName);
            WorkoutQuestinariumStepViewModel? previous = null;
            int index = 0;
            foreach (var step in steps)
            {
                var newStep = new WorkoutQuestinariumStepViewModel()
                {
                    Title = step.Title,
                    Number = step.Number,
                    Instruction = step.Instruction,
                    Variants = step.Answers.Select(item => new WorkoutQuestinariumVariantViewModel()
                    {
                        Title = item.Answer,
                        Option = item.Option,
                        IsMultiple = step.IsMultiple,
                        QuestionNumber = step.Number.ToString()
                    }).ToObservableCollection(),
                };

                newStep.Variants[0].IsChecked = true;

                CurrentStep ??= newStep;

                InsertAfter(previous, newStep);
                index++;
                previous = newStep;
            }

            InsertAfter(previous, null);
        }

        private void InsertAfter(WorkoutQuestinariumStepViewModel current, 
            WorkoutQuestinariumStepViewModel newStep)
        {
            newStep?.Previous = current;
            current?.Next = newStep;
        }

        public async void Next()
		{
            IsBusy = true;

            CurrentStepIndex++;
            OnPropertyChanged(nameof(CurrentStepIndex));
            var next = CurrentStep.Next;
			if (next is not null)
			{
				CurrentStep = next;
                IsBusy = false;
				return;
            }

			await PrepareRequest();
            IsBusy = false;
            await NavigateToHome();
        }

        private async Task PrepareRequest()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var step = CurrentStep;
                while (true)
                {
                    if (step is null)
                    {
                        break;
                    }

                    sb.Append($"{step.Title} ");
                    sb.Append($"{string.Join(" or ", step.Variants.Where(item => item.IsChecked).Select(item => item.Instruction))}. ");
                    step = step.Previous;
                }

                await SendRequest(sb.ToString());
            }
            catch (Exception)
            {

            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SendRequest(string message)
        {
            var response = await _dataService.GetExercisesByQueryAsync(message);
            if (response.Exercises.Any())
            {
                var nameOfWorkout = await Shell.Current.DisplayPromptAsync(AppResources.WorkoutAlmostCreated, AppResources.EnterTrainingName, 
                    AppResources.OkString, AppResources.CancelString, AppResources.NameString, maxLength: 50, keyboard: Keyboard.Text);

                if (nameOfWorkout is not null)
                {
                    await _workoutService.CreateWorkoutAsync(nameOfWorkout, response.Exercises, CancellationToken.None);
                }
            }
        }

        public async void Back()
		{
            IsBusy = true;
            var back = CurrentStep.Previous;
            CurrentStepIndex--;
            OnPropertyChanged(nameof(CurrentStepIndex));
            if (back is not null)
            {
                CurrentStep = back;
                IsBusy = false;
                return;
            }

            await NavigateToHome();
        }

        private static async Task NavigateToHome() => await Shell.Current.GoToAsync("//workouts");
    }
}

