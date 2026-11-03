using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using DualPath.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DualPath.Tests;
[Collection("platform")]
public sealed class DomainTests(PlatformFixture fixture)
{
    [Fact]public async Task LearningCompletionIsPersistedAndIdempotent()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var user=await fixture.NewUser(sp);var lesson=await db.Lessons.Include(l=>l.Course).FirstAsync(l=>l.Course.Position==1);var service=sp.GetRequiredService<LearningService>();Assert.False(await service.CompleteLesson(user,lesson.Id,0));Assert.True(await service.CompleteLesson(user,lesson.Id,1));Assert.True(await service.CompleteLesson(user,lesson.Id,1));
        await using var again=fixture.Scope();var saved=again.ServiceProvider.GetRequiredService<AppDb>();Assert.Single(await saved.Completions.Where(c=>c.UserId==user.Id).ToListAsync());Assert.Equal(3,await saved.Attempts.CountAsync(a=>a.UserId==user.Id));
    }
    [Fact]public async Task UnpaidLearnerCannotAccessPaidModule()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var user=await fixture.NewUser(sp);var db=sp.GetRequiredService<AppDb>();var l=await db.Lessons.Include(l=>l.Course).FirstAsync(l=>l.Course.Position==2);var error=await Assert.ThrowsAsync<DomainException>(()=>sp.GetRequiredService<LearningService>().CompleteLesson(user,l.Id,1));Assert.Equal(403,error.Status);
    }
    [Fact]public async Task PaidAccessRequiresAnExplicitUnexpiredPeriod()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var user=await fixture.NewUser(sp);var course=await db.Courses.SingleAsync(c=>c.Slug=="cold-email-fundamentals");var sub=new Subscription{UserId=user.Id,Status="Active",Plan="Independent"};db.Add(sub);await db.SaveChangesAsync();var access=sp.GetRequiredService<Access>();Assert.False(await access.CanLearn(user,course));sub.PaidThrough=DateTimeOffset.UtcNow.AddMinutes(-1);await db.SaveChangesAsync();Assert.False(await access.CanLearn(user,course));sub.PaidThrough=DateTimeOffset.UtcNow.AddDays(1);await db.SaveChangesAsync();Assert.True(await access.CanLearn(user,course));
    }
    [Fact]public async Task PersonalSubscriptionCannotUnlockAnInstitutionsPrivateCourse()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var user=await fixture.NewUser(sp);var org=new Organisation{Name="Private curriculum boundary"};db.Add(org);var course=new Course{OrganisationId=org.Id,Slug="private-course-"+Guid.NewGuid(),Title="Private institution curriculum",Published=true,Position=1};db.Add(course);db.Add(new Membership{OrganisationId=org.Id,UserId=user.Id,Role=Roles.Student});db.Add(new Subscription{UserId=user.Id,Status="Active",PaidThrough=DateTimeOffset.UtcNow.AddDays(1)});await db.SaveChangesAsync();Assert.False(await sp.GetRequiredService<Access>().CanLearn(user,course));
    }
    [Fact]public async Task InstructorPublishesStudentSubmitsAndFeedbackPersists()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var student=await fixture.User(sp,"student@demo.dualpath.test");var teacher=await fixture.User(sp,"instructor@demo.dualpath.test");var assignment=await db.Assignments.Include(a=>a.Course).SingleAsync(a=>a.Course.Position==2);var cohort=await db.Cohorts.SingleAsync();var service=sp.GetRequiredService<LearningService>();var sub=await service.SaveSubmission(student,assignment.Id,"A real contribution","I built a keyboard accessible form and chose to test it with classmates before adding animations.",cohort.Id,true);await service.PublishReview(teacher,sub.Id,4,3,4,"Clear contribution. Explain one alternative you considered and why you chose this approach.","Revision requested",1);
        Assert.Equal("Revision requested",sub.Status);var next=await service.SaveSubmission(student,assignment.Id,"A clearer contribution","I chose a native form control over a custom widget because keyboard navigation was more reliable in our tests.",cohort.Id,true,sub.Version);Assert.Equal(2,next.Revision);await service.PublishReview(teacher,next.Id,4,4,4,"The reasoning is specific and easy to follow. This revision demonstrates the learning objective.","Approved",2);Assert.Equal(2,await db.Reviews.CountAsync(r=>r.SubmissionId==sub.Id));Assert.Equal(2,await db.SubmissionSnapshots.CountAsync(s=>s.SubmissionId==sub.Id));Assert.True(await db.Notifications.AnyAsync(n=>n.UserId==student.Id&&n.Url.Contains(sub.Id.ToString())));
    }
    [Fact]public async Task OtherInstructorCannotReadOrReviewAnotherCohort()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var other=await fixture.NewUser(sp,"other-instructor");var org=new Organisation{Name="Unrelated institution"};db.Add(org);db.Add(new Membership{OrganisationId=org.Id,UserId=other.Id,Role=Roles.Instructor});await db.SaveChangesAsync();var access=sp.GetRequiredService<Access>();Assert.Empty(await access.Cohorts(other).ToListAsync());var cohort=await db.Cohorts.FirstAsync();Assert.Equal(403,(await Assert.ThrowsAsync<DomainException>(()=>access.RequireInstructor(other,cohort.Id))).Status);Assert.Empty(await access.Submissions(other).ToListAsync());
    }
    [Fact]public async Task ReflectionVisibilityFollowsExplicitCohortChoice()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var student=await fixture.User(sp,"student@demo.dualpath.test");var teacher=await fixture.User(sp,"instructor@demo.dualpath.test");var cohort=await db.Cohorts.FirstAsync();var service=sp.GetRequiredService<LearningService>();await service.AddReflection(student,"PRIVATE-ONLY: a smaller plan helped me begin.",null);await service.AddReflection(student,"COHORT-VISIBLE: I tried a focused weekly session.",cohort.Id);var visible=await sp.GetRequiredService<Access>().Reflections(teacher).ToListAsync();Assert.DoesNotContain(visible,r=>r.Body.StartsWith("PRIVATE-ONLY"));Assert.Contains(visible,r=>r.Body.StartsWith("COHORT-VISIBLE"));
    }
    [Fact]public async Task InvitationRequiresMatchingVerifiedEmailAndCannotBeReplayed()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var owner=await fixture.User(sp,"institution@demo.dualpath.test");var invited=await fixture.NewUser(sp);var wrong=await fixture.NewUser(sp);var db=sp.GetRequiredService<AppDb>();var org=await db.Organisations.FirstAsync();var c=await db.Cohorts.FirstAsync();var service=sp.GetRequiredService<InstitutionService>();var token=await service.Invite(owner,org.Id,c.Id,invited.Email!,Roles.Student);Assert.Equal(403,(await Assert.ThrowsAsync<DomainException>(()=>service.Accept(wrong,token))).Status);await service.Accept(invited,token);Assert.True(await db.Enrolments.AnyAsync(e=>e.UserId==invited.Id&&e.CohortId==c.Id));Assert.Equal(409,(await Assert.ThrowsAsync<DomainException>(()=>service.Accept(invited,token))).Status);
    }
    [Fact]public async Task StudentCannotInviteOrEscalateTheirRole()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var user=await fixture.User(sp,"student@demo.dualpath.test");var org=await sp.GetRequiredService<AppDb>().Organisations.FirstAsync();var e=await Assert.ThrowsAsync<DomainException>(()=>sp.GetRequiredService<InstitutionService>().Invite(user,org.Id,null,"x@example.com",Roles.InstitutionAdmin));Assert.Equal(403,e.Status);
    }
    [Fact]public async Task SeatLimitIsEnforcedWhenInvitationIsAccepted()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var owner=await fixture.NewUser(sp,"owner");var service=sp.GetRequiredService<InstitutionService>();var org=await service.Create(owner,"Seat limit integration test",owner.Email!);org.SeatLimit=1;await db.SaveChangesAsync();var first=await fixture.NewUser(sp);var second=await fixture.NewUser(sp);var t1=await service.Invite(owner,org.Id,null,first.Email!,Roles.Student);var t2=await service.Invite(owner,org.Id,null,second.Email!,Roles.Student);await service.Accept(first,t1);Assert.Equal(409,(await Assert.ThrowsAsync<DomainException>(()=>service.Accept(second,t2))).Status);Assert.Equal(1,await db.Memberships.CountAsync(m=>m.OrganisationId==org.Id&&m.Role==Roles.Student&&m.Active));
    }
    [Fact]public async Task PrivateFilesRejectSpoofedContentAndUnauthorisedDownload()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var student=await fixture.NewUser(sp);var assignment=await db.Assignments.Include(a=>a.Course).FirstAsync(a=>a.Course.Position==1);var work=await sp.GetRequiredService<LearningService>().SaveSubmission(student,assignment.Id,"Private draft","A private test draft with a useful concrete example.",null,false);var storage=sp.GetRequiredService<PrivateStorage>();await using var invalid=new MemoryStream(Encoding.UTF8.GetBytes("Not a PDF file"));var fake=new FormFile(invalid,0,invalid.Length,"file","notes.pdf");await Assert.ThrowsAsync<DomainException>(()=>storage.Upload(student,work.Id,fake));await using var valid=new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4\nminimal test fixture"));var file=await storage.Upload(student,work.Id,new FormFile(valid,0,valid.Length,"file","notes.pdf"));var other=await fixture.NewUser(sp);Assert.Equal(404,(await Assert.ThrowsAsync<DomainException>(()=>storage.Download(other,file.Id))).Status);Assert.Equal(file.Id,(await storage.Download(student,file.Id)).File.Id);
    }
    [Fact]
    public async Task AssignedInstructorCanReadAttachmentOnlyAfterSubmission()
    {
        await using var scope=fixture.Scope();
        var sp=scope.ServiceProvider;
        var db=sp.GetRequiredService<AppDb>();
        var student=await fixture.NewUser(sp);
        var teacher=await fixture.User(sp,"instructor@demo.dualpath.test");
        var cohort=await db.Cohorts.SingleAsync(c=>c.InstructorUserId==teacher.Id);
        var assignment=await db.Assignments.SingleAsync(a=>a.CourseId==cohort.CourseId);
        db.Memberships.Add(new Membership{OrganisationId=cohort.OrganisationId,UserId=student.Id,Role=Roles.Student});
        db.Enrolments.Add(new Enrolment{CohortId=cohort.Id,UserId=student.Id});
        db.Subscriptions.Add(new Subscription{UserId=student.Id,Status="Active",PaidThrough=DateTimeOffset.UtcNow.AddDays(1)});
        await db.SaveChangesAsync();

        const string body="A specific contribution to the team’s internship project.";
        var learning=sp.GetRequiredService<LearningService>();
        var work=await learning.SaveSubmission(student,assignment.Id,"Private draft",body,cohort.Id,false);
        var storage=sp.GetRequiredService<PrivateStorage>();
        await using var bytes=new MemoryStream("%PDF-1.4 test fixture"u8.ToArray());
        var file=await storage.Upload(student,work.Id,new FormFile(bytes,0,bytes.Length,"file","draft.pdf"));
        Assert.Equal(404,(await Assert.ThrowsAsync<DomainException>(()=>storage.Download(teacher,file.Id))).Status);

        await learning.SaveSubmission(student,assignment.Id,"Submitted work",body,cohort.Id,true,work.Version);
        Assert.Equal(file.Id,(await storage.Download(teacher,file.Id)).File.Id);
    }
    [Fact]public async Task DraftLessonChangesDoNotChangePublishedLearning()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var admin=await fixture.User(sp,"admin@demo.dualpath.test");var db=sp.GetRequiredService<AppDb>();var l=await db.Lessons.FirstAsync();var original=l.Body;var version=l.ContentVersion;var service=sp.GetRequiredService<LearningService>();await service.SaveLesson(admin,l.Id,l.Title,l.Objective,"A new draft body that remains private until it is explicitly published.",l.Question,["A","B","C"],1,false,l.Version);await db.Entry(l).ReloadAsync();Assert.Equal(original,l.Body);Assert.NotEmpty(l.DraftJson);await service.SaveLesson(admin,l.Id,l.Title,l.Objective,"A published body with an explicit new content version for future learners.",l.Question,["A","B","C"],1,true,l.Version);Assert.Equal(version+1,l.ContentVersion);Assert.True(await db.LessonSnapshots.AnyAsync(s=>s.LessonId==l.Id&&s.ContentVersion==version));
    }
    [Fact]public void WebhookSignaturesRejectTamperingAndStaleTimestamps()
    {
        var now=DateTimeOffset.UtcNow;var stamp=now.ToUnixTimeSeconds();var payload="{\"id\":\"test\"}";var secret="test-signing-secret";var sig=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(stamp+"."+payload)));BillingService.VerifySignature(payload,$"t={stamp},v1={sig}",secret,now);Assert.Throws<DomainException>(()=>BillingService.VerifySignature(payload+" ",$"t={stamp},v1={sig}",secret,now));Assert.Throws<DomainException>(()=>BillingService.VerifySignature(payload,$"t={stamp},v1={sig}",secret,now.AddMinutes(10)));
        Assert.Throws<DomainException>(()=>BillingService.VerifySignature(payload,$"t={long.MinValue},v1={sig}",secret,now));
    }
    [Fact]public async Task BillingEventsAreIdempotentAndOldEventsCannotRestoreCancelledAccess()
    {
        await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var db=sp.GetRequiredService<AppDb>();var actor=await fixture.NewUser(sp);var local=new Subscription{UserId=actor.Id,Plan="Independent"};db.Add(local);await db.SaveChangesAsync();var conf=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Billing:WebhookSecret"]="webhook-test"}).Build();var service=new BillingService(db,sp.GetRequiredService<Access>(),sp.GetRequiredService<LearningService>(),new HttpClient(),conf);var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        async Task<bool> Send(string id,string type,string status,long time){var payload=JsonSerializer.Serialize(new{id,type,created=time,data=new{ @object=new{id="sub_"+local.Id,customer="cus_test",status,cancel_at_period_end=false,current_period_end=now+3600,metadata=new{dualpath_id=local.Id.ToString()}}}});var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("webhook-test"),Encoding.UTF8.GetBytes(now+"."+payload)));return await service.HandleWebhook(payload,$"t={now},v1={signature}");}
        Assert.True(await Send("evt_"+Guid.NewGuid(),"customer.subscription.created","active",now-3));Assert.Equal("Active",local.Status);var eventId="evt_"+Guid.NewGuid();Assert.True(await Send(eventId,"customer.subscription.deleted","canceled",now));Assert.False(await Send(eventId,"customer.subscription.deleted","canceled",now));Assert.True(await Send("evt_"+Guid.NewGuid(),"customer.subscription.updated","active",now-2));Assert.Equal("Cancelled",local.Status);Assert.True(await Send("evt_"+Guid.NewGuid(),"customer.subscription.updated","active",now));Assert.Equal("Cancelled",local.Status);
    }
    [Fact]public async Task MissingAiProviderDoesNotInventFeedback()
    {
        var config=new ConfigurationBuilder().Build();var provider=new PracticeProvider(new HttpClient(),config);Assert.False(provider.Ready);Assert.Equal(503,(await Assert.ThrowsAsync<DomainException>(()=>provider.Generate("Question","Answer",default))).Status);
    }
}
