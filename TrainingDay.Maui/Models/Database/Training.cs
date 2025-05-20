﻿using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("Trainings")]
public class Training : TrainingDay.Common.Models.Training
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}