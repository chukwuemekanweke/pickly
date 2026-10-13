using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure;

public static class AuthenticationProblemDetails
{
    public static ObjectResult Create(
        int statusCode,
        string code,
        string title,
        string detail,
        IReadOnlyDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
        problem.Extensions["code"] = code;
        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }
        }

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
