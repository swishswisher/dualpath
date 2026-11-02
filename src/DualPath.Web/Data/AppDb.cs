using DualPath.Web.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Data;
public sealed class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonSnapshot> LessonSnapshots => Set<LessonSnapshot>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Completion> Completions => Set<Completion>();
    public DbSet<AssessmentAttempt> Attempts => Set<AssessmentAttempt>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionSnapshot> SubmissionSnapshots => Set<SubmissionSnapshot>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Reflection> Reflections => Set<Reflection>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<BillingEvent> BillingEvents => Set<BillingEvent>();
    public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<AuditEvent> Audit => Set<AuditEvent>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Membership>().HasIndex(x=>new{x.OrganisationId,x.UserId}).IsUnique();
        b.Entity<Course>().HasIndex(x=>x.Slug).IsUnique();
        b.Entity<Lesson>().HasIndex(x=>x.Slug).IsUnique();
        b.Entity<LessonSnapshot>().HasIndex(x=>new{x.LessonId,x.ContentVersion}).IsUnique();
        b.Entity<Enrolment>().HasIndex(x=>new{x.CohortId,x.UserId}).IsUnique();
        b.Entity<Completion>().HasIndex(x=>new{x.UserId,x.LessonId}).IsUnique();
        b.Entity<Submission>().HasIndex(x=>new{x.AssignmentId,x.UserId}).IsUnique();
        b.Entity<SubmissionSnapshot>().HasIndex(x=>new{x.SubmissionId,x.Revision}).IsUnique();
        b.Entity<Review>().HasIndex(x=>new{x.SubmissionId,x.Revision}).IsUnique();
        b.Entity<Invitation>().HasIndex(x=>x.TokenHash).IsUnique();
        b.Entity<BillingEvent>().HasIndex(x=>x.ProviderEventId).IsUnique();
        b.Entity<ContentPage>().HasIndex(x=>x.Slug).IsUnique();
        b.Entity<Subscription>().HasIndex(x=>x.ProviderId).IsUnique().HasFilter("\"ProviderId\" <> ''");
        b.Entity<Notification>().HasIndex(x=>new{x.UserId,x.Read,x.CreatedAt});
        b.Entity<PracticeSession>().HasIndex(x=>new{x.UserId,x.CreatedAt});
        b.Entity<OutboxMessage>().HasIndex(x=>new{x.Status,x.AvailableAt});
        b.Entity<Cohort>().HasOne(x=>x.Instructor).WithMany().HasForeignKey(x=>x.InstructorUserId);
        b.Entity<Submission>().HasOne(x=>x.User).WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<Course>().HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId);
        b.Entity<Completion>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<AssessmentAttempt>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<Reflection>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<PracticeSession>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<PracticeSession>().HasOne<AssessmentAttempt>().WithMany().HasForeignKey(x=>x.SourceAttemptId);
        b.Entity<Notification>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<Review>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.InstructorUserId);
        b.Entity<Subscription>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<Subscription>().HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId);
        b.Entity<Invitation>().HasOne<Organisation>().WithMany().HasForeignKey(x=>x.OrganisationId);
        b.Entity<Invitation>().HasOne<Cohort>().WithMany().HasForeignKey(x=>x.CohortId);
        b.Entity<SupportRequest>().HasOne<AppUser>().WithMany().HasForeignKey(x=>x.UserId);
        b.Entity<LessonSnapshot>().HasOne<Lesson>().WithMany().HasForeignKey(x=>x.LessonId);
        b.Entity<SubmissionSnapshot>().HasOne<Submission>().WithMany().HasForeignKey(x=>x.SubmissionId);
        foreach(var e in b.Model.GetEntityTypes())
        {
            if(typeof(Entity).IsAssignableFrom(e.ClrType)) b.Entity(e.ClrType).Property(nameof(Entity.Version)).IsConcurrencyToken();
            foreach(var f in e.GetForeignKeys()) f.DeleteBehavior=DeleteBehavior.Restrict;
        }
    }
    public override Task<int> SaveChangesAsync(CancellationToken token=default)
    {
        foreach(var entry in ChangeTracker.Entries<Entity>().Where(x=>x.State==EntityState.Modified)) entry.Entity.Version++;
        return base.SaveChangesAsync(token);
    }
}
