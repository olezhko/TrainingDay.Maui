﻿using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("Exercises")]
public class ExerciseDto : Common.Models.Exercise
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}