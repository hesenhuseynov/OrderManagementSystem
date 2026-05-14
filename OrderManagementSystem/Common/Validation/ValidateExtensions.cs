
using FluentValidation.Results;
using OrderManagementSystem.Common.Errors;

namespace OrderManagementSystem.Common.Validation
{
    public static class ValidateExtensions
    {
        public static  List<Error> ToErrorList( this  ValidationResult  validationResult)
        {
            ArgumentNullException.ThrowIfNull(validationResult);

            return validationResult.Errors.Select(e => Error.Validation(
                e.ErrorCode,
                e.ErrorMessage,
                e.PropertyName
                )).ToList();
        }
    }
}
