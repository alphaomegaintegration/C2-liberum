using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using static Microsoft.Playwright.Assertions;

namespace Liberum.PlaywrightTests.Tests;

[TestFixture]
public class LiberumSmokeTests : PageTest
{
    private const string BaseUrl = "http://localhost:5100";

    private async Task LoginAsAdmin()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.Locator("input[name='uid']").FillAsync("admin");
        await Page.Locator("input[name='password']").FillAsync("admin");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Logon" }).ClickAsync();
    }

    [Test]
    public async Task LoginPageLoads()
    {
        await Page.GotoAsync(BaseUrl);

        // Verify we were redirected to the login page
        await Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(".*/Logon.*")
        );

        // Verify page title
        await Expect(Page).ToHaveTitleAsync(
            new System.Text.RegularExpressions.Regex("Help Desk")
        );

        // Verify login controls
        await Expect(Page.Locator("input[name='uid']"))
            .ToBeVisibleAsync();

        await Expect(Page.Locator("input[name='password']"))
            .ToBeVisibleAsync();

        await Expect(
            Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Logon" }
            )
        ).ToBeVisibleAsync();

        // Verify registration link
        await Expect(
            Page.GetByRole(
                AriaRole.Link,
                new() { Name = "New User" }
            )
        ).ToBeVisibleAsync();
    }

    [Test]
    public async Task CanEnterLoginCredentials()
    {
        await Page.GotoAsync(BaseUrl);

        await Page.Locator("input[name='uid']")
            .FillAsync("testuser");

        await Page.Locator("input[name='password']")
            .FillAsync("testpassword");

        await Expect(Page.Locator("input[name='uid']"))
            .ToHaveValueAsync("testuser");

        await Expect(Page.Locator("input[name='password']"))
            .ToHaveValueAsync("testpassword");
    }

    [Test]
    public async Task UserCanSubmitANewProblemAndSeeItInProblemList()
    {
        var problemTitle = $"Playwright Problem {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        await LoginAsAdmin();

        await Page.GotoAsync($"{BaseUrl}/User/Problem/New");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/User/Problem/New$"));

        await Page.Locator("select[name='department']").SelectOptionAsync(new[] { new SelectOptionValue { Label = "Dept1" } });
        await Page.Locator("select[name='category']").SelectOptionAsync(new[] { new SelectOptionValue { Label = "General" } });
        await Page.Locator("select[name='priority']").SelectOptionAsync(new[] { new SelectOptionValue { Label = "HIGH" } });
        await Page.Locator("input[name='duedate']").FillAsync("2027-01-01");
        await Page.Locator("input[name='title']").FillAsync(problemTitle);
        await Page.Locator("textarea[name='description']").FillAsync("Created by Playwright target test.");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit Problem" }).ClickAsync();

        await Expect(Page.Locator("body")).ToContainTextAsync("Submitted");
        await Expect(Page.Locator("body")).ToContainTextAsync(problemTitle);

        await Page.GotoAsync($"{BaseUrl}/User/Problem/View");
        await Expect(Page.Locator("body")).ToContainTextAsync(problemTitle);
    }
}
