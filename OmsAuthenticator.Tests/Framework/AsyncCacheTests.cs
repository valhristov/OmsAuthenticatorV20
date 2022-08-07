using System.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests.Framework
{
    [TestClass]
    public class AsyncCacheTests
    {
        private readonly SystemTimeMock _systemTimeMock;
        private readonly AsyncResultCache<string, int> cache;

        public AsyncCacheTests()
        {
            _systemTimeMock = new SystemTimeMock();
            cache = new AsyncResultCache<string, int>(TimeSpan.FromMilliseconds(500), () => _systemTimeMock.UtcNow);
        }

        [TestMethod]
        public async Task Success_Result_Expires_After_Configured_Time()
        {
            var result1 = await cache.AddOrUpdate("a", NextSuccess);
            result1.GetValue().Should().Be(1); // cache miss

            _systemTimeMock.Wait(TimeSpan.FromMilliseconds(500));

            var result2 = await cache.AddOrUpdate("a", NextSuccess);
            result2.GetValue().Should().Be(2); // cache miss
            var result3 = await cache.AddOrUpdate("a", NextSuccess);
            result3.GetValue().Should().Be(2); // cache hit
        }

        [TestMethod]
        public async Task Success_Result_Is_Persisted_In_Cache()
        {
            var result1 = await cache.AddOrUpdate("a", NextSuccess);
            result1.GetValue().Should().Be(1); // cache miss
            var result2 = await cache.AddOrUpdate("a", NextSuccess);
            result2.GetValue().Should().Be(1); // cache hit
            var result3 = await cache.AddOrUpdate("a", NextSuccess);
            result3.GetValue().Should().Be(1); // cache hit
        }

        [TestMethod]
        public async Task Failure_Result_Is_Not_Persisted_In_Cache()
        {
            var result1 = await cache.AddOrUpdate("a", NextFailure);
            result1.GetErrors().Should().BeEquivalentTo(new[] { "1" }); // cache miss
            var result2 = await cache.AddOrUpdate("a", NextFailure);
            result2.GetErrors().Should().BeEquivalentTo(new[] { "2" }); // cache miss
            var result3 = await cache.AddOrUpdate("a", NextFailure);
            result3.GetErrors().Should().BeEquivalentTo(new[] { "3" }); // cache miss
        }

        [TestMethod]
        public async Task Clear_Removes_Stale_Items()
        {
            await cache.AddOrUpdate("a", NextSuccess); // this should get cleared in the end
            cache.Count.Should().Be(1);

            _systemTimeMock.Wait(TimeSpan.FromMilliseconds(300));

            await cache.AddOrUpdate("b", NextSuccess); // this should stay in cache in the end
            cache.Count.Should().Be(2);

            _systemTimeMock.Wait(TimeSpan.FromMilliseconds(300));

            // 600ms passed after resultA was added to cache, so this call should remove it
            cache.Cleanup();
            cache.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task Concurrent_Access()
        {
            // Run 5 concurent tasks to get the same item from the cache; all calls should receive the same value
            Task.WaitAll(
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop)
                );

            var result1 = await cache.AddOrUpdate("a", NextSuccess); // this should get cleared in the end
            result1.GetValue().Should().Be(1);

            void GetValueInLoop()
            {
                for (int i = 0; i < 10000; i++)
                {
                    cache.AddOrUpdate("a", NextSuccess);
                }
            }
        }

        private int _counterSuccess;

        public async Task<Result<int>> NextSuccess()
        {
            Debug.WriteLine("NextSuccess called");

            await Task.Delay(100);

            return Result.Success(Interlocked.Increment(ref _counterSuccess));
        }

        private int _counterFailure;

        public async Task<Result<int>> NextFailure()
        {
            Debug.WriteLine("NextFailure called");

            await Task.Delay(100);

            return Result.Failure<int>(Interlocked.Increment(ref _counterFailure).ToString());
        }
    }
}
