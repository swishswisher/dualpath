using System.Net;
using System.Text.RegularExpressions;
using Xunit;
namespace DualPath.Tests;
[Collection("platform")]
public sealed class VisualCaptureTests(PlatformFixture fixture)
{
    [Fact]public async Task ExportRenderedRazorViewsForVisualReview()
    {
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));var folder=Path.Combine(root,"artifacts/ui");Directory.CreateDirectory(folder);
        foreach(var (name,role,route) in new[]{("index","public","/"),("programmes","public","/programmes"),("pricing","public","/pricing"),("signin","public","/signin"),("dashboard","student","/dashboard"),("lesson","student","/lesson"),("work","student","/work"),("reviews","instructor","/reviews"),("reports","instructor","/reports"),("institution","institution","/organisation"),("author","admin","/author")})
        {
            using var client=role=="public"?fixture.Client():await fixture.Login(role+"@demo.dualpath.test");var response=await client.GetAsync(route);Assert.Equal(HttpStatusCode.OK,response.StatusCode);var html=await response.Content.ReadAsStringAsync();
            html=Regex.Replace(html,"<input[^>]*name=\"__RequestVerificationToken\"[^>]*>","");html=html.Replace("<form ","<form inert ");
            await File.WriteAllTextAsync(Path.Combine(folder,name+".html"),html);
        }
        var assets=Path.Combine(root,"src/DualPath.Web/wwwroot");foreach(var file in Directory.EnumerateFiles(assets,"*",SearchOption.AllDirectories)){var relative=Path.GetRelativePath(assets,file);var dest=Path.Combine(folder,relative);Directory.CreateDirectory(Path.GetDirectoryName(dest)!);File.Copy(file,dest,true);}
        await File.WriteAllTextAsync(Path.Combine(folder,"README.txt"),"Rendered HTML snapshots from the actual ASP.NET Razor application with synthetic development records. These snapshots are for visual QA; forms are inert. Run the .NET application for functional use.\n");
    }
}
