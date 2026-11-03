using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using DualPath.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Pages;
public sealed class FormInput
{
    public string Name {get;set;}="";public string Email {get;set;}="";public string Password {get;set;}="";public string CurrentPassword {get;set;}="";public string Code {get;set;}="";public string ReturnUrl {get;set;}="";
    public string Title {get;set;}="";public string Body {get;set;}="";public string Objective {get;set;}="";public string Question {get;set;}="";public string Option0 {get;set;}="";public string Option1 {get;set;}="";public string Option2 {get;set;}="";
    public string Role {get;set;}="Student";public string Topic {get;set;}="Learning";public string TimeZone {get;set;}="UTC";public string Education {get;set;}="";public string Plan {get;set;}="Independent";public string Decision {get;set;}="Approved";public string Instructor {get;set;}="";
    public Guid Id {get;set;}public Guid CourseId {get;set;}public Guid OrganisationId {get;set;}public Guid? CohortId {get;set;}public Guid AttemptId {get;set;}
    public int SeatLimit {get;set;}=50;public int Answer {get;set;}=-1;public int Research {get;set;}=4;public int Clarity {get;set;}=4;public int Tone {get;set;}=4;public int Revision {get;set;}=1;public long Version {get;set;}
    public bool Publish {get;set;}public bool Remember {get;set;}public bool AiConsent {get;set;}public bool Reminders {get;set;}public bool Marketing {get;set;}public bool Active {get;set;}=true;public bool Agree {get;set;}
    public IFormFile? File {get;set;}
}
public sealed class IndexModel(AppDb db,UserManager<AppUser> users,SignInManager<AppUser> signIn,Access access,LearningService learning,InstitutionService institutions,AccountService accounts,PrivateStorage storage,PracticeService practice,BillingService billing,IConfiguration config,IWebHostEnvironment env):PageModel
{
    [BindProperty(SupportsGet=true),FromRoute]public string Section {get;set;}="home";
    [BindProperty(SupportsGet=true)]public string? Item {get;set;}
    [BindProperty(SupportsGet=true)]public string Search {get;set;}="";
    [BindProperty(SupportsGet=true)]public int PageNumber {get;set;}=1;
    [BindProperty]public string Command {get;set;}="";
    [BindProperty]public FormInput Input {get;set;}=new();
    [TempData]public string? Flash {get;set;}
    public string? Failure {get;set;}
    public AppUser? Actor {get;private set;}
    public string Role {get;private set;}="Student";
    public bool Staff {get;private set;}
    public bool IsDemo=>config.GetValue<bool>("DualPath:SeedDemo")&&!env.IsProduction();
    public bool Public=>PublicSections.Contains(Section);
    public bool AuthPage=>new[]{"signin","signup","reset","reset-password","confirm"}.Contains(Section);
    public bool BillingReady=>billing.Ready;
    public List<Course> Courses {get;private set;}=[];
    public Dictionary<Guid,bool> Availability {get;private set;}=[];
    public HashSet<Guid> Completed {get;private set;}=[];
    public List<Assignment> Assignments {get;private set;}=[];
    public Lesson? Lesson {get;private set;}
    public Submission? Work {get;private set;}
    public List<Submission> Works {get;private set;}=[];
    public List<Reflection> Reflections {get;private set;}=[];
    public List<AssessmentAttempt> Attempts {get;private set;}=[];
    public List<PracticeSession> Practices {get;private set;}=[];
    public List<Notification> Notifications {get;private set;}=[];
    public HashSet<Guid> ManagedCohortIds {get;private set;}=[];
    public List<CohortReport> ReportRows {get;private set;}=[];
    public List<Cohort> Cohorts {get;private set;}=[];
    public Cohort? SelectedCohort {get;private set;}
    public List<Organisation> Organisations {get;private set;}=[];
    public List<Membership> Members {get;private set;}=[];
    public List<Invitation> Invitations {get;private set;}=[];
    public List<Subscription> Subscriptions {get;private set;}=[];
    public List<Announcement> Announcements {get;private set;}=[];
    public List<SupportRequest> Requests {get;private set;}=[];
    public List<ContentPage> PublicContent {get;private set;}=[];
    public ContentPage? Article {get;private set;}
    public List<AuditEvent> Audit {get;private set;}=[];
    public List<OutboxMessage> Emails {get;private set;}=[];
    public int LessonCount=>Courses.Where(c=>c.Published).Sum(c=>c.Lessons.Count(l=>l.Published));
    public int Percentage=>LessonCount==0?0:(int)Math.Round(100.0*Completed.Count/LessonCount);
    public Guid? DefaultCohort=>Cohorts.FirstOrDefault(c=>c.Enrolments.Any(e=>e.UserId==Actor?.Id))?.Id;
    public int CoursePercentage(Course c)=>c.Lessons.Count==0?0:(int)Math.Round(100.0*c.Lessons.Count(l=>Completed.Contains(l.Id))/c.Lessons.Count);
    public bool Available(Course c)=>Availability.GetValueOrDefault(c.Id);
    public static readonly HashSet<string> PublicSections=["home","programmes","programme","schools","resources","article","pricing","about","contact","privacy","terms","signin","signup","reset","reset-password","confirm","error","denied"];
    public string Title=>Titles.GetValueOrDefault(Section,"Your next step");
    private static readonly Dictionary<string,string> Titles=new(){["home"]="Your next chapter, by design",["programmes"]="Programmes",["programme"]="Internship Launchpad",["schools"]="For institutions",["resources"]="Resource library",["article"]="A useful next step",["pricing"]="Plans & pricing",["about"]="Our approach",["contact"]="Talk to us",["signin"]="Welcome back",["signup"]="Find your path",["reset"]="Recover your account",["reset-password"]="Choose a new password",["confirm"]="Confirm your email",["onboarding"]="Your starting point",["dashboard"]="Your dashboard",["learning"]="Your learning path",["lesson"]="Your next lesson",["assessment"]="Knowledge check",["assignment"]="Your practical work",["work"]="Your work portfolio",["reflections"]="Reflection journal",["practice"]="Practice studio",["feedback"]="Instructor feedback",["progress"]="Your progress",["notifications"]="Notifications",["account"]="Account & preferences",["billing"]="Plan & billing",["checkout"]="Choose your plan",["instructor"]="Instructor workspace",["courses"]="Courses & lessons",["author"]="Course editor",["cohorts"]="Your cohorts",["cohort"]="Cohort workspace",["reviews"]="Review queue",["review"]="Review submission",["reports"]="Learning reports",["organisation"]="Institution workspace",["members"]="People & permissions",["orgbilling"]="Institution plan",["admin"]="Platform operations",["content"]="Public website content",["audit"]="Activity & audit",["support"]="Help & support",["accept"]="Accept your invitation",["privacy"]="Privacy",["terms"]="Service terms",["error"]="Something needs attention",["denied"]="Access unavailable",["mailbox"]="Development email outbox"};
    public async Task<IActionResult> OnGetAsync()
    {
        Section=string.IsNullOrWhiteSpace(Section)?"home":Section.ToLowerInvariant();
        if(!Titles.ContainsKey(Section))return NotFound();
        try
        {
            Actor=await users.GetUserAsync(User);
            if(!Public&&Actor==null)return Challenge();
            if(Section=="confirm") {await accounts.Confirm(Request.Query["user"].ToString(),Request.Query["code"].ToString());Flash="Email verified. You can sign in now.";return Redirect("/signin");}
            await Load();return Page();
        }catch(DomainException e){Failure=e.Message;Response.StatusCode=e.Status;SetView();return Page();}
    }
    private void SetView(){ViewData["Title"]=Title;ViewData["Actor"]=Actor;ViewData["Role"]=Role;ViewData["Public"]=Public;ViewData["Staff"]=Staff;ViewData["Section"]=Section;ViewData["Demo"]=IsDemo;}
    private async Task Load()
    {
        PageNumber=Math.Clamp(PageNumber,1,10000);Search=(Search??"").Trim();if(Search.Length>100)Search=Search[..100];
        var query=db.Courses.Include(c=>c.Lessons).AsQueryable();
        if(Actor==null)query=query.Where(c=>c.Published&&c.OrganisationId==null);
        else query=query.Where(c=>(c.Published&&c.OrganisationId==null)||Actor.PlatformAdmin||(c.OrganisationId!=null&&db.Memberships.Any(m=>m.UserId==Actor.Id&&m.Active&&m.OrganisationId==c.OrganisationId&&(c.Published||m.Role!=Roles.Student))));
        Courses=await query.OrderBy(c=>c.Position).ThenBy(c=>c.Title).ToListAsync();foreach(var c in Courses)c.Lessons=c.Lessons.Where(l=>l.Published||Actor?.PlatformAdmin==true).OrderBy(l=>l.Position).ToList();
        PublicContent=await db.ContentPages.Where(x=>x.PublishedAt!=null).OrderBy(x=>x.Title).ToListAsync();
        if(Section=="article"){Article=PublicContent.FirstOrDefault(x=>x.Slug==Item)??throw new DomainException("Resource not found.",404);}
        if(Actor!=null)
        {
            Staff=await access.IsStaff(Actor);
            ManagedCohortIds=(await access.ManagedCohorts(Actor).Select(c=>c.Id).ToListAsync()).ToHashSet();var memberships=await db.Memberships.Where(m=>m.UserId==Actor.Id&&m.Active).ToListAsync();
            foreach(var course in Courses.Where(c=>c.OrganisationId!=null&&memberships.Any(m=>m.OrganisationId==c.OrganisationId&&m.Role!=Roles.Student)))course.Lessons=await db.Lessons.Where(l=>l.CourseId==course.Id).OrderBy(l=>l.Position).ToListAsync();
            Role=Actor.PlatformAdmin?"PlatformAdmin":memberships.Any(m=>m.Role==Roles.InstitutionAdmin)?Roles.InstitutionAdmin:memberships.Any(m=>m.Role==Roles.Instructor)?Roles.Instructor:Roles.Student;
            if(new[]{"instructor","courses","author","reviews","reports"}.Contains(Section)&&!Staff)throw new DomainException("A staff assignment is required for this workspace.",403);
            if(new[]{"admin","content","audit","mailbox"}.Contains(Section))access.RequirePlatformAdmin(Actor);
            if(Section=="mailbox"&&env.IsProduction())throw new DomainException("Development mailbox is unavailable.",404);
            Completed=(await db.Completions.Where(x=>x.UserId==Actor.Id&&db.Lessons.Any(l=>l.Id==x.LessonId&&l.Published&&l.Course.Published)).Select(x=>x.LessonId).ToListAsync()).ToHashSet();
            Completed.IntersectWith(Courses.SelectMany(c=>c.Lessons).Select(l=>l.Id));
            Availability=await access.CourseAvailability(Actor,Courses);
            Assignments=await db.Assignments.Include(a=>a.Course).Where(a=>a.Course.Published&&(a.Course.OrganisationId==null||memberships.Select(m=>m.OrganisationId).Contains(a.Course.OrganisationId.Value))).OrderBy(a=>a.Course.Position).ToListAsync();
            Works=await access.Submissions(Actor).Include(s=>s.User).Include(s=>s.Assignment).Include(s=>s.Files).Include(s=>s.Reviews).AsSplitQuery().OrderByDescending(s=>s.CreatedAt).ThenBy(s=>s.Id).Take(100).ToListAsync();
            Cohorts=await access.Cohorts(Actor).Include(c=>c.Organisation).Include(c=>c.Instructor).Include(c=>c.Enrolments).ThenInclude(e=>e.User).OrderBy(c=>c.Name).ToListAsync();
            Organisations=await access.Organisations(Actor).OrderBy(o=>o.Name).ToListAsync();
            if(new[]{"lesson","assessment","author"}.Contains(Section))
            {
                Lesson=Courses.SelectMany(c=>c.Lessons).FirstOrDefault(l=>l.Id.ToString()==Item||l.Slug==Item)??(Item==null?Courses.SelectMany(c=>c.Lessons).FirstOrDefault(l=>!Completed.Contains(l.Id)):null);
                if(Lesson==null)throw new DomainException("Lesson not found.",404);
                if(Section!="author")await learning.RequireLesson(Actor,Lesson.Id);else await access.RequireCourseAuthor(Actor,Lesson.CourseId);
            }
            if(Section is "assignment" or "feedback" or "review")
            {
                if(Section is "review" or "feedback")Work=Works.FirstOrDefault(s=>s.Id.ToString()==Item)??(Item==null?Works.FirstOrDefault(s=>Section=="review"?s.Status=="Submitted":s.UserId==Actor.Id):null);
                else {var assignment=Assignments.FirstOrDefault(a=>a.Id.ToString()==Item)??(Item==null?Assignments.FirstOrDefault():null);if(assignment==null)throw new DomainException("Assignment not found.",404);Input.Id=assignment.Id;Work=Works.FirstOrDefault(s=>s.AssignmentId==assignment.Id&&s.UserId==Actor.Id);}
                if(Section=="review"&&Work!=null){if(Work.CohortId==null)throw new DomainException("No instructor assignment exists.",403);await access.RequireInstructor(Actor,Work.CohortId.Value);}
            }
            if(Section=="cohort"){SelectedCohort=Cohorts.FirstOrDefault(c=>c.Id.ToString()==Item)??(Item==null?Cohorts.FirstOrDefault():null);if(SelectedCohort==null)throw new DomainException("Cohort not found.",404);if(SelectedCohort.InstructorUserId==Actor.Id)Reflections=await access.Reflections(Actor).Where(r=>r.CohortId==SelectedCohort.Id).OrderByDescending(r=>r.CreatedAt).Take(50).ToListAsync();Announcements=await db.Announcements.Where(a=>a.CohortId==SelectedCohort.Id).OrderByDescending(a=>a.CreatedAt).Take(25).ToListAsync();}
            if(Section is "reflections" or "dashboard")Reflections=await access.Reflections(Actor).Where(r=>r.UserId==Actor.Id).OrderByDescending(r=>r.CreatedAt).Take(30).ToListAsync();
            if(Section is "practice" or "progress"){Attempts=await db.Attempts.Include(a=>a.Lesson).Where(a=>a.UserId==Actor.Id).OrderByDescending(a=>a.CreatedAt).Take(30).ToListAsync();Practices=await db.PracticeSessions.Where(p=>p.UserId==Actor.Id&&p.Status=="Completed").OrderByDescending(p=>p.CreatedAt).Take(20).ToListAsync();}
            Notifications=await db.Notifications.Where(n=>n.UserId==Actor.Id).OrderByDescending(n=>n.CreatedAt).Take(40).ToListAsync();
            Subscriptions=await db.Subscriptions.Where(s=>s.UserId==Actor.Id||Organisations.Select(o=>o.Id).Contains(s.OrganisationId??Guid.Empty)).OrderByDescending(s=>s.CreatedAt).ToListAsync();
            if(Section is "members" or "organisation" or "orgbilling" or "cohorts")
            {
                var orgs=Organisations.Select(o=>o.Id).ToList();Members=await db.Memberships.Include(m=>m.User).Where(m=>orgs.Contains(m.OrganisationId)&&(Search==""||m.User.DisplayName.Contains(Search)||m.User.Email!.Contains(Search))).OrderBy(m=>m.User.DisplayName).Skip((PageNumber-1)*50).Take(50).ToListAsync();Invitations=await db.Invitations.Where(i=>orgs.Contains(i.OrganisationId)&&i.AcceptedAt==null&&i.ExpiresAt>DateTimeOffset.UtcNow).OrderByDescending(i=>i.CreatedAt).Take(50).ToListAsync();
            }
            if(Section=="reports")ReportRows=await new ReportingService(db,access).Rows(Actor,CancellationToken.None);
            if(Section=="support")Requests=await db.SupportRequests.Where(s=>s.UserId==Actor.Id||Actor.PlatformAdmin).OrderByDescending(s=>s.CreatedAt).Take(50).ToListAsync();
            if(Section=="content")PublicContent=await db.ContentPages.OrderBy(c=>c.Title).ToListAsync();
            if(Section=="audit")Audit=await db.Audit.OrderByDescending(a=>a.CreatedAt).Take(100).ToListAsync();
            if(Section=="admin")Emails=await db.Outbox.Where(e=>e.Status=="Failed").OrderBy(e=>e.CreatedAt).Take(30).ToListAsync();
            if(Section=="mailbox")Emails=await db.Outbox.OrderByDescending(e=>e.CreatedAt).Take(50).ToListAsync();
        }
        SetView();
    }
    private AppUser RequireActor()=>Actor??throw new DomainException("Sign in to continue.",401);
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Section=string.IsNullOrWhiteSpace(Section)?"home":Section.ToLowerInvariant();Actor=await users.GetUserAsync(User);
        try
        {
            var publicActions=new Dictionary<string,string>{{"register","signup"},{"login","signin"},{"resend","signin"},{"reset","reset"},{"reset-password","reset-password"},{"contact","contact"}};
            if(publicActions.TryGetValue(Command,out var expectedSection)&&Section!=expectedSection)throw new DomainException("Use the matching form for this action.");
            if(!ModelState.IsValid)throw new DomainException("Check the form fields and try again.");
            switch(Command)
            {
                case "register":if(!Input.Agree)throw new DomainException("Please review and accept the service terms.");await accounts.Register(Input.Name,Input.Email,Input.Password);Flash="Account created. Check your email for a verification link before signing in.";return Redirect("/signin");
                case "login":await accounts.Login(Input.Email,Input.Password,Input.Remember);return LocalRedirect(Url.IsLocalUrl(Input.ReturnUrl)?Input.ReturnUrl:"/dashboard");
                case "resend":await accounts.RequestConfirmation(Input.Email);Flash="If an unverified account exists, a new confirmation link has been queued.";return Redirect("/signin");
                case "reset":await accounts.RequestReset(Input.Email);Flash="If a verified account exists, password reset instructions have been queued for delivery.";return Redirect("/signin");
                case "reset-password":await accounts.ResetPassword(Input.Email,Input.Code,Input.Password);Flash="Password updated. Sign in with your new password.";return Redirect("/signin");
                case "contact":if(!Input.Agree)throw new DomainException("Please confirm you have read the privacy notice.");db.SupportRequests.Add(new SupportRequest{Name=Guard.Text(Input.Name,160,"Name"),Email=Guard.Email(Input.Email),Subject=Guard.Text(Input.Title,200,"Subject"),Topic="Enquiry",Body=Guard.Text(Input.Body,10000,"Message",10)});await db.SaveChangesAsync(ct);Flash="Your enquiry has been saved for the team.";return Redirect("/contact");
            }
            var actor=RequireActor();
            switch(Command)
            {
                case "logout":await signIn.SignOutAsync();return Redirect("/");
                case "onboard":actor.Education=Guard.Text(Input.Education,80,"Education");actor.Goal=Guard.Text(Input.Body,2000,"Goal",5);await users.UpdateAsync(actor);Flash="Your starting goal is saved. Begin with the free foundations module.";return Redirect("/dashboard");
                case "complete":var correct=await learning.CompleteLesson(actor,Input.Id,Input.Answer);Flash=correct?"Correct. Your lesson completion is saved.":"Not quite. Revisit the lesson and try a more specific answer.";return Redirect("/lesson/"+Input.Id);
                case "submission":var work=await learning.SaveSubmission(actor,Input.Id,Input.Title,Input.Body,Input.CohortId,Input.Publish,Input.Version);Flash=Input.Publish?"Your work has been submitted and locked for review.":"Draft saved.";return Redirect("/assignment/"+work.AssignmentId);
                case "upload":if(Input.File==null)throw new DomainException("Choose a file first.");await storage.Upload(actor,Input.Id,Input.File);Flash="Your private attachment has been saved.";return Redirect("/assignment/"+Item);
                case "review":await learning.PublishReview(actor,Input.Id,Input.Research,Input.Clarity,Input.Tone,Input.Body,Input.Decision,Input.Revision);Flash="Feedback published. The learner can now read it.";return Redirect("/reviews");
                case "reflection":await learning.AddReflection(actor,Input.Body,Input.CohortId);Flash="Reflection saved.";return Redirect("/reflections");
                case "practice":await practice.Run(actor,Input.AttemptId,Input.Body,ct);Flash="Practice feedback is ready. AI suggestions are separate from instructor assessment.";return Redirect("/practice");
                case "read":await db.Notifications.Where(n=>n.UserId==actor.Id&&!n.Read).ExecuteUpdateAsync(s=>s.SetProperty(n=>n.Read,true),ct);Flash="Notifications marked as read.";return Redirect("/notifications");
                case "account":actor.DisplayName=Guard.Text(Input.Name,120,"Name");try{TimeZoneInfo.FindSystemTimeZoneById(Input.TimeZone);}catch(TimeZoneNotFoundException){throw new DomainException("Choose a valid time zone.");}actor.TimeZone=Input.TimeZone;actor.AiConsent=Input.AiConsent;actor.Reminders=Input.Reminders;actor.Marketing=Input.Marketing;await users.UpdateAsync(actor);Flash="Account preferences saved.";return Redirect("/account");
                case "password":await accounts.ChangePassword(actor,Input.CurrentPassword,Input.Password);Flash="Password changed. Other sessions will be revalidated.";return Redirect("/account");
                case "institution":var org=await institutions.Create(actor,Input.Name,Input.Email);Flash="Your institution workspace is ready. Add an instructor, then create a cohort.";return Redirect("/organisation");
                case "invite":await institutions.Invite(actor,Input.OrganisationId,Input.CohortId,Input.Email,Input.Role);Flash="Invitation saved and queued for email delivery.";return Redirect("/members");
                case "accept":await institutions.Accept(actor,Input.Code);Flash="Invitation accepted. Your membership is active.";return Redirect("/dashboard");
                case "revoke-invite":await institutions.Revoke(actor,Input.Id);Flash="Invitation revoked.";return Redirect("/members");
                case "seat-limit":await institutions.SetSeatLimit(actor,Input.Id,Input.SeatLimit);Flash="Student seat capacity updated.";return Redirect("/members");
                case "retry-email":access.RequirePlatformAdmin(actor);await db.Outbox.Where(e=>e.Id==Input.Id&&e.Status=="Failed").ExecuteUpdateAsync(u=>u.SetProperty(e=>e.Status,"Pending").SetProperty(e=>e.Attempts,0).SetProperty(e=>e.AvailableAt,DateTimeOffset.UtcNow),ct);learning.Audit(actor,"EmailRetryQueued",Input.Id);await db.SaveChangesAsync(ct);Flash="Failed message queued for retry.";return Redirect("/admin");
                case "content-create":access.RequirePlatformAdmin(actor);var resource=new ContentPage{Slug="resource-"+Guid.NewGuid().ToString("N"),DraftTitle=Guard.Text(Input.Title,200,"Title"),DraftBody=Guard.Text(Input.Body,30000,"Content",20)};db.Add(resource);learning.Audit(actor,"ContentCreated",resource.Id);await db.SaveChangesAsync(ct);Flash="Resource draft created. Review and publish it below.";return Redirect("/content");
                case "membership":await institutions.SetMembership(actor,Input.Id,Input.Role,Input.Active);Flash="Membership updated.";return Redirect("/members");
                case "cohort":await learning.CreateCohort(actor,Input.OrganisationId,Input.CourseId,Input.Name,Input.Instructor);Flash="Cohort created.";return Redirect("/cohorts");
                case "announcement":await learning.PublishAnnouncement(actor,Input.Id,Input.Title,Input.Body);Flash="Announcement published to the cohort.";return Redirect("/cohort/"+Input.Id);
                case "lesson":await learning.SaveLesson(actor,Input.Id,Input.Title,Input.Objective,Input.Body,Input.Question,[Input.Option0,Input.Option1,Input.Option2],Input.Answer,Input.Publish,Input.Version);Flash=Input.Publish?"Lesson version published.":"Lesson draft saved.";return Redirect("/author/"+Input.Id);
                case "course":return await CreateCourse(actor);
                case "add-lesson":await access.RequireCourseAuthor(actor,Input.CourseId);var newLesson=new Lesson{CourseId=Input.CourseId,Slug="lesson-"+Guid.NewGuid().ToString("N"),Title=Guard.Text(Input.Title,200,"Lesson title"),Position=(await db.Lessons.Where(l=>l.CourseId==Input.CourseId).MaxAsync(l=>(int?)l.Position)??0)+1,OptionsJson="[\"\",\"\",\"\"]"};db.Add(newLesson);await db.SaveChangesAsync(ct);Flash="Lesson draft created.";return Redirect("/author/"+newLesson.Id);
                case "checkout":return Redirect(await billing.Checkout(actor,Input.Plan,Input.OrganisationId==Guid.Empty?null:Input.OrganisationId,ct));
                case "portal":return Redirect(await billing.Portal(actor,Input.OrganisationId==Guid.Empty?null:Input.OrganisationId,ct));
                case "support":db.SupportRequests.Add(new SupportRequest{UserId=actor.Id,Email=actor.Email!,Name=actor.DisplayName,Topic=Guard.Text(Input.Topic,40,"Topic"),Subject=Guard.Text(Input.Title,200,"Subject"),Body=Guard.Text(Input.Body,10000,"Message",5)});await db.SaveChangesAsync(ct);Flash="Your support request is saved. You can track it below.";return Redirect("/support");
                case "support-review":access.RequirePlatformAdmin(actor);var ticket=await db.SupportRequests.SingleOrDefaultAsync(x=>x.Id==Input.Id,ct)??throw new DomainException("Request not found.",404);ticket.Response=Guard.Text(Input.Body,10000,"Response");ticket.Status=Input.Decision=="Closed"?"Closed":"Replied";db.Outbox.Add(new OutboxMessage{ToEmail=ticket.Email,Subject="Update on your DualPath support request",Body=ticket.Response});learning.Audit(actor,"SupportResponded",ticket.Id);await db.SaveChangesAsync(ct);Flash="Support response saved and queued.";return Redirect("/support");
                case "content":access.RequirePlatformAdmin(actor);var page=await db.ContentPages.SingleOrDefaultAsync(x=>x.Id==Input.Id,ct)??throw new DomainException("Page not found.",404);if(page.Version!=Input.Version)throw new DomainException("This content changed. Reload before saving.",409);page.DraftTitle=Guard.Text(Input.Title,200,"Title");page.DraftBody=Guard.Text(Input.Body,30000,"Content",20);if(Input.Publish){page.Title=page.DraftTitle;page.PublishedBody=page.DraftBody;page.PublishedAt=DateTimeOffset.UtcNow;}learning.Audit(actor,Input.Publish?"ContentPublished":"ContentDraftSaved",page.Id);await db.SaveChangesAsync(ct);Flash="Content saved.";return Redirect("/content");
                case "export":var data=new{profile=new{actor.DisplayName,actor.Email,actor.Goal,actor.AiConsent},completions=await db.Completions.Where(x=>x.UserId==actor.Id).ToListAsync(ct),submissions=await db.Submissions.Where(x=>x.UserId==actor.Id).Select(x=>new{x.Title,x.Body,x.Status,x.CreatedAt}).ToListAsync(ct),reflections=await db.Reflections.Where(x=>x.UserId==actor.Id).Select(x=>new{x.Body,x.CreatedAt}).ToListAsync(ct)};learning.Audit(actor,"OwnDataExported",Guid.Empty);await db.SaveChangesAsync(ct);return File(JsonSerializer.SerializeToUtf8Bytes(data),"application/json","dualpath-your-records.json");
                case "report-export":return await ExportReport(actor,ct);
                default:throw new DomainException("That action is not supported.");
            }
        }
        catch(DomainException e){Failure=e.Message;Response.StatusCode=e.Status;}
        catch(DbUpdateConcurrencyException){Failure="This record changed in another session. Reload before saving.";Response.StatusCode=409;}
        catch(DbUpdateException){Failure="This change conflicts with an existing record. Reload and try again.";Response.StatusCode=409;}
        catch(HttpRequestException){Failure="An external service is temporarily unavailable. Your saved work is safe.";Response.StatusCode=503;}
        catch(TaskCanceledException)when(!ct.IsCancellationRequested){Failure="The external service took too long. Please try again later.";Response.StatusCode=503;}
        try{await Load();}catch(DomainException e){Failure??=e.Message;Response.StatusCode=e.Status;SetView();}
        return Page();
    }
    private async Task<IActionResult> CreateCourse(AppUser actor)
    {
        if(Input.OrganisationId!=Guid.Empty){if(!await db.Memberships.AnyAsync(m=>m.UserId==actor.Id&&m.OrganisationId==Input.OrganisationId&&m.Active&&m.Role!=Roles.Student))throw new DomainException("A staff membership is required.",403);}else access.RequirePlatformAdmin(actor);
        var c=new Course{OrganisationId=Input.OrganisationId==Guid.Empty?null:Input.OrganisationId,Slug="course-"+Guid.NewGuid().ToString("N"),Title=Guard.Text(Input.Title,160,"Course title"),Summary=Guard.Text(Input.Body,2000,"Summary"),Position=(await db.Courses.MaxAsync(x=>(int?)x.Position)??0)+1};db.Add(c);var l=new Lesson{CourseId=c.Id,Slug="lesson-"+Guid.NewGuid().ToString("N"),Title="New lesson",Position=1,OptionsJson="[\"\",\"\",\"\"]"};db.Add(l);db.Add(new Assignment{CourseId=c.Id,Title=c.Title+" — practical work",Instructions="Apply the learning objective to a real example. Describe your actions, reasoning and next improvement."});await db.SaveChangesAsync();Flash="Course draft created. Write and publish its first lesson.";return Redirect("/author/"+l.Id);
    }
    private async Task<IActionResult> ExportReport(AppUser actor,CancellationToken ct)
    {
        if(!await access.IsStaff(actor))throw new DomainException("Staff access is required.",403);
        var rows=await new ReportingService(db,access).Rows(actor,ct);
        static string Cell(string v)=>"\""+(v.Length>0&&"=+-@\t\r".Contains(v[0])?"'":"")+v.Replace("\"","\"\"")+"\"";
        var sb=new StringBuilder("Cohort,Learner,Completed lessons,Published lessons,Correct attempts,Total attempts,Submitted assignments,Approved assignments\r\n");
        foreach(var r in rows)sb.AppendJoin(',',Cell(r.Cohort),Cell(r.Learner),r.Completed,r.Lessons,r.CorrectAttempts,r.Attempts,r.Submitted,r.Approved).Append("\r\n");
        learning.Audit(actor,"CohortReportExported",Guid.Empty);await db.SaveChangesAsync(ct);return File(Encoding.UTF8.GetBytes(sb.ToString()),"text/csv","dualpath-cohort-report.csv");
    }
}
