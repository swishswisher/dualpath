using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DualPath.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RelationalIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_UserId",
                table: "SupportRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_OrganisationId",
                table: "Subscriptions",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_InstructorUserId",
                table: "Reviews",
                column: "InstructorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reflections_UserId",
                table: "Reflections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_SourceAttemptId",
                table: "PracticeSessions",
                column: "SourceAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CohortId",
                table: "Invitations",
                column: "CohortId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_OrganisationId",
                table: "Invitations",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OrganisationId",
                table: "Courses",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId",
                table: "Attempts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_AspNetUsers_UserId",
                table: "Attempts",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Completions_AspNetUsers_UserId",
                table: "Completions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Organisations_OrganisationId",
                table: "Courses",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Cohorts_CohortId",
                table: "Invitations",
                column: "CohortId",
                principalTable: "Cohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Organisations_OrganisationId",
                table: "Invitations",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LessonSnapshots_Lessons_LessonId",
                table: "LessonSnapshots",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeSessions_AspNetUsers_UserId",
                table: "PracticeSessions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeSessions_Attempts_SourceAttemptId",
                table: "PracticeSessions",
                column: "SourceAttemptId",
                principalTable: "Attempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reflections_AspNetUsers_UserId",
                table: "Reflections",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_InstructorUserId",
                table: "Reviews",
                column: "InstructorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmissionSnapshots_Submissions_SubmissionId",
                table: "SubmissionSnapshots",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserId",
                table: "Subscriptions",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_Organisations_OrganisationId",
                table: "Subscriptions",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupportRequests_AspNetUsers_UserId",
                table: "SupportRequests",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_AspNetUsers_UserId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Completions_AspNetUsers_UserId",
                table: "Completions");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Organisations_OrganisationId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Cohorts_CohortId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Organisations_OrganisationId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_LessonSnapshots_Lessons_LessonId",
                table: "LessonSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_AspNetUsers_UserId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_PracticeSessions_AspNetUsers_UserId",
                table: "PracticeSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_PracticeSessions_Attempts_SourceAttemptId",
                table: "PracticeSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Reflections_AspNetUsers_UserId",
                table: "Reflections");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_InstructorUserId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_SubmissionSnapshots_Submissions_SubmissionId",
                table: "SubmissionSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_AspNetUsers_UserId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_Organisations_OrganisationId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupportRequests_AspNetUsers_UserId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_SupportRequests_UserId",
                table: "SupportRequests");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_OrganisationId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_InstructorUserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reflections_UserId",
                table: "Reflections");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_SourceAttemptId",
                table: "PracticeSessions");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_CohortId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_OrganisationId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Courses_OrganisationId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId",
                table: "Attempts");
        }
    }
}
