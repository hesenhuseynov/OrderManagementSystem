using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Experimental;
using OrderManagementSystem.Common.Errors;
using System.IO.Enumeration;

namespace OrderManagementSystem.Common.Results
{
    public class Result
    {
        private readonly Error[] _errors;

        protected internal Result(bool isSucess, IEnumerable<Error> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            _errors = errors.ToArray();

            if (isSucess && _errors.Length > 0)
            {
                throw new InvalidOperationException("A successful result cannot  contain errors");
            }

            if (!isSucess && _errors.Length == 0)
            {
                throw new InvalidOperationException("A failed result must contain  at least one error");
            }

            if (_errors.Any(static e => e is null))
            {
                throw new ArgumentException("Errors cannot contain  null entries", nameof(errors));  
            }

            IsSuccess = isSucess;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public IReadOnlyList<Error> Errors => _errors;

        public Error? FirstError => _errors.FirstOrDefault();

        //ThreadRouteExcpetionFlow() = > new{ctor=>ctor.AsENThreadROUteExcpetionFLow() = >new{ctor=> s

        public static Result Success() => new(true, Array.Empty<Error>()); 

        public static Result Failure(Error error)
        {
            ArgumentNullException.ThrowIfNull(error);
            return new(false, new[] { error }); 
        }

        public static Result Failure( IEnumerable<Error>  errors)
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new(false, errors); 
        }

        public static Result<TValue> Success<TValue>(TValue value)
     where TValue : notnull
        {
            ArgumentNullException.ThrowIfNull(value);
            return new(value);
        }

        public static Result<TValue> Failure<TValue>(Error error)
            where TValue : notnull
        {
            ArgumentNullException.ThrowIfNull(error);
            return new(new[] { error });
        }

        public static Result<TValue> Failure<TValue>(IEnumerable<Error> errors)
     where TValue : notnull
        {
            ArgumentNullException.ThrowIfNull(errors);
            return new(errors);

        }
    }
}
