using System;

namespace ZeroPrimitives.Calendar
{
    /// <summary>
    /// Bitmask representing active working days in a week.
    /// Bit positions map directly to (1 &lt;&lt; (int)DayOfWeek).
    /// </summary>
    [Flags]
    public enum WorkDayMask : byte
    {
        None        = 0,
        Sunday      = 1 << DayOfWeek.Sunday,    // 1 << 0 = 1
        Monday      = 1 << DayOfWeek.Monday,    // 1 << 1 = 2
        Tuesday     = 1 << DayOfWeek.Tuesday,   // 1 << 2 = 4
        Wednesday   = 1 << DayOfWeek.Wednesday, // 1 << 3 = 8
        Thursday    = 1 << DayOfWeek.Thursday,  // 1 << 4 = 16
        Friday      = 1 << DayOfWeek.Friday,    // 1 << 5 = 32
        Saturday    = 1 << DayOfWeek.Saturday,  // 1 << 6 = 64

        /// <summary>
        /// Standard Monday through Friday working week (5 days, weekend off).
        /// </summary>
        Standard = Monday | Tuesday | Wednesday | Thursday | Friday,

        /// <summary>
        /// Factory/Manufacturing schedule: Monday through Saturday (6 days, Sunday off).
        /// </summary>
        FactoryDefault = Standard | Saturday,

        /// <summary>
        /// Continuous 24/7 schedule: All 7 days a week.
        /// </summary>
        Continuous = Standard | Saturday | Sunday
    }
}
