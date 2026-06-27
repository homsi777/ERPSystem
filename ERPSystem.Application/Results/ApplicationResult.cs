namespace ERPSystem.Application.Results;

public enum ApplicationResultStatus
{
    Success,
    Failure,
    ValidationFailed,
    NotFound,
    Conflict,
    PermissionDenied
}

public sealed class ValidationError
{
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";

    public ValidationError() { }

    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}

public sealed class OperationMessage
{
    public string Code { get; init; } = "";
    public string Text { get; init; } = "";
    public bool IsWarning { get; init; }

    public OperationMessage() { }

    public OperationMessage(string code, string text, bool isWarning = false)
    {
        Code = code;
        Text = text;
        IsWarning = isWarning;
    }
}

public class ApplicationResult
{
    public ApplicationResultStatus Status { get; protected init; }
    public string? ErrorMessage { get; protected init; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; protected init; } = [];
    public IReadOnlyList<OperationMessage> Messages { get; protected init; } = [];

    public bool IsSuccess => Status == ApplicationResultStatus.Success;

    public static ApplicationResult Success(params OperationMessage[] messages) => new()
    {
        Status = ApplicationResultStatus.Success,
        Messages = messages
    };

    public static ApplicationResult Failure(string message) => new()
    {
        Status = ApplicationResultStatus.Failure,
        ErrorMessage = message
    };

    public static ApplicationResult ValidationFailed(IEnumerable<ValidationError> errors) => new()
    {
        Status = ApplicationResultStatus.ValidationFailed,
        ValidationErrors = errors.ToList(),
        ErrorMessage = "Validation failed."
    };

    public static ApplicationResult ValidationFailed(string field, string message) =>
        ValidationFailed([new ValidationError(field, message)]);

    public static ApplicationResult NotFound(string message) => new()
    {
        Status = ApplicationResultStatus.NotFound,
        ErrorMessage = message
    };

    public static ApplicationResult Conflict(string message) => new()
    {
        Status = ApplicationResultStatus.Conflict,
        ErrorMessage = message
    };

    public static ApplicationResult PermissionDenied(string message) => new()
    {
        Status = ApplicationResultStatus.PermissionDenied,
        ErrorMessage = message
    };
}

public sealed class ApplicationResult<T> : ApplicationResult
{
    public T? Value { get; private init; }

    public static ApplicationResult<T> Success(T value, params OperationMessage[] messages) => new()
    {
        Status = ApplicationResultStatus.Success,
        Value = value,
        Messages = messages
    };

    public new static ApplicationResult<T> Failure(string message) => new()
    {
        Status = ApplicationResultStatus.Failure,
        ErrorMessage = message
    };

    public new static ApplicationResult<T> ValidationFailed(IEnumerable<ValidationError> errors) => new()
    {
        Status = ApplicationResultStatus.ValidationFailed,
        ValidationErrors = errors.ToList(),
        ErrorMessage = "Validation failed."
    };

    public new static ApplicationResult<T> ValidationFailed(string field, string message) =>
        ValidationFailed([new ValidationError(field, message)]);

    public new static ApplicationResult<T> NotFound(string message) => new()
    {
        Status = ApplicationResultStatus.NotFound,
        ErrorMessage = message
    };

    public new static ApplicationResult<T> Conflict(string message) => new()
    {
        Status = ApplicationResultStatus.Conflict,
        ErrorMessage = message
    };

    public new static ApplicationResult<T> PermissionDenied(string message) => new()
    {
        Status = ApplicationResultStatus.PermissionDenied,
        ErrorMessage = message
    };
}
