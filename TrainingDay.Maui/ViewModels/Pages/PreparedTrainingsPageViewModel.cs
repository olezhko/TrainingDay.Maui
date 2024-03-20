using Microsoft.AppCenter.Analytics;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Resources.Strings;
using TrainingDay.Maui.Services;
using TrainingDay.Maui.Views;

namespace TrainingDay.Maui.ViewModels.Pages;
public sealed class PreparedTrainingsPageViewModel : BaseViewModel
{
    public ICommand CreateNewTrainingCommand { get; set; }

    public ICommand ItemSelectedCommand { get; set; }

    public ObservableCollection<PreparedTrainingViewModel> PreparedTrainingsCollection { get; set; }

    public INavigation Navigation { get; set; }

    public PreparedTrainingsPageViewModel()
    {
        FillTrainings();
        CreateNewTrainingCommand = new Command(AddNewTraining);
        ItemSelectedCommand = new Command<PreparedTrainingViewModel>(ItemSelected);
    }

    private void ItemSelected(PreparedTrainingViewModel selectedTraining)
    {
        selectedTraining.CreateTraining.Invoke();
        SaveNewTrainingViewModelToDatabase(selectedTraining.Training, selectedTraining.SuperSets);
        Navigation.PopAsync();
    }

    private async void AddNewTraining()
    {
        var result = await MessageManager.DisplayPromptAsync(AppResources.CreateNewString, AppResources.EnterTrainingName,
            AppResources.OkString, AppResources.CancelString, AppResources.NameString);
        if (result.IsNotNullOrEmpty())
        {
            var training = new TrainingViewModel();
            training.Title = result;
            var vm = new ExerciseListPageViewModel()
            {
                Navigation = Navigation,
            };
            vm.ExercisesSelectFinished += async (sender, args) =>
            {
                foreach (var exerciseItem in args)
                {
                    training.Exercises.Add(new TrainingExerciseViewModel(exerciseItem.GetExercise(),
                        new TrainingExerciseComm()));
                }

                SaveNewTrainingViewModelToDatabase(training, null);
                await Navigation.PopToRootAsync();
            };
            await Navigation.PushModalAsync(new NavigationPage(new ExerciseListPage(vm)));
            await MessageManager.DisplayAlert(AppResources.AdviceString,
                AppResources.MarkTheRequiredExercisesAndPressFormat.Fill(AppResources.SelectString), AppResources.OkString);
            Analytics.TrackEvent($"{GetType().Name}: Training Created");
        }
    }

    public void FillTrainings()
    {
        PreparedTrainingsCollection = new ObservableCollection<PreparedTrainingViewModel>();
        var exerciseBase = App.Database.GetExerciseItems().ToList();

        var preparedTrainingForBeginners = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedTrainingForBeginners,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.begin.png"),
        };
        preparedTrainingForBeginners.CreateTraining = () =>
        {
            preparedTrainingForBeginners.Training = new TrainingViewModel()
            {
                Exercises = new ObservableCollection<TrainingExerciseViewModel>(
                    GetExercisesByCodeNum(exerciseBase, 40, 13, 34, 10, 16, 27, 31, 56, 58)),
                Title = AppResources.PreparedTrainingForBeginners,
            };
        };
        PreparedTrainingsCollection.Add(preparedTrainingForBeginners);

        var preparedFitnessString = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedFitnessString,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.fitness.png"),
        };
        preparedFitnessString.CreateTraining = () =>
        {
            preparedFitnessString.Training = new TrainingViewModel()
            {
                Exercises = new ObservableCollection<TrainingExerciseViewModel>(GetExercisesByCodeNum(exerciseBase,
                    113, 109, 114, 115, 116, 117, 118, 119, 108, 111, 84, 103, 102, 110)),
                Title = AppResources.PreparedFitnessString,
            };
        };
        PreparedTrainingsCollection.Add(preparedFitnessString);

        var preparedCardioString = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedCardioString,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.cardio.png"),
            MainMuscles =
                new ObservableCollection<MuscleViewModel>(
                    MusclesConverter.SetMuscles(TrainingDay.Common.MusclesEnum.Cardio)),
        };
        preparedCardioString.CreateTraining = () =>
        {
            preparedCardioString.Training = new TrainingViewModel()
            {
                Exercises = GetExerciseByMuscles(exerciseBase,
                    MusclesConverter.SetMuscles(TrainingDay.Common.MusclesEnum.Cardio).ToArray()),
                Title = AppResources.PreparedCardioString,
            };
        };
        PreparedTrainingsCollection.Add(preparedCardioString);

        var preparedNiceAbs = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedNiceStomach,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.flat_stomach.png"),
        };
        preparedNiceAbs.CreateTraining = () =>
        {
            preparedNiceAbs.Training = new TrainingViewModel()
            {
                Exercises = new ObservableCollection<TrainingExerciseViewModel>(
                    GetExercisesByCodeNum(exerciseBase, 45, 87, 46, 50, 88, 110, 111, 113, 118, 137, 126, 138)),
                Title = AppResources.PreparedNiceStomach,
            };
        };
        PreparedTrainingsCollection.Add(preparedNiceAbs);

        var preparedHomeString = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedHomeString,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.home.png"),
        };
        preparedHomeString.CreateTraining = () =>
        {
            preparedHomeString.Training = new TrainingViewModel()
            {
                Exercises = new ObservableCollection<TrainingExerciseViewModel>(exerciseBase
                    .Where(ex => TrainingDay.Common.ExerciseTools.ConvertFromIntToTagList(ex.TagsValue).Contains(TrainingDay.Common.ExerciseTags.CanDoAtHome)).Select(item =>
                        new TrainingExerciseViewModel(item, new TrainingExerciseComm())
                        {
                            WeightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>()
                            {
                                    new WeightAndRepsViewModel(0,15),new WeightAndRepsViewModel(0,15),new WeightAndRepsViewModel(0,15)
                            }
                        })),
                Title = AppResources.PreparedHomeString,
            };
        };
        PreparedTrainingsCollection.Add(preparedHomeString);

        var preparedWideShoulders = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedWideShoulders,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.wide_shoulders.png"),
        };
        preparedWideShoulders.CreateTraining = () =>
        {
            preparedWideShoulders.Training = new TrainingViewModel()
            {
                Exercises = new ObservableCollection<TrainingExerciseViewModel>(
                    GetExercisesByCodeNum(exerciseBase, 8, 9, 5, 6, 120, 122, 3, 101, 11, 86)),
                Title = AppResources.PreparedWideShoulders,
            };
        };
        PreparedTrainingsCollection.Add(preparedWideShoulders);

        var preparedBackReady = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedBackReady,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.back.png"),
            MainMuscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.SetMuscles(
                TrainingDay.Common.MusclesEnum.LowerBack, TrainingDay.Common.MusclesEnum.MiddleBack,
                TrainingDay.Common.MusclesEnum.WidestBack)),
        };
        preparedBackReady.CreateTraining = () =>
        {
            preparedBackReady.Training = new TrainingViewModel()
            {
                Exercises = GetExercisesByCodeNum(exerciseBase, new[] { 51, 60, 55, 73, 58, 52, 56, 10, 101 }),
                Title = AppResources.PreparedBackReady,
            };
        };
        PreparedTrainingsCollection.Add(preparedBackReady);

        var preparedMaximumUpperBody = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedMaximumUpperBody,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.up.png"),
            MainMuscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.SetMuscles(
                TrainingDay.Common.MusclesEnum.LowerBack, TrainingDay.Common.MusclesEnum.MiddleBack,
                TrainingDay.Common.MusclesEnum.WidestBack)),
            SuperSets = new List<PreparedSuperSet>(new[]
            {
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {1, 2},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {3, 4},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {5, 6, 7},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {8, 9},
                    },
                }),
        };
        preparedMaximumUpperBody.CreateTraining = () =>
        {
            preparedMaximumUpperBody.Training = new TrainingViewModel()
            {
                Exercises = GetExercisesByCodeNum(exerciseBase,
                    new[] { 121, 6, 56, 60, 89, 43, 48, 87, 4, 5, 1, 127 }),
                Title = AppResources.PreparedMaximumUpperBody,
            };
        };
        PreparedTrainingsCollection.Add(preparedMaximumUpperBody);

        var preparedArmsStrength = new PreparedTrainingViewModel()
        {
            Name = AppResources.PreparedArmsStrength,
            TrainingImageUrl = ImageSource.FromResource("TrainingDay.Maui.Resources.Images.prepared.arms.png"),
            MainMuscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.SetMuscles(
                TrainingDay.Common.MusclesEnum.LowerBack, TrainingDay.Common.MusclesEnum.MiddleBack,
                TrainingDay.Common.MusclesEnum.WidestBack)),
            SuperSets = new List<PreparedSuperSet>(new[]
            {
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {1, 2},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {3, 4},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {5, 6,},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {7, 8},
                    },
                    new PreparedSuperSet()
                    {
                        ExerciseOrderArray = new[] {9, 10},
                    },
                }),
        };
        preparedArmsStrength.CreateTraining = () =>
        {
            preparedArmsStrength.Training = new TrainingViewModel()
            {
                Exercises = GetExercisesByCodeNum(exerciseBase, new[] { 83, 13, 23, 15, 93, 16, 27, 17, 22, 89, 82 }),
                Title = AppResources.PreparedArmsStrength,
            };
        };
        PreparedTrainingsCollection.Add(preparedArmsStrength);

        OnPropertyChanged(nameof(PreparedTrainingsCollection));
    }

    private ObservableCollection<TrainingExerciseViewModel> GetExerciseByMuscles(List<Exercise> baseExercises,
        params MuscleViewModel[] muscles)
    {
        var result = new ObservableCollection<TrainingExerciseViewModel>();
        int order = 0;
        foreach (var baseExercise in baseExercises)
        {
            try
            {
                var exMuscles = MusclesConverter.ConvertFromStringToList(baseExercise.MusclesString);
                var sub = new List<MuscleViewModel>();
                foreach (var muscleViewModel in exMuscles)
                {
                    if (muscles.Any(a => a.Id == muscleViewModel.Id))
                    {
                        sub.Add(muscleViewModel);
                    }
                }

                if (sub.Any())
                {
                    result.Add(new TrainingExerciseViewModel(baseExercise, new TrainingExerciseComm()
                    {
                        OrderNumber = order,
                        ExerciseId = baseExercise.Id,
                    })
                    {
                        WeightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>(new[]
                        {
                                new WeightAndRepsViewModel(0, 15), new WeightAndRepsViewModel(0, 15),
                                new WeightAndRepsViewModel(0, 15),
                            }),
                    });
                    order++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return result;
    }

    private ObservableCollection<TrainingExerciseViewModel> GetExercisesByCodeNum(List<Exercise> baseExercises, params int[] codeNums)
    {
        var result = new ObservableCollection<TrainingExerciseViewModel>();
        int i = 0;
        foreach (var codeNum in codeNums)
        {
            try
            {
                var exercise = baseExercises.FirstOrDefault(item => item.CodeNum == codeNum);
                if (exercise != null)
                {
                    result.Add(new TrainingExerciseViewModel(exercise, new TrainingExerciseComm()
                    {
                        OrderNumber = i,
                        ExerciseId = exercise.Id,
                    })
                    {
                        WeightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>(new[]
                        {
                                new WeightAndRepsViewModel(0, 15), new WeightAndRepsViewModel(0, 15),
                                new WeightAndRepsViewModel(0, 15),
                            }),
                    });
                }

                i++;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        return result;
    }

    public static void SaveNewTrainingViewModelToDatabase(TrainingViewModel viewModel, List<PreparedSuperSet> superSets)
    {
        var id = App.Database.SaveTrainingItem(new Training()
        {
            Title = viewModel.Title,
        });

        if (superSets != null && superSets.Count != 0)
        {
            foreach (var superSet in superSets)
            {
                var superSetId = App.Database.SaveSuperSetItem(new SuperSet()
                {
                    TrainingId = id,
                });
                superSet.Id = superSetId;
            }
        }

        int order = 0;
        foreach (var exercise in viewModel.Exercises)
        {
            int superSetId = 0;
            if (superSets != null && superSets.Count != 0)
            {
                var superSet = superSets.FirstOrDefault(set => set.ExerciseOrderArray.Contains(order));
                if (superSet != null)
                {
                    superSetId = superSet.Id;
                }
            }

            var newTrEx = new TrainingExerciseComm()
            {
                ExerciseId = exercise.ExerciseId,
                TrainingId = id,
                OrderNumber = order,
                Id = exercise.TrainingExerciseId,
                SuperSetId = superSetId,
                WeightAndRepsString = ExerciseManager.ConvertJson(exercise.Tags, exercise),
            };

            App.Database.SaveTrainingExerciseItem(newTrEx);
            order++;
        }
    }
}