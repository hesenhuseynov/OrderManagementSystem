using OrderManagementSystem.Common.Errors;
using System.Diagnostics.CodeAnalysis;

namespace OrderManagementSystem.Common.Results
{
    public sealed class Result<TValue> : Result
     where TValue : notnull
    {
        private readonly TValue? _value;

        internal Result(TValue value)
            : base(true, Array.Empty<Error>())
        {
            ArgumentNullException.ThrowIfNull(value);
            _value = value;
        }

        internal Result(IEnumerable<Error> errors)
            : base(false, errors)
        {
            _value = default;
        }

        public TValue Value =>
            IsSuccess
                ? _value!
                : throw new InvalidOperationException("A failed result has no value.");
        public bool TryGetValue([NotNullWhen(true)] out TValue? value)
        {
            value = IsSuccess ? _value : default;
            return IsSuccess;
        }
    }
}
