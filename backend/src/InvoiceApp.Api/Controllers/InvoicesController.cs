using InvoiceApp.Api.Auth;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ApiControllerBase
{
    private readonly IInvoiceRepository _repository;
    private readonly ApprovalService _approvals;
    private readonly InvoiceAccessService _access;
    private readonly IOrgDirectory _orgDirectory;

    public InvoicesController(
        IInvoiceRepository repository,
        ApprovalService approvals,
        InvoiceAccessService access,
        IOrgDirectory orgDirectory)
    {
        _repository = repository;
        _approvals = approvals;
        _access = access;
        _orgDirectory = orgDirectory;
    }

    public record CreateInvoiceRequest(string CustomerName, decimal Amount, string ProjectId);
    public record ProjectOption(string Id, string Name);

    [HttpGet]
    public ActionResult<IReadOnlyList<InvoiceListItem>> GetAll([FromQuery] string? status)
    {
        var invoices = _access.GetInvoicesFor(HttpContext.GetSession().Username);

        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var parsed))
            {
                return BadRequest(new { error = $"Unknown status '{status}'." });
            }

            invoices = invoices.Where(invoice => invoice.Status == parsed).ToArray();
        }

        return Ok(invoices.Select(ToListItem).ToArray());
    }

    [HttpGet("{id}")]
    public ActionResult<InvoiceListItem> GetById(string id)
    {
        var invoice = _access.GetByIdFor(id, HttpContext.GetSession().Username);
        return invoice is null ? NotFound() : Ok(ToListItem(invoice));
    }

    [HttpGet("projects")]
    public ActionResult<IReadOnlyList<ProjectOption>> GetProjectOptions()
    {
        var projects = _access.GetProjectOptionsFor(HttpContext.GetSession().Username)
            .Select(project => new ProjectOption(project.Id, project.Id))
            .ToArray();
        return Ok(projects);
    }

    [HttpPost]
    public ActionResult<Invoice> Create([FromBody] CreateInvoiceRequest request)
    {
        var username = HttpContext.GetSession().Username;
        return Handle(
            () => _access.CreateDraft(
                request.CustomerName,
                request.Amount,
                request.ProjectId,
                username),
            invoice => CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice));
    }

    [HttpPost("{id}/submit")]
    public ActionResult<Invoice> Submit(string id) =>
        Handle(() => _approvals.Submit(id, HttpContext.GetSession().Username));

    [HttpDelete("{id}")]
    [RequireRole("Admin")]
    public IActionResult Delete(string id) =>
        _repository.Remove(id) ? NoContent() : NotFound();

    private InvoiceListItem ToListItem(Invoice invoice) =>
        InvoicePresentation.ToListItem(
            invoice,
            _orgDirectory,
            _access,
            _approvals,
            HttpContext.GetSession().ToViewer());
}
