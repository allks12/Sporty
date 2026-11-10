namespace Sporty.Services.Common;

public class OperationResult<T> {
    private OperationResult(bool succeeded, T? value, ErrorCode errorCode, IReadOnlyList<string> errors) {
        Succeeded = succeeded;
        Value = value;
        ErrorCode = errorCode;
        Errors = errors;
    }

    public bool Succeeded {get;}
    public T? Value {get;}
    public ErrorCode ErrorCode {get;}
    public IReadOnlyList<string> Errors {get;}

    public static OperationResult<T> Success(T value) => new(true, value, ErrorCode.None, []);

    public static OperationResult<T> Failure(ErrorCode errorCode, params string[] errors) =>
        new(false, default, errorCode, errors);
}
