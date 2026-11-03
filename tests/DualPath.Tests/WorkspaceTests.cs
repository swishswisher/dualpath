using System.Net;
using System.Text.RegularExpressions;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using DualPath.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace DualPath.Tests;
[Collection("platform")]
public sealed class WorkspaceTests(PlatformFixture fixture)
{
    [Theory]
    [InlineData("student","/dashboard")][InlineData("student","/learning")][InlineData("student","/lesson")][InlineData("student","/work")][InlineData("student","/assignment")][InlineData("student","/reflections")][InlineData("student","/practice")][InlineData("student","/progress")][InlineData("student","/notifications")][InlineData("student","/account")][InlineData("student","/billing")][InlineData("student","/cohorts")][InlineData("student","/cohort")][InlineData("student","/onboarding")]
    [InlineData("instructor","/instructor")][InlineData("instructor","/courses")][InlineData("instructor","/reviews")][InlineData("instructor","/reports")]
    [InlineData("institution","/organisation")][InlineData("institution","/members")][InlineData("institution","/cohorts")][InlineData("institution","/orgbilling")]
    [InlineData("admin","/admin")][InlineData("admin","/content")][InlineData("admin","/audit")][InlineData("admin","/mailbox")][InlineData("admin","/author")]
    public async Task RoleWorkspaceRenders(string role,string path)
    {using var client=await fixture.Login(role+"@demo.dualpath.test");var response=await client.GetAsync(path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("<main",await response.Content.ReadAsStringAsync());}
    [Fact]public async Task CohortCannotUseAnotherInstitutionsPrivateCourse()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var owner=await fixture.User(sp,"institution@demo.dualpath.test");var org=await db.Organisations.SingleAsync(o=>o.Name=="Demonstration institution");var other=new Organisation{Name="Course boundary test"};db.Add(other);var course=new Course{OrganisationId=other.Id,Slug="private-"+Guid.NewGuid(),Title="Private course",Published=true};db.Add(course);await db.SaveChangesAsync();var teacher=await fixture.User(sp,"instructor@demo.dualpath.test");await Assert.ThrowsAsync<DomainException>(()=>sp.GetRequiredService<LearningService>().CreateCohort(owner,org.Id,course.Id,"Invalid boundary",teacher.Id));
    }
    [Fact]public async Task UnrelatedStaffMembershipDoesNotExposeEnrolledCohortsReports()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var actor=await fixture.NewUser(sp,"mixed-role");var demo=await db.Cohorts.FirstAsync();db.Add(new Membership{OrganisationId=demo.OrganisationId,UserId=actor.Id,Role=Roles.Student});db.Add(new Enrolment{CohortId=demo.Id,UserId=actor.Id});var other=new Organisation{Name="Unrelated staff role"};db.Add(other);db.Add(new Membership{OrganisationId=other.Id,UserId=actor.Id,Role=Roles.Instructor});await db.SaveChangesAsync();Assert.True(await sp.GetRequiredService<Access>().Cohorts(actor).AnyAsync());Assert.Empty(await sp.GetRequiredService<Access>().ManagedCohorts(actor).ToListAsync());Assert.Empty(await new ReportingService(db,sp.GetRequiredService<Access>()).Rows(actor,default));
    }
    [Fact]public async Task RevokedInvitationCannotBeAccepted()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var owner=await fixture.User(sp,"institution@demo.dualpath.test");var recipient=await fixture.NewUser(sp);var org=await db.Organisations.SingleAsync(o=>o.Name=="Demonstration institution");var service=sp.GetRequiredService<InstitutionService>();var token=await service.Invite(owner,org.Id,null,recipient.Email!,Roles.Student);var invite=await db.Invitations.SingleAsync(i=>i.TokenHash==Guard.Hash(token));await service.Revoke(owner,invite.Id);Assert.Equal(409,(await Assert.ThrowsAsync<DomainException>(()=>service.Accept(recipient,token))).Status);
    }
    [Fact]public async Task InstructorCannotOpenGlobalCurriculumAuthor()
    {using var client=await fixture.Login("instructor@demo.dualpath.test");Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/author")).StatusCode);}
    [Fact]public async Task StaffCanCreateMultipleLessonsAndPublishTheirOwnCourse()
    {
        using var client=await fixture.Login("instructor@demo.dualpath.test");await using var scope=fixture.Scope();var db=scope.ServiceProvider.GetRequiredService<AppDb>();var org=await db.Organisations.SingleAsync(o=>o.Name=="Demonstration institution");var response=await PlatformFixture.Post(client,"/courses",new(){["Command"]="course",["Input.OrganisationId"]=org.Id.ToString(),["Input.Title"]="Course creation "+Guid.NewGuid(),["Input.Body"]="A practical course on writing one specific, respectful request."});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);var author=response.Headers.Location!.ToString();var lesson=await db.Lessons.SingleAsync(l=>l.Id==Guid.Parse(author.Split('/').Last()));Assert.False(lesson.Published);response=await PlatformFixture.Post(client,author,new(){["Command"]="add-lesson",["Input.CourseId"]=lesson.CourseId.ToString(),["Input.Title"]="Try a different perspective"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal(2,await db.Lessons.CountAsync(l=>l.CourseId==lesson.CourseId));
    }
    [Fact]public async Task RegistrationVerificationAndPasswordRecoveryUseRealIdentityTokens()
    {
        using var client=fixture.Client();var email="registration-"+Guid.NewGuid()+"@test.dualpath.test";var registered=await PlatformFixture.Post(client,"/signup",new(){["Command"]="register",["Input.Name"]="New learner",["Input.Email"]=email,["Input.Password"]=PlatformFixture.Password,["Input.Agree"]="true"});Assert.Equal(HttpStatusCode.Redirect,registered.StatusCode);await using var scope=fixture.Scope();var db=scope.ServiceProvider.GetRequiredService<AppDb>();var user=await db.Users.SingleAsync(u=>u.Email==email);Assert.False(user.EmailConfirmed);var message=await db.Outbox.SingleAsync(m=>m.ToEmail==email);var link=new Uri(Regex.Match(message.Body,"http[^ ]+").Value);Assert.Equal(HttpStatusCode.Redirect,(await client.GetAsync(link.PathAndQuery)).StatusCode);using var signed=await fixture.Login(email);var reset=await PlatformFixture.Post(client,"/reset",new(){["Command"]="reset",["Input.Email"]=email});Assert.Equal(HttpStatusCode.Redirect,reset.StatusCode);var recovery=await db.Outbox.SingleAsync(m=>m.ToEmail==email&&m.Subject.StartsWith("Reset"));Assert.Contains("/reset-password?",recovery.Body);
    }
}
