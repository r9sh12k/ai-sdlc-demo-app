using System.Reflection;
using System.Text.Json;
using InvoiceApp.Api;
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
            ProjectId = "PRJ-PORTAL",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DecidedBy = "previous.approver",
            DecidedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DecisionNote = "previous decision"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var (controller, httpContext) = CreateController(
            repository,
            "demo.manager",
            "Manager");

        var result = controller.Approve("INV-1");

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Equal(
            "This invoice is waiting for demo.director.",
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
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var (controller, httpContext) = CreateController(
            repository,
            "demo.requester",
            "Requester");
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
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-DIGITAL", "demo.admin");
        var (controller, httpContext) = CreateController(
            repository,
            "demo.admin",
            "Admin",
            directory);

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
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var (controller, httpContext) = CreateController(
            repository,
            "demo.admin",
            "Admin");

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
            Status = InvoiceStatus.PendingApproval,
            ProjectId = "PRJ-PORTAL"
        };
        var repository = new FakeInvoiceRepository(invoice);
        var (controller, httpContext) = CreateController(
            repository,
            "demo.requester",
            "Requester");

        var filterContext = ApplyActionRoleFilter("Reject", httpContext, controller);

        var response = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Equal(
            "Your account does not have permission to perform this action.",
            JsonSerializer.SerializeToElement(response.Value).GetProperty("error").GetString());
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void GetAll_ReturnsScopedPendingAndCallerDecisionsWithWaitingOn()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-PORTAL",
                CustomerName = "Portal work",
                Amount = 30_000m,
                Status = InvoiceStatus.PendingApproval,
                ProjectId = "PRJ-PORTAL",
                RoutedApprover1 = "demo.manager"
            },
            new Invoice
            {
                Id = "INV-ERP",
                CustomerName = "ERP work",
                Amount = 30_000m,
                Status = InvoiceStatus.PendingApproval,
                ProjectId = "PRJ-ERP",
                RoutedApprover1 = "demo.director2"
            },
            new Invoice
            {
                Id = "INV-DECIDED",
                CustomerName = "Decided work",
                Amount = 600_000m,
                Status = InvoiceStatus.Approved,
                ProjectId = "PRJ-ERP",
                FirstApprovedBy = "demo.manager"
            });
        var (controller, _) = CreateController(
            repository,
            "demo.manager",
            "Manager");

        var result = controller.GetAll();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var invoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListItem>>(response.Value);
        Assert.Equal(new[] { "INV-PORTAL", "INV-DECIDED" }, invoices.Select(invoice => invoice.Id));
        Assert.Equal(
            "demo.manager (demo.manager@example.com)",
            invoices[0].WaitingOn);
        Assert.Equal(string.Empty, invoices[1].WaitingOn);
    }

    [Theory]
    [InlineData("demo.director", "Director", true, true)]
    [InlineData("demo.admin", "Admin", false, true)]
    [InlineData("demo.manager", "Manager", false, false)]
    public void GetAll_ReportsCallerActionsAndFormatsFirstApprover(
        string username,
        string role,
        bool expectedCanApprove,
        bool expectedCanReject)
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-HIGH",
                CustomerName = "High value",
                Amount = 600_000m,
                Status = InvoiceStatus.PendingApproval,
                ProjectId = "PRJ-PORTAL",
                SubmittedBy = "demo.requester",
                RoutedApprover1 = "demo.manager",
                RoutedApprover2 = "demo.director",
                FirstApprovedBy = "demo.manager"
            });
        var (controller, _) = CreateController(repository, username, role);

        var result = controller.GetAll();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var invoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListItem>>(response.Value);
        var invoice = Assert.Single(invoices);
        Assert.Equal(expectedCanApprove, invoice.CanApprove);
        Assert.Equal(expectedCanReject, invoice.CanReject);
        Assert.False(invoice.CanSubmit);
        Assert.Equal("demo.manager (demo.manager@example.com)", invoice.FirstApprovedBy);
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
        Assert.Equal(expected.ProjectId, actual.ProjectId);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.SubmittedBy, actual.SubmittedBy);
        Assert.Equal(expected.FirstApprovedBy, actual.FirstApprovedBy);
        Assert.Equal(expected.RoutedApprover1, actual.RoutedApprover1);
        Assert.Equal(expected.RoutedApprover2, actual.RoutedApprover2);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
    }

    private static (ApprovalsController Controller, DefaultHttpContext HttpContext) CreateController(
        FakeInvoiceRepository repository,
        string username,
        string role,
        IOrgDirectory? directory = null)
    {
        directory ??= FakeOrgDirectory.CreateDemo();
        var controller = new ApprovalsController(
            new ApprovalService(repository, directory),
            new InvoiceAccessService(repository, directory),
            directory);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession(username, role);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return (controller, httpContext);
    }
}
