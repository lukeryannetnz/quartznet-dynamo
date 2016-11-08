using System;
using Xunit;
using Quartz.Impl;
using Quartz.Job;
using Quartz.Spi;
using Quartz.Simpl;
using Quartz.Impl.Triggers;
using System.Collections.Generic;

namespace Quartz.DynamoDB.Tests.Integration.JobStore
{
	/// <summary>
	/// Contains tests for the JobStore when triggers are fired.
	/// </summary>
    public class JobStoreTriggersFiredTests : IDisposable
	{
        private readonly DynamoDB.JobStore _sut;

		public JobStoreTriggersFiredTests ()
		{
			_sut = new Quartz.DynamoDB.JobStore ();
			var signaler = new Quartz.DynamoDB.Tests.Integration.RamJobStoreTests.SampleSignaler ();
			var loadHelper = new SimpleTypeLoadHelper ();

			_sut.Initialize(loadHelper, signaler);	
		}

		/// <summary>
		/// Tests that a single trigger can be fired successfully and the trigger and job keys are returned correctly
		/// when it is.
		/// </summary>
		[Fact]
		[Trait("Category", "Integration")]
		public void SingleTriggerFiredSuccessfully()
		{
			string jobName = Guid.NewGuid().ToString();
			string jobGroup = Guid.NewGuid().ToString();
			string triggerName = Guid.NewGuid().ToString();
			string triggerGroup = Guid.NewGuid().ToString();
			DateTimeOffset d = DateTime.UtcNow;

			JobDetailImpl job = new JobDetailImpl(jobName, jobGroup, typeof(NoOpJob));
			IOperableTrigger trigger = new SimpleTriggerImpl(triggerName, triggerGroup, job.Name, job.Group, d, null, 2, TimeSpan.FromSeconds(5));
			trigger.ComputeFirstFireTimeUtc(null);

			_sut.StoreJobAndTrigger(job, trigger);

			var acquired =_sut.AcquireNextTriggers(d.AddSeconds(9), 100, TimeSpan.FromSeconds(5));
			Assert.True(acquired.Count > 0);

			var result = _sut.TriggersFired(new List<IOperableTrigger> () { trigger });

			Assert.NotNull(result);
			Assert.Equal(1, result.Count);
			Assert.Equal(triggerName, result [0].TriggerFiredBundle.Trigger.Key.Name);
			Assert.Equal(triggerGroup, result [0].TriggerFiredBundle.Trigger.Key.Group);
			Assert.Equal(jobName, result [0].TriggerFiredBundle.JobDetail.Key.Name);
			Assert.Equal(jobGroup, result [0].TriggerFiredBundle.JobDetail.Key.Group);
		}

        public void Dispose()
        {
            _sut.Dispose();
        }
	}
}

