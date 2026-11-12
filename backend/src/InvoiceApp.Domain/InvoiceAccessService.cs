namespace InvoiceApp.Domain;

public sealed class InvoiceAccessService
{
    private readonly IInvoiceRepository _repository;
    private readonly IOrgDirectory _orgDirectory;

    public InvoiceAccessService(IInvoiceRepository repository, IOrgDirectory orgDirectory)
    {
        _repository = repository;
        _orgDirectory = orgDirectory;
    }

    public Invoice CreateDraft(
        string customerName,
        decimal amount,
        string projectId,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ArgumentException("Customer name is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("Project is required.");
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Signed-in user is required.");
        }

        var project = _orgDirectory.GetProject(projectId.Trim());
        if (project is null)
        {
            throw new ApprovalDeniedException($"Project {projectId} was not found.");
        }

        if (!_orgDirectory.IsAllocatedToProject(project.Id, createdBy))
        {
            throw new ApprovalDeniedException(
                $"You are not allocated to project {project.Id}.");
        }

        return _repository.Add(new Invoice
        {
            CustomerName = customerName.Trim(),
            Amount = amount,
            ProjectId = project.Id,
            SubmittedBy = createdBy.Trim()
        });
    }

    public IReadOnlyList<Project> GetProjectOptionsFor(string username) =>
        _orgDirectory.GetProjects()
            .Where(project => _orgDirectory.IsAllocatedToProject(project.Id, username))
            .ToArray();

    public IReadOnlyList<Invoice> GetInvoicesFor(string username) =>
        _repository.GetAll()
            .Where(invoice => CanViewInvoice(invoice, username))
            .ToArray();

    public IReadOnlyList<Invoice> GetApprovalsFor(string username) =>
        _repository.GetAll()
            .Where(invoice =>
                ApprovalRouting.SameUser(invoice.DecidedBy, username) ||
                ApprovalRouting.SameUser(invoice.FirstApprovedBy, username) ||
                (invoice.Status == InvoiceStatus.PendingApproval &&
                 IsInApprovalScope(invoice, username)))
            .ToArray();

    public Invoice? GetByIdFor(string invoiceId, string username)
    {
        var invoice = _repository.GetById(invoiceId);
        return invoice is not null && CanViewInvoice(invoice, username) ? invoice : null;
    }

    public string? GetWaitingOn(Invoice invoice) =>
        invoice.Status == InvoiceStatus.PendingApproval
            ? ApprovalRouting.NextApprover(invoice, _orgDirectory)
            : null;

    private bool CanViewInvoice(Invoice invoice, string username) =>
        IsAdmin(username) ||
        ApprovalRouting.SameUser(invoice.SubmittedBy, username) ||
        IsInApprovalScope(invoice, username);

    private bool IsInApprovalScope(Invoice invoice, string username) =>
        IsAdmin(username) ||
        IsDeliveryManagerForProject(invoice.ProjectId, username) ||
        IsBusinessUnitHeadForProject(invoice.ProjectId, username);

    private bool IsDeliveryManagerForProject(string projectId, string username) =>
        ApprovalRouting.SameUser(
            _orgDirectory.GetProject(projectId)?.DeliveryManagerUsername,
            username);

    private bool IsBusinessUnitHeadForProject(string projectId, string username)
    {
        var project = _orgDirectory.GetProject(projectId);
        var account = project is null ? null : _orgDirectory.GetAccount(project.AccountId);
        var businessUnit = account is null
            ? null
            : _orgDirectory.GetBusinessUnit(account.BusinessUnitId);

        return ApprovalRouting.SameUser(businessUnit?.HeadUsername, username);
    }

    private bool IsAdmin(string username) =>
        _orgDirectory.GetAdminUsernames()
            .Any(admin => ApprovalRouting.SameUser(admin, username));
}
