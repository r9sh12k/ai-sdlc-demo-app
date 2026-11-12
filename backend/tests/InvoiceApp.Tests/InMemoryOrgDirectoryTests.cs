using InvoiceApp.Api;
using InvoiceApp.Api.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace InvoiceApp.Tests;

public class InMemoryOrgDirectoryTests
{
    [Fact]
    public void DemoDirectoryContainsRequestedHierarchyAndAllocations()
    {
        var directory = CreateDirectory();

        Assert.Equal("demo.director", directory.GetBusinessUnit("BU-DIGITAL")?.HeadUsername);
        Assert.Equal("BU-DIGITAL", directory.GetAccount("ACC-ACME")?.BusinessUnitId);
        Assert.Equal("ACC-ACME", directory.GetProject("PRJ-PORTAL")?.AccountId);
        Assert.Equal("demo.manager", directory.GetProject("PRJ-PORTAL")?.DeliveryManagerUsername);
        Assert.Null(directory.GetProject("PRJ-ERP")?.DeliveryManagerUsername);
        Assert.True(directory.IsAllocatedToProject("PRJ-PORTAL", "demo.requester"));
        Assert.True(directory.IsAllocatedToProject("PRJ-MOBILE", "demo.manager2"));
        Assert.True(directory.IsAllocatedToProject("PRJ-ERP", "demo.requester2"));
        Assert.Equal(new[] { "demo.admin" }, directory.GetAdminUsernames());
    }

    private static InMemoryOrgDirectory CreateDirectory() =>
        new(Options.Create(new DemoUsersOptions
        {
            Users = new List<DemoUser>
            {
                new() { Username = "demo.admin", Role = "Admin" }
            }
        }));
}
