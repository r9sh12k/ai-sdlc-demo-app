namespace InvoiceApp.Domain;

public enum ProjectRole
{
    Requester,
    Contributor
}

public sealed record BusinessUnit(string Id, string? HeadUsername);

public sealed record Account(string Id, string BusinessUnitId);

public sealed record Project(
    string Id,
    string AccountId,
    string? DeliveryManagerUsername);

public sealed record Allocation(
    string ProjectId,
    string Username,
    ProjectRole Role);

public interface IOrgDirectory
{
    IReadOnlyList<BusinessUnit> GetBusinessUnits();
    BusinessUnit? GetBusinessUnit(string businessUnitId);
    Account? GetAccount(string accountId);
    IReadOnlyList<Project> GetProjects();
    Project? GetProject(string projectId);
    IReadOnlyList<Allocation> GetAllocations(string projectId);
    IReadOnlyList<string> GetAdminUsernames();
    bool IsAllocatedToProject(string projectId, string username);
}
