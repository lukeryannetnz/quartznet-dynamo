﻿#region License

/* 
 * All content copyright Terracotta, Inc., unless otherwise indicated. All rights reserved. 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not 
 * use this file except in compliance with the License. You may obtain a copy 
 * of the License at 
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0 
 *   
 * Unless required by applicable law or agreed to in writing, software 
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations 
 * under the License.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using Quartz.Job;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Quartz.DynamoDB.Tests
{
    /// <summary>
    ///  Unit test for JobStore. 
    /// <author>These tests were submitted to Quartz.NET for the RAMJobStoreTest by Johannes Zillmann as part of issue QUARTZ-306.</author>
    /// <author>Luke Ryan - Adapted to test the Dynamo DB store.</author>
    /// </summary>
    public class JobStoreTest
    {
        private readonly IJobStore fJobStore;
        private readonly JobDetailImpl fJobDetail;
        private readonly SampleSignaler fSignaler;

        public JobStoreTest()
        {
            fJobStore = new JobStore();
            fSignaler = new SampleSignaler();
            fJobStore.Initialize(null, fSignaler);
            fJobStore.SchedulerStarted();

            fJobDetail = new JobDetailImpl("job1", "jobGroup1", typeof(NoOpJob));
            fJobDetail.Durable = true;
            fJobStore.StoreJob(fJobDetail, false);
        }

        [Fact]
        public void TestAcquireNextTrigger()
        {
            DateTimeOffset d = DateBuilder.EvenMinuteDateAfterNow();
            IOperableTrigger trigger1 = new SimpleTriggerImpl("trigger1", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddSeconds(200), d.AddSeconds(200), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger2 = new SimpleTriggerImpl("trigger2", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddSeconds(50), d.AddSeconds(200), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger3 = new SimpleTriggerImpl("trigger1", "triggerGroup2", fJobDetail.Name, fJobDetail.Group, d.AddSeconds(100), d.AddSeconds(200), 2, TimeSpan.FromSeconds(2));

            trigger1.ComputeFirstFireTimeUtc(null);
            trigger2.ComputeFirstFireTimeUtc(null);
            trigger3.ComputeFirstFireTimeUtc(null);
            fJobStore.StoreTrigger(trigger1, false);
            fJobStore.StoreTrigger(trigger2, false);
            fJobStore.StoreTrigger(trigger3, false);

            DateTimeOffset firstFireTime = trigger1.GetNextFireTimeUtc().Value;

            Assert.Equal(0, fJobStore.AcquireNextTriggers(d.AddMilliseconds(10), 1, TimeSpan.Zero).Count);
            Assert.Equal(trigger2, fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 1, TimeSpan.Zero)[0]);
            Assert.Equal(trigger3, fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 1, TimeSpan.Zero)[0]);
            Assert.Equal(trigger1, fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 1, TimeSpan.Zero)[0]);
            Assert.Equal(0, fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 1, TimeSpan.Zero).Count);


            // release trigger3
            fJobStore.ReleaseAcquiredTrigger(trigger3);
            Assert.Equal(trigger3, fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 1, TimeSpan.FromMilliseconds(1))[0]);
        }

        [Fact]
        public void TestAcquireNextTriggerBatch()
        {
            DateTimeOffset d = DateBuilder.EvenMinuteDateAfterNow();

            IOperableTrigger early = new SimpleTriggerImpl("early", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d, d.AddMilliseconds(5), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger1 = new SimpleTriggerImpl("trigger1", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddMilliseconds(200000), d.AddMilliseconds(200005), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger2 = new SimpleTriggerImpl("trigger2", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddMilliseconds(200100), d.AddMilliseconds(200105), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger3 = new SimpleTriggerImpl("trigger3", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddMilliseconds(200200), d.AddMilliseconds(200205), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger4 = new SimpleTriggerImpl("trigger4", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, d.AddMilliseconds(200300), d.AddMilliseconds(200305), 2, TimeSpan.FromSeconds(2));
            IOperableTrigger trigger10 = new SimpleTriggerImpl("trigger10", "triggerGroup2", fJobDetail.Name, fJobDetail.Group, d.AddMilliseconds(500000), d.AddMilliseconds(700000), 2, TimeSpan.FromSeconds(2));

            early.ComputeFirstFireTimeUtc(null);
            trigger1.ComputeFirstFireTimeUtc(null);
            trigger2.ComputeFirstFireTimeUtc(null);
            trigger3.ComputeFirstFireTimeUtc(null);
            trigger4.ComputeFirstFireTimeUtc(null);
            trigger10.ComputeFirstFireTimeUtc(null);
            fJobStore.StoreTrigger(early, false);
            fJobStore.StoreTrigger(trigger1, false);
            fJobStore.StoreTrigger(trigger2, false);
            fJobStore.StoreTrigger(trigger3, false);
            fJobStore.StoreTrigger(trigger4, false);
            fJobStore.StoreTrigger(trigger10, false);

            DateTimeOffset firstFireTime = trigger1.GetNextFireTimeUtc().Value;

            IList<IOperableTrigger> acquiredTriggers = fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 4, TimeSpan.FromSeconds(1));
            Assert.Equal(4, acquiredTriggers.Count);
            Assert.Equal(early.Key, acquiredTriggers[0].Key);
            Assert.Equal(trigger1.Key, acquiredTriggers[1].Key);
            Assert.Equal(trigger2.Key, acquiredTriggers[2].Key);
            Assert.Equal(trigger3.Key, acquiredTriggers[3].Key);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);
            fJobStore.ReleaseAcquiredTrigger(trigger2);
            fJobStore.ReleaseAcquiredTrigger(trigger3);

            acquiredTriggers = this.fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 5, TimeSpan.FromMilliseconds(1000));
            Assert.Equal(5, acquiredTriggers.Count);
            Assert.Equal(early.Key, acquiredTriggers[0].Key);
            Assert.Equal(trigger1.Key, acquiredTriggers[1].Key);
            Assert.Equal(trigger2.Key, acquiredTriggers[2].Key);
            Assert.Equal(trigger3.Key, acquiredTriggers[3].Key);
            Assert.Equal(trigger4.Key, acquiredTriggers[4].Key);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);
            fJobStore.ReleaseAcquiredTrigger(trigger2);
            fJobStore.ReleaseAcquiredTrigger(trigger3);
            fJobStore.ReleaseAcquiredTrigger(trigger4);

            acquiredTriggers = fJobStore.AcquireNextTriggers(firstFireTime.AddSeconds(10), 6, TimeSpan.FromSeconds(1));
            Assert.Equal(5, acquiredTriggers.Count);
            Assert.Equal(early.Key, acquiredTriggers[0].Key);
            Assert.Equal(trigger1.Key, acquiredTriggers[1].Key);
            Assert.Equal(trigger2.Key, acquiredTriggers[2].Key);
            Assert.Equal(trigger3.Key, acquiredTriggers[3].Key);
            Assert.Equal(trigger4.Key, acquiredTriggers[4].Key);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);
            fJobStore.ReleaseAcquiredTrigger(trigger2);
            fJobStore.ReleaseAcquiredTrigger(trigger3);
            fJobStore.ReleaseAcquiredTrigger(trigger4);

            acquiredTriggers = fJobStore.AcquireNextTriggers(firstFireTime.AddMilliseconds(1), 5, TimeSpan.Zero);
            Assert.Equal(2, acquiredTriggers.Count);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);

            acquiredTriggers = fJobStore.AcquireNextTriggers(firstFireTime.AddMilliseconds(250), 5, TimeSpan.FromMilliseconds(199));
            Assert.Equal(5, acquiredTriggers.Count);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);
            fJobStore.ReleaseAcquiredTrigger(trigger2);
            fJobStore.ReleaseAcquiredTrigger(trigger3);
            fJobStore.ReleaseAcquiredTrigger(trigger4);

            acquiredTriggers = fJobStore.AcquireNextTriggers(firstFireTime.AddMilliseconds(150), 5, TimeSpan.FromMilliseconds(50L));
            Assert.Equal(4, acquiredTriggers.Count);
            fJobStore.ReleaseAcquiredTrigger(early);
            fJobStore.ReleaseAcquiredTrigger(trigger1);
            fJobStore.ReleaseAcquiredTrigger(trigger2);
            fJobStore.ReleaseAcquiredTrigger(trigger3);
        }

        [Fact]
        public void TestTriggerStates()
        {
            IOperableTrigger trigger = new SimpleTriggerImpl("trigger1", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, DateTimeOffset.Now.AddSeconds(100), DateTimeOffset.Now.AddSeconds(200), 2, TimeSpan.FromSeconds(2));
            trigger.ComputeFirstFireTimeUtc(null);
            Assert.Equal(TriggerState.None, fJobStore.GetTriggerState(trigger.Key));
            fJobStore.StoreTrigger(trigger, false);
            Assert.Equal(TriggerState.Normal, fJobStore.GetTriggerState(trigger.Key));

            fJobStore.PauseTrigger(trigger.Key);
            Assert.Equal(TriggerState.Paused, fJobStore.GetTriggerState(trigger.Key));

            fJobStore.ResumeTrigger(trigger.Key);
            Assert.Equal(TriggerState.Normal, fJobStore.GetTriggerState(trigger.Key));

            trigger = fJobStore.AcquireNextTriggers(trigger.GetNextFireTimeUtc().Value.AddSeconds(10), 1, TimeSpan.FromMilliseconds(1))[0];
            Assert.NotNull(trigger);
            fJobStore.ReleaseAcquiredTrigger(trigger);
            trigger = fJobStore.AcquireNextTriggers(trigger.GetNextFireTimeUtc().Value.AddSeconds(10), 1, TimeSpan.FromMilliseconds(1))[0];
            Assert.NotNull(trigger);
            Assert.Equal(0, fJobStore.AcquireNextTriggers(trigger.GetNextFireTimeUtc().Value.AddSeconds(10), 1, TimeSpan.FromMilliseconds(1)).Count);
        }

        [Fact]
        public void TestRemoveCalendarWhenTriggersPresent()
        {
            // QRTZNET-29

            IOperableTrigger trigger = new SimpleTriggerImpl("trigger1", "triggerGroup1", fJobDetail.Name, fJobDetail.Group, DateTimeOffset.Now.AddSeconds(100), DateTimeOffset.Now.AddSeconds(200), 2, TimeSpan.FromSeconds(2));
            trigger.ComputeFirstFireTimeUtc(null);
            ICalendar cal = new MonthlyCalendar();
            fJobStore.StoreTrigger(trigger, false);
            fJobStore.StoreCalendar("cal", cal, false, true);

            fJobStore.RemoveCalendar("cal");
        }

        [Fact]
        public void TestStoreTriggerReplacesTrigger()
        {
            string jobName = "StoreTriggerReplacesTrigger";
            string jobGroup = "StoreTriggerReplacesTriggerGroup";
            JobDetailImpl detail = new JobDetailImpl(jobName, jobGroup, typeof(NoOpJob));
            fJobStore.StoreJob(detail, false);

            string trName = "StoreTriggerReplacesTrigger";
            string trGroup = "StoreTriggerReplacesTriggerGroup";
            IOperableTrigger tr = new SimpleTriggerImpl(trName, trGroup, DateTimeOffset.Now);
            tr.JobKey = new JobKey(jobName, jobGroup);
            tr.CalendarName = null;

            fJobStore.StoreTrigger(tr, false);
            Assert.Equal(tr, fJobStore.RetrieveTrigger(new TriggerKey(trName, trGroup)));

            tr.CalendarName = "NonExistingCalendar";
            fJobStore.StoreTrigger(tr, true);
            Assert.Equal(tr, fJobStore.RetrieveTrigger(new TriggerKey(trName, trGroup)));
            Assert.Equal(tr.CalendarName, fJobStore.RetrieveTrigger(new TriggerKey(trName, trGroup)).CalendarName);

            bool exceptionRaised = false;
            try
            {
                fJobStore.StoreTrigger(tr, false);
            }
            catch (ObjectAlreadyExistsException)
            {
                exceptionRaised = true;
            }
            Assert.True(exceptionRaised, "an attempt to store duplicate trigger succeeded");
        }

        [Fact]
        public void PauseJobGroupPausesNewJob()
        {
            string jobName1 = "PauseJobGroupPausesNewJob";
            string jobName2 = "PauseJobGroupPausesNewJob2";
            string jobGroup = "PauseJobGroupPausesNewJobGroup";
            JobDetailImpl detail = new JobDetailImpl(jobName1, jobGroup, typeof(NoOpJob));
            detail.Durable = true;
            fJobStore.StoreJob(detail, false);
            fJobStore.PauseJobs(GroupMatcher<JobKey>.GroupEquals(jobGroup));

            detail = new JobDetailImpl(jobName2, jobGroup, typeof(NoOpJob));
            detail.Durable = true;
            fJobStore.StoreJob(detail, false);

            string trName = "PauseJobGroupPausesNewJobTrigger";
            string trGroup = "PauseJobGroupPausesNewJobTriggerGroup";
            IOperableTrigger tr = new SimpleTriggerImpl(trName, trGroup, DateTimeOffset.UtcNow);
            tr.JobKey = new JobKey(jobName2, jobGroup);
            fJobStore.StoreTrigger(tr, false);
            Assert.Equal(TriggerState.Paused, fJobStore.GetTriggerState(tr.Key));
        }

        [Fact]
        public void TestRetrieveJob_NoJobFound()
        {
            IJobDetail job = fJobStore.RetrieveJob(new JobKey("not", "existing"));
            Assert.Null(job);
        }

        [Fact]
        public void TestRetrieveTrigger_NoTriggerFound()
        {
            IOperableTrigger trigger = fJobStore.RetrieveTrigger(new TriggerKey("not", "existing"));
            Assert.Null(trigger);
        }

        [Fact]
        public void testStoreAndRetrieveJobs()
        {
            // Store jobs.
            for (int i = 0; i < 10; i++)
            {
                IJobDetail job = JobBuilder.Create<NoOpJob>().WithIdentity("job" + i).Build();
                fJobStore.StoreJob(job, false);
            }
            // Retrieve jobs.
            for (int i = 0; i < 10; i++)
            {
                JobKey jobKey = JobKey.Create("job" + i);
                IJobDetail storedJob = fJobStore.RetrieveJob(jobKey);
                Assert.Equal(jobKey, storedJob.Key);
            }
        }

        [Fact]
        public void TestStoreAndRetrieveTriggers()
        {
            // Store jobs and triggers.
            for (int i = 0; i < 10; i++)
            {
                IJobDetail job = JobBuilder.Create<NoOpJob>().WithIdentity("job" + i).Build();
                fJobStore.StoreJob(job, true);
                SimpleScheduleBuilder schedule = SimpleScheduleBuilder.Create();
                ITrigger trigger = TriggerBuilder.Create().WithIdentity("job" + i).WithSchedule(schedule).ForJob(job).Build();
                fJobStore.StoreTrigger((IOperableTrigger)trigger, true);
            }
            // Retrieve job and trigger.
            for (int i = 0; i < 10; i++)
            {
                JobKey jobKey = JobKey.Create("job" + i);
                IJobDetail storedJob = fJobStore.RetrieveJob(jobKey);
                Assert.Equal(jobKey, storedJob.Key);

                TriggerKey triggerKey = new TriggerKey("job" + i);
                ITrigger storedTrigger = fJobStore.RetrieveTrigger(triggerKey);
                Assert.Equal(triggerKey, storedTrigger.Key);
            }
        }

        [Fact]
        public void TestAcquireTriggers()
        {
            ISchedulerSignaler schedSignaler = new SampleSignaler();
            ITypeLoadHelper loadHelper = new SimpleTypeLoadHelper();
            loadHelper.Initialize();

            IJobStore store = new JobStore();
            store.Initialize(loadHelper, schedSignaler);

            // Setup: Store jobs and triggers.
            DateTime startTime0 = DateTime.UtcNow.AddMinutes(1).ToUniversalTime(); // a min from now.
            for (int i = 0; i < 10; i++)
            {
                DateTime startTime = startTime0.AddMinutes(i * 1); // a min apart
                IJobDetail job = JobBuilder.Create<NoOpJob>().WithIdentity("job" + i).Build();
                SimpleScheduleBuilder schedule = SimpleScheduleBuilder.RepeatMinutelyForever(2);
                IOperableTrigger trigger = (IOperableTrigger)TriggerBuilder.Create().WithIdentity("job" + i).WithSchedule(schedule).ForJob(job).StartAt(startTime).Build();

                // Manually trigger the first fire time computation that scheduler would do. Otherwise 
                // the store.acquireNextTriggers() will not work properly.
                DateTimeOffset? fireTime = trigger.ComputeFirstFireTimeUtc(null);
                Assert.Equal(true, fireTime != null);

                store.StoreJobAndTrigger(job, trigger);
            }

            // Test acquire one trigger at a time
            for (int i = 0; i < 10; i++)
            {
                DateTimeOffset noLaterThan = startTime0.AddMinutes(i);
                int maxCount = 1;
                TimeSpan timeWindow = TimeSpan.Zero;
                IList<IOperableTrigger> triggers = store.AcquireNextTriggers(noLaterThan, maxCount, timeWindow);
                Assert.Equal(1, triggers.Count);
                Assert.Equal("job" + i, triggers[0].Key.Name);

                // Let's remove the trigger now.
                store.RemoveJob(triggers[0].JobKey);
            }
        }

        [Fact]
        public void TestAcquireTriggersInBatch()
        {
            ISchedulerSignaler schedSignaler = new SampleSignaler();
            ITypeLoadHelper loadHelper = new SimpleTypeLoadHelper();
            loadHelper.Initialize();

            IJobStore store = new JobStore();
            store.Initialize(loadHelper, schedSignaler);

            // Setup: Store jobs and triggers.
            DateTimeOffset startTime0 = DateTimeOffset.UtcNow.AddMinutes(1); // a min from now.
            for (int i = 0; i < 10; i++)
            {
                DateTimeOffset startTime = startTime0.AddMinutes(i); // a min apart
                IJobDetail job = JobBuilder.Create<NoOpJob>().WithIdentity("job" + i).Build();
                SimpleScheduleBuilder schedule = SimpleScheduleBuilder.RepeatMinutelyForever(2);
                IOperableTrigger trigger = (IOperableTrigger)TriggerBuilder.Create().WithIdentity("job" + i).WithSchedule(schedule).ForJob(job).StartAt(startTime).Build();

                // Manually trigger the first fire time computation that scheduler would do. Otherwise 
                // the store.acquireNextTriggers() will not work properly.
                DateTimeOffset? fireTime = trigger.ComputeFirstFireTimeUtc(null);
                Assert.Equal(true, fireTime != null);

                store.StoreJobAndTrigger(job, trigger);
            }

            // Test acquire batch of triggers at a time
            DateTimeOffset noLaterThan = startTime0.AddMinutes(10);
            int maxCount = 7;
            TimeSpan timeWindow = TimeSpan.FromMinutes(8);
            IList<IOperableTrigger> triggers = store.AcquireNextTriggers(noLaterThan, maxCount, timeWindow);
            Assert.Equal(7, triggers.Count);
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal("job" + i, triggers[i].Key.Name);
            }
        }

        public class SampleSignaler : ISchedulerSignaler
        {
            internal int fMisfireCount = 0;

            public void NotifyTriggerListenersMisfired(ITrigger trigger)
            {
                fMisfireCount++;
            }

            public void NotifySchedulerListenersFinalized(ITrigger trigger)
            {
            }

            public void SignalSchedulingChange(DateTimeOffset? candidateNewNextFireTimeUtc)
            {
            }

            public void NotifySchedulerListenersError(string message, SchedulerException jpe)
            {
            }

            public void NotifySchedulerListenersJobDeleted(JobKey jobKey)
            {
            }
        }
    }
}
