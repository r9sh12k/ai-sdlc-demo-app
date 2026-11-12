using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalRoutingTests
{
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
    public void NextApprover_EscalatesPastRequesterWhoIsDeliveryManager()
    {
        var invoice = PendingInvoice(30_000m, submittedBy: "demo.manager");

        var nextApprover = ApprovalRouting.NextApprover(invoice, FakeOrgDirectory.CreateDemo());

        Assert.Equal("demo.director", nextApprover);
    }

    [Fact]
    public void NextApprover_EscalatesManagerSubmitterToDirectorThenAdminAboveDirectorLimit()
    {
        var invoice = PendingInvoice(600_000m, submittedBy: "demo.manager");
        var directory = FakeOrgDirectory.CreateDemo();

        var firstApprover = ApprovalRouting.NextApprover(invoice, directory);
        invoice.FirstApprovedBy = firstApprover;
        var secondApprover = ApprovalRouting.NextApprover(invoice, directory);

        Assert.Equal("demo.director", firstApprover);
        Assert.Equal("demo.admin", secondApprover);
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
