using System.Diagnostics.CodeAnalysis;

namespace InvoiceApp.Domain;

public static class ApprovalRouting
{
    public static string? NextApprover(
        Invoice invoice,
        IOrgDirectory orgDirectory,
        string? submittedBy = null,
        string? previouslyApprovedBy = null)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(orgDirectory);

        var priorApprover = previouslyApprovedBy ?? invoice.FirstApprovedBy;
        var hasPriorApproval = !string.IsNullOrWhiteSpace(priorApprover);

        if (!string.IsNullOrWhiteSpace(invoice.RoutedApprover1))
        {
            return hasPriorApproval ? invoice.RoutedApprover2 : invoice.RoutedApprover1;
        }

        var route = ResolveRoute(invoice, orgDirectory, submittedBy ?? invoice.SubmittedBy);
        return hasPriorApproval ? route.SecondApprover : route.FirstApprover;
    }

    internal static bool SameUser(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static ApprovalRoute ResolveRoute(
        Invoice invoice,
        IOrgDirectory orgDirectory,
        string? submittedBy)
    {
        var project = orgDirectory.GetProject(invoice.ProjectId);
        if (project is null)
        {
            return new ApprovalRoute(null, null);
        }

        var account = orgDirectory.GetAccount(project.AccountId);
        var businessUnit = account is null
            ? null
            : orgDirectory.GetBusinessUnit(account.BusinessUnitId);
        var admins = orgDirectory.GetAdminUsernames()
            .Where(username => !string.IsNullOrWhiteSpace(username))
            .Select(username => username.Trim())
            .ToArray();
        var requesterRank = Rank(submittedBy, project, businessUnit, admins);
        var headUsername = businessUnit?.HeadUsername;

        if (invoice.Amount <= ApprovalService.ManagerApprovalLimit)
        {
            var first = FirstAboveRequester(
                new[] { project.DeliveryManagerUsername, headUsername }.Concat(admins),
                submittedBy,
                requesterRank,
                project,
                businessUnit,
                admins);
            return new ApprovalRoute(first, null);
        }

        if (invoice.Amount <= ApprovalService.DirectorApprovalLimit)
        {
            var first = FirstAboveRequester(
                new[] { headUsername }.Concat(admins),
                submittedBy,
                requesterRank,
                project,
                businessUnit,
                admins);
            return new ApprovalRoute(first, null);
        }

        // Above the Director limit the second step must be a distinct Director (the Business
        // Unit head, never an Admin). Reserve that person first so the first step escalates
        // past them, for example to Admin when the project has no DM.
        var secondApprover = FirstAboveRequester(
            new[] { headUsername }.Where(
                username => !admins.Any(admin => SameUser(admin, username))),
            submittedBy,
            requesterRank,
            project,
            businessUnit,
            admins);
        var firstCandidates =
            new[] { project.DeliveryManagerUsername, headUsername }.Concat(admins).ToArray();
        var firstApprover = FirstAboveRequester(
                firstCandidates,
                submittedBy,
                requesterRank,
                project,
                businessUnit,
                admins,
                secondApprover)
            ?? FirstAboveRequester(
                firstCandidates,
                submittedBy,
                requesterRank,
                project,
                businessUnit,
                admins);
        return new ApprovalRoute(
            firstApprover,
            SameUser(firstApprover, secondApprover) ? null : secondApprover);
    }

    private static string? FirstAboveRequester(
        IEnumerable<string?> candidates,
        string? submittedBy,
        int requesterRank,
        Project project,
        BusinessUnit? businessUnit,
        IReadOnlyList<string> admins,
        string? excludedUsername = null)
    {
        foreach (var candidate in candidates)
        {
            if (IsBlankOrSameUser(candidate, submittedBy) ||
                SameUser(candidate, excludedUsername) ||
                Rank(candidate, project, businessUnit, admins) <= requesterRank)
            {
                continue;
            }

            return candidate.Trim();
        }

        return null;
    }

    // Stryker disable once Logical : Blank candidates have rank zero and fail the following rank check.
    private static bool IsBlankOrSameUser(
        [NotNullWhen(false)] string? candidate,
        string? username) =>
        string.IsNullOrWhiteSpace(candidate) || SameUser(candidate, username);

    private static int Rank(
        string? username,
        Project project,
        BusinessUnit? businessUnit,
        IReadOnlyList<string> admins)
    {
        // Stryker disable once Block : Blank usernames have rank zero in the final fallback too.
        if (string.IsNullOrWhiteSpace(username))
        {
            return 0;
        }

        if (admins.Any(admin => SameUser(admin, username)))
        {
            return 3;
        }

        if (SameUser(businessUnit?.HeadUsername, username))
        {
            return 2;
        }

        return SameUser(project.DeliveryManagerUsername, username) ? 1 : 0;
    }

    private sealed record ApprovalRoute(string? FirstApprover, string? SecondApprover);
}
