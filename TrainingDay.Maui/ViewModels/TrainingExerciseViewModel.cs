using Newtonsoft.Json;
using System.Collections.ObjectModel;
using TrainingDay.Common.Extensions;
using TrainingDay.Common.Models;
using TrainingDay.Maui.Extensions;
using TrainingDay.Maui.Services;
using ExerciseDto = TrainingDay.Maui.Models.Database.ExerciseDto;
using TrainingExerciseDto = TrainingDay.Maui.Models.Database.TrainingExerciseDto;

namespace TrainingDay.Maui.ViewModels;

public class TrainingExerciseViewModel : ExerciseViewModel
{
	private bool isSelected;
	private bool notFinished = true;
	private bool skipped;
	private ObservableCollection<WeightAndRepsViewModel> weightAndRepsItems = new ObservableCollection<WeightAndRepsViewModel>();
	private int superSetId = -1;
	private int superSetNum;
	private bool isTimeCalculating;
	private TimeSpan time;

	public TrainingExerciseViewModel()
	{
	}

	public TrainingExerciseViewModel(ExerciseDto exercise, TrainingExerciseDto comm)
	{
		try
		{
			Id = comm.Id;
			TrainingExerciseId = comm.Id;
			ExerciseId = exercise.Id;
			Muscles = new ObservableCollection<MuscleViewModel>(MusclesConverter.ConvertFromStringToList(exercise.MusclesString));
			Name = exercise.Name;
			TrainingId = comm.TrainingId;
			OrderNumber = comm.OrderNumber;
			SuperSetId = comm.SuperSetId;
			Tags = ExerciseExtensions.ConvertTagIntToList(exercise.TagsValue).ToList();
			CodeNum = exercise.CodeNum;
			ExerciseManager.ConvertJsonBack(this, comm.WeightAndRepsString);
			Description = DescriptionViewModel.ConvertFromJson(exercise.Description);
		}
		catch (Exception e)
		{
			LoggingService.TrackError(e);
		}
	}

	public bool IsSelected
	{
		get => isSelected;
		set
		{
			isSelected = value;
			OnPropertyChanged();
		}
	}

	public bool IsNotFinished
	{
		get => notFinished;
		set
		{
			notFinished = value;
			OnPropertyChanged();
		}
	}

	public bool IsSkipped
	{
		get => skipped;
		set
		{
			skipped = value;
			OnPropertyChanged();
		}
	}

	public int TrainingExerciseId { get; set; }

	public int ExerciseId { get; set; }

	public int TrainingId { get; set; }

	public int OrderNumber { get; set; }

	public int SuperSetId
	{
		get => superSetId;
		set
		{
			superSetId = value;
			OnPropertyChanged();
            OnPropertyChanged(nameof(IsCheckBoxVisible));
        }
    }

	public int SuperSetNum
	{
		get => superSetNum;
		set
		{
			superSetNum = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsCheckBoxVisible));
        }
	}

	public ObservableCollection<WeightAndRepsViewModel> WeightAndRepsItems
	{
		get => weightAndRepsItems;
		set
		{
			weightAndRepsItems = value;
			OnPropertyChanged();
		}
	}

	public DateTime StartCalculateDateTime { get; set; }

	public bool IsTimeCalculating
	{
		get => isTimeCalculating;
		set
		{
			isTimeCalculating = value;
			OnPropertyChanged();
		}
	}

	public TimeSpan Time
	{
		get => time;
		set
		{
			time = value;
			OnPropertyChanged();
		}
	}

	public TrainingExerciseDto GetTrainingExerciseComm()
	{
		string weightAndReps = ExerciseManager.ConvertJson(Tags, this);

		return new TrainingExerciseDto()
		{
			ExerciseId = ExerciseId,
			WeightAndRepsString = weightAndReps,
			TrainingId = TrainingId,
			Id = TrainingExerciseId,
			OrderNumber = OrderNumber,
			SuperSetId = SuperSetId,
		};
	}

	public override ExerciseDto GetExercise()
	{
		return new ExerciseDto()
		{
			Id = ExerciseId,
			Description = JsonConvert.SerializeObject(Description?.Model),
			Name = Name,
			MusclesString = MusclesConverter.ConvertFromListToString(Muscles.ToList()),
			TagsValue = ExerciseExtensions.ConvertTagListToInt(Tags),
			CodeNum = CodeNum,
		};
	}

	public TrainingExerciseViewModel Clone()
	{
		return new TrainingExerciseViewModel(GetExercise(), GetTrainingExerciseComm());
	}

	private bool isBeingDragged;
	public bool IsBeingDragged
	{
		get { return isBeingDragged; }
		set { SetProperty(ref isBeingDragged, value); }
	}

	private bool isBeingDraggedOver;
	public bool IsBeingDraggedOver
	{
		get { return isBeingDraggedOver; }
		set { SetProperty(ref isBeingDraggedOver, value); }
	}

	private bool isCheckBoxVisible = false;

	public bool IsCheckBoxVisible
	{
		get => isCheckBoxVisible;
		set => SetProperty(ref isCheckBoxVisible, value);
	}

	public static TrainingExerciseViewModel Create(ExerciseDto exercise)
	{
		return new TrainingExerciseViewModel(exercise, new TrainingExerciseDto()
		{
			WeightAndRepsString = GetDefaultWeightAndRepsString(exercise.TagsValue)
		});
	}

	private static string GetDefaultWeightAndRepsString(int tagsValue)
	{
		var tagsList = ExerciseExtensions.ConvertTagIntToList(tagsValue);
		string weightAndReps = string.Empty;
		if (tagsList.Contains(ExerciseTags.ExerciseByRepsAndWeight) || tagsList.Contains(ExerciseTags.ExerciseByReps))
		{
			weightAndReps = JsonConvert.SerializeObject(new List<WeightAndRepsViewModel>{
				new WeightAndRepsViewModel(0,15),new WeightAndRepsViewModel(0,15),new WeightAndRepsViewModel(0,15),
			});
		}

		return weightAndReps;
	}
}