// Name:
// Student Admin No.:
// Tutorial Group:

using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace ArcaneVault.Models.Results;

public record ApiResult<T>(
    HttpStatusCode StatusCode,
    T? Value = default,
    ProblemDetails? Problem = null)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public record ApiResult(
    HttpStatusCode StatusCode,
    ProblemDetails? Problem = null)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}
