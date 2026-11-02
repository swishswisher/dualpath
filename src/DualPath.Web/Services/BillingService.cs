using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class BillingService(AppDb db,Access access,LearningService learning,HttpClient http,IConfiguration config)
{
    public bool Ready => !string.IsNullOrWhiteSpace(config["Billing:SecretKey"])&&!string.IsNullOrWhiteSpace(config["Billing:WebhookSecret"]);
    private void RequireProvider()
    {
        if(!Ready)throw new DomainException("Secure billing is not configured yet. No payment has been taken.",503);
        if(config["Billing:SecretKey"]!.StartsWith("sk_live_",StringComparison.Ordinal)&&!config.GetValue<bool>("Billing:AllowLive"))throw new DomainException("Live billing is not enabled by the operator.",503);
    }
    private async Task<JsonDocument> Api(string path,Dictionary<string,string> fields,string? idempotency=null,CancellationToken ct=default)
    {
        RequireProvider();using var request=new HttpRequestMessage(HttpMethod.Post,config["Billing:ApiBase"]!.TrimEnd('/')+path);request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",config["Billing:SecretKey"]);request.Headers.Add("Stripe-Version","2025-06-30.basil");if(idempotency!=null)request.Headers.Add("Idempotency-Key",idempotency);request.Content=new FormUrlEncodedContent(fields);using var response=await http.SendAsync(request,ct);if(!response.IsSuccessStatusCode)throw new DomainException("The billing provider could not complete this request. Your access has not been changed.",503);return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public async Task<string> Checkout(AppUser actor,string plan,Guid? org,CancellationToken ct)
    {
        RequireProvider();if(plan is not ("Independent" or "Guided" or "Institution"))throw new DomainException("Choose a valid plan.");if((org!=null)!=(plan=="Institution"))throw new DomainException("Choose the matching personal or institution plan.");if(org!=null)await access.RequireOrganisationAdmin(actor,org.Value);
        var price=config[$"Billing:{plan}PriceId"];if(string.IsNullOrWhiteSpace(price))throw new DomainException("This plan’s billing price is not configured.",503);
        Subscription sub;
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            await learning.Lock(org?.ToString()??actor.Id);
            if(await db.Subscriptions.AnyAsync(s=>s.Status=="Active"&&(org!=null?s.OrganisationId==org:s.UserId==actor.Id&&s.OrganisationId==null)))throw new DomainException("Manage your current subscription in the secure billing portal.",409);
            var cutoff=DateTimeOffset.UtcNow.AddHours(-20);
            sub=await db.Subscriptions.FirstOrDefaultAsync(s=>s.UserId==actor.Id&&s.OrganisationId==org&&s.Plan==plan&&s.Status=="Pending"&&s.CreatedAt>cutoff)??new Subscription{UserId=actor.Id,OrganisationId=org,Plan=plan};
            if(db.Entry(sub).State==EntityState.Detached)db.Add(sub);await db.SaveChangesAsync();await tx.CommitAsync();
        }
        var origin=config["DualPath:BaseUrl"]!.TrimEnd('/');
        using var json=await Api("/v1/checkout/sessions",new(){["mode"]="subscription",["line_items[0][price]"]=price,["line_items[0][quantity]"]="1",["customer_email"]=actor.Email!,["success_url"]=origin+"/billing?checkout=returned",["cancel_url"]=origin+"/billing?checkout=cancelled",["subscription_data[metadata][dualpath_id]"]=sub.Id.ToString(),["metadata[dualpath_id]"]=sub.Id.ToString()},"checkout-"+sub.Id,ct);
        return ProviderUrl(json.RootElement.GetProperty("url").GetString(),"checkout.stripe.com");
    }
    public async Task<string> Portal(AppUser actor,Guid? org,CancellationToken ct)
    {
        if(org!=null)await access.RequireOrganisationAdmin(actor,org.Value);
        var sub=await db.Subscriptions.Where(s=>(org!=null?s.OrganisationId==org:s.UserId==actor.Id&&s.OrganisationId==null)&&s.CustomerId!=""&&!s.DevelopmentOnly).OrderByDescending(s=>s.CreatedAt).FirstOrDefaultAsync()??throw new DomainException("There is no provider-managed subscription for this account.");
        using var json=await Api("/v1/billing_portal/sessions",new(){["customer"]=sub.CustomerId,["return_url"]=config["DualPath:BaseUrl"]!.TrimEnd('/')+"/billing"},ct:ct);return ProviderUrl(json.RootElement.GetProperty("url").GetString(),"billing.stripe.com");
    }
    private static string ProviderUrl(string? value,string host)
    {if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||uri.Scheme!="https"||uri.Host!=host)throw new DomainException("Unexpected billing redirect. Contact support.",503);return value!;}
    public static void VerifySignature(string payload,string header,string secret,DateTimeOffset now)
    {
        if(string.IsNullOrWhiteSpace(secret))throw new DomainException("Webhook is not configured.",503);
        var parts=header.Split(',').Select(x=>x.Split('=',2)).Where(x=>x.Length==2).ToList();var stamp=parts.FirstOrDefault(x=>x[0]=="t")?.ElementAtOrDefault(1);
        if(!long.TryParse(stamp,out var timestamp)||Math.Abs(now.ToUnixTimeSeconds()-timestamp)>300)throw new DomainException("Invalid webhook signature.",400);
        var expected=HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(stamp+"."+payload));
        var valid=false;foreach(var p in parts.Where(x=>x[0]=="v1")){try{if(CryptographicOperations.FixedTimeEquals(expected,Convert.FromHexString(p[1])))valid=true;}catch(FormatException){}}
        if(!valid)throw new DomainException("Invalid webhook signature.",400);
    }
    public async Task<bool> HandleWebhook(string payload,string signature)
    {
        VerifySignature(payload,signature,config["Billing:WebhookSecret"]??"",DateTimeOffset.UtcNow);
        using var json=JsonDocument.Parse(payload);var root=json.RootElement;var id=root.GetProperty("id").GetString()??throw new DomainException("Missing event ID.");var type=root.GetProperty("type").GetString()??"";var at=DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("created").GetInt64());
        await using var tx=await db.Database.BeginTransactionAsync();await learning.Lock("billing-events");if(await db.BillingEvents.AnyAsync(x=>x.ProviderEventId==id))return false;
        var data=root.GetProperty("data").GetProperty("object");
        if(type is "customer.subscription.created" or "customer.subscription.updated" or "customer.subscription.deleted")
        {
            var providerId=data.GetProperty("id").GetString()!;
            var sub=await db.Subscriptions.SingleOrDefaultAsync(s=>s.ProviderId==providerId);
            if(sub==null&&data.TryGetProperty("metadata",out var meta)&&meta.TryGetProperty("dualpath_id",out var local)&&Guid.TryParse(local.GetString(),out var localId))sub=await db.Subscriptions.SingleOrDefaultAsync(s=>s.Id==localId);
            if(sub!=null)
            {
                if(sub.ProviderId!=""&&sub.ProviderId!=providerId)throw new DomainException("Subscription reference conflict.",409);
                if((sub.Status!="Cancelled"||type.EndsWith("deleted",StringComparison.Ordinal))&&(sub.LastProviderEventAt==null||at>=sub.LastProviderEventAt))
                {
                    sub.ProviderId=providerId;sub.CustomerId=data.GetProperty("customer").GetString()??"";
                    var providerStatus=data.GetProperty("status").GetString();sub.Status=type.EndsWith("deleted",StringComparison.Ordinal)?"Cancelled":providerStatus is "active" or "trialing"?"Active":providerStatus=="past_due"?"Past due":"Inactive";
                    sub.CancelAtPeriodEnd=data.TryGetProperty("cancel_at_period_end",out var cap)&&cap.GetBoolean();
                    if(data.TryGetProperty("current_period_end",out var period))sub.PaidThrough=DateTimeOffset.FromUnixTimeSeconds(period.GetInt64());
                    else if(data.TryGetProperty("items",out var items)){var values=items.GetProperty("data").EnumerateArray().Where(x=>x.TryGetProperty("current_period_end",out _)).Select(x=>x.GetProperty("current_period_end").GetInt64()).ToList();if(values.Count>0)sub.PaidThrough=DateTimeOffset.FromUnixTimeSeconds(values.Min());}
                    sub.LastProviderEventAt=at;
                    db.Notifications.Add(new Notification{UserId=sub.UserId,Title="Subscription updated",Body="Your plan status is now "+sub.Status+".",Url="/billing"});
                }
            }
        }
        db.BillingEvents.Add(new BillingEvent{ProviderEventId=Guard.Text(id,160,"Event ID"),Type=Guard.Text(type,100,"Event type")});await db.SaveChangesAsync();await tx.CommitAsync();return true;
    }
}
