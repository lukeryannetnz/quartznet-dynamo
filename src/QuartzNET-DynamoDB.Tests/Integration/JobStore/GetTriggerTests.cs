﻿using System;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Quartz.DynamoDB.Tests
{
	public class GetTriggerTests
	{
		IJobStore _sut;

		public GetTriggerTests ()
		{
			_sut = new JobStore ();
			var signaler = new Quartz.DynamoDB.Tests.Integration.RamJobStoreTests.SampleSignaler ();
			var loadHelper = new SimpleTypeLoadHelper ();

			_sut.Initialize (loadHelper, signaler);
		}

		[Fact]
		[Trait ("Category", "Integration")]
		public void GetNumberOfTriggersIncrementsWhenTriggerAdded ()
		{
		}
	}
}

