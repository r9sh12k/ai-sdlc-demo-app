using InvoiceApp.Domain;

namespace InvoiceApp.Api;

/// <summary>In-memory store so the demo app runs with no database. Seeded with a few invoices.</summary>
public class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly object _lock = new();
    private int _nextNumber = 1006;

    private readonly List<Invoice> _invoices = new()
    {
        new Invoice
        {
            Id = "INV-1001",
            CustomerName = "Lakeside School District",
            Amount = 4250.00m,
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL",
            SubmittedBy = "demo.requester",
            RoutedApprover1 = "demo.manager"
        },
        new Invoice
        {
            Id = "INV-1002",
            CustomerName = "Hilltop Academy",
            Amount = 980.50m,
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-MOBILE",
            SubmittedBy = "demo.requester",
            RoutedApprover1 = "demo.manager2"
        },
        new Invoice
        {
            Id = "INV-1003",
            CustomerName = "Riverside Charter",
            Amount = 12300.00m,
            Status = InvoiceStatus.Approved,
            ProjectId = "PRJ-ERP",
            SubmittedBy = "demo.requester2",
            RoutedApprover1 = "demo.director2",
            DecidedBy = "demo.director2",
            DecidedAt = DateTime.UtcNow.AddDays(-2)
        },
        new Invoice
        {
            Id = "INV-1004",
            CustomerName = "Oakwood Independent",
            Amount = 615.75m,
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL",
            SubmittedBy = "demo.requester",
            RoutedApprover1 = "demo.manager"
        },
        new Invoice
        {
            Id = "INV-1005",
            CustomerName = "Maple Grove Prep",
            Amount = 310.00m,
            ProjectId = "PRJ-PORTAL",
            SubmittedBy = "demo.requester"
        },
    };

    public IReadOnlyList<Invoice> GetAll()
    {
        lock (_lock) return _invoices.Select(Copy).ToList();
    }

    public Invoice? GetById(string id)
    {
        lock (_lock)
        {
            var invoice = _invoices.FirstOrDefault(i => i.Id == id);
            return invoice is null ? null : Copy(invoice);
        }
    }

    public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        lock (_lock) return _invoices.Where(i => wanted.Contains(i.Id)).Select(Copy).ToList();
    }

    public Invoice Add(Invoice invoice)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(invoice.Id))
            {
                invoice.Id = $"INV-{_nextNumber++}";
            }

            _invoices.Add(Copy(invoice));
            return Copy(invoice);
        }
    }

    public void Update(Invoice invoice)
    {
        lock (_lock)
        {
            var index = _invoices.FindIndex(i => i.Id == invoice.Id);
            if (index >= 0) _invoices[index] = Copy(invoice);
        }
    }

    public bool Remove(string id)
    {
        lock (_lock) return _invoices.RemoveAll(i => i.Id == id) > 0;
    }

    private static Invoice Copy(Invoice invoice) => new()
    {
        Id = invoice.Id,
        CustomerName = invoice.CustomerName,
        Amount = invoice.Amount,
        Status = invoice.Status,
        ProjectId = invoice.ProjectId,
        CreatedAt = invoice.CreatedAt,
        SubmittedBy = invoice.SubmittedBy,
        FirstApprovedBy = invoice.FirstApprovedBy,
        RoutedApprover1 = invoice.RoutedApprover1,
        RoutedApprover2 = invoice.RoutedApprover2,
        DecidedBy = invoice.DecidedBy,
        DecidedAt = invoice.DecidedAt,
        DecisionNote = invoice.DecisionNote
    };
}
