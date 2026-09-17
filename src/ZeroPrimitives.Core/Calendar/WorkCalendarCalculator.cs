using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZeroPrimitives.Calendar
{
    /// <summary>
    /// Ultra-fast, zero-allocation work day and working hours calculator.
    /// Decouples pure mathematical calendar computation from user-defined holiday policies.
    /// </summary>
    public static class WorkCalendarCalculator
    {
        /// <summary>
        /// Determines if a specific DayOfWeek is an active working day according to the given mask.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWorkDay(DayOfWeek dayOfWeek, WorkDayMask mask)
        {
            return ((byte)mask & (1 << (int)dayOfWeek)) != 0;
        }

        /// <summary>
        /// Determines if a specific date is a working day, checking both weekly schedule and user-defined holidays.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWorkDay(
            DateTime date,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            if (!IsWorkDay(date.DayOfWeek, workDays)) return false;

            if (!customHolidays.IsEmpty)
            {
                DateTime target = date.Date;
                for (int i = 0; i < customHolidays.Length; i++)
                {
                    if (customHolidays[i].Date == target) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Counts total active working days per week in the specified mask (0 to 7).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorkDaysPerWeek(WorkDayMask mask)
        {
            byte b = (byte)mask;
            int count = 0;
            while (b != 0)
            {
                count += b & 1;
                b >>= 1;
            }
            return count;
        }

        /// <summary>
        /// Counts the total number of working days between startDate and endDate (inclusive),
        /// excluding non-working days in the week and user-specified holidays.
        /// Zero heap allocation when using ReadOnlySpan.
        /// </summary>
        public static int CountWorkingDays(
            DateTime startDate,
            DateTime endDate,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            if (startDate > endDate) return 0;

            DateTime start = startDate.Date;
            DateTime end = endDate.Date;

            int rawDays = CalculateRawWorkDays(start, end, workDays);
            if (rawDays == 0 || customHolidays.IsEmpty)
            {
                return rawDays;
            }

            int holidayExclusions = 0;
            for (int i = 0; i < customHolidays.Length; i++)
            {
                DateTime h = customHolidays[i].Date;
                if (h < start || h > end) continue;
                if (!IsWorkDay(h.DayOfWeek, workDays)) continue;

                // Prevent double-counting duplicate holiday inputs
                bool duplicate = false;
                for (int j = 0; j < i; j++)
                {
                    if (customHolidays[j].Date == h)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    holidayExclusions++;
                }
            }

            int result = rawDays - holidayExclusions;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// Counts working days with custom holidays provided as an IEnumerable collection.
        /// </summary>
        public static int CountWorkingDays(
            DateTime startDate,
            DateTime endDate,
            WorkDayMask workDays,
            IEnumerable<DateTime>? customHolidays)
        {
            if (customHolidays == null)
            {
                return CountWorkingDays(startDate, endDate, workDays, default(ReadOnlySpan<DateTime>));
            }

            if (customHolidays is DateTime[] array)
            {
                return CountWorkingDays(startDate, endDate, workDays, new ReadOnlySpan<DateTime>(array));
            }

            if (startDate > endDate) return 0;

            DateTime start = startDate.Date;
            DateTime end = endDate.Date;
            int rawDays = CalculateRawWorkDays(start, end, workDays);
            if (rawDays == 0) return 0;

            var set = customHolidays as HashSet<DateTime> ?? new HashSet<DateTime>(customHolidays);
            if (set.Count == 0) return rawDays;

            int holidayExclusions = 0;
            foreach (var holiday in set)
            {
                DateTime h = holiday.Date;
                if (h >= start && h <= end && IsWorkDay(h.DayOfWeek, workDays))
                {
                    holidayExclusions++;
                }
            }

            int result = rawDays - holidayExclusions;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// Calculates total working hours across the date range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal CalculateWorkingHours(
            DateTime startDate,
            DateTime endDate,
            decimal hoursPerDay = 8m,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            int days = CountWorkingDays(startDate, endDate, workDays, customHolidays);
            return days * hoursPerDay;
        }

        /// <summary>
        /// Calculates total working hours across the date range with IEnumerable holidays.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal CalculateWorkingHours(
            DateTime startDate,
            DateTime endDate,
            decimal hoursPerDay,
            WorkDayMask workDays,
            IEnumerable<DateTime>? customHolidays)
        {
            int days = CountWorkingDays(startDate, endDate, workDays, customHolidays);
            return days * hoursPerDay;
        }

        /// <summary>
        /// Calculates average daily working hours (totalWorkingHours / workingDaysCount).
        /// </summary>
        public static decimal CalculateDailyAverageHours(
            DateTime startDate,
            DateTime finishDate,
            decimal totalWorkingHours,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            int totalDays = CountWorkingDays(startDate, finishDate, workDays, customHolidays);
            if (totalDays <= 0 || totalWorkingHours <= 0)
                return 0m;

            return Math.Round(totalWorkingHours / totalDays, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates average daily working hours with IEnumerable holidays.
        /// </summary>
        public static decimal CalculateDailyAverageHours(
            DateTime startDate,
            DateTime finishDate,
            decimal totalWorkingHours,
            WorkDayMask workDays,
            IEnumerable<DateTime>? customHolidays)
        {
            int totalDays = CountWorkingDays(startDate, finishDate, workDays, customHolidays);
            if (totalDays <= 0 || totalWorkingHours <= 0)
                return 0m;

            return Math.Round(totalWorkingHours / totalDays, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Adds a specified number of working days to a given date, skipping weekends and holidays.
        /// </summary>
        public static DateTime AddWorkDays(
            DateTime startDate,
            int workDaysToAdd,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            if (workDaysToAdd == 0)
            {
                return IsWorkDay(startDate, workDays, customHolidays)
                    ? startDate
                    : GetNextWorkDay(startDate, workDays, customHolidays);
            }

            int direction = workDaysToAdd > 0 ? 1 : -1;
            int remaining = Math.Abs(workDaysToAdd);
            DateTime current = startDate.Date;

            while (remaining > 0)
            {
                current = current.AddDays(direction);
                if (IsWorkDay(current, workDays, customHolidays))
                {
                    remaining--;
                }
            }

            return current;
        }

        /// <summary>
        /// Finds the next valid working day on or after the specified date.
        /// </summary>
        public static DateTime GetNextWorkDay(
            DateTime date,
            WorkDayMask workDays = WorkDayMask.Standard,
            ReadOnlySpan<DateTime> customHolidays = default)
        {
            DateTime current = date.Date;
            while (!IsWorkDay(current, workDays, customHolidays))
            {
                current = current.AddDays(1);
            }
            return current;
        }

        /// <summary>
        /// Internal mathematical O(1) computation of standard working days without allocations.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateRawWorkDays(DateTime start, DateTime end, WorkDayMask workDays)
        {
            int totalCalendarDays = (end - start).Days + 1;
            if (totalCalendarDays <= 0) return 0;

            int daysPerWeek = GetWorkDaysPerWeek(workDays);
            if (daysPerWeek == 7) return totalCalendarDays;
            if (daysPerWeek == 0) return 0;

            int fullWeeks = totalCalendarDays / 7;
            int remainder = totalCalendarDays % 7;
            int count = fullWeeks * daysPerWeek;

            if (remainder > 0)
            {
                int startDay = (int)start.DayOfWeek;
                for (int i = 0; i < remainder; i++)
                {
                    DayOfWeek dow = (DayOfWeek)((startDay + i) % 7);
                    if (IsWorkDay(dow, workDays))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }
}
