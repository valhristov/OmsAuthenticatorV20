using System.Collections.Immutable;
using OmsAuthenticator.Framework;

namespace OmsAuthenticator.Tests.Helpers
{
    public static class ResultExtensions
    {
        public static T GetValue<T>(this Result<T> result) =>
            result switch
            {
                Result<T>.Success success => success.Value,
                Result<T>.Failure failure => throw new InvalidOperationException("Result<>.Failure does not have a value"),
                _ => throw new NotImplementedException(),
            };

        public static ImmutableArray<string> GetErrors<T>(this Result<T> result) =>
            result switch
            {
                Result<T>.Success success => throw new InvalidOperationException("Result<>.Success does not have errors"),
                Result<T>.Failure failure => failure.Errors,
                _ => throw new NotImplementedException(),
            };

    }
}
