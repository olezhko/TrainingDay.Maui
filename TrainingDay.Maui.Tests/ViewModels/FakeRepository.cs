using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.Services;

namespace TrainingDay.Maui.Tests.ViewModels;

/// <summary>
/// In-memory stand-in for the SQLite-backed <see cref="Repository"/>, used so
/// DataManageViewModel's export/import logic can run outside of a MAUI platform host.
/// </summary>
internal class FakeRepository : IRepository
{
    private int _nextId = 1;

    public List<ExerciseEntity> Exercises { get; } = new();
    public List<TrainingEntity> Trainings { get; } = new();
    public List<TrainingUnionEntity> TrainingGroups { get; } = new();
    public List<SuperSetEntity> SuperSets { get; } = new();
    public List<TrainingExerciseEntity> TrainingExercises { get; } = new();
    public List<LastTrainingEntity> LastTrainings { get; } = new();
    public List<LastTrainingExerciseEntity> LastTrainingExercises { get; } = new();
    public List<WeightNoteEntity> WeightNotes { get; } = new();

    public IEnumerable<ExerciseEntity> GetExerciseItems() => Exercises;

    public int SaveExerciseItem(ExerciseEntity item)
    {
        item.Id = _nextId++;
        Exercises.Add(item);
        return item.Id;
    }

    public IEnumerable<TrainingEntity> GetTrainingItems() => Trainings;

    public int SaveTrainingItem(TrainingEntity item)
    {
        item.Id = _nextId++;
        Trainings.Add(item);
        return item.Id;
    }

    public IEnumerable<TrainingUnionEntity> GetTrainingsGroups() => TrainingGroups;

    public int SaveTrainingGroup(TrainingUnionEntity item)
    {
        item.Id = _nextId++;
        TrainingGroups.Add(item);
        return item.Id;
    }

    public IEnumerable<SuperSetEntity> GetSuperSetItems() => SuperSets;

    public int SaveSuperSetItem(SuperSetEntity item)
    {
        item.Id = _nextId++;
        SuperSets.Add(item);
        return item.Id;
    }

    public IEnumerable<TrainingExerciseEntity> GetTrainingExerciseItems() => TrainingExercises;

    public int SaveTrainingExerciseItem(TrainingExerciseEntity item)
    {
        item.Id = _nextId++;
        TrainingExercises.Add(item);
        return item.Id;
    }

    public IEnumerable<LastTrainingEntity> GetLastTrainingItems() => LastTrainings;

    public int SaveLastTrainingItem(LastTrainingEntity item)
    {
        item.Id = _nextId++;
        LastTrainings.Add(item);
        return item.Id;
    }

    public IEnumerable<LastTrainingExerciseEntity> GetLastTrainingExerciseItems() => LastTrainingExercises;

    public int SaveLastTrainingExerciseItem(LastTrainingExerciseEntity item)
    {
        item.Id = _nextId++;
        LastTrainingExercises.Add(item);
        return item.Id;
    }

    public IEnumerable<WeightNoteEntity> GetWeightNotesItems() => WeightNotes;

    public int SaveItem(object item, int id = 0)
    {
        if (item is WeightNoteEntity note)
        {
            note.Id = _nextId++;
            WeightNotes.Add(note);
            return note.Id;
        }

        return 0;
    }
}
