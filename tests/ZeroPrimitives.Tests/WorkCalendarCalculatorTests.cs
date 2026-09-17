using System;
using System.Collections.Generic;
using Xunit;
using ZeroPrimitives.Calendar;

namespace ZeroPrimitives.Tests
{
    public class WorkCalendarCalculatorTests
    {
        [Fact]
        public void IsWorkDay_DayOfWeek_EvaluatesCorrectly()
        {
            Assert.True(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Monday, WorkDayMask.Standard));
            Assert.True(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Friday, WorkDayMask.Standard));
            Assert.False(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Saturday, WorkDayMask.Standard));
            Assert.False(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Sunday, WorkDayMask.Standard));

            Assert.True(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Saturday, WorkDayMask.FactoryDefault));
            Assert.False(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Sunday, WorkDayMask.FactoryDefault));

            Assert.True(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Sunday, WorkDayMask.Continuous));
            Assert.False(WorkCalendarCalculator.IsWorkDay(DayOfWeek.Monday, WorkDayMask.None));
        }

        [Fact]
        public void CountWorkingDays_SingleWeek_ReturnsCorrectDays()
        {
            // 2026-09-14 is Monday, 2026-09-20 is Sunday
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime friday = new DateTime(2026, 9, 18);
            DateTime sunday = new DateTime(2026, 9, 20);

            // Monday to Friday: 5 days
            Assert.Equal(5, WorkCalendarCalculator.CountWorkingDays(monday, friday, WorkDayMask.Standard));

            // Monday to Sunday:
            // Standard (T2-T6): 5 days
            Assert.Equal(5, WorkCalendarCalculator.CountWorkingDays(monday, sunday, WorkDayMask.Standard));
            // FactoryDefault (T2-T7): 6 days
            Assert.Equal(6, WorkCalendarCalculator.CountWorkingDays(monday, sunday, WorkDayMask.FactoryDefault));
            // Continuous (T2-CN): 7 days
            Assert.Equal(7, WorkCalendarCalculator.CountWorkingDays(monday, sunday, WorkDayMask.Continuous));
        }

        [Fact]
        public void CountWorkingDays_MultiWeeksAndMonthBoundary()
        {
            // 2026-09-01 (Tuesday) to 2026-09-30 (Wednesday)
            // Total 30 calendar days: 4 full weeks (28 days) + 2 remainder days (Tue 29, Wed 30)
            // 4 * 5 = 20 work days + 2 remainder days = 22 work days.
            DateTime start = new DateTime(2026, 9, 1);
            DateTime end = new DateTime(2026, 9, 30);

            Assert.Equal(22, WorkCalendarCalculator.CountWorkingDays(start, end, WorkDayMask.Standard));
        }

        [Fact]
        public void CountWorkingDays_WithCustomHolidays_SpanAndCollection()
        {
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime friday = new DateTime(2026, 9, 18);

            // 1 holiday on Wednesday (2026-09-16)
            var holidays = new[] { new DateTime(2026, 9, 16) };

            // Using ReadOnlySpan
            int spanResult = WorkCalendarCalculator.CountWorkingDays(
                monday, friday, WorkDayMask.Standard, holidays.AsSpan());
            Assert.Equal(4, spanResult);

            // Using IEnumerable
            int listResult = WorkCalendarCalculator.CountWorkingDays(
                monday, friday, WorkDayMask.Standard, (IEnumerable<DateTime>)holidays);
            Assert.Equal(4, listResult);
        }

        [Fact]
        public void CountWorkingDays_HolidayOnWeekend_DoesNotDoubleSubtract()
        {
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime sunday = new DateTime(2026, 9, 20);

            // Holiday falls on Saturday 2026-09-19
            var weekendHoliday = new[] { new DateTime(2026, 9, 19) };

            // In Standard schedule, Saturday is already off, so it should still be 5 days, NOT 4
            int resultStandard = WorkCalendarCalculator.CountWorkingDays(
                monday, sunday, WorkDayMask.Standard, weekendHoliday.AsSpan());
            Assert.Equal(5, resultStandard);

            // In Factory schedule (works Saturday), Saturday holiday IS subtracted: 6 - 1 = 5
            int resultFactory = WorkCalendarCalculator.CountWorkingDays(
                monday, sunday, WorkDayMask.FactoryDefault, weekendHoliday.AsSpan());
            Assert.Equal(5, resultFactory);
        }

        [Fact]
        public void CountWorkingDays_DuplicateHolidaysInInput_HandledSafely()
        {
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime friday = new DateTime(2026, 9, 18);

            // User accidentally passed the same holiday twice
            var duplicateHolidays = new[]
            {
                new DateTime(2026, 9, 16),
                new DateTime(2026, 9, 16)
            };

            int result = WorkCalendarCalculator.CountWorkingDays(
                monday, friday, WorkDayMask.Standard, duplicateHolidays.AsSpan());
            Assert.Equal(4, result); // Subtracted once, not twice
        }

        [Fact]
        public void CountWorkingDays_EdgeCases_StartDateAfterEndDate()
        {
            DateTime start = new DateTime(2026, 9, 20);
            DateTime end = new DateTime(2026, 9, 14);

            Assert.Equal(0, WorkCalendarCalculator.CountWorkingDays(start, end, WorkDayMask.Standard));
        }

        [Fact]
        public void CalculateWorkingHours_ComputesAccurately()
        {
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime friday = new DateTime(2026, 9, 18);

            // 5 days * 8h = 40h
            decimal hoursStandard = WorkCalendarCalculator.CalculateWorkingHours(monday, friday);
            Assert.Equal(40m, hoursStandard);

            // 5 days * 7.5h = 37.5h
            decimal hoursCustom = WorkCalendarCalculator.CalculateWorkingHours(monday, friday, hoursPerDay: 7.5m);
            Assert.Equal(37.5m, hoursCustom);
        }

        [Fact]
        public void CalculateDailyAverageHours_RoundsCorrectly()
        {
            DateTime monday = new DateTime(2026, 9, 14);
            DateTime wednesday = new DateTime(2026, 9, 16);

            // 3 working days (Mon, Tue, Wed), total 20 hours -> 20 / 3 = 6.67
            decimal avg = WorkCalendarCalculator.CalculateDailyAverageHours(monday, wednesday, 20m);
            Assert.Equal(6.67m, avg);

            // Total hours 0 or invalid days -> 0
            Assert.Equal(0m, WorkCalendarCalculator.CalculateDailyAverageHours(monday, wednesday, 0m));
        }

        [Fact]
        public void AddWorkDays_SkipsWeekendsAndHolidays()
        {
            // 2026-09-18 is Friday
            DateTime friday = new DateTime(2026, 9, 18);
            
            // Add 1 work day -> Monday 2026-09-21
            DateTime nextWorkDay = WorkCalendarCalculator.AddWorkDays(friday, 1, WorkDayMask.Standard);
            Assert.Equal(new DateTime(2026, 9, 21), nextWorkDay);

            // Add 1 work day with Monday 2026-09-21 as Holiday -> Tuesday 2026-09-22
            var mondayHoliday = new[] { new DateTime(2026, 9, 21) };
            DateTime nextAfterHoliday = WorkCalendarCalculator.AddWorkDays(
                friday, 1, WorkDayMask.Standard, mondayHoliday.AsSpan());
            Assert.Equal(new DateTime(2026, 9, 22), nextAfterHoliday);
        }

        [Fact]
        public void GetNextWorkDay_FromWeekend_ReturnsMonday()
        {
            // Saturday 2026-09-19
            DateTime saturday = new DateTime(2026, 9, 19);
            DateTime next = WorkCalendarCalculator.GetNextWorkDay(saturday, WorkDayMask.Standard);
            Assert.Equal(new DateTime(2026, 9, 21), next);
        }
    }
}
