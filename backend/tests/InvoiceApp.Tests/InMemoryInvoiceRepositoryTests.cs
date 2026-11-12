using InvoiceApp.Api;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InMemoryInvoiceRepositoryTests
{
    [Fact]
    public void ReadMethods_ReturnCopies()
    {
        var repository = new InMemoryInvoiceRepository();

        repository.GetAll()[0].CustomerName = "Changed";
        repository.GetById("INV-1001")!.Status = InvoiceStatus.Approved;
        repository.GetByIds(new[] { "INV-1002" })[0].CustomerName = "Changed";

        Assert.Equal("Lakeside School District", repository.GetById("INV-1001")!.CustomerName);
        Assert.Equal(InvoiceStatus.PendingApproval, repository.GetById("INV-1001")!.Status);
        Assert.Equal("Hilltop Academy", repository.GetById("INV-1002")!.CustomerName);
    }

    [Fact]
    public void Add_StoresAndReturnsCopies()
    {
        var repository = new InMemoryInvoiceRepository();
        var invoice = new Invoice { Id = "INV-NEW", CustomerName = "Original" };

        var addedInvoice = repository.Add(invoice);
        invoice.CustomerName = "Changed input";
        addedInvoice.CustomerName = "Changed result";

        Assert.Equal("Original", repository.GetById("INV-NEW")!.CustomerName);
    }

    [Fact]
    public void Update_StoresCopy()
    {
        var repository = new InMemoryInvoiceRepository();
        var updatedInvoice = repository.GetById("INV-1001")!;
        updatedInvoice.CustomerName = "Updated";

        repository.Update(updatedInvoice);
        updatedInvoice.CustomerName = "Changed after update";

        Assert.Equal("Updated", repository.GetById("INV-1001")!.CustomerName);
    }

    [Fact]
    public void Copies_PreserveSubmittedBy()
    {
        var repository = new InMemoryInvoiceRepository();
        var invoice = new Invoice
        {
            Id = "INV-SUBMITTED",
            CustomerName = "Submitted School",
            SubmittedBy = "demo.requester"
        };

        repository.Add(invoice);
        var retrieved = repository.GetById("INV-SUBMITTED")!;

        Assert.Equal("demo.requester", retrieved.SubmittedBy);
        retrieved.SubmittedBy = "demo.manager";
        repository.Update(retrieved);

        Assert.Equal("demo.manager", repository.GetAll()
            .Single(savedInvoice => savedInvoice.Id == "INV-SUBMITTED").SubmittedBy);
    }
}
