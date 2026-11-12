using InvoiceApp.Domain;

namespace InvoiceApp.Api.Auth;

/// <summary>Maps a session's role string to the Domain approver role.</summary>
public static class SessionRoles
{
    /// <returns>The approver role, or <c>null</c> when the role cannot approve (for example Requester).</returns>
    public static ApproverRole? ToApproverRole(string role) => role switch
    {
        "Manager" => ApproverRole.Manager,
        "Director" => ApproverRole.Director,
        "Admin" => ApproverRole.Admin,
        _ => null
    };

    public static InvoiceViewer ToViewer(this UserSession session) =>
        new(session.Username, ToApproverRole(session.Role));
}
