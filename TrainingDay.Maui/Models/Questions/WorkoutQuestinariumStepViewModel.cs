using System;
using System.Collections.ObjectModel;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Questions;

public class WorkoutQuestinariumStepViewModel : BaseViewModel
{
    private string title;
    private string instruction;
    private WorkoutQuestinariumStepViewModel? next;
    private WorkoutQuestinariumStepViewModel? previous;

    public int Number { get => field; set => field = value; }
    public string Title { get => title; set => SetProperty(ref title, value); }
    public string Instruction { get => instruction; set => SetProperty(ref instruction, value); }

    public ObservableCollection<WorkoutQuestinariumVariantViewModel> Variants { get; set; }

    public WorkoutQuestinariumStepViewModel? Next { get => next; set => SetProperty(ref next, value); }

    public WorkoutQuestinariumStepViewModel? Previous { get => previous; set => SetProperty(ref previous, value); }
}