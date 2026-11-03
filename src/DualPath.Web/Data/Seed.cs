using System.Text.Json;
using DualPath.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Data;
public static class Seed
{
    private sealed record Resource(string Slug,string Title,string Body);
    private sealed record Catalog(string Slug,string Title,string Summary,int Position,LessonSeed[] Lessons);
    private sealed record LessonSeed(string Slug,string Title,string Objective,string Body,string Question,string[] Options,int Answer,int Position);
    public static async Task Run(IServiceProvider services,IConfiguration config,IWebHostEnvironment env)
    {
        var db=services.GetRequiredService<AppDb>();var users=services.GetRequiredService<UserManager<AppUser>>();
        if(env.IsProduction()&&(await db.Users.AnyAsync(u=>u.Email!=null&&u.Email.EndsWith("@demo.dualpath.test"))||await db.Subscriptions.AnyAsync(s=>s.DevelopmentOnly)))throw new InvalidOperationException("Production refuses a database containing demonstration accounts or plans.");
        await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728314927)");
        var catalog=JsonSerializer.Deserialize<Catalog[]>(await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath,"Data/catalog.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        foreach(var row in catalog)
        {
            if(await db.Courses.AnyAsync(c=>c.Slug==row.Slug))continue;
            var c=new Course{Slug=row.Slug,Title=row.Title,Summary=row.Summary,Position=row.Position,Published=true};db.Add(c);
            foreach(var l in row.Lessons){var lesson=new Lesson{CourseId=c.Id,Slug=l.Slug,Title=l.Title,Objective=l.Objective,Body=l.Body,Position=l.Position,Published=true,Question=l.Question,OptionsJson=JsonSerializer.Serialize(l.Options),CorrectOption=l.Answer};db.Add(lesson);db.LessonSnapshots.Add(new LessonSnapshot{LessonId=lesson.Id,ContentVersion=1,Title=l.Title,Body=l.Body,AssessmentJson=JsonSerializer.Serialize(new{l.Question,l.Options,l.Answer})});}
            db.Assignments.Add(new Assignment{CourseId=c.Id,Title=c.Position==2?"Your outreach brief":c.Title+" — practical work",Instructions=c.Position==2?"Choose a professional whose public work connects with your interests. Explain the connection, then write a concise email with one specific, respectful question. Include your own research notes. Do not include private contact information.":"Choose a real example connected to this module. Explain the situation, the action you took or plan to take, the reasoning behind it, and one thing you would improve. Submit work in your own words."});
        }
        var resources=JsonSerializer.Deserialize<Resource[]>(await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath,"Data/resources.json")),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
        foreach(var resource in resources)
            if(!await db.ContentPages.AnyAsync(c=>c.Slug==resource.Slug))db.Add(new ContentPage{Slug=resource.Slug,Title=resource.Title,DraftTitle=resource.Title,DraftBody=resource.Body,PublishedBody=resource.Body,PublishedAt=DateTimeOffset.UtcNow});
        await db.SaveChangesAsync();
        var adminEmail=config["DualPath:BootstrapAdminEmail"];var adminPassword=config["DualPath:BootstrapAdminPassword"];
        if(!string.IsNullOrWhiteSpace(adminEmail)&&!string.IsNullOrWhiteSpace(adminPassword)&&await users.FindByEmailAsync(adminEmail)==null)
        {var admin=new AppUser{UserName=adminEmail,Email=adminEmail,DisplayName="Platform administrator",EmailConfirmed=true,PlatformAdmin=true};var r=await users.CreateAsync(admin,adminPassword);if(!r.Succeeded)throw new InvalidOperationException("Bootstrap administrator creation failed: "+string.Join(" ",r.Errors.Select(x=>x.Code)));}
        if(config.GetValue<bool>("DualPath:SeedDemo"))
        {
            if(env.IsProduction())throw new InvalidOperationException("Demo records cannot be seeded in Production.");
            var password=config["DualPath:DemoPassword"];if(string.IsNullOrWhiteSpace(password))throw new InvalidOperationException("Set a development-only DualPath:DemoPassword.");
            foreach(var (email,name,admin) in new[]{("student@demo.dualpath.test","Avery Morgan",false),("instructor@demo.dualpath.test","Dr. Maya Ellis",false),("institution@demo.dualpath.test","Alex Rivera",false),("admin@demo.dualpath.test","Jordan Lee",true)})
                if(await users.FindByEmailAsync(email)==null){var r=await users.CreateAsync(new AppUser{UserName=email,Email=email,DisplayName=name,EmailConfirmed=true,PlatformAdmin=admin},password);if(!r.Succeeded)throw new InvalidOperationException("Demo user seeding failed.");}
            if(!await db.Organisations.AnyAsync(o=>o.Name=="Demonstration institution"))
            {
                var student=(await users.FindByEmailAsync("student@demo.dualpath.test"))!;var instructor=(await users.FindByEmailAsync("instructor@demo.dualpath.test"))!;var owner=(await users.FindByEmailAsync("institution@demo.dualpath.test"))!;
                var org=new Organisation{Name="Demonstration institution",BillingEmail=owner.Email!,SeatLimit=50};db.Add(org);
                db.AddRange(new Membership{OrganisationId=org.Id,UserId=student.Id},new Membership{OrganisationId=org.Id,UserId=instructor.Id,Role=Roles.Instructor},new Membership{OrganisationId=org.Id,UserId=owner.Id,Role=Roles.InstitutionAdmin});
                var course=await db.Courses.SingleAsync(c=>c.Position==2);var cohort=new Cohort{OrganisationId=org.Id,CourseId=course.Id,Name="Autumn Launchpad — demonstration",InstructorUserId=instructor.Id};db.Add(cohort);db.Add(new Enrolment{CohortId=cohort.Id,UserId=student.Id});
                db.Add(new Subscription{UserId=owner.Id,OrganisationId=org.Id,Plan="Institution",Status="Active",PaidThrough=DateTimeOffset.UtcNow.AddYears(1),DevelopmentOnly=true});
                db.Add(new Notification{UserId=student.Id,Title="Welcome to your development workspace",Body="These accounts and the institution are demonstration records. Your actions will persist in this development database.",Url="/learning"});await db.SaveChangesAsync();
            }
        }
        await tx.CommitAsync();
    }
}
