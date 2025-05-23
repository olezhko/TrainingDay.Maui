﻿using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.ViewModels;

public class MuscleViewModel : BaseViewModel
{
    private string name;

    public int Id { get; set; }

    public string Name
    {
        get => name;
        set
        {
            name = value;
            OnPropertyChanged();
        }
    }

    public MuscleViewModel(MusclesEnum muscle)
    {
        Id = (int)muscle;
        Name = ExerciseExtensions.GetEnumDescription(muscle, Settings.GetLanguage());
    }
}