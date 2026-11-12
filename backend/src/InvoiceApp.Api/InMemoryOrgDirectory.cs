using InvoiceApp.Api.Auth;
using InvoiceApp.Domain;
using Microsoft.Extensions.Options;

namespace InvoiceApp.Api;

public sealed class InMemoryOrgDirectory : IOrgDirectory
{
    private readonly IReadOnlyList<string> _adminUsernames;

    private static readonly IReadOnlyList<BusinessUnit> BusinessUnits = new[]
    {
        new BusinessUnit("BU-DIGITAL", "demo.director"),
        new BusinessUnit("BU-ENTERPRISE", "demo.director2")
    };

    private static readonly IReadOnlyList<Account> Accounts = new[]
    {
        new Account("ACC-ACME", "BU-DIGITAL"),
        new Account("ACC-GLOBEX", "BU-ENTERPRISE")
    };

    private static readonly IReadOnlyList<Project> Projects = new[]
    {
        new Project("PRJ-PORTAL", "ACC-ACME", "demo.manager"),
        new Project("PRJ-MOBILE", "ACC-ACME", "demo.manager2"),
        new Project("PRJ-ERP", "ACC-GLOBEX", null)
    };

    private static readonly IReadOnlyList<Allocation> Allocations = new[]
    {
        new Allocation("PRJ-PORTAL", "demo.requester", ProjectRole.Requester),
        new Allocation("PRJ-MOBILE", "demo.requester", ProjectRole.Requester),
        new Allocation("PRJ-ERP", "demo.requester2", ProjectRole.Requester)
    };

    public InMemoryOrgDirectory(IOptions<DemoUsersOptions> usersOptions)
    {
        _adminUsernames = usersOptions.Value.Users
            .Where(user => string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            .Select(user => user.Username)
            .ToArray();
    }

    public IReadOnlyList<BusinessUnit> GetBusinessUnits() => BusinessUnits;

    public BusinessUnit? GetBusinessUnit(string businessUnitId) =>
        BusinessUnits.FirstOrDefault(businessUnit =>
            string.Equals(
                businessUnit.Id,
                businessUnitId,
                StringComparison.OrdinalIgnoreCase));

    public Account? GetAccount(string accountId) =>
        Accounts.FirstOrDefault(account =>
            string.Equals(account.Id, accountId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Project> GetProjects() => Projects;

    public Project? GetProject(string projectId) =>
        Projects.FirstOrDefault(project =>
            string.Equals(project.Id, projectId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Allocation> GetAllocations(string projectId) =>
        Allocations.Where(allocation =>
                string.Equals(
                    allocation.ProjectId,
                    projectId,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public IReadOnlyList<string> GetAdminUsernames() => _adminUsernames;

    public bool IsAllocatedToProject(string projectId, string username)
    {
        var project = GetProject(projectId);
        return SameUser(project?.DeliveryManagerUsername, username) ||
               GetAllocations(projectId).Any(allocation =>
                   SameUser(allocation.Username, username));
    }

    private static bool SameUser(string? left, string right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
