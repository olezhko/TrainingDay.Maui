using System;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Models.Questions;

public class WorkoutQuestinariumVariantViewModel : BaseViewModel
{
    private bool isChecked;
    private string title;
    private bool isMultiple;
    private string questionNumber;
    private string instruction;
    private string option;

    public bool IsChecked { get => isChecked; set => SetProperty(ref isChecked, value); }
    public bool IsMultiple { get => isMultiple; set => SetProperty(ref isMultiple, value); }
    public string Title { get => title; set => SetProperty(ref title, value); }
    public string QuestionNumber { get => questionNumber; set => SetProperty(ref questionNumber, value); }
    
    public string Instruction { get => instruction; set => SetProperty(ref instruction, value); }
    public string Option { get => option; set => SetProperty(ref option, value); }
}