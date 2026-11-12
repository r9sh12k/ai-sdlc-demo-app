using System.Globalization;

namespace InvoiceApp.Domain;

/// <summary>
/// Moves an invoice through its lifecycle: Draft -> PendingApproval -> Approved or Rejected.
/// Approval authority depends on the invoice amount and approver role.
/// </summary>
public class ApprovalService
{
    public const decimal ManagerApprovalLimit = 50_000.00m;
    public const decimal DirectorApprovalLimit = 500_000.00m;

    private readonly IInvoiceRepository _repository;

    public ApprovalService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public Invoice Submit(string invoiceId)
    {
        var invoice = Find(invoiceId);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Invoice {invoiceId} is {invoice.Status}; only drafts can be submitted.");
        }

        invoice.Status = InvoiceStatus.PendingApproval;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Approve(string invoiceId, string approver, ApproverRole role)
    {
        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        if (role == ApproverRole.Admin ||
            (role != ApproverRole.Manager && role != ApproverRole.Director))
        {
            throw new ApprovalDeniedException("Only Managers and Directors may approve invoices.");
        }

        if (invoice.Amount > DirectorApprovalLimit)
        {
            if (string.IsNullOrWhiteSpace(invoice.FirstApprovedBy))
            {
                invoice.FirstApprovedBy = approver;
                _repository.Update(invoice);
                return invoice;
            }

            if (role != ApproverRole.Director)
            {
                throw new ApprovalDeniedException("A Director must provide the second approval.");
            }

            if (string.Equals(
                invoice.FirstApprovedBy.Trim(),
                approver.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new ApprovalDeniedException(
                    "A different Director must provide the second approval.");
            }
        }

        if (role == ApproverRole.Manager && invoice.Amount > ManagerApprovalLimit)
        {
            var limit = ManagerApprovalLimit.ToString("N2", CultureInfo.InvariantCulture);
            throw new ApprovalDeniedException(
                $"Managers may approve invoices up to {limit}.");
        }

        invoice.Status = InvoiceStatus.Approved;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        _repository.Update(invoice);
        return invoice;
    }

    public Invoice Reject(string invoiceId, string approver, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A reason is required to reject an invoice.");
        }

        var invoice = Find(invoiceId);
        EnsurePending(invoice);

        invoice.Status = InvoiceStatus.Rejected;
        invoice.DecidedBy = approver;
        invoice.DecidedAt = DateTime.UtcNow;
        invoice.DecisionNote = reason;
        _repository.Update(invoice);
        return invoice;
    }

    private Invoice Find(string invoiceId) =>
        _repository.GetById(invoiceId)
        ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

    private static void EnsurePending(Invoice invoice)
    {
        if (invoice.Status != InvoiceStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.Id} is {invoice.Status}; only pending invoices can be decided.");
        }
    }
}
