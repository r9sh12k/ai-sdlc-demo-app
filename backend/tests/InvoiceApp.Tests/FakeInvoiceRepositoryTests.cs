using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class FakeInvoiceRepositoryTests
{
    [Fact]
    public void ReadMethods_ReturnCopies()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice { Id = "INV-1", CustomerName = "Original", Status = InvoiceStatus.Draft },
            new Invoice { Id = "INV-2", CustomerName = "Second", Status = InvoiceStatus.PendingApproval });

        repository.GetAll()[0].CustomerName = "Changed";
        repository.GetById("INV-1")!.Status = InvoiceStatus.Approved;
        repository.GetByIds(new[] { "INV-2" })[0].CustomerName = "Changed";

        Assert.Equal("Original", repository.GetById("INV-1")!.CustomerName);
        Assert.Equal(InvoiceStatus.Draft, repository.GetById("INV-1")!.Status);
        Assert.Equal("Second", repository.GetById("INV-2")!.CustomerName);
    }

    [Fact]
    public void Add_StoresAndReturnsCopies()
    {
        var repository = new FakeInvoiceRepository();
        var invoice = new Invoice { Id = "INV-1", CustomerName = "Original" };

        var addedInvoice = repository.Add(invoice);
        invoice.CustomerName = "Changed input";
        addedInvoice.CustomerName = "Changed result";

        Assert.Equal("Original", repository.GetById("INV-1")!.CustomerName);
    }

    [Fact]
    public void Update_StoresCopyAndIncrementsUpdateCount()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice { Id = "INV-1", CustomerName = "Original" });
        var updatedInvoice = repository.GetById("INV-1")!;
        updatedInvoice.CustomerName = "Updated";

        repository.Update(updatedInvoice);
        updatedInvoice.CustomerName = "Changed after update";

        Assert.Equal(1, repository.UpdateCount);
        Assert.Equal("Updated", repository.GetById("INV-1")!.CustomerName);
    }
}
