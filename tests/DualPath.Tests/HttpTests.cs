using System.Net;
using System.Text.RegularExpressions;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace DualPath.Tests;
[Collection("platform")]
public sealed class HttpTests(PlatformFixture fixture)
{
    [Theory][InlineData("/")][InlineData("/programmes")][InlineData("/programme")][InlineData("/schools")][InlineData("/resources")][InlineData("/pricing")][InlineData("/about")][InlineData("/contact")][InlineData("/signin")][InlineData("/signup")][InlineData("/privacy")][InlineData("/terms")]
    public async Task PublicPagesRenderWithoutPrototypeShell(string path)
    {
        using var client=fixture.Client();var response=await client.GetAsync(path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);var html=await response.Content.ReadAsStringAsync();Assert.Contains("DualPath",html);Assert.DoesNotContain("DESIGN 01",html);Assert.DoesNotContain("data-form=",html);Assert.True(response.Headers.CacheControl?.NoStore);
    }
    [Theory][InlineData("/dashboard")][InlineData("/work")][InlineData("/reviews")][InlineData("/account")][InlineData("/admin")]
    public async Task AnonymousPrivateRequestsChallengeToSignIn(string path)
    {using var client=fixture.Client();var r=await client.GetAsync(path);Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Contains("/signin",r.Headers.Location?.ToString());}
    [Fact]public async Task PostWithoutAntiforgeryTokenIsRejected()
    {using var client=fixture.Client();var r=await client.PostAsync("/signin",new FormUrlEncodedContent(new Dictionary<string,string>{{"Command","login"},{"Input.Email","student@demo.dualpath.test"},{"Input.Password",PlatformFixture.Password}}));Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);}
    [Fact]public async Task StudentCannotAccessPlatformAdministration()
    {using var client=await fixture.Login();var r=await client.GetAsync("/admin");Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode);Assert.DoesNotContain("Platform operations</h1>",await r.Content.ReadAsStringAsync());}
    [Fact]public async Task SavedReflectionSurvivesLogoutAndNewSession()
    {
        var unique="A persisted reflection "+Guid.NewGuid();using(var client=await fixture.Login())
        {var r=await PlatformFixture.Post(client,"/reflections",new(){["Command"]="reflection",["Input.Body"]=unique});Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);await PlatformFixture.Post(client,"/account",new(){["Command"]="logout"});Assert.Equal(HttpStatusCode.Redirect,(await client.GetAsync("/reflections")).StatusCode);}
        using var next=await fixture.Login();Assert.Contains(unique,await next.GetStringAsync("/reflections"));
    }
    [Fact]public async Task UntrustedTextIsEncodedInSavedReflections()
    {
        using var client=await fixture.Login();await PlatformFixture.Post(client,"/reflections",new(){["Command"]="reflection",["Input.Body"]="<script>alert('unsafe')</script> A test of encoded learning content."});var html=await client.GetStringAsync("/reflections");Assert.DoesNotContain("<script>alert",html);Assert.Contains("&lt;script&gt;",html);
    }
    [Fact]public async Task CheckoutReturnParameterCannotActivateAPlan()
    {
        await using var scope=fixture.Scope();var user=await fixture.NewUser(scope.ServiceProvider);using var client=await fixture.Login(user.Email!);var response=await client.GetAsync("/billing?checkout=returned&plan=Guided&status=Active");Assert.Equal(HttpStatusCode.OK,response.StatusCode);var db=scope.ServiceProvider.GetRequiredService<AppDb>();Assert.False(await db.Subscriptions.AnyAsync(s=>s.UserId==user.Id&&s.Status=="Active"));
    }
    [Fact]public async Task LoginRejectsExternalReturnUrl()
    {
        using var client=fixture.Client();var r=await PlatformFixture.Post(client,"/signin",new(){["Command"]="login",["Input.Email"]="student@demo.dualpath.test",["Input.Password"]=PlatformFixture.Password,["Input.ReturnUrl"]="https://untrusted.example/"});Assert.Equal("/dashboard",r.Headers.Location?.ToString());
    }
    [Fact]public async Task EnquiryIsSavedAndVisibleOnlyToPlatformSupport()
    {
        var subject="Enquiry "+Guid.NewGuid();using var client=fixture.Client();var r=await PlatformFixture.Post(client,"/contact",new(){["Command"]="contact",["Input.Name"]="Integration visitor",["Input.Email"]="visitor@example.test",["Input.Title"]=subject,["Input.Body"]="We would like to discuss a student cohort.",["Input.Agree"]="true"});Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);using var admin=await fixture.Login("admin@demo.dualpath.test");Assert.Contains(subject,await admin.GetStringAsync("/support"));using var student=await fixture.Login();Assert.DoesNotContain(subject,await student.GetStringAsync("/support"));
    }
    [Fact]public async Task MalformedWebhookIsRejectedBeforeAnyEntitlementWrite()
    {using var c=fixture.Client();var r=await c.PostAsync("/webhooks/stripe",new StringContent("{}"));Assert.Equal(HttpStatusCode.ServiceUnavailable,r.StatusCode);}
    [Fact]public async Task AuthenticationActionsCannotMoveToUnthrottledRoutes()
    {using var client=fixture.Client();var response=await PlatformFixture.Post(client,"/contact",new(){["Command"]="login",["Input.Email"]="student@demo.dualpath.test",["Input.Password"]=PlatformFixture.Password});Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);}
    [Fact]public async Task SensitiveFormsAreRateLimited()
    {
        using var factory=fixture.Factory.WithWebHostBuilder(b=>b.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["RateLimits:FormRequestsPerMinute"]="1"})));
        using var client=factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions{AllowAutoRedirect=false});
        var first=await PlatformFixture.Post(client,"/reset",new(){["Command"]="reset",["Input.Email"]="unknown@example.test"});Assert.Equal(HttpStatusCode.Redirect,first.StatusCode);
        var second=await PlatformFixture.Post(client,"/reset",new(){["Command"]="reset",["Input.Email"]="unknown@example.test"});Assert.Equal((HttpStatusCode)429,second.StatusCode);
    }
    [Fact]public async Task SecurityHeadersAndPrivateCachePolicyArePresent()
    {using var client=await fixture.Login();var r=await client.GetAsync("/dashboard");Assert.Contains("frame-ancestors 'none'",r.Headers.GetValues("Content-Security-Policy").Single());Assert.Equal("nosniff",r.Headers.GetValues("X-Content-Type-Options").Single());Assert.True(r.Headers.CacheControl?.NoStore);}
}
