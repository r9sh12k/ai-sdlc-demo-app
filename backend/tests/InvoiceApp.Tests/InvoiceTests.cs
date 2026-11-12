using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceTests
{
    [Fact]
    public void NewInvoice_InitializesRequiredTextPropertiesToEmptyStrings()
    {
        var invoice = new Invoice();

        Assert.Equal(string.Empty, invoice.Id);
        Assert.Equal(string.Empty, invoice.CustomerName);
        Assert.Equal(string.Empty, invoice.ProjectId);
    }
}
