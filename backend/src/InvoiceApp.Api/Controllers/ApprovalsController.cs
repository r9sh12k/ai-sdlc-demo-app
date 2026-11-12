using InvoiceApp.Api.Auth;
using InvoiceApp.Api;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ApiControllerBase
{
    private readonly ApprovalService _approvals;
    private readonly InvoiceAccessService _access;
    private readonly IOrgDirectory _orgDirectory;

    public ApprovalsController(
        ApprovalService approvals,
        InvoiceAccessService access,
        IOrgDirectory orgDirectory)
    {
        _approvals = approvals;
        _access = access;
        _orgDirectory = orgDirectory;
    }

    public record RejectRequest(string Reason);

    [HttpGet]
    public ActionResult<IReadOnlyList<InvoiceListItem>> GetAll()
    {
        var session = HttpContext.GetSession();
        var viewer = session.ToViewer();
        var invoices = _access.GetApprovalsFor(session.Username)
            .Select(invoice => InvoicePresentation.ToListItem(
                invoice,
                _orgDirectory,
                _access,
                _approvals,
                viewer))
            .ToArray();
        return Ok(invoices);
    }

    [RequireRole("Manager", "Director", "Admin")]
    [HttpPost("{invoiceId}/approve")]
    public ActionResult<Invoice> Approve(string invoiceId)
    {
        var session = HttpContext.GetSession();
        return Handle(() => _approvals.Approve(invoiceId, session.Username, RequireApproverRole(session.Role)));
    }

    [RequireRole("Manager", "Director", "Admin")]
    [HttpPost("{invoiceId}/reject")]
    public ActionResult<Invoice> Reject(string invoiceId, [FromBody] RejectRequest request) =>
        Handle(() => _approvals.Reject(invoiceId, HttpContext.GetSession().Username, request.Reason));

    private static ApproverRole RequireApproverRole(string role) =>
        SessionRoles.ToApproverRole(role)
        ?? throw new ApprovalDeniedException(
            "Only Managers, Directors, and Admins may approve invoices.");
}
