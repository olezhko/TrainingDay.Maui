﻿using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TrainingDay.Common.Extensions;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Questions;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels.Pages
{
	public class WorkoutQuestinariumPageViewModel : BaseViewModel
	{
		private WorkoutQuestinariumStepViewModel currentStep;
        private readonly IDataService _dataService;

        public WorkoutQuestinariumStepViewModel CurrentStep
		{
			get => currentStep;
			set => SetProperty(ref currentStep, value);
		}

        public int CurrentStepIndex { get; set; } = 1;
        public ICommand BackOrCancelCommand { get; set; }
        public ICommand NextOrFinishCommand { get; set; }

        IWorkoutService _workoutService;
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
                    Variants = step.Answers.Select(item => new WorkoutQuestinariumVariantViewModel()
                    {
                        Title = item,
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

        private void InsertAfter(WorkoutQuestinariumStepViewModel current, WorkoutQuestinariumStepViewModel newStep)
        {
            if (newStep is not null)
                newStep.Previous = current;

            if (current is not null)
                current.Next = newStep;
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

			await CreateWorkout();
            IsBusy = false;
            await NavigateToHome();
        }

        private async Task CreateWorkout()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                var step = CurrentStep;
                while (true)
                {
                    step = step.Previous;
                    if (step is null)
                    {
                        break;
                    }

                    sb.Append($"Question: {step.Title}.");
                    sb.Append($" {string.Join(", ", step.Variants.Where(item => item.IsChecked).Select(item => item.Title))}. "); // answer
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
            var response = await _dataService.GetExercisesByQuery(message);
            if (response.Any())
            {
                var nameOfWorkout = await Shell.Current.DisplayPromptAsync("Workout Created", "Your workout has been created successfully. You can find it in the workouts section.", "OK", "Cancel", "Enter a name for your workout", maxLength: 50, keyboard: Keyboard.Text);
                if (nameOfWorkout is not null)
                {
                    await _workoutService.CreateWorkoutAsync(nameOfWorkout, response, CancellationToken.None);
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

        private async Task NavigateToHome()
        {
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("..");
        }
    }

	public class WorkoutQuestinariumStepViewModel : BaseViewModel
	{
        private string title;
        private WorkoutQuestinariumStepViewModel? next;
        private WorkoutQuestinariumStepViewModel? previous;

        public string Title { get => title; set => SetProperty(ref title, value); }

        public ObservableCollection<WorkoutQuestinariumVariantViewModel> Variants { get; set; }

		public WorkoutQuestinariumStepViewModel? Next { get => next; set => SetProperty(ref next, value); }

        public WorkoutQuestinariumStepViewModel? Previous { get => previous; set => SetProperty(ref previous, value); }
    }

	public class WorkoutQuestinariumVariantViewModel : BaseViewModel
	{
        private bool isChecked;
        private string title;
        private bool isMultiple;
        private string questionNumber;

        public bool IsChecked { get => isChecked; set => SetProperty(ref isChecked, value); }
        public bool IsMultiple { get => isMultiple; set => SetProperty(ref isMultiple, value); }
        public string Title { get => title; set => SetProperty(ref title, value); }
        public string QuestionNumber { get => questionNumber; set => SetProperty(ref questionNumber, value); }
    }
}

