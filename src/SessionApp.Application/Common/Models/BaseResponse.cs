using System.Collections.Generic;

namespace SessionApp.Application.Common.Models;

public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static BaseResponse<T> Success(T data, string? message = null)
    {
        return new BaseResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static BaseResponse<T> Failure(List<string> errors, string? message = null)
    {
        return new BaseResponse<T>
        {
            IsSuccess = false,
            Errors = errors,
            Message = message
        };
    }

    public static BaseResponse<T> Failure(string error, string? message = null)
    {
        return new BaseResponse<T>
        {
            IsSuccess = false,
            Errors = new List<string> { error },
            Message = message
        };
    }
}

public class BaseResponse : BaseResponse<object>
{
    public static BaseResponse Success(string? message = null)
    {
        return new BaseResponse
        {
            IsSuccess = true,
            Message = message
        };
    }

    public static new BaseResponse Failure(List<string> errors, string? message = null)
    {
        return new BaseResponse
        {
            IsSuccess = false,
            Errors = errors,
            Message = message
        };
    }

    public static new BaseResponse Failure(string error, string? message = null)
    {
        return new BaseResponse
        {
            IsSuccess = false,
            Errors = new List<string> { error },
            Message = message
        };
    }
}
