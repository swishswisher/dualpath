using System.Security.Cryptography;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class InstitutionService(AppDb db, Access access, LearningService learning,IConfiguration config)
{
    public async Task<Organisation> Create(AppUser actor,string name,string email)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await learning.Lock(actor.Id);
        if(await db.Memberships.AnyAsync(m=>m.UserId==actor.Id&&m.Role==Roles.InstitutionAdmin&&m.Active))throw new DomainException("You already manage an institution. Contact support to add another.");
        var org=new Organisation{Name=Guard.Text(name,160,"Institution name"),BillingEmail=Guard.Email(email),SeatLimit=config.GetValue("DualPath:InstitutionSeatLimit",50)};
        db.Add(org);db.Add(new Membership{OrganisationId=org.Id,UserId=actor.Id,Role=Roles.InstitutionAdmin});learning.Audit(actor,"InstitutionCreated",org.Id,org.Id);await db.SaveChangesAsync();await tx.CommitAsync();return org;
    }
    public async Task<string> Invite(AppUser actor,Guid org,Guid? cohort,string email,string role)
    {
        if(!Roles.Valid(role))throw new DomainException("Choose a valid role.");
        await access.RequireOrganisationAdmin(actor,org);
        if(cohort!=null&&!await db.Cohorts.AnyAsync(c=>c.Id==cohort&&c.OrganisationId==org))throw new DomainException("The cohort does not belong to this institution.",403);
        if(cohort!=null&&role!=Roles.Student)throw new DomainException("Cohort invitations are for learners. Invite staff without a cohort.");
        email=Guard.Email(email);
        var token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invite=new Invitation{OrganisationId=org,CohortId=cohort,Email=email,Role=role,TokenHash=Guard.Hash(token),ExpiresAt=DateTimeOffset.UtcNow.AddDays(7)};
        db.Add(invite);var url=config["DualPath:BaseUrl"]!.TrimEnd('/')+"/accept?token="+token;
        db.Outbox.Add(new OutboxMessage{ToEmail=email,Subject="Your invitation to DualPath",Body="You have been invited to an institution workspace. Sign in using this email, then accept the invitation: "+url});
        learning.Audit(actor,"InvitationCreated",invite.Id,org);await db.SaveChangesAsync();return token;
    }
    public async Task Accept(AppUser actor,string token)
    {
        var hash=Guard.Hash(Guard.Text(token,64,"Invitation token",64));
        await using var tx=await db.Database.BeginTransactionAsync();
        var invitation=await db.Invitations.SingleOrDefaultAsync(i=>i.TokenHash==hash)??throw new DomainException("This invitation is invalid or expired.",404);
        await learning.Lock(invitation.OrganisationId.ToString());await db.Entry(invitation).ReloadAsync();
        if(invitation.AcceptedAt!=null||invitation.ExpiresAt<DateTimeOffset.UtcNow)throw new DomainException("This invitation is invalid or expired.",409);
        if(!actor.EmailConfirmed||!string.Equals(actor.Email,invitation.Email,StringComparison.OrdinalIgnoreCase))throw new DomainException("Sign in with the verified email address this invitation was sent to.",403);
        var org=await db.Organisations.SingleAsync(x=>x.Id==invitation.OrganisationId);
        var membership=await db.Memberships.SingleOrDefaultAsync(m=>m.OrganisationId==org.Id&&m.UserId==actor.Id);
        if(membership==null||!membership.Active)
        {
            if(invitation.Role==Roles.Student&&await db.Memberships.CountAsync(m=>m.OrganisationId==org.Id&&m.Active&&m.Role==Roles.Student)>=org.SeatLimit)throw new DomainException("This institution has no available student seats. Ask its administrator for help.",409);
            if(membership==null){membership=new Membership{OrganisationId=org.Id,UserId=actor.Id,Role=invitation.Role};db.Add(membership);}else{membership.Active=true;membership.Role=invitation.Role;}
        }
        else if(membership.Role!=invitation.Role)throw new DomainException("Existing roles cannot be changed through an invitation. Ask your administrator.",409);
        if(invitation.CohortId!=null&&!await db.Enrolments.AnyAsync(e=>e.UserId==actor.Id&&e.CohortId==invitation.CohortId))db.Add(new Enrolment{UserId=actor.Id,CohortId=invitation.CohortId.Value});
        invitation.AcceptedAt=DateTimeOffset.UtcNow;learning.Notify(actor.Id,"Welcome to your institution","Your invitation has been accepted.","/dashboard");learning.Audit(actor,"InvitationAccepted",invitation.Id,org.Id);
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Revoke(AppUser actor,Guid id)
    {
        var invite=await db.Invitations.SingleOrDefaultAsync(i=>i.Id==id)??throw new DomainException("Invitation not found.",404);
        await access.RequireOrganisationAdmin(actor,invite.OrganisationId);invite.ExpiresAt=DateTimeOffset.UtcNow;learning.Audit(actor,"InvitationRevoked",id,invite.OrganisationId);await db.SaveChangesAsync();
    }
    public async Task SetSeatLimit(AppUser actor,Guid id,int limit)
    {
        access.RequirePlatformAdmin(actor);if(limit<0||limit>100000)throw new DomainException("Choose a capacity between 0 and 100,000.");
        await using var tx=await db.Database.BeginTransactionAsync();await learning.Lock(id.ToString());
        var org=await db.Organisations.SingleOrDefaultAsync(o=>o.Id==id)??throw new DomainException("Institution not found.",404);
        if(limit<await db.Memberships.CountAsync(m=>m.OrganisationId==id&&m.Active&&m.Role==Roles.Student))throw new DomainException("Capacity cannot be lower than active student membership.",409);
        org.SeatLimit=limit;learning.Audit(actor,"SeatCapacityUpdated",id,id);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task SetMembership(AppUser actor,Guid id,string role,bool active)
    {
        var m=await db.Memberships.SingleOrDefaultAsync(m=>m.Id==id)??throw new DomainException("Membership not found.",404);await access.RequireOrganisationAdmin(actor,m.OrganisationId);
        if(!Roles.Valid(role))throw new DomainException("Choose a valid role.");
        await using var tx=await db.Database.BeginTransactionAsync();await learning.Lock(m.OrganisationId.ToString());
        if(m.Role==Roles.InstitutionAdmin&&(!active||role!=Roles.InstitutionAdmin)&&await db.Memberships.CountAsync(x=>x.OrganisationId==m.OrganisationId&&x.Active&&x.Role==Roles.InstitutionAdmin)<=1)throw new DomainException("Keep at least one institution administrator.",409);
        if((!m.Active||m.Role!=Roles.Student)&&active&&role==Roles.Student){var limit=await db.Organisations.Where(o=>o.Id==m.OrganisationId).Select(o=>o.SeatLimit).SingleAsync();if(await db.Memberships.CountAsync(x=>x.OrganisationId==m.OrganisationId&&x.Active&&x.Role==Roles.Student)>=limit)throw new DomainException("Student seat limit reached.",409);}
        m.Role=role;m.Active=active;learning.Audit(actor,"MembershipUpdated",m.Id,m.OrganisationId);await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
