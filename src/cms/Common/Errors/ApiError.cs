namespace cms.Common.Errors;

public class ApiError : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public ApiError(int status, string code, string message) : base(message)
    {
        StatusCode = status; Code = code;
    }
}