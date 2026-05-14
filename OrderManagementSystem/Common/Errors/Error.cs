using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection.Metadata;

namespace OrderManagementSystem.Common.Errors
{
    public sealed record Error
    {
        public Error(string code, string description, ErrorType type, string? field = null)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(code);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(description);

            Code = code;
            Description = description;
            Type = type;
            Field = string.IsNullOrEmpty(field) ? null : field;
        }

        public string Code { get; }
        public string Description { get; }
        public ErrorType Type { get; }
        public string? Field { get; }

        public static Error Failure(string code, string description) =>
            new(code, description, ErrorType.Failure);

        public static Error Validation(string code, string description, string? field = null) =>
            new(code, description, ErrorType.Validation,field);

        public static Error NotFound(string code, string description)
            => new(code, description, ErrorType.NotFound);

        public static Error Conflict(string code, string description) =>
            new(code, description, ErrorType.Conflict);

        public static Error UnAuthorized(string code, string description) =>
            new(code, description, ErrorType.Unauthorized);

        public static Error Forbidden(string code, string description) =>
            new(code, description, ErrorType.Forbidden); 
    }
}