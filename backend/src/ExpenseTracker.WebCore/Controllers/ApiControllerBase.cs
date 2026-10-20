using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.WebCore.Controllers;

/// <summary>Base class for all API controllers: conventional routing.</summary>
[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
}
