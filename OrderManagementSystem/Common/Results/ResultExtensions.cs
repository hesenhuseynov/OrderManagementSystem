using OrderManagementSystem.Common.Errors;
using System.Numerics;

namespace OrderManagementSystem.Common.Results
{
    public static class ResultExtensions
    {
        public static TOut Match<TOut>(
    this Result result,
    Func<TOut> onSuccess,
    Func<IReadOnlyList<Error>, TOut> onFailure)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return result.IsSuccess
                ? onSuccess()
                : onFailure(result.Errors);
        }

        public static TOut Match<TValue, TOut>(
            this Result<TValue> result,
            Func<TValue, TOut> onSuccess,   
            Func<IReadOnlyList<Error>, TOut> onFailure)
            where TValue : notnull
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(onSuccess);
            ArgumentNullException.ThrowIfNull(onFailure);

            return result.IsSuccess
                ? onSuccess(result.Value)
                : onFailure(result.Errors);
        }

        public static Result<TOut> Map<TValue, TOut>(
            this Result<TValue> result,
            Func<TValue, TOut> map)
            where TValue : notnull
            where TOut : notnull
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(map);

            return result.IsSuccess
                ? Result.Success(map(result.Value))
                : Result.Failure<TOut>(result.Errors);
        }

        public static Result<TOut> Bind<TValue, TOut>(
            this Result<TValue> result,
            Func<TValue, Result<TOut>> bind)
            where TValue : notnull
            where TOut : notnull
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(bind);

            return result.IsSuccess
                ? bind(result.Value)
                : Result.Failure<TOut>(result.Errors);
        }
    }
}
