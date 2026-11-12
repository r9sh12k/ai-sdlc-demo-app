using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status) =>
        new() { Id = id, CustomerName = "Test School", Amount = 100m, Status = status };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, repository.GetById("INV-1")!.Status);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_SetsStatusApproverAndTimestamp()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.approver");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.approver", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(() => service.Approve("INV-1", "demo.approver"));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(() => service.Approve("INV-404", "demo.approver"));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Rejected, storedInvoice.Status);
        Assert.Equal("demo.approver", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal("Duplicate invoice", storedInvoice.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var exception = Assert.Throws<ArgumentException>(
            () => service.Reject("INV-1", "demo.approver", "  "));

        Assert.Equal("A reason is required to reject an invoice.", exception.Message);
    }
}
