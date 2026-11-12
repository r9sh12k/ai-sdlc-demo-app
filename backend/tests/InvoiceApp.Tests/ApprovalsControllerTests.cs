using System.Reflection;
using System.Text.Json;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using InvoiceApp.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
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

    [Fact]
    public void Approve_RequesterIsDeniedBeforeInvoiceCanChange()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 30_000m,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] =
            new UserSession("demo.requester", "Requester");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var executingContext = ApplyActionRoleFilter("Approve", httpContext, controller);

        var response = Assert.IsType<ObjectResult>(executingContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Equal(
            "Your account does not have permission to perform this action.",
            JsonSerializer.SerializeToElement(response.Value).GetProperty("error").GetString());
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Approve_AdminCanApproveEligibleInvoice()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 100_000m,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("demo.admin", "Admin");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var filterContext = ApplyActionRoleFilter("Approve", httpContext, controller);
        Assert.Null(filterContext.Result);

        var result = controller.Approve("INV-1");

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(InvoiceStatus.Approved, repository.GetById("INV-1")!.Status);
        Assert.Equal("demo.admin", repository.GetById("INV-1")!.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Reject_AdminCanRejectPendingInvoiceWithReason()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 30_000m,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("demo.admin", "Admin");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var filterContext = ApplyActionRoleFilter("Reject", httpContext, controller);
        Assert.Null(filterContext.Result);

        var result = controller.Reject(
            "INV-1",
            new ApprovalsController.RejectRequest("Duplicate invoice."));

        Assert.IsType<OkObjectResult>(result.Result);
        var rejectedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Rejected, rejectedInvoice.Status);
        Assert.Equal("demo.admin", rejectedInvoice.DecidedBy);
        Assert.Equal("Duplicate invoice.", rejectedInvoice.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RequesterIsDeniedBeforeInvoiceCanChange()
    {
        var invoice = new Invoice
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = 30_000m,
            Status = InvoiceStatus.PendingApproval
        };
        var repository = new FakeInvoiceRepository(invoice);
        var controller = new ApprovalsController(new ApprovalService(repository));
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] =
            new UserSession("demo.requester", "Requester");
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var filterContext = ApplyActionRoleFilter("Reject", httpContext, controller);

        var response = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Equal(
            "Your account does not have permission to perform this action.",
            JsonSerializer.SerializeToElement(response.Value).GetProperty("error").GetString());
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    private static ActionExecutingContext ApplyActionRoleFilter(
        string actionName,
        HttpContext httpContext,
        ApprovalsController controller)
    {
        var action = typeof(ApprovalsController).GetMethod(actionName)!;
        var requireRole = action.GetCustomAttribute<RequireRoleAttribute>();
        Assert.NotNull(requireRole);

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);

        requireRole.OnActionExecuting(executingContext);
        return executingContext;
    }

    private static void AssertInvoiceEqual(Invoice expected, Invoice actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CustomerName, actual.CustomerName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.SubmittedBy, actual.SubmittedBy);
        Assert.Equal(expected.FirstApprovedBy, actual.FirstApprovedBy);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
    }
}
