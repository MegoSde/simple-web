namespace cms.Models;
public sealed record JsonValidationError(string Path, string Code, string Message, string? ComponentId = null);

public sealed class ValidationResult
{
    public bool Ok { get; init; }
    public List<JsonValidationError> Errors { get; init; } = new();

    public static ValidationResult Success() => new() { Ok = true };
    public static ValidationResult Fail(string path, string code, string message, string? componentId = null)
        => new() { Ok = false, Errors = { new JsonValidationError(path, code, message, componentId) } };
}