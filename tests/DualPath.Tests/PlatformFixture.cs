using System.Net;
using System.Text.RegularExpressions;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

[assembly:CollectionBehavior(DisableTestParallelization=true)]
namespace DualPath.Tests;
public sealed class PlatformFixture:IAsyncLifetime
{
    public const string Password="TestOnly.DualPath2026!";
    public WebApplicationFactory<Program> Factory {get;private set;}=null!;
    public string Connection {get;private set;}="";
    public async Task InitializeAsync()
    {
        var raw=Environment.GetEnvironmentVariable("DUALPATH_TEST_DATABASE")??throw new InvalidOperationException("Set DUALPATH_TEST_DATABASE to an isolated PostgreSQL database. Tests do not fall back to an in-memory database.");
        var schema="dp_test_"+Guid.NewGuid().ToString("N");await using(var c=new NpgsqlConnection(raw)){await c.OpenAsync();await using var cmd=new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"",c);await cmd.ExecuteNonQueryAsync();}
        var cs=new NpgsqlConnectionStringBuilder(raw){SearchPath=schema};Connection=cs.ConnectionString;
        Factory=new TestFactory(Connection);
        using var client=Factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false});var response=await client.GetAsync("/health/ready");Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var verify=Factory.Services.CreateScope();var db=verify.ServiceProvider.GetRequiredService<AppDb>();await db.Database.OpenConnectionAsync();await using var check=db.Database.GetDbConnection().CreateCommand();check.CommandText="SELECT current_schema()";Assert.Equal(schema,(string?)await check.ExecuteScalarAsync());await db.Database.CloseConnectionAsync();
    }
    private sealed class TestFactory(string connection):WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services=>{services.RemoveAll<DbContextOptions<AppDb>>();services.RemoveAll<IDbContextOptionsConfiguration<AppDb>>();services.AddDbContext<AppDb>(o=>o.UseNpgsql(connection).AddInterceptors(new SchemaInterceptor(new NpgsqlConnectionStringBuilder(connection).SearchPath!)));});
            builder.UseEnvironment("Testing");builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["RateLimits:FormRequestsPerMinute"]="1000",["RateLimits:RequestsPerMinute"]="5000",["ConnectionStrings:Database"]=connection,["DualPath:ApplyMigrations"]="true",["DualPath:SeedDemo"]="true",["DualPath:DemoPassword"]=Password,["DualPath:BaseUrl"]="http://localhost",["Storage:Root"]=Path.Combine(Path.GetTempPath(),"dualpath-tests",Guid.NewGuid().ToString("N"))}));
        }
    }
    public AsyncServiceScope Scope()=>Factory.Services.CreateAsyncScope();
    public async Task<AppUser> User(IServiceProvider services,string email)=>await services.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email)??throw new Exception("Missing fixture user");
    public async Task<AppUser> NewUser(IServiceProvider services,string prefix="learner")
    {
        var email=prefix+Guid.NewGuid().ToString("N")+"@test.dualpath.test";var user=new AppUser{UserName=email,Email=email,DisplayName="Integration learner",EmailConfirmed=true};var r=await services.GetRequiredService<UserManager<AppUser>>().CreateAsync(user,Password);Assert.True(r.Succeeded);return user;
    }
    public HttpClient Client()=>Factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false,HandleCookies=true});
    public static async Task<string> Token(HttpClient client,string path)
    {
        var r=await client.GetAsync(path);var html=await r.Content.ReadAsStringAsync();var match=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");if(!match.Success)match=Regex.Match(html,"value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"");Assert.True(match.Success,"Missing antiforgery input at "+path);return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    public static async Task<HttpResponseMessage> Post(HttpClient client,string path,Dictionary<string,string> fields)
    {fields["__RequestVerificationToken"]=await Token(client,path);return await client.PostAsync(path,new FormUrlEncodedContent(fields));}
    public async Task<HttpClient> Login(string email="student@demo.dualpath.test")
    {
        var c=Client();var r=await Post(c,"/signin",new(){["Command"]="login",["Input.Email"]=email,["Input.Password"]=Password});Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);return c;
    }
    public Task DisposeAsync(){Factory?.Dispose();return Task.CompletedTask;}
}
[CollectionDefinition("platform")]
public sealed class PlatformCollection:ICollectionFixture<PlatformFixture>;

internal sealed class SchemaInterceptor(string schema):Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
{
    public override void ConnectionOpened(System.Data.Common.DbConnection connection,Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
    {using var cmd=connection.CreateCommand();cmd.CommandText="SET search_path = \""+schema+"\"";cmd.ExecuteNonQuery();}
    public override async Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection,Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData,CancellationToken cancellationToken=default)
    {await using var cmd=connection.CreateCommand();cmd.CommandText="SET search_path = \""+schema+"\"";await cmd.ExecuteNonQueryAsync(cancellationToken);}
}
