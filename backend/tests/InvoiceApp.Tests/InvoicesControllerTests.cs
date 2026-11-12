using InvoiceApp.Api.Auth;
using InvoiceApp.Api;
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
            new Invoice
            {
                Id = "INV-1",
                CustomerName = "Test School",
                Amount = 30_000m,
                ProjectId = "PRJ-PORTAL"
            });
        var controller = CreateController(repository, "demo.manager", "Manager");

        controller.Submit("INV-1");

        Assert.Equal("demo.manager", repository.GetById("INV-1")!.SubmittedBy);
    }

    [Fact]
    public void RequesterCanSubmitInvoiceThatManagerCanApprove()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-1",
                CustomerName = "Test School",
                Amount = 30_000m,
                ProjectId = "PRJ-PORTAL"
            });
        var requester = CreateController(repository, "demo.requester", "Requester");
        var manager = CreateApprovalsController(repository, "demo.manager", "Manager");

        requester.Submit("INV-1");
        var result = manager.Approve("INV-1");

        Assert.IsType<OkObjectResult>(result.Result);
        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.requester", storedInvoice.SubmittedBy);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.Equal(2, repository.UpdateCount);
    }

    [Fact]
    public void GetProjectOptions_ReturnsOnlyProjectsAllocatedToSignedInUser()
    {
        var controller = CreateController(
            new FakeInvoiceRepository(),
            "demo.requester",
            "Requester");

        var result = controller.GetProjectOptions();

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var projects = Assert.IsAssignableFrom<IReadOnlyList<InvoicesController.ProjectOption>>(
            response.Value);
        Assert.Equal(new[] { "PRJ-PORTAL", "PRJ-MOBILE" }, projects.Select(project => project.Id));
    }

    [Fact]
    public void Create_RecordsAllocatedProjectAndRejectsUnallocatedUserWithoutAddingInvoice()
    {
        var repository = new FakeInvoiceRepository();
        var requester = CreateController(repository, "demo.requester", "Requester");

        var created = requester.Create(
            new InvoicesController.CreateInvoiceRequest("Portal work", 12_500m, "PRJ-PORTAL"));

        Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal("PRJ-PORTAL", repository.GetAll().Single().ProjectId);
        Assert.Equal("demo.requester", repository.GetAll().Single().SubmittedBy);

        var manager2 = CreateController(repository, "demo.manager2", "Manager");
        var denied = manager2.Create(
            new InvoicesController.CreateInvoiceRequest("Portal work", 12_500m, "PRJ-PORTAL"));

        var response = Assert.IsType<ObjectResult>(denied.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.Single(repository.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsOnlyScopedInvoicesWithDisplayFields()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-PORTAL",
                CustomerName = "Portal work",
                Amount = 50_000m,
                Status = InvoiceStatus.PendingApproval,
                ProjectId = "PRJ-PORTAL",
                SubmittedBy = "demo.requester",
                RoutedApprover1 = "demo.manager"
            },
            new Invoice
            {
                Id = "INV-ERP",
                CustomerName = "ERP work",
                Amount = 30_000m,
                Status = InvoiceStatus.Approved,
                ProjectId = "PRJ-ERP",
                SubmittedBy = "demo.requester2"
            });
        var controller = CreateController(repository, "demo.requester", "Requester");

        var result = controller.GetAll(null);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var invoices = Assert.IsAssignableFrom<IReadOnlyList<InvoiceListItem>>(response.Value);
        var invoice = Assert.Single(invoices);
        Assert.Equal("PRJ-PORTAL", invoice.Project);
        Assert.Equal("demo.requester (demo.requester@example.com)", invoice.RequestedBy);
        Assert.Equal("demo.manager (demo.manager@example.com)", invoice.Approval1);
        Assert.Equal("Not applicable", invoice.Approval2);
        Assert.Equal("demo.manager (demo.manager@example.com)", invoice.WaitingOn);
        Assert.Equal(string.Empty, invoice.RejectionReason);
    }

    [Fact]
    public void GetAll_ReportsSubmitOnlyForTheDraftRequester()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-DRAFT",
                CustomerName = "Portal draft",
                Amount = 10_000m,
                ProjectId = "PRJ-PORTAL",
                SubmittedBy = "demo.requester"
            });

        var requesterItem = Assert.Single(ListFor(repository, "demo.requester", "Requester"));
        var managerItem = Assert.Single(ListFor(repository, "demo.manager", "Manager"));

        Assert.True(requesterItem.CanSubmit);
        Assert.False(requesterItem.CanApprove);
        Assert.False(requesterItem.CanReject);
        Assert.False(managerItem.CanSubmit);
    }

    [Fact]
    public void Submit_NonRequesterGetsForbiddenAndInvoiceIsUnchanged()
    {
        var repository = new FakeInvoiceRepository(
            new Invoice
            {
                Id = "INV-DRAFT",
                CustomerName = "Portal draft",
                Amount = 10_000m,
                ProjectId = "PRJ-PORTAL",
                SubmittedBy = "demo.requester"
            });
        var manager = CreateController(repository, "demo.manager", "Manager");

        var result = manager.Submit("INV-DRAFT");

        var response = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        var stored = repository.GetById("INV-DRAFT")!;
        Assert.Equal(InvoiceStatus.Draft, stored.Status);
        Assert.Equal("demo.requester", stored.SubmittedBy);
        Assert.Null(stored.RoutedApprover1);
        Assert.Equal(0, repository.UpdateCount);
    }

    private static IReadOnlyList<InvoiceListItem> ListFor(
        FakeInvoiceRepository repository,
        string username,
        string role)
    {
        var result = CreateController(repository, username, role).GetAll(null);
        var response = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IReadOnlyList<InvoiceListItem>>(response.Value);
    }

    private static InvoicesController CreateController(
        FakeInvoiceRepository repository,
        string username,
        string role)
    {
        var directory = FakeOrgDirectory.CreateDemo();
        var controller = new InvoicesController(
            repository,
            new ApprovalService(repository, directory),
            new InvoiceAccessService(repository, directory),
            directory);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession(username, role);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static ApprovalsController CreateApprovalsController(
        FakeInvoiceRepository repository,
        string username,
        string role)
    {
        var directory = FakeOrgDirectory.CreateDemo();
        var controller = new ApprovalsController(
            new ApprovalService(repository, directory),
            new InvoiceAccessService(repository, directory),
            directory);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession(username, role);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}
