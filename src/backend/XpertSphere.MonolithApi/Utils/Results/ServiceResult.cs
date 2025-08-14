namespace XpertSphere.MonolithApi.Utils.Results;

/// <summary>
/// Generic service result class for standardizing service layer responses
/// </summary>
/// <typeparam name="T">The type of data returned</typeparam>
public class ServiceResult<T>
{
    public bool IsSuccess { get; protected set; }
    public T? Data { get; protected set; }
    public string Message { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = [];
    public int? StatusCode { get; protected set; }

    protected ServiceResult() { }

    /// <summary>
    /// Create a successful result with data
    /// </summary>
    public static ServiceResult<T> Success(T data, string message = Constants.OPERATION_SUCCEEDED)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Create a successful result without data
    /// </summary>
    public static ServiceResult<T> Success(string message = Constants.OPERATION_SUCCEEDED)
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Message = message,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Create a failure result with a single error message
    /// </summary>
    public static ServiceResult<T> Failure(string error, int statusCode = 400)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = error,
            Errors = [error],
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create a failure result with multiple error messages
    /// </summary>
    public static ServiceResult<T> Failure(List<string> errors, string message = Constants.OPERATION_FAILED, int statusCode = 400)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create a not found result
    /// </summary>
    public static ServiceResult<T> NotFound(string message = Constants.RESOURCE_NOT_FOUND)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 404
        };
    }

    /// <summary>
    /// Create an unauthorized result
    /// </summary>
    public static ServiceResult<T> Unauthorized(string message = Constants.ACCESS_DENIED)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 401
        };
    }

    /// <summary>
    /// Create a forbidden result
    /// </summary>
    public static ServiceResult<T> Forbidden(string message = Constants.ACCESS_FORBIDDEN)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 403
        };
    }

    /// <summary>
    /// Create a conflict result
    /// </summary>
    public static ServiceResult<T> Conflict(string message = Constants.RESOURCE_CONFLICTED)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 409
        };
    }

    /// <summary>
    /// Create a validation error result
    /// </summary>
    public static ServiceResult<T> ValidationError(List<string> validationErrors)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = "Validation failed",
            Errors = validationErrors,
            StatusCode = 422
        };
    }

    /// <summary>
    /// Create an internal server error result
    /// </summary>
    public static ServiceResult<T> InternalError(string message = Constants.INTERNAL_SERVER_ERROR)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 500
        };
    }

    /// <summary>
    /// Transform the result to a different type
    /// </summary>
    public ServiceResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!IsSuccess)
        {
            return new ServiceResult<TNew>
            {
                IsSuccess = false,
                Message = Message,
                Errors = Errors,
                StatusCode = StatusCode
            };
        }

        var mappedData = Data != null ? mapper(Data) : default;
        return new ServiceResult<TNew>
        {
            IsSuccess = true,
            Data = mappedData,
            Message = Message,
            StatusCode = StatusCode
        };
    }

    /// <summary>
    /// Check if the result has a specific error
    /// </summary>
    public bool HasError(string error)
    {
        return Errors.Contains(error);
    }

    /// <summary>
    /// Get the first error message
    /// </summary>
    public string? GetFirstError()
    {
        return Errors.FirstOrDefault();
    }
}

/// <summary>
/// Non-generic service result for operations that don't return data
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public List<string> Errors { get; private set; } = [];
    public int? StatusCode { get; private set; }

    protected ServiceResult() { }

    /// <summary>
    /// Create a successful result
    /// </summary>
    public static ServiceResult Success(string message = Constants.OPERATION_SUCCEEDED)
    {
        return new ServiceResult
        {
            IsSuccess = true,
            Message = message,
            StatusCode = 200
        };
    }

    /// <summary>
    /// Create a failure result
    /// </summary>
    public static ServiceResult Failure(string error, int statusCode = 400)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = error,
            Errors = [error],
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create a failure result with multiple errors
    /// </summary>
    public static ServiceResult Failure(List<string> errors, string message = Constants.OPERATION_FAILED, int statusCode = 400)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            Errors = errors,
            StatusCode = statusCode
        };
    }
    
    /// <summary>
    /// Create an internal server error result
    /// </summary>
    public static ServiceResult InternalError(string message = Constants.INTERNAL_SERVER_ERROR, int statusCode = 500)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create a not found result
    /// </summary>
    public static ServiceResult NotFound(string message = Constants.RESOURCE_NOT_FOUND)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 404
        };
    }
    

    /// <summary>
    /// Create an unauthorized result
    /// </summary>
    public static ServiceResult Unauthorized(string message = Constants.ACCESS_DENIED)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 401
        };
    }

    /// <summary>
    /// Create a forbidden result
    /// </summary>
    public static ServiceResult Forbidden(string message = Constants.ACCESS_FORBIDDEN)
    {
        return new ServiceResult
        {
            IsSuccess = false,
            Message = message,
            Errors = [message],
            StatusCode = 403
        };
    }

    /// <summary>
    /// Check if the result has a specific error
    /// </summary>
    public bool HasError(string error)
    {
        return Errors.Contains(error);
    }

    /// <summary>
    /// Get the first error message
    /// </summary>
    public string? GetFirstError()
    {
        return Errors.FirstOrDefault();
    }
}
