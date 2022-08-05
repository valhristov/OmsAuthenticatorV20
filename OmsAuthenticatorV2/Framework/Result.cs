using System.Collections.Immutable;
using System.Diagnostics;

namespace OmsAuthenticator.Framework
{
    public static class Result
    {
        public static Result<T> Failure<T>(params string[] errors) =>
            new Result<T>.Failure(ImmutableArray.Create(errors));

        public static Result<T> Failure<T>(ImmutableArray<string> errors) =>
            new Result<T>.Failure(errors);

        public static Result<T> Success<T>(T value) =>
            new Result<T>.Success(value);
    }

    public class Result<T>
    {
        public bool IsFailure => this is Failure;

        [DebuggerStepThrough]
        public TOut Select<TOut>(Func<T, TOut> onSuccess, Func<ImmutableArray<string>, TOut> onFailure) =>
            this switch
            {
                Success success => onSuccess(success.Value),
                Failure failure => onFailure(failure.Errors),
                _ => throw new NotImplementedException(),
            };

        [DebuggerStepThrough]
        public Result<TOut> Convert<TOut>(Func<T, Result<TOut>> convert) =>
            this switch
            {
                Success success => convert(success.Value),
                Failure failure => Result.Failure<TOut>(failure.Errors),
                _ => throw new NotImplementedException(),
            };

        [DebuggerStepThrough]
        public async Task<Result<TOut>> ConvertAsync<TOut>(Func<T, Task<Result<TOut>>> convertAsync) =>
            this switch
            {
                Success success => await convertAsync(success.Value),
                Failure failure => Result.Failure<TOut>(failure.Errors),
                _ => throw new NotImplementedException(),
            };

        public sealed class Success : Result<T>
        {
            public T Value { get; }

            public Success(T value)
            {
                Value = value;
            }
        }

        public sealed class Failure : Result<T>
        {
            public ImmutableArray<string> Errors { get; }

            public Failure(ImmutableArray<string> errors)
            {
                Errors = errors;
            }
        }
    }
}
