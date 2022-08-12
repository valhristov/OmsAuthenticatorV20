using System.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests.Framework
{
    [TestClass]
    public class AsyncCacheTests
    {
        private SystemTimeMock SystemTimeMock { get; }
        private TokenCache Cache { get; }

        private static readonly TimeSpan Expiration = TimeSpan.FromHours(1);
        
        public AsyncCacheTests()
        {
            SystemTimeMock = new SystemTimeMock();
            Cache = new TokenCache(SystemTimeMock);
        }

        [TestMethod]
        public async Task Success_Result_Expires_After_Configured_Time()
        {
            var tokenKey = new TokenKey("a", "a", "a");

            var result1 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result1.GetValue().Value.Should().Be("1"); // cache miss

            SystemTimeMock.Wait(Expiration + TimeSpan.FromSeconds(1));

            var result2 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result2.GetValue().Value.Should().Be("2"); // cache miss
            var result3 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result3.GetValue().Value.Should().Be("2"); // cache hit
        }

        [TestMethod]
        public async Task Success_Result_Is_Persisted_In_Cache()
        {
            var tokenKey = new TokenKey("a", "a", "a");

            var result1 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result1.GetValue().Value.Should().Be("1"); // cache miss
            var result2 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result2.GetValue().Value.Should().Be("1"); // cache hit
            var result3 = await Cache.AddOrUpdate(tokenKey, NextSuccess);
            result3.GetValue().Value.Should().Be("1"); // cache hit
        }

        [TestMethod]
        public async Task Failure_Result_Is_Not_Persisted_In_Cache()
        {
            var tokenKey = new TokenKey("a", "a", "a");
            var result1 = await Cache.AddOrUpdate(tokenKey, NextFailure);
            result1.GetErrors().Should().BeEquivalentTo(new[] { "1" }); // cache miss
            var result2 = await Cache.AddOrUpdate(tokenKey, NextFailure);
            result2.GetErrors().Should().BeEquivalentTo(new[] { "2" }); // cache miss
            var result3 = await Cache.AddOrUpdate(tokenKey, NextFailure);
            result3.GetErrors().Should().BeEquivalentTo(new[] { "3" }); // cache miss
        }

        [TestMethod]
        public async Task Clear_Removes_Stale_Items()
        {
            await Cache.AddOrUpdate(new TokenKey("a", "a", "a"), NextSuccess); // this should get cleared in the end
            Cache.Count.Should().Be(1);

            SystemTimeMock.Wait(Expiration / 2);

            await Cache.AddOrUpdate(new TokenKey("b", "b", "b"), NextSuccess); // this should stay in cache in the end
            Cache.Count.Should().Be(2);

            SystemTimeMock.Wait(Expiration);

            // 600ms passed after resultA was added to cache, so this call should remove it
            Cache.Cleanup();
            Cache.Count.Should().Be(1);
        }

        [TestMethod]
        public async Task Concurrent_Access()
        {
            var tokenKey = new TokenKey("oms-id", "connection-id", "request-id");

            // Run 5 concurent tasks to get the same item from the cache; all calls should receive the same value
            Task.WaitAll(
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop),
                Task.Run(GetValueInLoop)
                );

            var result1 = await Cache.AddOrUpdate(tokenKey, NextSuccess); // this should get cleared in the end
            result1.GetValue().Value.Should().Be("1");

            void GetValueInLoop()
            {
                for (int i = 0; i < 10000; i++)
                {
                    Cache.AddOrUpdate(tokenKey, NextSuccess);
                }
            }
        }

        private int _counterSuccess;

        private Task<Result<Token>> NextSuccess(TokenKey key)
        {
            Debug.WriteLine("NextSuccess called");

            var token = new Token(
                Interlocked.Increment(ref _counterSuccess).ToString(),
                key.RequestId,
                SystemTimeMock.UtcNow.Add(Expiration));

            return Task.FromResult(Result.Success(token));
        }

        private int _counterFailure;

        private Task<Result<Token>> NextFailure(TokenKey key)
        {
            Debug.WriteLine("NextFailure called");

            return Task.FromResult(Result.Failure<Token>(Interlocked.Increment(ref _counterFailure).ToString()));
        }
    }
}
