using TrainingDay.Maui.Models.Database;

namespace TrainingDay.Maui.ViewModels;

public class AlarmViewModel : BaseViewModel
{
    public Alarm AlarmItem { get; set; }
    public AlarmViewModel()
    {
        AlarmItem = new Alarm();
        AlarmItem.TimeOffset = DateTimeOffset.Now;
    }

    public AlarmViewModel(Alarm alarmItem)
    {
        AlarmItem = alarmItem;
    }

    public int TrainingId { get; set; }

    public bool IsActive
    {
        get => AlarmItem.IsActive;
        set
        {
            if (AlarmItem.IsActive != value)
            {
                AlarmItem.IsActive = value;
                OnPropertyChanged();
            }
        }
    }

    public DaysOfWeek Days
    {
        get => DaysOfWeek.Parse(AlarmItem.Days);
        set
        {
            if (AlarmItem.Days != value.Value)
            {
                AlarmItem.Days = value.Value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => AlarmItem.Name;
        set
        {
            AlarmItem.Name = value;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset TimeOffset
    {
        get => AlarmItem.TimeOffset;
        set
        {
            AlarmItem.TimeOffset = value;
            OnPropertyChanged();
        }
    }

    public TimeSpan Time
    {
        get { return AlarmItem.TimeOffset.LocalDateTime.TimeOfDay; }
        set { AlarmItem.TimeOffset = GetDateTimeOffsetFromTimeSpan(value); }
    }

    public DateTimeOffset GetDateTimeOffsetFromTimeSpan(TimeSpan time)
    {
        var now = DateTime.Now;
        var dateTime = new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, time.Seconds);
        return new DateTimeOffset(dateTime);
    }
}