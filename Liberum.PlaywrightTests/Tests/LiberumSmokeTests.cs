using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using static Microsoft.Playwright.Assertions;

namespace Liberum.PlaywrightTests.Tests;

[TestFixture]
public class LiberumSmokeTests : PageTest
{
    [Test]
    public async Task LoginPageLoads()
    {
        await Page.GotoAsync("http://localhost:5100");

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
    public async Task CanEnterLoginCredentials()
    {
        await Page.GotoAsync("http://localhost:5100");

        await Page.Locator("input[name='uid']")
            .FillAsync("testuser");

        await Page.Locator("input[name='password']")
            .FillAsync("testpassword");

        await Expect(Page.Locator("input[name='uid']"))
            .ToHaveValueAsync("testuser");

        await Expect(Page.Locator("input[name='password']"))
            .ToHaveValueAsync("testpassword");
    }
}
