using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Runs a service call and maps its failures to HTTP status codes.</summary>
    protected ActionResult<T> Handle<T>(Func<T> action) =>
        Handle(action, value => Ok(value));

    protected ActionResult<T> Handle<T>(
        Func<T> action,
        Func<T, ActionResult<T>> onSuccess)
    {
        try
        {
            return onSuccess(action());
        }
        catch (ApprovalDeniedException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
