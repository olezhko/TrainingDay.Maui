using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using TrainingDay.Common.Communication;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Models.Serialize;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Mappers;

/// <summary>Conversions between <see cref="TrainingExerciseViewModel"/> and its stored / serialized / API shapes.</summary>
public static class TrainingExerciseMapper
{
    public static string ToWeightAndRepsString(TrainingExerciseViewModel item)
    {
        return TrainingExerciseViewModelExtensions.ConvertJson(item.Tags, item);
    }

    /// <summary>Row for the training the view model already belongs to, keeping its id, training and order.</summary>
    public static TrainingExerciseEntity ToEntity(TrainingExerciseViewModel item)
    {
        return ToEntity(item, item.TrainingId, item.OrderNumber);
    }

    /// <summary>Row that updates the existing training exercise with a new training / order.</summary>
    public static TrainingExerciseEntity ToEntity(TrainingExerciseViewModel item, int trainingId, int orderNumber)
    {
        return new TrainingExerciseEntity
        {
            Id = item.TrainingExerciseId,
            ExerciseId = item.ExerciseId,
            TrainingId = trainingId,
            OrderNumber = orderNumber,
            SuperSetId = item.SuperSetId,
            WeightAndRepsString = ToWeightAndRepsString(item),
        };
    }

    /// <summary>New row (no id) — used when copying an exercise into another training.</summary>
    public static TrainingExerciseEntity ToNewEntity(TrainingExerciseViewModel item, int trainingId, int orderNumber)
    {
        var entity = ToEntity(item, trainingId, orderNumber);
        entity.Id = 0;
        return entity;
    }

    public static TrainingExerciseSerialize ToSerialize(TrainingExerciseViewModel item, int trainingId)
    {
        var serialize = new TrainingExerciseSerialize
        {
            TrainingExerciseId = item.TrainingExerciseId,
            SuperSetId = item.SuperSetId,
            OrderNumber = item.OrderNumber,
            TrainingId = trainingId,
            Muscles = MuscleViewModelExtensions.ConvertFromListToString(item.Muscles.ToList()),
            ExerciseId = item.ExerciseId,
            Description = JsonSerializer.Serialize(item.Description?.Model),
            Name = item.Name,
            SuperSetNum = item.SuperSetNum,
            TagsValue = ExerciseExtensions.ConvertTagListToInt(item.Tags),
            WeightAndRepsString = ToWeightAndRepsString(item),
            CodeNum = item.CodeNum,
        };

        if (item is ImplementTrainingExerciseViewModel implementItem)
        {
            serialize.IsNotFinished = implementItem.IsNotFinished;
            serialize.IsSkipped = implementItem.IsSkipped;
        }

        return serialize;
    }

    public static ImplementTrainingExerciseViewModel FromSerialize(TrainingExerciseSerialize serialize)
    {
        var item = new ImplementTrainingExerciseViewModel
        {
            TrainingExerciseId = serialize.TrainingExerciseId,
            ExerciseId = serialize.ExerciseId,
            TrainingId = serialize.TrainingId,
            IsNotFinished = serialize.IsNotFinished,
            Muscles = new ObservableCollection<MuscleViewModel>(MuscleViewModelExtensions.ConvertFromStringToList(serialize.Muscles)),
            OrderNumber = serialize.OrderNumber,
            Name = serialize.Name,
            SuperSetId = serialize.SuperSetId,
            SuperSetNum = serialize.SuperSetNum,
            IsSkipped = serialize.IsSkipped,
            Tags = ExerciseExtensions.ConvertTagIntToList(serialize.TagsValue).ToList(),
            Description = DescriptionViewModel.ConvertFromJson(serialize.Description),
            CodeNum = serialize.CodeNum,
        };

        TrainingExerciseViewModelExtensions.ConvertJsonBack(item, serialize.WeightAndRepsString);
        return item;
    }

    public static LastTrainingExerciseEntity ToLastTrainingExercise(TrainingExerciseViewModel item, int lastTrainingId)
    {
        return new LastTrainingExerciseEntity
        {
            LastTrainingId = lastTrainingId,
            OrderNumber = item.OrderNumber,
            ExerciseName = item.Name,
            MusclesString = MuscleViewModelExtensions.ConvertFromListToString(item.Muscles.ToList()),
            Description = JsonSerializer.Serialize(item.Description?.Model),
            SuperSetId = item.SuperSetId,
            TagsValue = ExerciseExtensions.ConvertTagListToInt(item.Tags),
            CodeNum = item.CodeNum,
            WeightAndRepsString = ToWeightAndRepsString(item),
        };
    }

    public static TrainingExerciseViewModel FromLastTrainingExercise(LastTrainingExerciseEntity entity)
    {
        var item = new TrainingExerciseViewModel
        {
            Name = entity.ExerciseName,
            Muscles = new ObservableCollection<MuscleViewModel>(MuscleViewModelExtensions.ConvertFromStringToList(entity.MusclesString)),
            OrderNumber = entity.OrderNumber,
            Description = DescriptionViewModel.ConvertFromJson(entity.Description),
            TrainingExerciseId = entity.Id,
            SuperSetId = entity.SuperSetId,
            CodeNum = entity.CodeNum,
            Tags = ExerciseExtensions.ConvertTagIntToList(entity.TagsValue).ToList(),
        };

        TrainingExerciseViewModelExtensions.ConvertJsonBack(item, entity.WeightAndRepsString);
        return item;
    }

    /// <summary>Weight-and-reps JSON for an exercise suggested by the workout generator API.</summary>
    public static string ToWeightAndRepsString(int tagsValue, ExerciseQueryResponse exercise)
    {
        var exerciseModel = new TrainingExerciseViewModel();
        var tags = ExerciseExtensions.ConvertTagIntToList(tagsValue);
        try
        {
            if (tags.Contains(ExerciseTags.ExerciseByReps) || tags.Contains(ExerciseTags.ExerciseByRepsAndWeight))
            {
                exerciseModel.WeightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>();
                for (int i = 0; i < exercise.CountOfSets; i++)
                {
                    var isValid = TryGetReps(exercise.CountOfRepsOrTime, out var result);
                    exerciseModel.WeightAndRepsItems.Add(new WeightAndRepsViewModel(exercise.WorkingWeight, isValid ? result : WeightAndRepsViewModel.DefaultRepetitions));
                }
            }

            if (tags.Contains(ExerciseTags.ExerciseByTime))
            {
                exerciseModel.Time = TimeSpan.Parse(exercise.CountOfRepsOrTime);
            }
        }
        catch (Exception)
        {

        }

        return TrainingExerciseViewModelExtensions.ConvertJson(tags, exerciseModel);
    }

    public static bool TryGetReps(string input, out int number1)
    {
        number1 = 0;
        var number2 = 0;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Pattern 1: "{number} {text}" (e.g., "123 abc")
        var pattern1 = @"^\s*(\d+)\s+([a-zA-Z]+)\s*$";

        // Pattern 2: "{number}-{number}" (e.g., "123-456")
        var pattern2 = @"^\s*(\d+)\s*-\s*(\d+)\s*$";

        var match1 = Regex.Match(input, pattern1);
        if (match1.Success)
        {
            if (int.TryParse(match1.Groups[1].Value, out number1))
            {
                return true;
            }
        }

        var match2 = Regex.Match(input, pattern2);
        if (match2.Success)
        {
            if (int.TryParse(match2.Groups[1].Value, out number1) &&
                int.TryParse(match2.Groups[2].Value, out number2))
            {
                return true;
            }
        }

        return false;
    }
}
