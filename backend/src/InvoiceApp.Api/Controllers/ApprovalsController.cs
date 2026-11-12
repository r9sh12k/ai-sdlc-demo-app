using InvoiceApp.Api.Auth;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ApiControllerBase
{
    private readonly ApprovalService _approvals;

    public ApprovalsController(ApprovalService approvals)
    {
        _approvals = approvals;
    }

    public record RejectRequest(string Reason);

    [RequireRole("Manager", "Director", "Admin")]
    [HttpPost("{invoiceId}/approve")]
    public ActionResult<Invoice> Approve(string invoiceId)
    {
        var session = HttpContext.GetSession();
        return Handle(() => _approvals.Approve(invoiceId, session.Username, MapRole(session.Role)));
    }

    [RequireRole("Manager", "Director", "Admin")]
    [HttpPost("{invoiceId}/reject")]
    public ActionResult<Invoice> Reject(string invoiceId, [FromBody] RejectRequest request) =>
        Handle(() => _approvals.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason));

    private static ApproverRole MapRole(string role) => role switch
    {
        "Manager" => ApproverRole.Manager,
        "Director" => ApproverRole.Director,
        "Admin" => ApproverRole.Admin,
        _ => throw new ApprovalDeniedException(
            "Only Managers, Directors, and Admins may approve invoices.")
    };
}
