using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ApprovalServiceTests
{
    private const string PortalProjectId = "PRJ-PORTAL";

    [Fact]
    public void Submit_MovesDraftToPendingApprovalAndSnapshotsApprovers()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft, 500_000.01m);
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        service.Submit("INV-1", "demo.requester");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Equal("demo.requester", storedInvoice.SubmittedBy);
        Assert.Equal("demo.manager", storedInvoice.RoutedApprover1);
        Assert.Equal("demo.director", storedInvoice.RoutedApprover2);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Submit_RejectsInvoiceThatIsNotADraft()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Approved);
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Submit("INV-1", "demo.manager"));

        Assert.Equal("Invoice INV-1 is Approved; only drafts can be submitted.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Submit_RejectsHighValueInvoiceWhenNoDistinctSecondApproverExists()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft, 600_000m);
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-DIGITAL", "demo.manager")
            .WithoutAdmins();
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository, directory);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Submit("INV-1", "demo.requester"));

        Assert.Equal("No second approver available", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ManagerCanApproveAtSingleApproverLimit()
    {
        var repository = PendingRepository(50_000.00m);
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.manager", ApproverRole.Manager);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ManagerCannotApproveAboveSingleApproverLimitWhenDirectorIsRouted()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 50_000.01m,
            username: "demo.manager",
            role: ApproverRole.Manager,
            expectedReason: "This invoice is waiting for demo.director.");
    }

    [Fact]
    public void Approve_DirectorCanApproveAtSingleApproverLimit()
    {
        var repository = PendingRepository(500_000.00m);
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.director", ApproverRole.Director);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.director", storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.FirstApprovedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ManagerRecordsFirstApprovalForHighValueInvoice()
    {
        var repository = PendingRepository(500_000.01m);
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.manager", ApproverRole.Manager);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Equal("demo.manager", storedInvoice.FirstApprovedBy);
        Assert.Null(storedInvoice.DecidedBy);
        Assert.Null(storedInvoice.DecidedAt);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_DirectorCannotSkipTheFirstRoutedApprover()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 500_000.01m,
            username: "demo.director",
            role: ApproverRole.Director,
            expectedReason: "This invoice is waiting for demo.manager.");
    }

    [Fact]
    public void Approve_DifferentRoutedApproverCompletesHighValueApproval()
    {
        var invoice = PendingInvoice(500_000.01m);
        invoice.RoutedApprover1 = "demo.manager";
        invoice.RoutedApprover2 = "demo.director";
        invoice.FirstApprovedBy = "demo.manager";
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.director", ApproverRole.Director);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.manager", storedInvoice.FirstApprovedBy);
        Assert.Equal("demo.director", storedInvoice.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData("demo.manager")]
    [InlineData(" Demo.Manager ")]
    public void Approve_SameUserCannotCompleteHighValueApproval(string secondApprover)
    {
        var invoice = PendingInvoice(500_000.01m);
        invoice.RoutedApprover1 = "demo.manager";
        invoice.RoutedApprover2 = "demo.director";
        invoice.FirstApprovedBy = "demo.manager";
        AssertApprovalDeniedWithoutMutation(
            invoice,
            secondApprover,
            ApproverRole.Manager,
            "A different person must provide the second approval.");
    }

    [Fact]
    public void Approve_Manager2CannotApprovePortalInvoiceRoutedToManager()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 30_000m,
            username: "demo.manager2",
            role: ApproverRole.Manager,
            expectedReason: "This invoice is waiting for demo.manager.");
    }

    [Fact]
    public void Approve_AdminCanApproveAtDirectorLimitWhenRoutedButCannotStartHigherValueApproval()
    {
        var directory = FakeOrgDirectory.CreateDemo()
            .WithBusinessUnitHead("BU-DIGITAL", "demo.admin");
        var atLimitRepository = PendingRepository(500_000m, directory);
        var atLimitService = ServiceFor(atLimitRepository, directory);

        atLimitService.Approve("INV-1", "demo.admin", ApproverRole.Admin);

        Assert.Equal(InvoiceStatus.Approved, atLimitRepository.GetById("INV-1")!.Status);
        Assert.Equal(1, atLimitRepository.UpdateCount);

        var aboveLimit = PendingInvoice(500_000.01m);
        var aboveLimitRepository = PendingRepository(aboveLimit, directory);
        var aboveLimitService = ServiceFor(aboveLimitRepository, directory);
        var exception = Assert.Throws<ApprovalDeniedException>(
            () => aboveLimitService.Approve("INV-1", "demo.admin", ApproverRole.Admin));

        Assert.Equal("This invoice is waiting for demo.manager.", exception.Message);
        AssertInvoiceEqual(aboveLimit, aboveLimitRepository.GetById("INV-1")!);
        Assert.Equal(0, aboveLimitRepository.UpdateCount);
    }

    [Fact]
    public void Approve_AdminCanCompleteHighValueApprovalWhenRoutedAsDistinctSecondApprover()
    {
        var invoice = PendingInvoice(600_000m, submittedBy: "demo.manager");
        invoice.RoutedApprover1 = "demo.director";
        invoice.RoutedApprover2 = "demo.admin";
        invoice.FirstApprovedBy = "demo.director";
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.admin", ApproverRole.Admin);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Equal("demo.admin", storedInvoice.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData("demo.admin", "demo.admin", 10_000_000, ApproverRole.Admin)]
    [InlineData("demo.manager", " Demo.Manager ", 3_000_000, ApproverRole.Manager)]
    [InlineData("demo.director", "demo.director", 60_000_000, ApproverRole.Director)]
    public void Approve_SubmitterCannotApproveTheirOwnInvoice(
        string submittedBy,
        string approver,
        int amountInCents,
        ApproverRole role)
    {
        var amount = amountInCents / 100m;
        var invoice = PendingInvoice(amount, submittedBy: submittedBy);
        var repository = PendingRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Approve("INV-1", approver, role));

        Assert.Equal("You submitted this invoice, so someone else must approve it.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Approve_SubmitterCannotProvideTheSecondApproval()
    {
        var invoice = PendingInvoice(600_000m, submittedBy: "demo.director2");
        invoice.RoutedApprover1 = "demo.manager";
        invoice.RoutedApprover2 = "demo.director";
        invoice.FirstApprovedBy = "demo.manager";
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Approve("INV-1", "demo.director2", ApproverRole.Director));

        Assert.Equal("You submitted this invoice, so someone else must approve it.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Approve_InvoiceWithoutSubmitterCanBeApprovedByRoutedUser()
    {
        var repository = PendingRepository(PendingInvoice(30_000m, submittedBy: null));
        var service = ServiceFor(repository);

        service.Approve("INV-1", "demo.manager", ApproverRole.Manager);

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Approved, storedInvoice.Status);
        Assert.Null(storedInvoice.SubmittedBy);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_RejectsUnsupportedRoleWithoutMutation()
    {
        AssertApprovalDeniedWithoutMutation(
            amount: 1_000m,
            username: "demo.manager",
            role: (ApproverRole)999,
            expectedReason: "Only Managers, Directors, and Admins may approve invoices.");
    }

    [Fact]
    public void Approve_RejectsInvoiceThatIsStillADraft()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Draft);
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Approve("INV-1", "demo.manager", ApproverRole.Manager));

        Assert.Equal("Invoice INV-1 is Draft; only pending invoices can be decided.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Approve_ThrowsWhenInvoiceDoesNotExist()
    {
        var repository = new FakeInvoiceRepository();
        var service = ServiceFor(repository);

        var exception = Assert.Throws<KeyNotFoundException>(
            () => service.Approve("INV-404", "demo.manager", ApproverRole.Manager));

        Assert.Equal("Invoice INV-404 was not found.", exception.Message);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RejectsInvoiceThatIsNotPending()
    {
        var invoice = InvoiceWith("INV-1", InvoiceStatus.Approved);
        var repository = new FakeInvoiceRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Reject("INV-1", "demo.manager", "Duplicate invoice"));

        Assert.Equal("Invoice INV-1 is Approved; only pending invoices can be decided.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RecordsReasonAndRoutedApprover()
    {
        var repository = PendingRepository(30_000m);
        var service = ServiceFor(repository);

        service.Reject("INV-1", "demo.manager", "Duplicate invoice");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.Rejected, storedInvoice.Status);
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.NotNull(storedInvoice.DecidedAt);
        Assert.Equal("Duplicate invoice", storedInvoice.DecisionNote);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Reject_NonRoutedUserIsDeniedWithoutMutation()
    {
        var invoice = PendingInvoice(30_000m);
        var repository = PendingRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Reject("INV-1", "demo.manager2", "Duplicate invoice"));

        Assert.Equal("This invoice is waiting for demo.manager.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Reject_RequiresAReasonAndLeavesInvoiceUnchanged()
    {
        var invoice = PendingInvoice(30_000m);
        var repository = PendingRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Reject("INV-1", "demo.manager", "  "));

        Assert.Equal("A reason is required to reject an invoice.", exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Reject_AdminMayRejectAnyPendingInvoice()
    {
        var repository = PendingRepository(30_000m);
        var service = ServiceFor(repository);

        service.Reject("INV-1", "demo.admin", "Duplicate invoice");

        Assert.Equal(InvoiceStatus.Rejected, repository.GetById("INV-1")!.Status);
        Assert.Equal("demo.admin", repository.GetById("INV-1")!.DecidedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Theory]
    [InlineData("demo.manager")]
    [InlineData("demo.requester2")]
    [InlineData("demo.admin")]
    public void Submit_NonRequesterIsDeniedWithoutMutation(string otherUser)
    {
        var draft = DraftOwnedBy("demo.requester");
        var repository = new FakeInvoiceRepository(draft);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Submit("INV-1", otherUser));

        Assert.Equal("Only the requester who created this draft can submit it.", exception.Message);
        AssertInvoiceEqual(draft, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    [Fact]
    public void Submit_RequesterMatchesIgnoringCaseAndSpaces()
    {
        var repository = new FakeInvoiceRepository(DraftOwnedBy("demo.requester"));
        var service = ServiceFor(repository);

        service.Submit("INV-1", " Demo.Requester ");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal(InvoiceStatus.PendingApproval, storedInvoice.Status);
        Assert.Equal("demo.requester", storedInvoice.SubmittedBy);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Approve_StoresTrimmedDecider()
    {
        var repository = PendingRepository(30_000m);
        var service = ServiceFor(repository);

        service.Approve("INV-1", " demo.manager ", ApproverRole.Manager);

        Assert.Equal("demo.manager", repository.GetById("INV-1")!.DecidedBy);
    }

    [Fact]
    public void Reject_StoresTrimmedDeciderAndReason()
    {
        var repository = PendingRepository(30_000m);
        var service = ServiceFor(repository);

        service.Reject("INV-1", " demo.manager ", "  Duplicate invoice.  ");

        var storedInvoice = repository.GetById("INV-1")!;
        Assert.Equal("demo.manager", storedInvoice.DecidedBy);
        Assert.Equal("Duplicate invoice.", storedInvoice.DecisionNote);
    }

    [Theory]
    [InlineData("demo.requester", true)]
    [InlineData(" DEMO.REQUESTER ", true)]
    [InlineData("demo.manager", false)]
    [InlineData("demo.admin", false)]
    public void CanSubmit_OnlyTheDraftRequester(string username, bool expected)
    {
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.Equal(expected, service.CanSubmit(DraftOwnedBy("demo.requester"), username));
    }

    [Fact]
    public void CanSubmit_FalseOnceSubmitted()
    {
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.False(service.CanSubmit(PendingInvoice(30_000m), "demo.requester"));
    }

    [Theory]
    [InlineData("demo.manager", ApproverRole.Manager, true)]
    [InlineData("demo.manager2", ApproverRole.Manager, false)]
    [InlineData("demo.director", ApproverRole.Director, false)]
    [InlineData("demo.admin", ApproverRole.Admin, false)]
    public void CanApprove_OnlyTheCurrentRoutedApprover(
        string username,
        ApproverRole role,
        bool expected)
    {
        var invoice = RoutedPendingInvoice(30_000m);
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.Equal(expected, service.CanApprove(invoice, username, role));
    }

    [Fact]
    public void CanApprove_FalseWithoutAnApproverRole()
    {
        var invoice = RoutedPendingInvoice(30_000m);
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.False(service.CanApprove(invoice, "demo.manager", role: null));
    }

    [Fact]
    public void CanApprove_FalseForTheSubmitterEvenWhenRouted()
    {
        var invoice = RoutedPendingInvoice(30_000m);
        invoice.SubmittedBy = "demo.manager";
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.False(service.CanApprove(invoice, "demo.manager", ApproverRole.Manager));
    }

    [Fact]
    public void CanApprove_HighValueMovesToTheSecondApproverAfterTheFirst()
    {
        var invoice = RoutedPendingInvoice(600_000m);
        invoice.FirstApprovedBy = "demo.manager";
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.False(service.CanApprove(invoice, "demo.manager", ApproverRole.Manager));
        Assert.True(service.CanApprove(invoice, "demo.director", ApproverRole.Director));
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Approved)]
    [InlineData(InvoiceStatus.Rejected)]
    public void CanApproveAndCanReject_FalseWhenNotPending(InvoiceStatus status)
    {
        var invoice = RoutedPendingInvoice(30_000m);
        invoice.Status = status;
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.False(service.CanApprove(invoice, "demo.manager", ApproverRole.Manager));
        Assert.False(service.CanReject(invoice, "demo.manager"));
        Assert.False(service.CanReject(invoice, "demo.admin"));
    }

    [Theory]
    [InlineData("demo.manager", true)]
    [InlineData("demo.admin", true)]
    [InlineData("demo.manager2", false)]
    [InlineData("demo.director", false)]
    [InlineData("demo.requester", false)]
    public void CanReject_RoutedApproverOrAdmin(string username, bool expected)
    {
        var invoice = RoutedPendingInvoice(30_000m);
        var service = ServiceFor(new FakeInvoiceRepository());

        Assert.Equal(expected, service.CanReject(invoice, username));
    }

    private static Invoice DraftOwnedBy(string requester)
    {
        var draft = InvoiceWith("INV-1", InvoiceStatus.Draft, 30_000m);
        draft.SubmittedBy = requester;
        draft.DecidedBy = null;
        draft.DecidedAt = null;
        draft.DecisionNote = null;
        return draft;
    }

    private static Invoice RoutedPendingInvoice(decimal amount)
    {
        var invoice = PendingInvoice(amount);
        invoice.RoutedApprover1 = "demo.manager";
        invoice.RoutedApprover2 = amount > ApprovalService.DirectorApprovalLimit
            ? "demo.director"
            : null;
        return invoice;
    }

    private static FakeInvoiceRepository PendingRepository(
        decimal amount,
        IOrgDirectory? directory = null) =>
        PendingRepository(PendingInvoice(amount), directory);

    private static FakeInvoiceRepository PendingRepository(
        Invoice invoice,
        IOrgDirectory? directory = null)
    {
        var orgDirectory = directory ?? FakeOrgDirectory.CreateDemo();
        if (string.IsNullOrWhiteSpace(invoice.RoutedApprover1))
        {
            var firstApprover = ApprovalRouting.NextApprover(
                invoice,
                orgDirectory,
                previouslyApprovedBy: null);
            if (!string.IsNullOrWhiteSpace(firstApprover))
            {
                invoice.RoutedApprover1 = firstApprover;
                if (invoice.Amount > ApprovalService.DirectorApprovalLimit)
                {
                    invoice.RoutedApprover2 = ApprovalRouting.NextApprover(
                        invoice,
                        orgDirectory,
                        previouslyApprovedBy: firstApprover);
                }
            }
        }

        return new FakeInvoiceRepository(invoice);
    }

    private static ApprovalService ServiceFor(
        FakeInvoiceRepository repository,
        IOrgDirectory? directory = null) =>
        new(repository, directory ?? FakeOrgDirectory.CreateDemo());

    private static void AssertApprovalDeniedWithoutMutation(
        decimal amount,
        string username,
        ApproverRole role,
        string expectedReason)
    {
        var invoice = PendingInvoice(amount);
        AssertApprovalDeniedWithoutMutation(invoice, username, role, expectedReason);
    }

    private static void AssertApprovalDeniedWithoutMutation(
        Invoice invoice,
        string username,
        ApproverRole role,
        string expectedReason)
    {
        var repository = PendingRepository(invoice);
        var service = ServiceFor(repository);

        var exception = Assert.Throws<ApprovalDeniedException>(
            () => service.Approve("INV-1", username, role));

        Assert.Equal(expectedReason, exception.Message);
        AssertInvoiceEqual(invoice, repository.GetById("INV-1")!);
        Assert.Equal(0, repository.UpdateCount);
    }

    private static Invoice InvoiceWith(
        string id,
        InvoiceStatus status,
        decimal amount = 100m) =>
        new()
        {
            Id = id,
            CustomerName = "Test School",
            Amount = amount,
            Status = status,
            ProjectId = PortalProjectId,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DecidedBy = "previous.approver",
            DecidedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            DecisionNote = "previous decision"
        };

    private static Invoice PendingInvoice(
        decimal amount,
        string? submittedBy = "demo.requester") =>
        new()
        {
            Id = "INV-1",
            CustomerName = "Test School",
            Amount = amount,
            Status = InvoiceStatus.PendingApproval,
            ProjectId = PortalProjectId,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SubmittedBy = submittedBy
        };

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
}
