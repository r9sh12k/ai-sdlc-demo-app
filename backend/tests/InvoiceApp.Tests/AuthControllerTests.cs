using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xunit;

namespace InvoiceApp.Tests;

public class AuthControllerTests
{
    [Theory]
    [InlineData("demo.manager")]
    [InlineData(" Demo.Manager ")]
    [InlineData("DEMO.MANAGER")]
    public void Login_MatchesUsernameIgnoringCaseAndSpacesAndUsesCanonicalName(string typedUsername)
    {
        var sessions = new SessionStore();
        var controller = CreateController(sessions);

        var result = controller.Login(new AuthController.LoginRequest(typedUsername, "ChangeMe123!"));

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var login = Assert.IsType<AuthController.LoginResponse>(response.Value);
        Assert.Equal("demo.manager", login.Username);
        Assert.Equal("Manager", login.Role);
        Assert.Equal("demo.manager", sessions.Get(login.Token)!.Username);
    }

    [Theory]
    [InlineData("demo.manager", "changeme123!")]
    [InlineData("demo.manager", "ChangeMe123! ")]
    [InlineData("demo.unknown", "ChangeMe123!")]
    public void Login_RejectsWrongPasswordOrUnknownUser(string username, string password)
    {
        var controller = CreateController(new SessionStore());

        var result = controller.Login(new AuthController.LoginRequest(username, password));

        var response = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal(
            "Invalid username or password.",
            response.Value!.GetType().GetProperty("error")!.GetValue(response.Value));
    }

    private static AuthController CreateController(SessionStore sessions) =>
        new(
            Options.Create(new DemoUsersOptions
            {
                Users =
                {
                    new DemoUser { Username = "demo.manager", Password = "ChangeMe123!", Role = "Manager" },
                    new DemoUser { Username = "demo.requester", Password = "ChangeMe123!", Role = "Requester" }
                }
            }),
            sessions);
}
