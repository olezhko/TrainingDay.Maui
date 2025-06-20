﻿using SQLite;

namespace TrainingDay.Maui.Models.Database;

[Table("LastTrainings")]
public class LastTrainingDto : TrainingDay.Common.Models.LastTraining
{
    [PrimaryKey, AutoIncrement, Column("_id")]
    public new int Id { get; set; }
}