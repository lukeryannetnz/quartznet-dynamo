using System;
using Xunit;
using Quartz.DynamoDB.DataModel.Storage;
using Quartz.DynamoDB.DataModel;
using System.Linq;
using System.Collections.Generic;
using Amazon.DynamoDBv2.Model;

namespace Quartz.DynamoDB.Tests.Integration.Repository
{
    /// <summary>
    /// Contains tests for the repository class.
    /// </summary>
    public class RepositoryTests : IDisposable
    {
        private Repository<DynamoScheduler> _sut;
        private DynamoClientFactory _testFactory;

        [Fact]
        [Trait("Category", "Integration")]
        public void PersistTwoSchedulersSameId_OneRecord()
        {
            _testFactory = new DynamoClientFactory();
            var client = _testFactory.BootStrapDynamo();
            _sut = new Repository<DynamoScheduler>(client);

            int initialSchedulerCount = _sut.Scan(null, null, null).Count();

            var scheduler = new DynamoScheduler
            {
                InstanceId = "testInstance" + DateTime.UtcNow.Ticks.ToString(),
                ExpiresUtc = (SystemTime.Now() + new TimeSpan(0, 10, 0)).UtcDateTime,
                State = "Running"
            };

            _sut.Store(scheduler);

            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":instance", new AttributeValue { S = scheduler.InstanceId }}
            };

            var scheduler2 = _sut.Scan(expressionAttributeValues, null, "InstanceId = :instance").Single();

            scheduler2.ExpiresUtc = (SystemTime.Now() + new TimeSpan(0, 20, 0)).UtcDateTime;

            _sut.Store(scheduler2);

            int finalCount = _sut.Scan(null, null, null).Count();

            Assert.Equal(initialSchedulerCount + 1, finalCount);
        }

        /// <summary>
        /// Tests the repository Store method that wraps the Dynamo BatchWriteItem API.
        /// 25 is the maximum number of items for one batch.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void StoreTwentyFiveEntities()
        {
            _testFactory = new DynamoClientFactory();
            var client = _testFactory.BootStrapDynamo();
            _sut = new Repository<DynamoScheduler>(client);

            int initialSchedulerCount = _sut.Scan(null, null, null).Count();

            var items = new List<DynamoScheduler>();

            for (int i = 0; i < 25; i++)
            {
                var scheduler = new DynamoScheduler
                {
                    InstanceId = "testInstance" + DateTime.UtcNow.Ticks.ToString() + i,
                    ExpiresUtc = (SystemTime.Now() + new TimeSpan(0, 10, 0)).UtcDateTime,
                    State = "Running"
                };

                items.Add(scheduler);
            }
          
            _sut.Store(items);

            int finalCount = _sut.Scan(null, null, null).Count();

            Assert.Equal(initialSchedulerCount + 25, finalCount);
        }

        /// <summary>
        /// Tests the repository Store method that wraps the Dynamo BatchWriteItem API.
        /// 26 is enough items for two batches.
        /// </summary>
        [Fact]
        [Trait("Category", "Integration")]
        public void StoreTwentySixEntities()
        {
            _testFactory = new DynamoClientFactory();
            var client = _testFactory.BootStrapDynamo();
            _sut = new Repository<DynamoScheduler>(client);

            int initialSchedulerCount = _sut.Scan(null, null, null).Count();

            var items = new List<DynamoScheduler>();

            for (int i = 0; i < 26; i++)
            {
                var scheduler = new DynamoScheduler
                {
                    InstanceId = "testInstance" + DateTime.UtcNow.Ticks.ToString() + i,
                    ExpiresUtc = (SystemTime.Now() + new TimeSpan(0, 10, 0)).UtcDateTime,
                    State = "Running"
                };

                items.Add(scheduler);
            }

            _sut.Store(items);

            int finalCount = _sut.Scan(null, null, null).Count();

            Assert.Equal(initialSchedulerCount + 26, finalCount);
        }

        #region IDisposable implementation

        bool _disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _testFactory.CleanUpDynamo();

                    if (_sut != null)
                    {
                        _sut.Dispose();
                    }
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}

