using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SessionApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    protected string? CurrentUsername => User.FindFirstValue(ClaimTypes.Name);
}
