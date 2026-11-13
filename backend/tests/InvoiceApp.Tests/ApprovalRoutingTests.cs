using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalRoutingTests
{
    [Fact]
    public void NextApprover_RejectsNullInvoice()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ApprovalRouting.NextApprover(null!, FakeOrgDirectory.CreateDemo()));

        Assert.Equal("invoice", exception.ParamName);
    }

    [Fact]
    public void NextApprover_RejectsNullDirectory()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ApprovalRouting.NextApprover(PendingInvoice(30_000m), null!));

        Assert.Equal("orgDirectory", exception.ParamName);
    }

    [Theory]
    [InlineData(50_000.00, "demo.manager")]
    [InlineData(50_000.01, "demo.director")]
    [InlineData(500_000.00, "demo.director")]
    public void NextApprover_UsesExpectedSingleStepAtAmountBoundary(
        decimal amount,
        string expectedApprover)
    {
        var invoice = PendingInvoice(amount);

        var nextApprover = ApprovalRouting.NextApprover(invoice, FakeOrgDirectory.CreateDemo());

        Assert.Equal(expectedApprover, nextApprover);
    }

    [Fact]
    public void NextApprover_UsesDifferentApproversAboveDirectorLimit()
    {
        var invoice = PendingInvoice(500_000.01m);
        var directory = FakeOrgDirectory.CreateDemo();

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.manager", firstApprover);
        Assert.Equal("demo.director", secondApprover);
        Assert.NotEqual(firstApprover, secondApprover);
    }

    [Fact]
    public void NextApprover_UsesBusinessUnitHeadWhenProjectHasNoDeliveryManager()
    {
        var invoice = PendingInvoice(30_000m, "PRJ-ERP");

        var nextApprover = ApprovalRouting.NextApprover(invoice, FakeOrgDirectory.CreateDemo());

        Assert.Equal("demo.director2", nextApprover);
    }

    [Fact]
    public void NextApprover_UsesManager2ForMobileProject()
    {
        var invoice = PendingInvoice(50_000m, "PRJ-MOBILE");

        var nextApprover = ApprovalRouting.NextApprover(invoice, FakeOrgDirectory.CreateDemo());

        Assert.Equal("demo.manager2", nextApprover);
    }

    [Fact]
    public void NextApprover_UsesExplicitSubmitterInsteadOfInvoiceSubmitter()
    {
        var invoice = PendingInvoice(30_000m, submittedBy: "demo.manager");

        var nextApprover = ApprovalRouting.NextApprover(
            invoice,
            FakeOrgDirectory.CreateDemo(),
            submittedBy: "demo.requester");

        Assert.Equal("demo.manager", nextApprover);
    }

    [Fact]
    public void NextApprover_UsesExplicitPriorApproverOverInvoiceValue()
    {
        var invoice = PendingInvoice(600_000m);
        invoice.RoutedApprover1 = "demo.manager";
        invoice.RoutedApprover2 = "demo.director";
        invoice.FirstApprovedBy = string.Empty;

        var nextApprover = ApprovalRouting.NextApprover(
            invoice,
            FakeOrgDirectory.CreateDemo(),
            previouslyApprovedBy: "demo.manager");

        Assert.Equal("demo.director", nextApprover);
    }

    [Fact]
    public void NextApprover_SkipsCandidateAtRequestersRank()
    {
        var invoice = PendingInvoice(30_000m, submittedBy: "demo.admin");
        var directory = FakeOrgDirectory.CreateDemo()
            .WithAdmins("demo.admin", "demo.admin2");

        var nextApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Null(nextApprover);
    }

    [Fact]
    public void NextApprover_UsesAdminWhenProjectHasNoAccount()
    {
        var invoice = PendingInvoice(30_000m, "PRJ-ORPHAN");
        var directory = FakeOrgDirectory.CreateDemo()
            .WithProjectWithoutAccount("PRJ-ORPHAN");

        var nextApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.admin", nextApprover);
    }

    [Fact]
    public void NextApprover_ReturnsNullWhenProjectIsMissing()
    {
        var nextApprover = ApprovalRouting.NextApprover(
            PendingInvoice(30_000m, "PRJ-MISSING"),
            FakeOrgDirectory.CreateDemo());

        Assert.Null(nextApprover);
    }

    [Fact]
    public void NextApprover_DoesNotUseAdminForSecondHighValueApprovalWhenHeadIsUnavailable()
    {
        var invoice = PendingInvoice(600_000m);
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-DIGITAL", null);

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.manager", firstApprover);
        Assert.Null(secondApprover);
    }

    [Fact]
    public void NextApprover_DoesNotUseAdminAsSecondWhenAdminIsBusinessUnitHead()
    {
        var invoice = PendingInvoice(600_000m);
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-DIGITAL", "demo.admin");

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.manager", firstApprover);
        Assert.Null(secondApprover);
    }

    [Fact]
    public void NextApprover_UsesAdminAsHighValueFirstApproverWhenOtherCandidatesAreUnavailable()
    {
        var invoice = PendingInvoice(600_000m, "PRJ-ERP");
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-ENTERPRISE", null);

        var nextApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.admin", nextApprover);
    }

    [Fact]
    public void NextApprover_EscalatesPastRequesterWhoIsDeliveryManager()
    {
        var invoice = PendingInvoice(30_000m, submittedBy: "demo.manager");

        var nextApprover = ApprovalRouting.NextApprover(invoice, FakeOrgDirectory.CreateDemo());

        Assert.Equal("demo.director", nextApprover);
    }

    [Fact]
    public void NextApprover_ReservesDirectorForSecondStepWhenRequesterIsDeliveryManager()
    {
        var invoice = PendingInvoice(600_000m, submittedBy: "demo.manager");
        var directory = FakeOrgDirectory.CreateDemo();

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.admin", firstApprover);
        Assert.Equal("demo.director", secondApprover);
    }

    [Fact]
    public void NextApprover_UsesDirectorAsSecondWhenNoAdminsAreConfigured()
    {
        var invoice = PendingInvoice(600_000m);
        var directory = FakeOrgDirectory.CreateDemo().WithoutAdmins();

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.manager", firstApprover);
        Assert.Equal("demo.director", secondApprover);
    }

    [Fact]
    public void NextApprover_ReservesDirectorForSecondStepWhenProjectHasNoDeliveryManager()
    {
        var invoice = PendingInvoice(600_000m, "PRJ-ERP", submittedBy: "demo.requester2");
        var directory = FakeOrgDirectory.CreateDemo();

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.admin", firstApprover);
        Assert.Equal("demo.director2", secondApprover);
    }

    [Fact]
    public void Directory_DeliveryManagerCountsAsAllocatedToTheirProject()
    {
        var directory = FakeOrgDirectory.CreateDemo();

        Assert.True(directory.IsAllocatedToProject("PRJ-PORTAL", "demo.manager"));
        Assert.True(directory.IsAllocatedToProject("PRJ-MOBILE", "demo.manager2"));
        Assert.False(directory.IsAllocatedToProject("PRJ-PORTAL", "demo.manager2"));
        Assert.True(directory.IsAllocatedToProject("PRJ-ERP", "demo.requester2"));
    }

    private static Invoice PendingInvoice(
        decimal amount,
        string projectId = "PRJ-PORTAL",
        string? submittedBy = "demo.requester") =>
        new()
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval,
            ProjectId = projectId,
            SubmittedBy = submittedBy
        };
}
