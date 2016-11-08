using System;
using System.Threading;
using Quartz.Impl.Calendar;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Quartz.DynamoDB.Tests.Integration.JobStore
{
    /// <summary>
    /// Contains tests related to the loading of calendars.
    /// </summary>
    public class CalendarGetTests : IDisposable
    {
        private readonly DynamoDB.JobStore _sut;

        public CalendarGetTests()
        {
            _sut = new Quartz.DynamoDB.JobStore();
            var signaler = new Quartz.DynamoDB.Tests.Integration.RamJobStoreTests.SampleSignaler();
            var loadHelper = new SimpleTypeLoadHelper();

            _sut.Initialize(loadHelper, signaler);
        }

        /// <summary>
        /// Tests that after a calendar is added, the number of calendars increments.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void GetNumberOfCalendarsIncrementsWhenCalendarAdded()
        {
            var calendarCount = _sut.GetNumberOfCalendars();

            MonthlyCalendar cal = new MonthlyCalendar();
            string calName = Guid.NewGuid().ToString();
            _sut.StoreCalendar(calName, cal, false, true);

            // Dynamo describe table is eventually consistent so give it a little time. Flaky I know, but hey - what are you going to do?
            Thread.Sleep(50);

            var newCount = _sut.GetNumberOfCalendars();

            Assert.Equal(calendarCount + 1, newCount);
        }

        /// <summary>
        /// Tests that after a calendar is added, the calendar exists method returns true.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void CalendarExistsWhenCalendarAdded()
        {
            MonthlyCalendar cal = new MonthlyCalendar();
            string calName = Guid.NewGuid().ToString();

            Assert.False(_sut.CalendarExists(calName));

            _sut.StoreCalendar(calName, cal, false, true);

            Assert.True(_sut.CalendarExists(calName));
        }

        /// <summary>
        /// Tests that after a calendar is added, the get calendar names method returns its name.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void CalendarNamesReturned()
        {
            MonthlyCalendar cal = new MonthlyCalendar();
            string calName = Guid.NewGuid().ToString();

            _sut.StoreCalendar(calName, cal, false, true);

            var result = _sut.GetCalendarNames();

            Assert.True(result.Contains(calName));
        }

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}

