using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;
namespace DualPath.Web.Services;
public sealed record CohortReport(string Cohort,string Learner,int Completed,int Lessons,int CorrectAttempts,int Attempts,int Submitted,int Approved);
public sealed class ReportingService(AppDb db,Access access)
{
    public async Task<List<CohortReport>> Rows(AppUser actor,CancellationToken ct)
    {
        var cohorts=access.ManagedCohorts(actor);
        return await db.Enrolments.Where(e=>cohorts.Any(c=>c.Id==e.CohortId)&&db.Memberships.Any(m=>m.OrganisationId==e.Cohort.OrganisationId&&m.UserId==e.UserId&&m.Active))
            .OrderBy(e=>e.Cohort.Name).ThenBy(e=>e.User.DisplayName).Select(e=>new CohortReport(e.Cohort.Name,e.User.DisplayName,
                db.Completions.Count(x=>x.UserId==e.UserId&&x.Lesson.CourseId==e.Cohort.CourseId&&x.Lesson.Published),
                db.Lessons.Count(l=>l.CourseId==e.Cohort.CourseId&&l.Published),
                db.Attempts.Count(x=>x.UserId==e.UserId&&x.Lesson.CourseId==e.Cohort.CourseId&&x.Correct),
                db.Attempts.Count(x=>x.UserId==e.UserId&&x.Lesson.CourseId==e.Cohort.CourseId),
                db.Submissions.Count(x=>x.UserId==e.UserId&&x.CohortId==e.CohortId&&x.SubmittedAt!=null),
                db.Submissions.Count(x=>x.UserId==e.UserId&&x.CohortId==e.CohortId&&x.Status=="Approved"))).Take(5000).ToListAsync(ct);
    }
}
