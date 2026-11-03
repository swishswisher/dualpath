using System.Net;
using System.Text;
using System.Text.Json;
using DualPath.Web.Services;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace DualPath.Tests;
public sealed class ProviderTests
{
    private sealed class Handler(Func<HttpRequestMessage,Task<HttpResponseMessage>> run):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)=>run(request);}
    [Fact]public async Task PracticeSendsOnlyExplicitAnswerWithStorageDisabled()
    {
        string payload="";string? auth=null;
        var client=new HttpClient(new Handler(async request=>{payload=await request.Content!.ReadAsStringAsync();auth=request.Headers.Authorization?.ToString();return new(HttpStatusCode.OK){Content=new StringContent("{\"status\":\"completed\",\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"Use one specific example and a clearer next action.\"}]}]}")};}));
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Practice:ApiKey"]="synthetic-test-key",["Practice:Model"]="configured-model",["Practice:ApiBase"]="https://api.openai.com"}).Build();
        var feedback=await new PracticeProvider(client,config).Generate("What is one useful next action?","Research two teams and write one question for each.",default);
        using var json=JsonDocument.Parse(payload);Assert.False(json.RootElement.GetProperty("store").GetBoolean());Assert.Equal(700,json.RootElement.GetProperty("max_output_tokens").GetInt32());Assert.Equal("Bearer synthetic-test-key",auth);Assert.Contains("specific example",feedback);Assert.DoesNotContain("@demo",payload);Assert.DoesNotContain("reflection",json.RootElement.GetProperty("input").GetString());
    }
    [Fact]public async Task FailedProviderNeverReturnsSyntheticSuccess()
    {
        var client=new HttpClient(new Handler(_=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests))));var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Practice:ApiKey"]="synthetic-test-key",["Practice:Model"]="configured-model",["Practice:ApiBase"]="https://api.openai.com"}).Build();Assert.Equal(503,(await Assert.ThrowsAsync<DomainException>(()=>new PracticeProvider(client,config).Generate("Q","Answer",default))).Status);
    }
}
