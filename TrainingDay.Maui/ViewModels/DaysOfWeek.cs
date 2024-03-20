using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TrainingDay.Maui.ViewModels;

public class DaysOfWeek : BaseViewModel
{
    public static DaysOfWeek Parse(int alarmItemDays)
    {
        DaysOfWeek value = new DaysOfWeek();
        BitArray days = new BitArray(new[] { alarmItemDays });
        value.Monday = days.Get(0);
        value.Tuesday = days.Get(1);
        value.Wednesday = days.Get(2);
        value.Thursday = days.Get(3);
        value.Friday = days.Get(4);
        value.Saturday = days.Get(5);
        value.Sunday = days.Get(6);
        return value;
    }

    public int Value
    {
        get
        {
            return (int)(Math.Pow(2, 0) * (Monday ? 1 : 0) +
                         Math.Pow(2, 1) * (Tuesday ? 1 : 0) +
                         Math.Pow(2, 2) * (Wednesday ? 1 : 0) +
                         Math.Pow(2, 3) * (Thursday ? 1 : 0) +
                         Math.Pow(2, 4) * (Friday ? 1 : 0) +
                         Math.Pow(2, 5) * (Saturday ? 1 : 0) +
                         Math.Pow(2, 6) * (Sunday ? 1 : 0));
        }
    }

    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }

    public bool[] AllDays => new bool[] { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday };

    public DaysOfWeek() { }

    public DaysOfWeek(bool[] allDays)
    {
        if (allDays.Length != 7) return;

        Monday = allDays[0];
        Tuesday = allDays[1];
        Wednesday = allDays[2];
        Thursday = allDays[3];
        Friday = allDays[4];
        Saturday = allDays[5];
        Sunday = allDays[6];
    }

    public DaysOfWeek(bool monday, bool tuesday, bool wednesday, bool thursday, bool friday, bool saturday, bool sunday) : this(new bool[] { monday, tuesday, wednesday, thursday, friday, saturday, sunday })
    {
    }

    public static bool GetHasADayBeenSelected(DaysOfWeek days)
    {
        if (days == null)
            return false;

        return days.AllDays.Contains(true);
    }

    public bool Contains(int curDay)
    {
        if (curDay < 0)
        {
            return false;
        }

        return AllDays[curDay];
    }
}