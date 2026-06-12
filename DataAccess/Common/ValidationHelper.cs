using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DataAccess.Common;

public static class Guard
{
    public static void NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    public static void NotNullOrWhiteSpace([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"'{paramName}' cannot be null or whitespace.", paramName);
    }

    public static void InRange(int value, int min, int max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, $"'{paramName}' must be between {min} and {max}.");
    }
}

public sealed class ValidationResult
{
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public ValidationResult AddError(string error)
    {
        _errors.Add(error);
        return this;
    }

    public Result<T> ToResult<T>(T value) =>
        IsValid ? Result<T>.Success(value) : Result<T>.Fail(string.Join("; ", _errors));

    public Result ToResult() =>
        IsValid ? Result.Success() : Result.Fail(string.Join("; ", _errors));
}

public static class Validator
{
    public static ValidationResult NotEmpty(string? value, string fieldName)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(value))
            result.AddError($"'{fieldName}' is required.");
        return result;
    }

    public static ValidationResult MaxLength(string? value, int max, string fieldName)
    {
        var result = new ValidationResult();
        if (value?.Length > max)
            result.AddError($"'{fieldName}' must not exceed {max} characters.");
        return result;
    }

    public static ValidationResult MinLength(string? value, int min, string fieldName)
    {
        var result = new ValidationResult();
        if (value?.Length < min)
            result.AddError($"'{fieldName}' must be at least {min} characters.");
        return result;
    }

    public static ValidationResult Range(int value, int min, int max, string fieldName)
    {
        var result = new ValidationResult();
        if (value < min || value > max)
            result.AddError($"'{fieldName}' must be between {min} and {max}.");
        return result;
    }

    public static ValidationResult Email(string? email, string fieldName)
    {
        var result = new ValidationResult();
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            result.AddError($"'{fieldName}' is not a valid email address.");
        return result;
    }

    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var combined = new ValidationResult();
        foreach (var r in results)
            foreach (var e in r.Errors)
                combined.AddError(e);
        return combined;
    }

    public static Result<T> ValidateDto<T>(T dto, Func<T, ValidationResult> rule)
    {
        var result = rule(dto);
        return result.ToResult(dto);
    }
}
