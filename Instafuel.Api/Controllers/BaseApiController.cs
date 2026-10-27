using Microsoft.AspNetCore.Mvc;

namespace Instafuel.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
}

