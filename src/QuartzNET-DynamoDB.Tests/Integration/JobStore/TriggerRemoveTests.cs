using System;
using System.Collections.Generic;
using Quartz.DynamoDB.Tests.Integration;
using Quartz.Impl;
using Quartz.Job;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Quartz.DynamoDB.Tests.Integration.JobStore
{
    public class TriggerRemoveTests : IDisposable
    {
        private readonly DynamoDB.JobStore _sut;

        public TriggerRemoveTests()
        {
            _sut = new Quartz.DynamoDB.JobStore();
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

            var result = _sut.RemoveTriggers(new[] { inMemoryTr.Key});

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

        public void Dispose()
        {
            _sut.Dispose();
        }
    }
}

