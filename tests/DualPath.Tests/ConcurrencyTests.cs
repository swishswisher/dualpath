using DualPath.Web.Data;
using DualPath.Web.Domain;
using DualPath.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace DualPath.Tests;
[Collection("platform")]
public sealed class ConcurrencyTests(PlatformFixture fixture)
{
    [Fact]public async Task ConcurrentInvitationAcceptanceCannotOversellTheLastSeat()
    {
        string firstToken,secondToken;string firstId,secondId;Guid orgId;
        await using(var scope=fixture.Scope())
        {
            var sp=scope.ServiceProvider;var owner=await fixture.NewUser(sp,"capacity-owner");var service=sp.GetRequiredService<InstitutionService>();var org=await service.Create(owner,"Concurrent capacity",owner.Email!);org.SeatLimit=1;await sp.GetRequiredService<AppDb>().SaveChangesAsync();orgId=org.Id;
            var first=await fixture.NewUser(sp);var second=await fixture.NewUser(sp);firstId=first.Id;secondId=second.Id;firstToken=await service.Invite(owner,org.Id,null,first.Email!,Roles.Student);secondToken=await service.Invite(owner,org.Id,null,second.Email!,Roles.Student);
        }
        async Task<int> Accept(string id,string token)
        {await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var actor=await sp.GetRequiredService<AppDb>().Users.SingleAsync(u=>u.Id==id);try{await sp.GetRequiredService<InstitutionService>().Accept(actor,token);return 200;}catch(DomainException e){return e.Status;}}
        var results=await Task.WhenAll(Accept(firstId,firstToken),Accept(secondId,secondToken));Assert.Equal(new[]{200,409},results.Order().ToArray());await using var check=fixture.Scope();Assert.Equal(1,await check.ServiceProvider.GetRequiredService<AppDb>().Memberships.CountAsync(m=>m.OrganisationId==orgId&&m.Active&&m.Role==Roles.Student));
    }
    [Fact]public async Task ConcurrentCompletionsProduceOneCompletionRecord()
    {
        string userId;Guid lessonId;await using(var scope=fixture.Scope()){var sp=scope.ServiceProvider;var u=await fixture.NewUser(sp);userId=u.Id;lessonId=await sp.GetRequiredService<AppDb>().Lessons.Where(l=>l.Course.Slug=="discipline-ownership").OrderBy(l=>l.Position).Select(l=>l.Id).FirstAsync();}
        async Task Complete(){await using var scope=fixture.Scope();var sp=scope.ServiceProvider;var user=await sp.GetRequiredService<AppDb>().Users.SingleAsync(u=>u.Id==userId);Assert.True(await sp.GetRequiredService<LearningService>().CompleteLesson(user,lessonId,1));}
        await Task.WhenAll(Complete(),Complete());await using var check=fixture.Scope();var db=check.ServiceProvider.GetRequiredService<AppDb>();Assert.Equal(1,await db.Completions.CountAsync(c=>c.UserId==userId&&c.LessonId==lessonId));Assert.Equal(2,await db.Attempts.CountAsync(a=>a.UserId==userId&&a.LessonId==lessonId));
    }
}
