using System;
using System.Collections.Generic;
using Quartz.Impl;
using Quartz.Job;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Quartz.DynamoDB.Tests.Integration.JobStore
{
    public class TriggerRemoveTests : JobStoreIntegrationTest
    {
        public TriggerRemoveTests()
        {
            _testFactory = new DynamoClientFactory();
            _sut = _testFactory.CreateTestJobStore();
            var signaler = new Quartz.DynamoDB.Tests.Integration.RamJobStoreTests.SampleSignaler();
            var loadHelper = new SimpleTypeLoadHelper();

            _sut.Initialize(loadHelper, signaler);
        }

        /// <summary>
        /// Tests that when remove triggers is called when no triggers exist, false is returned.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void RemoveTriggersNoTriggers()
        {
            // Create a trigger, dont store it.
            IOperableTrigger inMemoryTr = TestTriggerFactory.CreateTestTrigger("whatever");

            var result = _sut.RemoveTriggers(new[] { inMemoryTr.Key });

            Assert.False(result);
        }

        /// <summary>
        /// Tests that when remove triggers is called when all triggers exist, true is returned.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void RemoveTriggersAllRemoved()
        {
            // Create a random job, store it.
            string jobName = Guid.NewGuid().ToString();
            JobDetailImpl detail = new JobDetailImpl(jobName, "JobGroup", typeof(NoOpJob));
            _sut.StoreJob(detail, false);

            // Create a trigger for the job, in the trigger group.
            IOperableTrigger tr = TestTriggerFactory.CreateTestTrigger(jobName);
            var triggerGroup = tr.Key.Group;
            _sut.StoreTrigger(tr, false);

            var result = _sut.RemoveTriggers(new List<TriggerKey>() { tr.Key });
            Assert.True(result);
        }

        /// <summary>
        /// Tests that when remove triggers is called when one of two triggers exists, false is returned.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void RemoveTriggersOneRemoved()
        {
            // Create a random job, store it.
            string jobName = Guid.NewGuid().ToString();
            JobDetailImpl detail = new JobDetailImpl(jobName, "JobGroup", typeof(NoOpJob));
            _sut.StoreJob(detail, false);

            // Create a trigger for the job, in the trigger group.
            IOperableTrigger tr = TestTriggerFactory.CreateTestTrigger(jobName);
            var triggerGroup = tr.Key.Group;
            _sut.StoreTrigger(tr, false);

            // Create a trigger, dont store it.
            IOperableTrigger inMemoryTr = TestTriggerFactory.CreateTestTrigger("whatever");

            var result = _sut.RemoveTriggers(new List<TriggerKey>() { tr.Key, inMemoryTr.Key });
            Assert.False(result);
        }

        /// <summary>
        /// Tests that when remove trigger is called with the only trigger for the job,
        /// the orphaned job is also removed.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void RemoveTriggerRemovesOrphanJob()
        {
            // Create a random job, store it.
            string jobName = Guid.NewGuid().ToString();
            JobDetailImpl detail = new JobDetailImpl(jobName, "JobGroup", typeof(NoOpJob));
            _sut.StoreJob(detail, false);

            // Create a trigger for the job, in the trigger group.
            IOperableTrigger tr = TestTriggerFactory.CreateTestTrigger(jobName);
            var triggerGroup = tr.Key.Group;
            _sut.StoreTrigger(tr, false);

            var result = _sut.RemoveTrigger(tr.Key);

            // Check trigger and job are both removed
            Assert.True(result);
            Assert.Equal(0, _sut.GetNumberOfTriggers());
            Assert.Equal(0, _sut.GetNumberOfJobs());
        }

        /// <summary>
        /// Tests that when remove trigger is called with one of many triggers for the job,
        /// the job is not removed.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void RemoveTriggerDoesNotRemoveJobWithOtherTriggers()
        {
            // Create a random job, store it.
            string jobName = Guid.NewGuid().ToString();
            JobDetailImpl detail = new JobDetailImpl(jobName, "JobGroup", typeof(NoOpJob));
            _sut.StoreJob(detail, false);

            // Create 2 triggers for the job, in the trigger group.
            IOperableTrigger tr = TestTriggerFactory.CreateTestTrigger(jobName);
            var triggerGroup = tr.Key.Group;
            _sut.StoreTrigger(tr, false);

            IOperableTrigger tr2 = TestTriggerFactory.CreateTestTrigger(jobName);
            var triggerGroup2 = tr2.Key.Group;
            _sut.StoreTrigger(tr2, false);

            var result = _sut.RemoveTrigger(tr.Key);

            // Check only one trigger is removed and the job isn't removed
            Assert.True(result);
            Assert.Equal(1, _sut.GetNumberOfTriggers());
            Assert.Equal(1, _sut.GetNumberOfJobs());
        }
    }
}

