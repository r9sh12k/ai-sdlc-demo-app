using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

internal class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> _invoices = new();

    public int UpdateCount { get; private set; }

    public FakeInvoiceRepository(params Invoice[] seed) => _invoices.AddRange(seed.Select(Copy));

    public IReadOnlyList<Invoice> GetAll() => _invoices.Select(Copy).ToList();
    public Invoice? GetById(string id)
    {
        var invoice = _invoices.FirstOrDefault(i => i.Id == id);
        return invoice is null ? null : Copy(invoice);
    }

    public IReadOnlyList<Invoice> GetByIds(IEnumerable<string> ids)
    {
        var wanted = ids.ToHashSet();
        return _invoices.Where(i => wanted.Contains(i.Id)).Select(Copy).ToList();
    }

    public Invoice Add(Invoice invoice)
    {
        _invoices.Add(Copy(invoice));
        return Copy(invoice);
    }

    public void Update(Invoice invoice)
    {
        UpdateCount++;
        var index = _invoices.FindIndex(i => i.Id == invoice.Id);
        if (index >= 0) _invoices[index] = Copy(invoice);
    }

    public bool Remove(string id) => _invoices.RemoveAll(i => i.Id == id) > 0;

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
