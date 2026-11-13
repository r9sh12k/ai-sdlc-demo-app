namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Approval authority is determined by the invoice's project route.
/// </summary>
public class ApprovalService
{
    public const decimal ManagerApprovalLimit = 50_000.00m;
    public const decimal DirectorApprovalLimit = 500_000.00m;

    private readonly IInvoiceRepository _repository;
    private readonly IOrgDirectory _orgDirectory;

    public ApprovalService(IInvoiceRepository repository, IOrgDirectory orgDirectory)
    {
        _repository = repository;
        _orgDirectory = orgDirectory;
    }

    public Invoice Submit(string invoiceId, string submittedBy)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        var denial = SubmitDenial(invoice, submittedBy);
        if (denial is not null)
        {
            throw new ApprovalDeniedException(denial);
        }

        var firstApprover = ApprovalRouting.NextApprover(invoice, _orgDirectory, submittedBy);
        if (string.IsNullOrWhiteSpace(firstApprover))
        {
            throw new ApprovalDeniedException("No approver available for this project.");
        }

        string? secondApprover = null;
        if (invoice.Amount > DirectorApprovalLimit)
        {
            secondApprover =
                ApprovalRouting.NextApprover(invoice, _orgDirectory, submittedBy, firstApprover);
            if (string.IsNullOrWhiteSpace(secondApprover))
            {
                throw new ApprovalDeniedException("No second approver available");
            }
        }

        invoice.RoutedApprover1 = firstApprover;
        invoice.RoutedApprover2 = secondApprover;
        invoice.SubmittedBy = string.IsNullOrWhiteSpace(invoice.SubmittedBy)
            ? submittedBy.Trim()
            : invoice.SubmittedBy.Trim();
        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, ApproverRole role)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        var denial = ApprovalDenial(invoice, approver, role);
        if (denial is not null)
        {
            throw new ApprovalDeniedException(denial);
        }

        if (invoice.Amount > DirectorApprovalLimit &&
            string.IsNullOrWhiteSpace(invoice.FirstApprovedBy))
        {
            invoice.FirstApprovedBy = approver.Trim();
            _repository.Update(invoice);
            return invoice;
        }

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver.Trim();
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ApprovalDeniedException("A reason is required to reject an invoice.");
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        var denial = RejectionDenial(invoice, approver);
        if (denial is not null)
        {
            throw new ApprovalDeniedException(denial);
        }

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver.Trim();
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason.Trim();
        _repository.Update(invoice);
        return invoice;
    }

    /// <summary>True when <paramref name="username"/> may submit this draft now.</summary>
    public bool CanSubmit(Invoice invoice, string username) =>
        invoice.Status == InvoiceStatus.Draft && SubmitDenial(invoice, username) is null;

    /// <summary>True when <paramref name="username"/> may approve this invoice now.</summary>
    public bool CanApprove(Invoice invoice, string username, ApproverRole? role) =>
        invoice.Status == InvoiceStatus.PendingApproval &&
        ApprovalDenial(invoice, username, role) is null;

    /// <summary>True when <paramref name="username"/> may reject this invoice now.</summary>
    public bool CanReject(Invoice invoice, string username) =>
        invoice.Status == InvoiceStatus.PendingApproval &&
        RejectionDenial(invoice, username) is null;

    private static string? SubmitDenial(Invoice invoice, string submittedBy) =>
        string.IsNullOrWhiteSpace(invoice.SubmittedBy) ||
        ApprovalRouting.SameUser(invoice.SubmittedBy, submittedBy)
            ? null
            : "Only the requester who created this draft can submit it.";

    private string? ApprovalDenial(Invoice invoice, string approver, ApproverRole? role)
    {
        if (ApprovalRouting.SameUser(invoice.SubmittedBy, approver))
        {
            return "You submitted this invoice, so someone else must approve it.";
        }

        if (role is null || !Enum.IsDefined(role.Value))
        {
            return "Only Managers, Directors, and Admins may approve invoices.";
        }

        if (invoice.Amount > DirectorApprovalLimit &&
            !string.IsNullOrWhiteSpace(invoice.FirstApprovedBy))
        {
            if (ApprovalRouting.SameUser(invoice.FirstApprovedBy, approver))
            {
                return "A different person must provide the second approval.";
            }

            if (role != ApproverRole.Director)
            {
                return "Only a Director may provide the second approval.";
            }
        }
        else if (role == ApproverRole.Manager &&
            invoice.Amount > ManagerApprovalLimit &&
            invoice.Amount <= DirectorApprovalLimit)
        {
            return "Managers may only approve invoices up to 50,000.00.";
        }

        return WaitingForSomeoneElse(invoice, approver);
    }

    private string? RejectionDenial(Invoice invoice, string approver) =>
        IsAdmin(approver) ? null : WaitingForSomeoneElse(invoice, approver);

    private string? WaitingForSomeoneElse(Invoice invoice, string username)
    {
        var nextApprover = ApprovalRouting.NextApprover(invoice, _orgDirectory);
        if (ApprovalRouting.SameUser(nextApprover, username))
        {
            return null;
        }

        return nextApprover is null
            ? "No approver is currently assigned to this invoice."
            : $"This invoice is waiting for {nextApprover}.";
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private bool IsAdmin(string username) =>
        _orgDirectory.GetAdminUsernames()
            .Any(admin => ApprovalRouting.SameUser(admin, username));

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
