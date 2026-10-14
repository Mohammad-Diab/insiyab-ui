using System.Globalization;

namespace Insiyab.Avalonia.Controls;

/// <summary>The calendar a date field shows and is picked in. What it holds is always a Gregorian <see cref="DateTime"/>, as the web field always sends Gregorian ISO.</summary>
public enum CalendarKind
{
    /// <summary>The Gregorian calendar.</summary>
    Gregorian,

    /// <summary>Umm al-Qura, the Hijri calendar most people mean (the web's <c>data-ins-calendar="hijri"</c>).</summary>
    Hijri,

    /// <summary>The tabular (arithmetical) Islamic calendar (the web's <c>hijri-civil</c>).</summary>
    HijriCivil,
}

internal readonly record struct DayParts(int Y, int M, int D);

// A calendar system as insiyab.js defines one: parts, fromParts (overflowing months and days roll on), monthLength, and an optional typed-date reader.
internal abstract class PickerCalendar
{
    public static readonly PickerCalendar Gregorian = new GregorianSystem();
    public static readonly PickerCalendar Hijri = new HijriSystem(new UmAlQuraCalendar());
    public static readonly PickerCalendar HijriCivil = new HijriSystem(new HijriCalendar { HijriAdjustment = -1 });

    public static PickerCalendar Of(CalendarKind kind) => kind switch
    {
        CalendarKind.Hijri => Hijri,
        CalendarKind.HijriCivil => HijriCivil,
        _ => Gregorian,
    };

    public abstract bool IsGregorian { get; }

    public abstract DayParts Parts(DateTime d);

    public abstract DateTime FromParts(int y, int m, int d);

    public abstract int MonthLength(int y, int m);

    // A typed date in this calendar; handled is false where the core's number reading should run instead.
    public virtual DateTime? Parse(string s, out bool handled)
    {
        handled = false;
        return null;
    }

    public bool SameMonth(DateTime a, DateTime b)
    {
        var p = Parts(a);
        var q = Parts(b);
        return p.Y == q.Y && p.M == q.M;
    }

    public DateTime MonthStart(DateTime d)
    {
        var p = Parts(d);
        return FromParts(p.Y, p.M, 1);
    }

    public DateTime AddMonths(DateTime d, int n)
    {
        var p = Parts(d);
        var y = p.Y;
        var m = p.M + n;
        y += (int)Math.Floor(m / 12.0);
        m = ((m % 12) + 12) % 12;
        return FromParts(y, m, Math.Min(p.D, MonthLength(y, m)));
    }

    private sealed class GregorianSystem : PickerCalendar
    {
        public override bool IsGregorian => true;

        public override DayParts Parts(DateTime d) => new(d.Year, d.Month - 1, d.Day);

        public override DateTime FromParts(int y, int m, int d)
        {
            y += (int)Math.Floor(m / 12.0);
            m = ((m % 12) + 12) % 12;
            y = Math.Clamp(y, 1, 9998);
            return new DateTime(y, m + 1, 1).AddDays(d - 1);
        }

        public override int MonthLength(int y, int m)
        {
            y += (int)Math.Floor(m / 12.0);
            m = ((m % 12) + 12) % 12;
            return DateTime.DaysInMonth(Math.Clamp(y, 1, 9999), m + 1);
        }
    }

    private sealed class HijriSystem(Calendar cal) : PickerCalendar
    {
        private readonly int _minYear = cal.GetYear(cal.MinSupportedDateTime.AddDays(2)) + 1;
        private readonly int _maxYear = cal.GetYear(cal.MaxSupportedDateTime.AddDays(-2)) - 1;

        public override bool IsGregorian => false;

        private DateTime Clamp(DateTime d) =>
            d < cal.MinSupportedDateTime ? cal.MinSupportedDateTime.Date.AddDays(1) : d > cal.MaxSupportedDateTime ? cal.MaxSupportedDateTime.Date.AddDays(-1) : d.Date;

        public override DayParts Parts(DateTime d)
        {
            d = Clamp(d);
            return new(cal.GetYear(d), cal.GetMonth(d) - 1, cal.GetDayOfMonth(d));
        }

        public override DateTime FromParts(int y, int m, int d)
        {
            y += (int)Math.Floor(m / 12.0);
            m = ((m % 12) + 12) % 12;
            y = Math.Clamp(y, _minYear, _maxYear);
            return cal.ToDateTime(y, m + 1, 1, 0, 0, 0, 0).AddDays(d - 1);
        }

        public override int MonthLength(int y, int m)
        {
            y += (int)Math.Floor(m / 12.0);
            m = ((m % 12) + 12) % 12;
            return cal.GetDaysInMonth(Math.Clamp(y, _minYear, _maxYear), m + 1);
        }

        // Day first as Hijri dates are written, unless a four-digit year leads; a year past 1700 is the Gregorian date meant, a two-digit one this Hijri century.
        public override DateTime? Parse(string s, out bool handled)
        {
            handled = false;
            var n = PickerParse.Numbers(s);
            if (n.Count != 3) return null;
            handled = true;
            int y, m, d;
            if (n[0].Length == 4) { y = PickerParse.Int(n[0]); m = PickerParse.Int(n[1]); d = PickerParse.Int(n[2]); }
            else { d = PickerParse.Int(n[0]); m = PickerParse.Int(n[1]); y = PickerParse.Int(n[2]); }
            if (m < 1 || m > 12 || d < 1) return null;
            if (y > 1700) return PickerParse.RealGregorian(y, m, d);
            if (y < 100) y += 1400;
            if (y < _minYear || y > _maxYear) return null;
            if (d > MonthLength(y, m - 1)) return null;
            return FromParts(y, m - 1, d);
        }
    }
}
