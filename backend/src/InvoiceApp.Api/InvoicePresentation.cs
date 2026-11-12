using InvoiceApp.Domain;

namespace InvoiceApp.Api;

/// <summary>The signed-in person a list item is built for, in Domain terms.</summary>
public sealed record InvoiceViewer(string Username, ApproverRole? Role);

public sealed record InvoiceListItem(
    string Id,
    string CustomerName,
    decimal Amount,
    InvoiceStatus Status,
    string Project,
    string RequestedBy,
    string Approval1,
    string Approval2,
    string WaitingOn,
    string RejectionReason,
    string FirstApprovedBy,
    DateTime CreatedAt,
    bool CanSubmit,
    bool CanApprove,
    bool CanReject);

public static class InvoicePresentation
{
    public static InvoiceListItem ToListItem(
        Invoice invoice,
        IOrgDirectory orgDirectory,
        InvoiceAccessService accessService,
        ApprovalService approvalService,
        InvoiceViewer viewer)
    {
        var waitingOnUsername = accessService.GetWaitingOn(invoice);
        var rejectionReason = invoice.Status == InvoiceStatus.Rejected &&
                              !string.IsNullOrWhiteSpace(invoice.DecisionNote) &&
                              !string.IsNullOrWhiteSpace(invoice.DecidedBy)
            ? $"{FormatPerson(invoice.DecidedBy)}: {invoice.DecisionNote.Trim()}"
            : string.Empty;

        return new InvoiceListItem(
            invoice.Id,
            invoice.CustomerName,
            invoice.Amount,
            invoice.Status,
            orgDirectory.GetProject(invoice.ProjectId)?.Id ?? invoice.ProjectId,
            FormatPerson(invoice.SubmittedBy),
            FormatPerson(invoice.RoutedApprover1),
            invoice.Amount <= ApprovalService.DirectorApprovalLimit
                ? "Not applicable"
                : FormatPerson(invoice.RoutedApprover2),
            FormatPerson(waitingOnUsername),
            rejectionReason,
            FormatPerson(invoice.FirstApprovedBy),
            invoice.CreatedAt,
            approvalService.CanSubmit(invoice, viewer.Username),
            approvalService.CanApprove(invoice, viewer.Username, viewer.Role),
            approvalService.CanReject(invoice, viewer.Username));
    }

    public static string FormatPerson(string? username) =>
        string.IsNullOrWhiteSpace(username)
            ? string.Empty
            : $"{username.Trim()} ({username.Trim()}@example.com)";
}
