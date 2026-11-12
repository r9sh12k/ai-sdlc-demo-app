using InvoiceApp.Domain;

namespace InvoiceApp.Tests;

internal sealed class FakeOrgDirectory : IOrgDirectory
{
    private readonly IReadOnlyList<BusinessUnit> _businessUnits;
    private readonly IReadOnlyList<Account> _accounts;
    private readonly IReadOnlyList<Project> _projects;
    private readonly IReadOnlyList<Allocation> _allocations;
    private readonly IReadOnlyList<string> _adminUsernames;

    private FakeOrgDirectory(
        IReadOnlyList<BusinessUnit> businessUnits,
        IReadOnlyList<Account> accounts,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<string> adminUsernames)
    {
        _businessUnits = businessUnits;
        _accounts = accounts;
        _projects = projects;
        _allocations = allocations;
        _adminUsernames = adminUsernames;
    }

    public static FakeOrgDirectory CreateDemo() => new(
        new[]
        {
            new BusinessUnit("BU-DIGITAL", "demo.director"),
            new BusinessUnit("BU-ENTERPRISE", "demo.director2")
        },
        new[]
        {
            new Account("ACC-ACME", "BU-DIGITAL"),
            new Account("ACC-GLOBEX", "BU-ENTERPRISE")
        },
        new[]
        {
            new Project("PRJ-PORTAL", "ACC-ACME", "demo.manager"),
            new Project("PRJ-MOBILE", "ACC-ACME", "demo.manager2"),
            new Project("PRJ-ERP", "ACC-GLOBEX", null)
        },
        new[]
        {
            new Allocation("PRJ-PORTAL", "demo.requester", ProjectRole.Requester),
            new Allocation("PRJ-MOBILE", "demo.requester", ProjectRole.Requester),
            new Allocation("PRJ-ERP", "demo.requester2", ProjectRole.Requester)
        },
        new[] { "demo.admin" });

    public FakeOrgDirectory WithBusinessUnitHead(string businessUnitId, string? headUsername) =>
        new(
            _businessUnits
                .Select(unit => string.Equals(unit.Id, businessUnitId, StringComparison.OrdinalIgnoreCase)
                    ? unit with { HeadUsername = headUsername }
                    : unit)
                .ToArray(),
            _accounts,
            _projects,
            _allocations,
            _adminUsernames);

    public FakeOrgDirectory WithoutAdmins() =>
        new(_businessUnits, _accounts, _projects, _allocations, Array.Empty<string>());

    public FakeOrgDirectory WithAdmins(params string[] usernames) =>
        new(_businessUnits, _accounts, _projects, _allocations, usernames);

    public FakeOrgDirectory WithProjectDeliveryManager(string projectId, string? username) =>
        new(
            _businessUnits,
            _accounts,
            _projects
                .Select(project => string.Equals(project.Id, projectId, StringComparison.OrdinalIgnoreCase)
                    ? project with { DeliveryManagerUsername = username }
                    : project)
                .ToArray(),
            _allocations,
            _adminUsernames);

    public FakeOrgDirectory WithProjectWithoutAccount(
        string projectId,
        string? deliveryManagerUsername = null) =>
        new(
            _businessUnits,
            _accounts,
            _projects.Append(new Project(projectId, "MISSING-ACCOUNT", deliveryManagerUsername)).ToArray(),
            _allocations,
            _adminUsernames);

    public IReadOnlyList<BusinessUnit> GetBusinessUnits() => _businessUnits;

    public BusinessUnit? GetBusinessUnit(string businessUnitId) =>
        _businessUnits.FirstOrDefault(unit =>
            string.Equals(unit.Id, businessUnitId, StringComparison.OrdinalIgnoreCase));

    public Account? GetAccount(string accountId) =>
        _accounts.FirstOrDefault(account =>
            string.Equals(account.Id, accountId, StringComparison.OrdinalIgnoreCase));

    public Project? GetProject(string projectId) =>
        _projects.FirstOrDefault(project =>
            string.Equals(project.Id, projectId, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<Project> GetProjects() => _projects;

    public IReadOnlyList<Allocation> GetAllocations(string projectId) =>
        _allocations
            .Where(allocation =>
                string.Equals(allocation.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public IReadOnlyList<string> GetAdminUsernames() => _adminUsernames;

    public bool IsAllocatedToProject(string projectId, string username)
    {
        var project = GetProject(projectId);
        return (project is not null &&
                SameUser(project.DeliveryManagerUsername, username)) ||
            GetAllocations(projectId).Any(allocation => SameUser(allocation.Username, username));
    }

    private static bool SameUser(string? left, string right) =>
        !string.IsNullOrWhiteSpace(left) &&
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
