using Control_OS_Lunix.Backend.Results;
using Microsoft.AspNetCore.Mvc;

namespace ControlOS.Api.Features.Shared;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult OkOrProblem<T>(Result<T> result, string? title = null) where T : class =>
        result is { IsSuccess: true, Value: not null }
            ? Ok(result.Value)
            : Problem(title: title, detail: result.Message, statusCode: StatusCodes.Status400BadRequest);

    protected IActionResult OkOrProblem(Result result, string? title = null) =>
        result.IsSuccess
            ? Ok(new { message = result.Message })
            : Problem(title: title, detail: result.Message, statusCode: StatusCodes.Status400BadRequest);

    protected IActionResult NoContentOrProblem(Result result, string? title = null) =>
        result.IsSuccess
            ? NoContent()
            : Problem(title: title, detail: result.Message, statusCode: StatusCodes.Status400BadRequest);
}
