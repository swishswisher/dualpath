using System.Text;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class AccountService(AppDb db,UserManager<AppUser> users,SignInManager<AppUser> signIn,IConfiguration config,IWebHostEnvironment env,EmailProvider email)
{
    private string BaseUrl=>config["DualPath:BaseUrl"]!.TrimEnd('/');
    public async Task Register(string name,string address,string password)
    {
        if(env.IsProduction()&&!email.Ready)throw new DomainException("Registration is temporarily unavailable while email delivery is configured.",503);
        address=Guard.Email(address);var u=new AppUser{UserName=address,Email=address,DisplayName=Guard.Text(name,120,"Name")};
        var result=await users.CreateAsync(u,password);if(!result.Succeeded)throw new DomainException(string.Join(" ",result.Errors.Select(x=>x.Description)));
        await QueueConfirmation(u);
    }
    public async Task RequestConfirmation(string address)
    {
        var user=await users.FindByEmailAsync(Guard.Email(address));
        if(user!=null&&!user.EmailConfirmed)await QueueConfirmation(user);
    }
    public async Task QueueConfirmation(AppUser user)
    {
        var token=await users.GenerateEmailConfirmationTokenAsync(user);var code=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        db.Outbox.Add(new OutboxMessage{ToEmail=user.Email!,Subject="Confirm your DualPath account",Body=$"Confirm your email: {BaseUrl}/confirm?user={Uri.EscapeDataString(user.Id)}&code={code}"});await db.SaveChangesAsync();
    }
    public async Task Confirm(string user,string code)
    {
        var u=await users.FindByIdAsync(user)??throw new DomainException("The confirmation link is invalid.");
        string token;try{token=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));}catch(FormatException){throw new DomainException("The confirmation link is invalid.");}
        var result=await users.ConfirmEmailAsync(u,token);if(!result.Succeeded)throw new DomainException("The confirmation link is invalid or expired.");
    }
    public async Task Login(string address,string password,bool remember)
    {
        var result=await signIn.PasswordSignInAsync(Guard.Email(address),password,remember,lockoutOnFailure:true);
        if(!result.Succeeded)throw new DomainException(result.IsLockedOut?"Sign-in is temporarily locked. Try again later.":"Sign-in failed. Check your details and verify your email.",400);
    }
    public async Task RequestReset(string address)
    {
        address=Guard.Email(address);var u=await users.FindByEmailAsync(address);if(u==null||!u.EmailConfirmed)return;
        var token=await users.GeneratePasswordResetTokenAsync(u);var code=WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        db.Outbox.Add(new OutboxMessage{ToEmail=address,Subject="Reset your DualPath password",Body=$"Reset your password: {BaseUrl}/reset-password?email={Uri.EscapeDataString(address)}&code={code}"});await db.SaveChangesAsync();
    }
    public async Task ResetPassword(string address,string code,string password)
    {
        var u=await users.FindByEmailAsync(Guard.Email(address));if(u==null)throw new DomainException("The reset link is invalid or expired.");
        string token;try{token=Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));}catch(FormatException){throw new DomainException("The reset link is invalid or expired.");}
        var r=await users.ResetPasswordAsync(u,token,password);if(!r.Succeeded)throw new DomainException(string.Join(" ",r.Errors.Select(x=>x.Description)));
    }
    public async Task ChangePassword(AppUser actor,string current,string next)
    {var result=await users.ChangePasswordAsync(actor,current,next);if(!result.Succeeded)throw new DomainException(string.Join(" ",result.Errors.Select(x=>x.Description)));await signIn.RefreshSignInAsync(actor);}
}
