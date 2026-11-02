using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DualPath.Web.Domain;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    [ConcurrencyCheck] public long Version { get; set; }
}
public sealed class AppUser : IdentityUser
{
    [MaxLength(120)] public string DisplayName { get; set; } = "";
    [MaxLength(100)] public string TimeZone { get; set; } = "UTC";
    [MaxLength(80)] public string Education { get; set; } = "";
    [MaxLength(2000)] public string Goal { get; set; } = "";
    public bool AiConsent { get; set; }
    public bool Reminders { get; set; } = true;
    public bool Marketing { get; set; }
    public bool PlatformAdmin { get; set; }
}
public static class Roles
{
    public const string Student = "Student", Instructor = "Instructor", InstitutionAdmin = "InstitutionAdmin";
    public static bool Valid(string role) => role is Student or Instructor or InstitutionAdmin;
}
public sealed class Organisation : Entity
{
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(200)] public string BillingEmail { get; set; } = "";
    public int SeatLimit { get; set; } = 50;
}
public sealed class Membership : Entity
{
    public Guid OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    [MaxLength(40)] public string Role { get; set; } = Roles.Student;
    public bool Active { get; set; } = true;
}
public sealed class Course : Entity
{
    public Guid? OrganisationId { get; set; }
    [MaxLength(120)] public string Slug { get; set; } = "";
    [MaxLength(160)] public string Title { get; set; } = "";
    [MaxLength(2000)] public string Summary { get; set; } = "";
    public int Position { get; set; }
    public bool Published { get; set; }
    public int ContentVersion { get; set; } = 1;
    public List<Lesson> Lessons { get; set; } = [];
}
public sealed class Lesson : Entity
{
    [MaxLength(40000)] public string DraftJson { get; set; } = "";
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [MaxLength(160)] public string Slug { get; set; } = "";
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Objective { get; set; } = "";
    [MaxLength(30000)] public string Body { get; set; } = "";
    public int Position { get; set; }
    public bool Published { get; set; }
    public int ContentVersion { get; set; } = 1;
    [MaxLength(1000)] public string Question { get; set; } = "";
    [MaxLength(5000)] public string OptionsJson { get; set; } = "[]";
    public int CorrectOption { get; set; }
}
public sealed class LessonSnapshot : Entity
{
    public Guid LessonId { get; set; }
    public int ContentVersion { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(30000)] public string Body { get; set; } = "";
    [MaxLength(5000)] public string AssessmentJson { get; set; } = "";
}
public sealed class Cohort : Entity
{
    public Guid OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(450)] public string InstructorUserId { get; set; } = "";
    public AppUser Instructor { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = [];
}
public sealed class Enrolment : Entity
{
    public Guid CohortId { get; set; }
    public Cohort Cohort { get; set; } = null!;
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
}
public sealed class Completion : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int LessonVersion { get; set; }
}
public sealed class AssessmentAttempt : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public int LessonVersion { get; set; }
    public int Answer { get; set; }
    public bool Correct { get; set; }
}
public sealed class Assignment : Entity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(10000)] public string Instructions { get; set; } = "";
}
public sealed class Submission : Entity
{
    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    [MaxLength(450)] public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(20000)] public string Body { get; set; } = "";
    [MaxLength(40)] public string Status { get; set; } = "Draft";
    public int Revision { get; set; } = 1;
    public DateTimeOffset? SubmittedAt { get; set; }
    public List<Review> Reviews { get; set; } = [];
    public List<PrivateFile> Files { get; set; } = [];
}
public sealed class SubmissionSnapshot : Entity
{
    public Guid SubmissionId { get; set; }
    public int Revision { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(20000)] public string Body { get; set; } = "";
}
public sealed class Review : Entity
{
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;
    [MaxLength(450)] public string InstructorUserId { get; set; } = "";
    public int Revision { get; set; }
    public int Research { get; set; }
    public int Clarity { get; set; }
    public int Tone { get; set; }
    [MaxLength(10000)] public string Feedback { get; set; } = "";
    [MaxLength(40)] public string Decision { get; set; } = "Approved";
}
public sealed class Reflection : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public Guid? CohortId { get; set; }
    public Cohort? Cohort { get; set; }
    [MaxLength(8000)] public string Body { get; set; } = "";
}
public sealed class PracticeSession : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public Guid SourceAttemptId { get; set; }
    [MaxLength(5000)] public string Answer { get; set; } = "";
    [MaxLength(10000)] public string Feedback { get; set; } = "";
    [MaxLength(40)] public string Status { get; set; } = "Reserved";
}
public sealed class Notification : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Body { get; set; } = "";
    [MaxLength(200)] public string Url { get; set; } = "/dashboard";
    public bool Read { get; set; }
}
public sealed class Invitation : Entity
{
    public Guid OrganisationId { get; set; }
    public Guid? CohortId { get; set; }
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(40)] public string Role { get; set; } = Roles.Student;
    [MaxLength(64)] public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}
public sealed class Subscription : Entity
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    public Guid? OrganisationId { get; set; }
    [MaxLength(30)] public string Plan { get; set; } = "Independent";
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    [MaxLength(160)] public string ProviderId { get; set; } = "";
    [MaxLength(160)] public string CustomerId { get; set; } = "";
    public DateTimeOffset? PaidThrough { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTimeOffset? LastProviderEventAt { get; set; }
    public bool DevelopmentOnly { get; set; }
}
public sealed class BillingEvent : Entity
{
    [MaxLength(160)] public string ProviderEventId { get; set; } = "";
    [MaxLength(100)] public string Type { get; set; } = "";
}
public sealed class SupportRequest : Entity
{
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(200)] public string Email { get; set; } = "";
    [MaxLength(160)] public string Name { get; set; } = "";
    [MaxLength(40)] public string Topic { get; set; } = "";
    [MaxLength(200)] public string Subject { get; set; } = "";
    [MaxLength(10000)] public string Body { get; set; } = "";
    [MaxLength(40)] public string Status { get; set; } = "Open";
    [MaxLength(10000)] public string Response { get; set; } = "";
}
public sealed class ContentPage : Entity
{
    [MaxLength(200)] public string DraftTitle { get; set; } = "";
    [MaxLength(120)] public string Slug { get; set; } = "";
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(30000)] public string DraftBody { get; set; } = "";
    [MaxLength(30000)] public string PublishedBody { get; set; } = "";
    public DateTimeOffset? PublishedAt { get; set; }
}
public sealed class Announcement : Entity
{
    public Guid CohortId { get; set; }
    public Cohort Cohort { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(6000)] public string Body { get; set; } = "";
}
public sealed class OutboxMessage : Entity
{
    [MaxLength(200)] public string ToEmail { get; set; } = "";
    [MaxLength(200)] public string Subject { get; set; } = "";
    [MaxLength(12000)] public string Body { get; set; } = "";
    [MaxLength(40)] public string Status { get; set; } = "Pending";
    public int Attempts { get; set; }
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeaseUntil { get; set; }
    public Guid? LeaseOwner { get; set; }
}
public sealed class AuditEvent : Entity
{
    [MaxLength(450)] public string ActorUserId { get; set; } = "";
    public Guid? OrganisationId { get; set; }
    [MaxLength(80)] public string Action { get; set; } = "";
    [MaxLength(160)] public string Target { get; set; } = "";
}

public sealed class PrivateFile : Entity
{
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;
    public string OriginalName { get; set; } = "";
    public string StorageKey { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
}

public sealed record LessonDraftData(string Title,string Objective,string Body,string Question,string[] Options,int Answer);
