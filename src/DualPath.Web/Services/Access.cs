using System.Security.Cryptography;
using System.Text;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class DomainException(string message, int status = 400) : Exception(message)
{
    public int Status { get; } = status;
}
public static class Guard
{
    public static string Text(string? value, int max, string label, int min = 1)
    {
        var v=(value??"").Trim();
        if(v.Length<min || v.Length>max)throw new DomainException($"{label} must be between {min} and {max} characters.");
        return v;
    }
    public static string Email(string? value)
    {
        var v=Text(value,200,"Email").ToLowerInvariant();
        if(!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(v))throw new DomainException("Enter a valid email address.");
        return v;
    }
    public static Guid Id(string? value) => Guid.TryParse(value,out var id)?id:throw new DomainException("A valid record is required.");
    public static int Score(int value) => value is >=1 and <=4?value:throw new DomainException("Rubric scores must be from 1 to 4.");
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
public sealed class Access(AppDb db, IWebHostEnvironment environment)
{
    public IQueryable<Cohort> ManagedCohorts(AppUser actor) => actor.PlatformAdmin?db.Cohorts:db.Cohorts.Where(c=>db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==c.OrganisationId&&m.Active&&(m.Role==Roles.InstitutionAdmin||(m.Role==Roles.Instructor&&c.InstructorUserId==actor.Id))));
    public async Task RequireCourseAuthor(AppUser actor,Guid course)
    {
        if(!actor.PlatformAdmin&&!await db.Courses.AnyAsync(c=>c.Id==course&&c.OrganisationId!=null&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==c.OrganisationId&&m.Active&&m.Role!=Roles.Student)))throw new DomainException("You cannot edit this course.",403);
    }
    public IQueryable<Cohort> Cohorts(AppUser actor) => actor.PlatformAdmin?db.Cohorts:
        db.Cohorts.Where(c=>db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==c.OrganisationId&&m.Active&&(m.Role==Roles.InstitutionAdmin||(m.Role==Roles.Instructor&&c.InstructorUserId==actor.Id)))||db.Enrolments.Any(e=>e.UserId==actor.Id&&e.CohortId==c.Id)&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==c.OrganisationId&&m.Active));
    public IQueryable<Organisation> Organisations(AppUser actor) => actor.PlatformAdmin?db.Organisations:db.Organisations.Where(o=>db.Memberships.Any(m=>m.OrganisationId==o.Id&&m.UserId==actor.Id&&m.Active&&m.Role==Roles.InstitutionAdmin));
    public IQueryable<Submission> Submissions(AppUser actor) => db.Submissions.Where(s=>s.UserId==actor.Id||(s.Status!="Draft"&&s.Cohort!=null&&s.Cohort.InstructorUserId==actor.Id&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==s.Cohort.OrganisationId&&m.Active&&m.Role==Roles.Instructor)));
    public IQueryable<Reflection> Reflections(AppUser actor) => db.Reflections.Where(r=>r.UserId==actor.Id||(r.Cohort!=null&&r.Cohort.InstructorUserId==actor.Id&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==r.Cohort.OrganisationId&&m.Active&&m.Role==Roles.Instructor)));
    public async Task<bool> IsStaff(AppUser actor) => actor.PlatformAdmin||await db.Memberships.AnyAsync(m=>m.UserId==actor.Id&&m.Active&&m.Role!=Roles.Student);
    public async Task RequireOrganisationAdmin(AppUser actor,Guid org)
    {
        if(!await Organisations(actor).AnyAsync(o=>o.Id==org))throw new DomainException("You do not have access to manage this institution.",403);
    }
    public async Task RequireInstructor(AppUser actor,Guid cohort)
    {
        if(!await db.Cohorts.AnyAsync(c=>c.Id==cohort&&c.InstructorUserId==actor.Id&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.Active&&m.OrganisationId==c.OrganisationId&&m.Role==Roles.Instructor)))throw new DomainException("Only the assigned instructor can review this work.",403);
    }
    public async Task<Dictionary<Guid,bool>> CourseAvailability(AppUser actor,IEnumerable<Course> source)
    {
        var courses=source.ToList();
        if(actor.PlatformAdmin)return courses.ToDictionary(c=>c.Id,_=>true);
        var members=await db.Memberships.AsNoTracking().Where(m=>m.UserId==actor.Id&&m.Active).ToListAsync();
        var orgIds=members.Select(m=>m.OrganisationId).ToList();var now=DateTimeOffset.UtcNow;
        var plans=await db.Subscriptions.AsNoTracking().Where(s=>s.Status=="Active"&&(!s.DevelopmentOnly||!environment.IsProduction())&&s.PaidThrough!=null&&s.PaidThrough>now&&((s.UserId==actor.Id&&s.OrganisationId==null)||(s.OrganisationId!=null&&orgIds.Contains(s.OrganisationId.Value)))).ToListAsync();
        var enrolments=await db.Enrolments.AsNoTracking().Where(e=>e.UserId==actor.Id&&orgIds.Contains(e.Cohort.OrganisationId)).Select(e=>new{e.Cohort.OrganisationId,e.Cohort.CourseId}).ToListAsync();
        var personal=plans.Any(s=>s.OrganisationId==null);var paidOrgs=plans.Where(s=>s.OrganisationId!=null).Select(s=>s.OrganisationId!.Value).ToHashSet();
        var institutionPath=members.Any(m=>m.Role!=Roles.Student&&paidOrgs.Contains(m.OrganisationId))||enrolments.Any(e=>paidOrgs.Contains(e.OrganisationId));
        return courses.ToDictionary(c=>c.Id,c=>
        {
            if(c.OrganisationId is Guid org)
            {
                var member=members.FirstOrDefault(m=>m.OrganisationId==org);if(member==null)return false;
                if(member.Role!=Roles.Student)return true;
                return c.Published&&paidOrgs.Contains(org)&&enrolments.Any(e=>e.OrganisationId==org&&e.CourseId==c.Id);
            }
            if(!c.Published)return false;
            return c.Slug=="discipline-ownership"||personal||institutionPath;
        });
    }
    public async Task<bool> CanLearn(AppUser actor,Course course)=>(await CourseAvailability(actor,[course]))[course.Id];
    public void RequirePlatformAdmin(AppUser actor)
    {if(!actor.PlatformAdmin)throw new DomainException("Platform administrator access is required.",403);}
}
