using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.Services;

public interface IRepository
{
    IEnumerable<ExerciseEntity> GetExerciseItems();
    int SaveExerciseItem(ExerciseEntity item);

    IEnumerable<TrainingEntity> GetTrainingItems();
    int SaveTrainingItem(TrainingEntity item);

    IEnumerable<TrainingUnionEntity> GetTrainingsGroups();
    int SaveTrainingGroup(TrainingUnionEntity item);

    IEnumerable<SuperSetEntity> GetSuperSetItems();
    int SaveSuperSetItem(SuperSetEntity item);

    IEnumerable<TrainingExerciseEntity> GetTrainingExerciseItems();
    int SaveTrainingExerciseItem(TrainingExerciseEntity item);

    IEnumerable<LastTrainingEntity> GetLastTrainingItems();
    int SaveLastTrainingItem(LastTrainingEntity item);

    IEnumerable<LastTrainingExerciseEntity> GetLastTrainingExerciseItems();
    int SaveLastTrainingExerciseItem(LastTrainingExerciseEntity item);

    IEnumerable<WeightNoteEntity> GetWeightNotesItems();
    int SaveItem(object item, int id = 0);
}
