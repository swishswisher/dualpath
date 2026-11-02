using System.Threading.RateLimiting;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using DualPath.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDb>((services,o)=>
{
    var connection=services.GetRequiredService<IConfiguration>().GetConnectionString("Database");
    if(string.IsNullOrWhiteSpace(connection))throw new InvalidOperationException("Set ConnectionStrings__Database to a PostgreSQL connection string.");
    o.UseNpgsql(connection);
});
builder.Services.AddIdentity<AppUser,IdentityRole>(o=>
{
    o.User.RequireUniqueEmail=true;o.SignIn.RequireConfirmedEmail=true;o.Password.RequiredLength=12;o.Password.RequireNonAlphanumeric=true;o.Lockout.MaxFailedAccessAttempts=5;o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
}).AddEntityFrameworkStores<AppDb>().AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(o=>o.TokenLifespan=TimeSpan.FromHours(2));
builder.Services.ConfigureApplicationCookie(o=>{o.LoginPath="/signin";o.AccessDeniedPath="/denied";o.Cookie.Name="DualPath.Session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.Cookie.SecurePolicy=builder.Environment.IsProduction()?CookieSecurePolicy.Always:CookieSecurePolicy.SameAsRequest;o.ExpireTimeSpan=TimeSpan.FromHours(8);o.SlidingExpiration=true;});
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath,"App_Data/keys"))).SetApplicationName("DualPath");
builder.Services.Configure<FormOptions>(o=>{o.MultipartBodyLengthLimit=11*1024*1024;o.ValueLengthLimit=40000;});
builder.Services.AddRazorPages(o=>o.Conventions.ConfigureFilter(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute())).AddMvcOptions(o=>o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=true);
builder.Services.AddScoped<Access>();builder.Services.AddScoped<LearningService>();builder.Services.AddScoped<InstitutionService>();builder.Services.AddScoped<PrivateStorage>();builder.Services.AddScoped<AccountService>();builder.Services.AddScoped<PracticeService>();
builder.Services.AddHttpClient<PracticeProvider>(c=>c.Timeout=TimeSpan.FromSeconds(45));builder.Services.AddHttpClient<EmailProvider>(c=>c.Timeout=TimeSpan.FromSeconds(20));builder.Services.AddHttpClient<BillingService>(c=>c.Timeout=TimeSpan.FromSeconds(30));
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddRateLimiter(o=>
{
    o.RejectionStatusCode=429;
    o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(c=>
    {
        var sensitive=HttpMethods.IsPost(c.Request.Method)&&new[]{"/signin","/signup","/reset","/reset-password","/contact"}.Contains(c.Request.Path.Value);
        var key=(sensitive?"forms:":"requests:")+(c.User.Identity?.Name??c.Connection.RemoteIpAddress?.ToString()??"unknown");
        return RateLimitPartition.GetFixedWindowLimiter(key,_=>new FixedWindowRateLimiterOptions{PermitLimit=sensitive?builder.Configuration.GetValue("RateLimits:FormRequestsPerMinute",12):builder.Configuration.GetValue("RateLimits:RequestsPerMinute",120),Window=TimeSpan.FromMinutes(1),QueueLimit=0});
    });
});
var app=builder.Build();
if(app.Environment.IsProduction())
{
    if(app.Configuration.GetValue<bool>("DualPath:SeedDemo"))throw new InvalidOperationException("Demo seeding is prohibited in Production.");
    if(!Uri.TryCreate(app.Configuration["DualPath:BaseUrl"],UriKind.Absolute,out var baseUri)||baseUri.Scheme!="https")throw new InvalidOperationException("Production requires a configured HTTPS DualPath:BaseUrl.");
}
app.UseExceptionHandler("/error");if(!app.Environment.IsDevelopment()&&!app.Environment.IsEnvironment("Testing")){app.UseHsts();app.UseHttpsRedirection();}
app.Use(async(context,next)=>
{
    context.Response.Headers.XContentTypeOptions="nosniff";context.Response.Headers["Referrer-Policy"]="strict-origin-when-cross-origin";context.Response.Headers["Permissions-Policy"]="camera=(), microphone=(), geolocation=()";context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if(!context.Request.Path.StartsWithSegments("/css")&&!context.Request.Path.StartsWithSegments("/js"))context.Response.Headers.CacheControl="no-cache, no-store";
    await next();
});
app.UseStaticFiles();app.UseRouting();app.UseAuthentication();app.UseAuthorization();app.UseRateLimiter();
app.MapGet("/health/live",()=>Results.Ok(new{status="live"}));
app.MapGet("/health/ready",async(AppDb db)=>await db.Database.CanConnectAsync()?Results.Ok(new{status="ready"}):Results.StatusCode(503));
app.MapPost("/webhooks/stripe",async(HttpContext context,BillingService billing)=>
{
    if(context.Request.ContentLength>256000)return Results.StatusCode(413);
    using var reader=new StreamReader(context.Request.Body);var payload=await reader.ReadToEndAsync(context.RequestAborted);if(payload.Length>256000)return Results.StatusCode(413);
    try{await billing.HandleWebhook(payload,context.Request.Headers["Stripe-Signature"].ToString());return Results.Ok();}catch(DomainException e){return Results.StatusCode(e.Status);}catch(System.Text.Json.JsonException){return Results.BadRequest();}
}).DisableAntiforgery();
app.MapGet("/files/{id:guid}",async(Guid id,HttpContext context,UserManager<AppUser> users,PrivateStorage storage)=>
{
    var actor=await users.GetUserAsync(context.User);if(actor==null)return Results.Unauthorized();try{var result=await storage.Download(actor,id);return Results.File(result.Path,result.File.ContentType,result.File.OriginalName,enableRangeProcessing:false);}catch(DomainException e){return Results.StatusCode(e.Status);}
}).RequireAuthorization();
app.MapRazorPages();
await using(var scope=app.Services.CreateAsyncScope())
{
    var db=scope.ServiceProvider.GetRequiredService<AppDb>();
    if(app.Configuration.GetValue<bool>("DualPath:ApplyMigrations"))await db.Database.MigrateAsync();
    if(await db.Database.CanConnectAsync())await Seed.Run(scope.ServiceProvider,app.Configuration,app.Environment);
}
app.Run();
public partial class Program;
