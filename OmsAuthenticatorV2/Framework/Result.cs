﻿using System.Collections.Immutable;
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

        public static Result<IEnumerable<T>> Combine<T>(IEnumerable<Result<T>> results)
        {
            var lookup = results.ToLookup(x => x.IsFailure);
            return lookup[true].Any() // failures
                ? Failure<IEnumerable<T>>(lookup[true].OfType<Result<T>.Failure>().SelectMany(x => x.Errors).ToImmutableArray())
                : Success<IEnumerable<T>>(lookup[false].OfType<Result<T>.Success>().Select(x => x.Value).ToImmutableArray());
        }

        public static Result<(T1, T2)> Combine<T1, T2>(Result<T1> r1, Result<T2> r2)
        {
            if (r1 is Result<T1>.Failure failure1)
            {
                return Failure<(T1, T2)>(failure1.Errors);
            }
            if (r2 is Result<T2>.Failure failure2)
            {
                return Failure<(T1, T2)>(failure2.Errors);
            }
            return Success((((Result<T1>.Success)r1).Value, ((Result<T2>.Success)r2).Value));
        }
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