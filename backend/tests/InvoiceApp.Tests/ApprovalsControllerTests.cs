using System.Text.Json;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalsControllerTests
{
    [Fact]
    public void Approve_MapsApprovalDenialToForbiddenResponse()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 50_000.01m,
            Status = InvoiceStatus.PendingApproval,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DecidedBy = "previous.approver",
            DecidedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DecisionNote = "previous decision"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("demo.manager", "Manager");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = controller.Approve("INV-1");

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Equal(
            "Managers may approve invoices up to 50,000.00.",
            JsonSerializer.SerializeToElement(response.Value).GetProperty("error").GetString());
        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(invoice.Id, storedInvoice.Id);
        Assert.Equal(invoice.CustomerName, storedInvoice.CustomerName);
        Assert.Equal(invoice.Amount, storedInvoice.Amount);
        Assert.Equal(invoice.Status, storedInvoice.Status);
        Assert.Equal(invoice.CreatedAt, storedInvoice.CreatedAt);
        Assert.Equal(invoice.FirstApprovedBy, storedInvoice.FirstApprovedBy);
        Assert.Equal(invoice.DecidedBy, storedInvoice.DecidedBy);
        Assert.Equal(invoice.DecidedAt, storedInvoice.DecidedAt);
        Assert.Equal(invoice.DecisionNote, storedInvoice.DecisionNote);
        Assert.Equal(0, repository.UpdateCount);
    }
}
