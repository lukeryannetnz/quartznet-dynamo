﻿using Quartz.DynamoDB.DataModel;
using Quartz.Impl.Triggers;
using Xunit;

namespace Quartz.DynamoDB.Tests.Unit
{
    /// <summary>
    /// Tests the Dynamo TriggerConverter for all attributes of the ISimpleTrigger trigger type.
    /// </summary>
    public class TriggerConverterSimpleTriggerTests
    {
        [Fact] [Trait("Category", "Unit")]

        public void RepeatCountSerializesCorrectly()
        {
            
            SimpleTriggerImpl trigger = CreateSimpleTrigger();

            var serialized = new DynamoTrigger(trigger).;
            SimpleTriggerImpl result = (SimpleTriggerImpl)serialized.(serialized);

            Assert.Equal(trigger.RepeatCount, result.RepeatCount);
        }

        [Fact] [Trait("Category", "Unit")]

        public void RepeatIntervalSerializesCorrectly()
        {
            
            SimpleTriggerImpl trigger = CreateSimpleTrigger();

            var serialized = new DynamoTrigger(trigger);
            SimpleTriggerImpl result = (SimpleTriggerImpl)sut.FromEntry(serialized);

            Assert.Equal(trigger.RepeatInterval, result.RepeatInterval);
        }

        [Fact] [Trait("Category", "Unit")]

        public void TimesTriggeredSerializesCorrectly()
        {
            
            SimpleTriggerImpl trigger = CreateSimpleTrigger();
            trigger.TimesTriggered = 9;

            var serialized = new DynamoTrigger(trigger);
            SimpleTriggerImpl result = (SimpleTriggerImpl)sut.FromEntry(serialized);

            Assert.Equal(trigger.TimesTriggered, result.TimesTriggered);
        }

        [Fact] [Trait("Category", "Unit")]

        public void FinalFireTimeUtcSerializesCorrectly()
        {
            
            SimpleTriggerImpl trigger = CreateSimpleTrigger();

            var serialized = new DynamoTrigger(trigger);
            SimpleTriggerImpl result = (SimpleTriggerImpl)sut.FromEntry(serialized);

            Assert.Equal(trigger.FinalFireTimeUtc, result.FinalFireTimeUtc);
        }


        private static SimpleTriggerImpl CreateSimpleTrigger()
        {
            var jobKey = new JobKey("test");
            SimpleTriggerImpl trigger = (SimpleTriggerImpl)TriggerBuilder.Create()
                .ForJob(jobKey)
                .WithSimpleSchedule(x => x. WithIntervalInSeconds(39).WithRepeatCount(7).WithMisfireHandlingInstructionNextWithRemainingCount())
                .Build();
            return trigger;
        }
    }
}
