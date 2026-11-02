using System.Data;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class LearningService(AppDb db, Access access)
{
    public void Audit(AppUser actor,string action,Guid target,Guid? org=null) => db.Audit.Add(new AuditEvent{ActorUserId=actor.Id,Action=action,Target=target.ToString(),OrganisationId=org});
    public void Notify(string userId,string title,string body,string url) => db.Notifications.Add(new Notification{UserId=userId,Title=title,Body=body,Url=url});
    public async Task<Lesson> RequireLesson(AppUser actor,Guid id)
    {
        var lesson=await db.Lessons.Include(x=>x.Course).SingleOrDefaultAsync(x=>x.Id==id)??throw new DomainException("Lesson not found.",404);
        if(!lesson.Published||!await access.CanLearn(actor,lesson.Course))throw new DomainException("This lesson requires an active programme plan or institution enrolment.",403);
        return lesson;
    }
    public async Task<bool> CompleteLesson(AppUser actor,Guid lessonId,int answer)
    {
        await using var tx=await db.Database.BeginTransactionAsync();
        await Lock(actor.Id);
        var lesson=await RequireLesson(actor,lessonId);
        var options=System.Text.Json.JsonSerializer.Deserialize<string[]>(lesson.OptionsJson)??[];
        if(answer<0||answer>=options.Length)throw new DomainException("Select a valid answer.");
        var correct=answer==lesson.CorrectOption;
        db.Attempts.Add(new AssessmentAttempt{UserId=actor.Id,LessonId=lesson.Id,LessonVersion=lesson.ContentVersion,Answer=answer,Correct=correct});
        if(correct&&!await db.Completions.AnyAsync(x=>x.UserId==actor.Id&&x.LessonId==lesson.Id))db.Completions.Add(new Completion{UserId=actor.Id,LessonId=lesson.Id,LessonVersion=lesson.ContentVersion});
        Audit(actor,correct?"LessonCompleted":"AssessmentAttempted",lesson.Id);
        await db.SaveChangesAsync();await tx.CommitAsync();return correct;
    }
    public async Task<Submission> SaveSubmission(AppUser actor,Guid assignmentId,string title,string body,Guid? cohortId,bool submit,long? version=null)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await Lock(actor.Id);
        var assignment=await db.Assignments.Include(x=>x.Course).SingleOrDefaultAsync(x=>x.Id==assignmentId)??throw new DomainException("Assignment not found.",404);
        if(!await access.CanLearn(actor,assignment.Course))throw new DomainException("Your programme plan does not include this assignment.",403);
        if(cohortId!=null&&!await db.Enrolments.AnyAsync(x=>x.UserId==actor.Id&&x.CohortId==cohortId&&x.Cohort.CourseId==assignment.CourseId&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==x.Cohort.OrganisationId&&m.Active)))throw new DomainException("You are not enrolled in this assignment’s cohort.",403);
        var item=await db.Submissions.SingleOrDefaultAsync(x=>x.AssignmentId==assignmentId&&x.UserId==actor.Id);
        if(item==null){item=new Submission{UserId=actor.Id,AssignmentId=assignmentId};db.Add(item);}
        else
        {
            if(version!=null&&version!=item.Version)throw new DomainException("This draft changed in another session. Reload before saving.",409);
            if(item.Status is "Submitted" or "Approved")throw new DomainException("Submitted work is locked. Request a revision before editing.",409);
            if(item.Status=="Revision requested")item.Revision++;
        }
        item.Title=Guard.Text(title,200,"Title");item.Body=Guard.Text(body,20000,"Your work",submit?30:1);item.CohortId=cohortId;
        item.Status=submit?"Submitted":"Draft";
        if(submit)
        {
            item.SubmittedAt=DateTimeOffset.UtcNow;
            db.SubmissionSnapshots.Add(new SubmissionSnapshot{SubmissionId=item.Id,Revision=item.Revision,Title=item.Title,Body=item.Body});
            if(cohortId!=null){var c=await db.Cohorts.SingleAsync(x=>x.Id==cohortId);Notify(c.InstructorUserId,"Work ready for review",item.Title,"/review/"+item.Id);}
        }
        Audit(actor,submit?"WorkSubmitted":"DraftSaved",item.Id);await db.SaveChangesAsync();await tx.CommitAsync();return item;
    }
    public async Task PublishReview(AppUser actor,Guid submissionId,int research,int clarity,int tone,string feedback,string decision,int revision)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await Lock(submissionId.ToString());
        var s=await db.Submissions.SingleOrDefaultAsync(x=>x.Id==submissionId)??throw new DomainException("Submission not found.",404);
        if(s.UserId==actor.Id)throw new DomainException("You cannot review your own submission.",403);
        if(s.CohortId==null)throw new DomainException("This submission has no assigned instructor.",403);
        await access.RequireInstructor(actor,s.CohortId.Value);
        if(s.Status!="Submitted"||s.Revision!=revision)throw new DomainException("This submission has already been reviewed or revised.",409);
        if(decision is not ("Approved" or "Revision requested"))throw new DomainException("Choose a review decision.");
        db.Reviews.Add(new Review{SubmissionId=s.Id,InstructorUserId=actor.Id,Revision=s.Revision,Research=Guard.Score(research),Clarity=Guard.Score(clarity),Tone=Guard.Score(tone),Feedback=Guard.Text(feedback,10000,"Feedback",10),Decision=decision});
        s.Status=decision;Notify(s.UserId,"Feedback on your work",s.Title,"/feedback/"+s.Id);Audit(actor,"ReviewPublished",s.Id);
        var email=await db.Users.Where(x=>x.Id==s.UserId).Select(x=>x.Email).SingleAsync();
        if(email!=null)db.Outbox.Add(new OutboxMessage{ToEmail=email,Subject="New feedback in DualPath",Body="Your instructor has reviewed your work. Sign in to read the feedback."});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task AddReflection(AppUser actor,string body,Guid? cohort)
    {
        if(cohort!=null&&!await db.Enrolments.AnyAsync(x=>x.UserId==actor.Id&&x.CohortId==cohort&&db.Memberships.Any(m=>m.UserId==actor.Id&&m.OrganisationId==x.Cohort.OrganisationId&&m.Active)))throw new DomainException("That cohort is not available to you.",403);
        db.Reflections.Add(new Reflection{UserId=actor.Id,CohortId=cohort,Body=Guard.Text(body,8000,"Reflection",5)});await db.SaveChangesAsync();
    }
    public async Task<Cohort> CreateCohort(AppUser actor,Guid org,Guid course,string name,string instructor)
    {
        await access.RequireOrganisationAdmin(actor,org);
        if(!await db.Courses.AnyAsync(x=>x.Id==course&&(x.OrganisationId==null||x.OrganisationId==org)))throw new DomainException("Select a valid course.");
        if(!await db.Memberships.AnyAsync(m=>m.OrganisationId==org&&m.UserId==instructor&&m.Active&&m.Role==Roles.Instructor))throw new DomainException("Assign an active instructor from this institution.");
        var c=new Cohort{OrganisationId=org,CourseId=course,Name=Guard.Text(name,160,"Cohort name"),InstructorUserId=instructor};db.Add(c);Audit(actor,"CohortCreated",c.Id,org);await db.SaveChangesAsync();return c;
    }
    public async Task PublishAnnouncement(AppUser actor,Guid cohort,string title,string body)
    {
        var c=await access.Cohorts(actor).SingleOrDefaultAsync(x=>x.Id==cohort)??throw new DomainException("Cohort unavailable.",403);
        if(c.InstructorUserId!=actor.Id)await access.RequireOrganisationAdmin(actor,c.OrganisationId);
        title=Guard.Text(title,200,"Title");body=Guard.Text(body,6000,"Message");db.Announcements.Add(new Announcement{CohortId=cohort,Title=title,Body=body});
        foreach(var uid in await db.Enrolments.Where(x=>x.CohortId==cohort).Select(x=>x.UserId).ToListAsync())Notify(uid,title,body,"/cohort/"+cohort);
        Audit(actor,"AnnouncementPublished",cohort,c.OrganisationId);await db.SaveChangesAsync();
    }
    public async Task SaveLesson(AppUser actor,Guid lessonId,string title,string objective,string body,string question,string[] options,int answer,bool publish,long version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await Lock(lessonId.ToString());
        var l=await db.Lessons.SingleOrDefaultAsync(x=>x.Id==lessonId)??throw new DomainException("Lesson not found.",404);
        await access.RequireCourseAuthor(actor,l.CourseId);
        if(version!=l.Version)throw new DomainException("This lesson changed. Reload before saving.",409);
        if(options.Length!=3||answer<0||answer>2)throw new DomainException("Provide three choices and a correct answer.");
        var draft=new LessonDraftData(Guard.Text(title,200,"Title"),Guard.Text(objective,1000,"Objective"),Guard.Text(body,30000,"Lesson body",30),Guard.Text(question,1000,"Question"),options.Select(x=>Guard.Text(x,1000,"Answer choice")).ToArray(),answer);
        if(!publish)l.DraftJson=System.Text.Json.JsonSerializer.Serialize(draft);
        else
        {
            l.Title=draft.Title;l.Objective=draft.Objective;l.Body=draft.Body;l.Question=draft.Question;l.OptionsJson=System.Text.Json.JsonSerializer.Serialize(draft.Options);l.CorrectOption=answer;l.DraftJson="";
            l.ContentVersion++;l.Published=true;var course=await db.Courses.SingleAsync(c=>c.Id==l.CourseId);course.Published=true;
            db.LessonSnapshots.Add(new LessonSnapshot{LessonId=l.Id,ContentVersion=l.ContentVersion,Title=l.Title,Body=l.Body,AssessmentJson=System.Text.Json.JsonSerializer.Serialize(new{l.Question,l.OptionsJson,l.CorrectOption})});
        }
        Audit(actor,publish?"LessonPublished":"LessonDraftSaved",l.Id);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public Task Lock(string key)=>db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))");
}
