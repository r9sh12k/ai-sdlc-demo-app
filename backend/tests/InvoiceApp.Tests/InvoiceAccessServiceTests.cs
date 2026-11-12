using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class InvoiceAccessServiceTests
{
    [Fact]
    public void CreateDraft_RecordsProjectAndRequesterForAllocatedUser()
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var invoice = service.CreateDraft(
            "  Portal work  ",
            12_500m,
            "PRJ-PORTAL",
            "demo.requester");

        Assert.Equal("Portal work", invoice.CustomerName);
        Assert.Equal("PRJ-PORTAL", invoice.ProjectId);
        Assert.Equal("demo.requester", invoice.SubmittedBy);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Single(repository.GetAll());
    }

    [Fact]
    public void CreateDraft_AllowsDeliveryManagerBecauseTheyAreAllocated()
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var invoice = service.CreateDraft(
            "Portal work",
            12_500m,
            "PRJ-PORTAL",
            "demo.manager");

        Assert.Equal("demo.manager", invoice.SubmittedBy);
        Assert.Equal("PRJ-PORTAL", invoice.ProjectId);
    }

    [Fact]
    public void CreateDraft_DeniesUnallocatedUserWithoutAddingInvoice()
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.CreateDraft(
                "Portal work",
                12_500m,
                "PRJ-PORTAL",
                "demo.manager2"));

        Assert.Equal("You are not allocated to project PRJ-PORTAL.", exception.Message);
        Assert.Empty(repository.GetAll());
    }

    [Theory]
    [InlineData("", 12_500, "PRJ-PORTAL", "demo.requester", "Customer name is required.")]
    [InlineData("Portal work", 0, "PRJ-PORTAL", "demo.requester", "Amount must be greater than zero.")]
    [InlineData("Portal work", 12_500, "", "demo.requester", "Project is required.")]
    [InlineData("Portal work", 12_500, "PRJ-PORTAL", " ", "Signed-in user is required.")]
    public void CreateDraft_RejectsInvalidInputWithoutAddingInvoice(
        string customerName,
        decimal amount,
        string projectId,
        string createdBy,
        string expectedMessage)
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ArgumentException>(
            () => service.CreateDraft(customerName, amount, projectId, createdBy));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Empty(repository.GetAll());
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void CreateDraft_DeniesUnknownProjectWithoutAddingInvoice()
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.CreateDraft("Portal work", 12_500m, "PRJ-MISSING", "demo.requester"));

        Assert.Equal("Project PRJ-MISSING was not found.", exception.Message);
        Assert.Empty(repository.GetAll());
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void GetProjectOptionsFor_ReturnsOnlyProjectsWhereUserIsAllocated()
    {
        var service = ServiceFor(new FakeInvoiceRepository());

        var requesterProjects = service.GetProjectOptionsFor("demo.requester");
        var managerProjects = service.GetProjectOptionsFor("demo.manager");
        var adminProjects = service.GetProjectOptionsFor("demo.admin");

        Assert.Equal(
            new[] { "PRJ-PORTAL", "PRJ-MOBILE" },
            requesterProjects.Select(project => project.Id));
        Assert.Equal(new[] { "PRJ-PORTAL" }, managerProjects.Select(project => project.Id));
        Assert.Empty(adminProjects);
    }

    [Fact]
    public void GetInvoicesFor_UsesRequesterDmBusinessUnitHeadAndAdminScopes()
    {
        var repository = SeedInvoices();
        var service = ServiceFor(repository);

        Assert.Equal(
            new[] { "INV-PORTAL", "INV-MOBILE" },
            InvoiceIds(service.GetInvoicesFor("demo.requester")));
        Assert.Equal(
            new[] { "INV-PORTAL" },
            InvoiceIds(service.GetInvoicesFor("demo.manager")));
        Assert.Equal(
            new[] { "INV-PORTAL", "INV-MOBILE" },
            InvoiceIds(service.GetInvoicesFor("demo.director")));
        Assert.Equal(
            new[] { "INV-PORTAL", "INV-MOBILE", "INV-ERP" },
            InvoiceIds(service.GetInvoicesFor("demo.admin")));
    }

    [Theory]
    [InlineData("PRJ-MISSING")]
    [InlineData("PRJ-ORPHAN")]
    public void GetInvoicesFor_ExcludesInvoicesWithoutAProjectAccount(string projectId)
    {
        var directory = FakeOrgDirectory.CreateDemo()
            .WithProjectWithoutAccount("PRJ-ORPHAN");
        var repository = new FakeInvoiceRepository(
            InvoiceFor("INV-UNROUTED", projectId, InvoiceStatus.PendingApproval));
        var service = ServiceFor(repository, directory);

        Assert.Empty(service.GetInvoicesFor("unrelated.user"));
    }

    [Fact]
    public void GetInvoicesFor_DoesNotTreatAnEmptyAdminListAsAllowingEveryone()
    {
        var directory = FakeOrgDirectory.CreateDemo().WithoutAdmins();
        var service = ServiceFor(SeedInvoices(), directory);

        Assert.Empty(service.GetInvoicesFor("unrelated.user"));
    }

    [Fact]
    public void GetApprovalsFor_ReturnsPendingScopeAndInvoicesDecidedByCaller()
    {
        var repository = SeedInvoices(
            InvoiceFor("INV-PORTAL", "PRJ-PORTAL", InvoiceStatus.PendingApproval),
            InvoiceFor("INV-MOBILE", "PRJ-MOBILE", InvoiceStatus.PendingApproval),
            InvoiceFor("INV-ERP", "PRJ-ERP", InvoiceStatus.PendingApproval),
            InvoiceFor("INV-DECIDED", "PRJ-ERP", InvoiceStatus.Approved, decidedBy: "demo.manager"),
            InvoiceFor(
                "INV-FIRST-APPROVAL",
                "PRJ-ERP",
                InvoiceStatus.Approved,
                firstApprovedBy: "demo.manager"));
        var service = ServiceFor(repository);

        Assert.Equal(
            new[] { "INV-PORTAL", "INV-DECIDED", "INV-FIRST-APPROVAL" },
            InvoiceIds(service.GetApprovalsFor("demo.manager")));
        Assert.Equal(
            new[] { "INV-PORTAL", "INV-MOBILE" },
            InvoiceIds(service.GetApprovalsFor("demo.director")));
        Assert.Equal(
            new[] { "INV-PORTAL", "INV-MOBILE", "INV-ERP" },
            InvoiceIds(service.GetApprovalsFor("demo.admin")));
        Assert.Empty(service.GetApprovalsFor("demo.requester"));
    }

    [Fact]
    public void GetByIdFor_HidesInvoiceOutsideUsersInvoiceScope()
    {
        var service = ServiceFor(SeedInvoices());

        Assert.Null(service.GetByIdFor("INV-ERP", "demo.requester"));
        Assert.Equal("INV-ERP", service.GetByIdFor("INV-ERP", "demo.requester2")?.Id);
    }

    [Fact]
    public void GetWaitingOn_ReturnsCurrentRoutedApproverAndIsEmptyAfterDecision()
    {
        var service = ServiceFor(new FakeInvoiceRepository());
        var pending = InvoiceFor("INV-HIGH", "PRJ-PORTAL", InvoiceStatus.PendingApproval);
        pending.RoutedApprover1 = "demo.manager";
        pending.RoutedApprover2 = "demo.director";
        pending.FirstApprovedBy = "demo.manager";

        Assert.Equal("demo.director", service.GetWaitingOn(pending));

        pending.Status = InvoiceStatus.Approved;
        Assert.Null(service.GetWaitingOn(pending));
    }

    private static InvoiceAccessService ServiceFor(
        FakeInvoiceRepository repository,
        IOrgDirectory? directory = null) =>
        new(repository, directory ?? FakeOrgDirectory.CreateDemo());

    private static FakeInvoiceRepository SeedInvoices(params Invoice[] additionalInvoices)
    {
        var invoices = additionalInvoices.Length > 0
            ? additionalInvoices
            : new[]
        {
            InvoiceFor("INV-PORTAL", "PRJ-PORTAL", InvoiceStatus.Approved, "demo.requester"),
            InvoiceFor("INV-MOBILE", "PRJ-MOBILE", InvoiceStatus.Approved, "demo.requester"),
            InvoiceFor("INV-ERP", "PRJ-ERP", InvoiceStatus.Approved, "demo.requester2")
        };
        return new FakeInvoiceRepository(invoices);
    }

    private static Invoice InvoiceFor(
        string id,
        string projectId,
        InvoiceStatus status,
        string? submittedBy = null,
        string? decidedBy = null,
        string? firstApprovedBy = null) =>
        new()
        {
            Id = id,
            CustomerName = id,
            Amount = 1_000m,
            ProjectId = projectId,
            Status = status,
            SubmittedBy = submittedBy,
            DecidedBy = decidedBy,
            FirstApprovedBy = firstApprovedBy
        };

    private static string[] InvoiceIds(IReadOnlyList<Invoice> invoices) =>
        invoices.Select(invoice => invoice.Id).ToArray();
}
