using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private static Invoice InvoiceWith(string id, InvoiceStatus status, decimal amount = 100m) =>
        new()
        {
            Id = id,
            CustomerName = "Test School",
            Amount = amount,
            Status = status,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DecidedBy = "previous.approver",
            DecidedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DecisionNote = "previous decision"
        };

    private static ApprovalService ServiceFor(params Invoice[] invoices) =>
        new(new FakeInvoiceRepository(invoices));

    [Fact]
    public void Submit_MovesDraftToPendingApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Submit("INV-1");

        Assert.Equal(InvoiceStatus.PendingApproval, repository.GetById("INV-1")!.Status);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Approved));

        Assert.Throws<InvalidOperationException>(() => service.Submit("INV-1"));
    }

    [Fact]
    public void Approve_ManagerCanApproveAtSingleApproverLimit()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 50_000.00m));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.manager", ApproverRole.Manager);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ManagerCannotApproveAboveSingleApproverLimit()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 50_000.01m,
            role: ApproverRole.Manager,
            username: "demo.manager",
            expectedReason: "Managers may approve invoices up to 50,000.00.");
    }

    [Fact]
    public void Approve_DirectorCanApproveAtSingleApproverLimit()
    {
        var repository = new FakeInvoiceRepository(
            InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000.00m));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.director", ApproverRole.Director);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.director", storedInvoice.DecidedBy);
        Assert.Null(GetFirstApprovedBy(storedInvoice));
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ManagerRecordsFirstApprovalForHighValueInvoice()
    {
        var repository = new FakeInvoiceRepository(PendingInvoice("INV-1", 500_000.01m));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.manager", ApproverRole.Manager);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Equal("demo.manager", GetFirstApprovedBy(storedInvoice));
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_DirectorRecordsFirstApprovalForHighValueInvoice()
    {
        var repository = new FakeInvoiceRepository(PendingInvoice("INV-1", 500_000.01m));
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.director", ApproverRole.Director);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Equal("demo.director", GetFirstApprovedBy(storedInvoice));
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_DifferentDirectorCompletesHighValueApproval()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000.01m);
        SetFirstApprovedBy(invoice, "demo.manager");
        var repository = new FakeInvoiceRepository(invoice);
        var service = new ApprovalService(repository);

        service.Approve("INV-1", "demo.director", ApproverRole.Director);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.manager", GetFirstApprovedBy(storedInvoice));
        Assert.Equal("demo.director", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_SameDirectorCannotCompleteHighValueApproval()
    {
        AssertSecondApprovalDeniedWithoutMutation(
            firstApprover: "demo.director",
            secondApprover: "demo.director",
            role: ApproverRole.Director,
            expectedReason: "A different Director must provide the second approval.");
    }

    [Fact]
    public void Approve_SameDirectorCannotCompleteHighValueApprovalWithDifferentCasingAndWhitespace()
    {
        AssertSecondApprovalDeniedWithoutMutation(
            firstApprover: "demo.director",
            secondApprover: " Demo.Director ",
            role: ApproverRole.Director,
            expectedReason: "A different Director must provide the second approval.");
    }

    [Fact]
    public void Approve_ManagerCannotCompleteHighValueApproval()
    {
        AssertSecondApprovalDeniedWithoutMutation(
            firstApprover: "demo.director",
            secondApprover: "demo.manager",
            role: ApproverRole.Manager,
            expectedReason: "A Director must provide the second approval.");
    }

    [Fact]
    public void Approve_AdminCannotApprove()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 1_000m,
            role: ApproverRole.Admin,
            username: "demo.admin",
            expectedReason: "Only Managers and Directors may approve invoices.");
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.Draft));

        Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", ApproverRole.Manager));
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var service = ServiceFor();

        Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-404", "demo.manager", ApproverRole.Manager));
    }

    [Fact]
    public void Reject_RecordsReasonAndApprover()
    {
        var repository = new FakeInvoiceRepository(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));
        var service = new ApprovalService(repository);

        service.Reject("INV-1", "demo.approver", "Duplicate invoice");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Rejected, storedInvoice.Status);
        Assert.Equal("demo.approver", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal("Duplicate invoice", storedInvoice.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RequiresAReason()
    {
        var service = ServiceFor(InvoiceWith("INV-1", InvoiceStatus.PendingApproval));

        var exception = Assert.Throws<ArgumentException>(
            () => service.Reject("INV-1", "demo.approver", "  "));

        Assert.Equal("A reason is required to reject an invoice.", exception.Message);
    }

    private static void AssertApprovalDeniedWithoutMutation(
        decimal amount,
        ApproverRole role,
        string username,
        string expectedReason)
    {
        var originalInvoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, amount);
        var repository = new FakeInvoiceRepository(originalInvoice);
        var service = new ApprovalService(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Approve("INV-1", username, role));

        Assert.Equal(expectedReason, exception.Message);
        AssertInvoiceEqual(originalInvoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    private static void AssertSecondApprovalDeniedWithoutMutation(
        string firstApprover,
        string secondApprover,
        ApproverRole role,
        string expectedReason)
    {
        var originalInvoice = InvoiceWith("INV-1", InvoiceStatus.PendingApproval, 500_000.01m);
        SetFirstApprovedBy(originalInvoice, firstApprover);
        var repository = new FakeInvoiceRepository(originalInvoice);
        var service = new ApprovalService(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Approve("INV-1", secondApprover, role));

        Assert.Equal(expectedReason, exception.Message);
        AssertInvoiceEqual(originalInvoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    private static Invoice PendingInvoice(string id, decimal amount) =>
        new()
        {
            Id = id,
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private static void AssertInvoiceEqual(Invoice expected, Invoice actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.CustomerName, actual.CustomerName);
        Assert.Equal(expected.Amount, actual.Amount);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.FirstApprovedBy, actual.FirstApprovedBy);
        Assert.Equal(expected.DecidedBy, actual.DecidedBy);
        Assert.Equal(expected.DecidedAt, actual.DecidedAt);
        Assert.Equal(expected.DecisionNote, actual.DecisionNote);
    }

    private static string? GetFirstApprovedBy(Invoice invoice) => invoice.FirstApprovedBy;

    private static void SetFirstApprovedBy(Invoice invoice, string? username) =>
        invoice.FirstApprovedBy = username;
}
