using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoicesControllerTests
{
    [Fact]
    public void Submit_RecordsSignedInUsername()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice { Id = "INV-1", CustomerName = "Test School", Amount = 30_000m });
        var controller = CreateController(repository, "demo.manager", "Manager");

        controller.Submit("INV-1");

        Assert.Equal("demo.manager", repository.GetById("INV-1")!.SubmittedBy);
    }

    [Fact]
    public void RequesterCanSubmitInvoiceThatManagerCanApprove()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice { Id = "INV-1", CustomerName = "Test School", Amount = 30_000m });
        var requester = CreateController(repository, "demo.requester", "Requester");
        var manager = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] =
            new UserSession("demo.manager", "Manager");
        manager.ControllerContext = new ControllerContext { HttpContext = httpContext };

        requester.Submit("INV-1");
        var result = manager.Approve("INV-1");

        Assert.IsType<OkObjectResult>(result.Result);
        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.requester", storedInvoice.SubmittedBy);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.Equal(2, repository.UpdateCount);
    }

    private static InvoicesController CreateController(
        FakeInvoiceRepository repository,
        string username,
        string role)
    {
        var controller = new InvoicesController(repository, new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession(username, role);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}
