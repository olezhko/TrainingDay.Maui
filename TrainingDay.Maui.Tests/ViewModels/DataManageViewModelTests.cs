using System.Globalization;
using TrainingDay.Maui.Models.Database;
using TrainingDay.Maui.ViewModels;

namespace TrainingDay.Maui.Tests.ViewModels;

[TestFixture]
public class DataManageViewModelTests
{
    private CultureInfo _originalCulture;
    private CultureInfo _originalUiCulture;

    [SetUp]
    public void SetUp()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUiCulture = CultureInfo.CurrentUICulture;
    }

    [TearDown]
    public void TearDown()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUiCulture;
    }

    private static void WithCulture(string cultureName, Action action)
    {
        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        action();
    }

    [Test]
    public void ExportThenImport_RoundTripsWeightAndDate_WhenExportedUnderRuAndImportedUnderEn()
    {
        var sourceRepository = new FakeRepository();
        var date = new DateTime(2026, 3, 15, 8, 30, 0, DateTimeKind.Utc);
        sourceRepository.WeightNotes.Add(new WeightNoteEntity { Weight = 82.5, Date = date, Type = 1 });

        string json = null;
        WithCulture("ru-RU", () =>
        {
            var exportViewModel = new DataManageViewModel(sourceRepository);
            json = exportViewModel.BuildExportJson();
        });

        // The exported number must use an invariant decimal point, not the ru-RU comma.
        Assert.That(json, Does.Contain("82.5"));
        Assert.That(json, Does.Not.Contain("82,5"));

        var targetRepository = new FakeRepository();
        WithCulture("en-US", () =>
        {
            var importViewModel = new DataManageViewModel(targetRepository);
            importViewModel.ApplyImportJson(json);
        });

        Assert.That(targetRepository.WeightNotes, Has.Count.EqualTo(1));
        Assert.That(targetRepository.WeightNotes[0].Weight, Is.EqualTo(82.5));
        Assert.That(targetRepository.WeightNotes[0].Date, Is.EqualTo(date));
    }

    [Test]
    public void ExportThenImport_RoundTripsElapsedTime_WhenExportedUnderRuAndImportedUnderEn()
    {
        var sourceRepository = new FakeRepository();
        var training = new TrainingEntity { Title = "Leg day" };
        sourceRepository.SaveTrainingItem(training);
        var trainingTime = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc);
        var elapsed = new TimeSpan(0, 1, 23, 45);
        sourceRepository.LastTrainings.Add(new LastTrainingEntity
        {
            Title = training.Title,
            Time = trainingTime,
            ElapsedTime = elapsed,
            TrainingId = training.Id,
        });

        string json = null;
        WithCulture("ru-RU", () =>
        {
            var exportViewModel = new DataManageViewModel(sourceRepository);
            json = exportViewModel.BuildExportJson();
        });

        var targetRepository = new FakeRepository();
        WithCulture("en-US", () =>
        {
            var importViewModel = new DataManageViewModel(targetRepository);
            importViewModel.ApplyImportJson(json);
        });

        Assert.That(targetRepository.LastTrainings, Has.Count.EqualTo(1));
        Assert.That(targetRepository.LastTrainings[0].Time, Is.EqualTo(trainingTime));
        Assert.That(targetRepository.LastTrainings[0].ElapsedTime, Is.EqualTo(elapsed));
    }

    [Test]
    public void ExportThenImport_RemapsTrainingAndExerciseIds()
    {
        var sourceRepository = new FakeRepository();
        var exercise = new ExerciseEntity { Name = "Squat", CodeNum = 0 };
        sourceRepository.SaveExerciseItem(exercise);
        var training = new TrainingEntity { Title = "Leg day" };
        sourceRepository.SaveTrainingItem(training);
        var superSet = new SuperSetEntity { Count = 2, TrainingId = training.Id };
        sourceRepository.SaveSuperSetItem(superSet);
        var trainingExercise = new TrainingExerciseEntity
        {
            TrainingId = training.Id,
            ExerciseId = exercise.Id,
            SuperSetId = superSet.Id,
            OrderNumber = 1,
            WeightAndRepsString = "[]",
        };
        sourceRepository.SaveTrainingExerciseItem(trainingExercise);

        var exportViewModel = new DataManageViewModel(sourceRepository);
        var json = exportViewModel.BuildExportJson();

        var targetRepository = new FakeRepository();
        // Simulate the target phone already having its own data with clashing ids.
        targetRepository.SaveExerciseItem(new ExerciseEntity { Name = "Existing", CodeNum = 0 });
        targetRepository.SaveTrainingItem(new TrainingEntity { Title = "Existing" });

        var importViewModel = new DataManageViewModel(targetRepository);
        importViewModel.ApplyImportJson(json);

        Assert.That(targetRepository.Exercises, Has.Count.EqualTo(2));
        Assert.That(targetRepository.Trainings, Has.Count.EqualTo(2));
        Assert.That(targetRepository.TrainingExercises, Has.Count.EqualTo(1));

        var importedExercise = targetRepository.Exercises.Last();
        var importedTraining = targetRepository.Trainings.Last();
        var importedSuperSet = targetRepository.SuperSets.Single();
        var importedTrainingExercise = targetRepository.TrainingExercises.Single();

        Assert.That(importedTrainingExercise.ExerciseId, Is.EqualTo(importedExercise.Id));
        Assert.That(importedTrainingExercise.TrainingId, Is.EqualTo(importedTraining.Id));
        Assert.That(importedTrainingExercise.SuperSetId, Is.EqualTo(importedSuperSet.Id));
        Assert.That(importedSuperSet.TrainingId, Is.EqualTo(importedTraining.Id));
    }

    [Test]
    public void ApplyImportJson_SkipsOrphanedSuperSetAndLastTraining_InsteadOfThrowing()
    {
        // Reproduces a real exported file where a SuperSet and several LastTrainings
        // referenced a TrainingId whose Training row no longer existed (deleted on the
        // source phone without cleaning up its dependents).
        var validTraining = new TrainingEntity { Id = 1, Title = "Leg day" };
        var orphanedSuperSet = new SuperSetEntity { Id = 1, Count = 2, TrainingId = 8 };
        var validLastTraining = new LastTrainingEntity { Id = 1, Title = "Leg day", TrainingId = 1, Time = DateTime.UtcNow, ElapsedTime = TimeSpan.FromMinutes(30) };
        var orphanedLastTraining = new LastTrainingEntity { Id = 2, Title = "Deleted training", TrainingId = 8, Time = DateTime.UtcNow, ElapsedTime = TimeSpan.FromMinutes(10) };

        var data = new RepositoryData
        {
            Trainings = new[] { validTraining },
            TrainingExercises = Array.Empty<TrainingExerciseEntity>(),
            Groups = Array.Empty<TrainingUnionEntity>(),
            Exercises = Array.Empty<ExerciseEntity>(),
            WeightNotes = Array.Empty<WeightNoteEntity>(),
            SuperSets = new[] { orphanedSuperSet },
            LastTrainings = new[] { validLastTraining, orphanedLastTraining },
            LastTrainingExercises = Array.Empty<LastTrainingExerciseEntity>(),
        };
        var json = System.Text.Json.JsonSerializer.Serialize(data);

        var targetRepository = new FakeRepository();
        var importViewModel = new DataManageViewModel(targetRepository);

        Assert.DoesNotThrow(() => importViewModel.ApplyImportJson(json));

        Assert.That(targetRepository.Trainings, Has.Count.EqualTo(1));
        Assert.That(targetRepository.SuperSets, Is.Empty);
        Assert.That(targetRepository.LastTrainings, Has.Count.EqualTo(1));
        Assert.That(targetRepository.LastTrainings[0].Title, Is.EqualTo("Leg day"));
    }

    [Test]
    public void ApplyImportJson_SkipsUnmatchedLibraryExercise_InsteadOfThrowing()
    {
        // A library exercise (CodeNum != 0) whose CodeNum has no match in the target's
        // base exercise list previously threw a NullReferenceException from
        // FirstOrDefault(...).Id, aborting the whole import.
        var unmatchedExercise = new ExerciseEntity { Id = 1, Name = "Retired exercise", CodeNum = 999 };
        var trainingExerciseForUnmatched = new TrainingExerciseEntity
        {
            Id = 1,
            TrainingId = 1,
            ExerciseId = 1,
            OrderNumber = 1,
            WeightAndRepsString = "[]",
        };
        var matchedExercise = new ExerciseEntity { Id = 2, Name = "Squat", CodeNum = 42 };
        var trainingExerciseForMatched = new TrainingExerciseEntity
        {
            Id = 2,
            TrainingId = 1,
            ExerciseId = 2,
            OrderNumber = 2,
            WeightAndRepsString = "[]",
        };
        var training = new TrainingEntity { Id = 1, Title = "Leg day" };

        var data = new RepositoryData
        {
            Trainings = new[] { training },
            TrainingExercises = new[] { trainingExerciseForUnmatched, trainingExerciseForMatched },
            Groups = Array.Empty<TrainingUnionEntity>(),
            Exercises = new[] { unmatchedExercise, matchedExercise },
            WeightNotes = Array.Empty<WeightNoteEntity>(),
            SuperSets = Array.Empty<SuperSetEntity>(),
            LastTrainings = Array.Empty<LastTrainingEntity>(),
            LastTrainingExercises = Array.Empty<LastTrainingExerciseEntity>(),
        };
        var json = System.Text.Json.JsonSerializer.Serialize(data);

        var targetRepository = new FakeRepository();
        // Target's base library only has the CodeNum 42 exercise; CodeNum 999 doesn't exist here.
        var baseExercise = new ExerciseEntity { Name = "Squat", CodeNum = 42 };
        targetRepository.SaveExerciseItem(baseExercise);

        var importViewModel = new DataManageViewModel(targetRepository);

        Assert.DoesNotThrow(() => importViewModel.ApplyImportJson(json));

        Assert.That(targetRepository.Trainings, Has.Count.EqualTo(1));
        Assert.That(targetRepository.TrainingExercises, Has.Count.EqualTo(1));
        Assert.That(targetRepository.TrainingExercises[0].ExerciseId, Is.EqualTo(baseExercise.Id));
    }

    [Test]
    public void ExportThenImport_MatchesBaseExerciseByCodeNum_InsteadOfDuplicating()
    {
        var sourceRepository = new FakeRepository();
        var exercise = new ExerciseEntity { Name = "Bench press", CodeNum = 42 };
        sourceRepository.SaveExerciseItem(exercise);

        var exportViewModel = new DataManageViewModel(sourceRepository);
        var json = exportViewModel.BuildExportJson();

        var targetRepository = new FakeRepository();
        var baseExercise = new ExerciseEntity { Name = "Bench press", CodeNum = 42 };
        targetRepository.SaveExerciseItem(baseExercise);

        var importViewModel = new DataManageViewModel(targetRepository);
        importViewModel.ApplyImportJson(json);

        Assert.That(targetRepository.Exercises, Has.Count.EqualTo(1));
        Assert.That(targetRepository.Exercises[0].Id, Is.EqualTo(baseExercise.Id));
    }
}
