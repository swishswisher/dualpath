using System.IO.Compression;
using DualPath.Web.Data;
using DualPath.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace DualPath.Web.Services;
public sealed class PrivateStorage(AppDb db, Access access,IConfiguration config,IWebHostEnvironment env,LearningService learning)
{
    private string Root => Path.GetFullPath(config["Storage:Root"]??"App_Data/private",env.ContentRootPath);
    public async Task<PrivateFile> Upload(AppUser actor,Guid submission,IFormFile file)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await learning.Lock(actor.Id);
        var item=await db.Submissions.SingleOrDefaultAsync(x=>x.Id==submission&&x.UserId==actor.Id)??throw new DomainException("Submission not found.",404);
        if(item.Status!="Draft")throw new DomainException("Attach files while the work is a draft.",409);
        if(await db.Set<PrivateFile>().CountAsync(f=>f.SubmissionId==submission)>=5)throw new DomainException("Each assignment can have up to five attachments.",409);
        var used=await db.Set<PrivateFile>().Where(f=>f.Submission.UserId==actor.Id).SumAsync(f=>(long?)f.Length)??0;
        if(used+file.Length>100*1024*1024)throw new DomainException("Your private file allowance is full. Contact support to manage your files.",409);
        var originalName=Guard.Text(Path.GetFileName(file.FileName),200,"File name");
        var max=config.GetValue<long>("Storage:MaximumBytes",10485760);
        if(file.Length<5||file.Length>max)throw new DomainException("Choose a PDF or DOCX no larger than 10 MB.");
        var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
        if(ext is not (".pdf" or ".docx"))throw new DomainException("Only PDF and DOCX files are supported.");
        using var bytes=new MemoryStream();await file.CopyToAsync(bytes);
        var raw=bytes.ToArray();
        if(ext==".pdf"&&!raw.AsSpan(0,5).SequenceEqual("%PDF-"u8))throw new DomainException("The file is not a valid PDF.");
        if(ext==".docx")
        {
            try{using var zip=new ZipArchive(new MemoryStream(raw));if(zip.GetEntry("[Content_Types].xml")==null||zip.GetEntry("word/document.xml")==null||zip.Entries.Sum(e=>e.Length)>50*1024*1024||zip.Entries.Count>1000)throw new InvalidDataException();}
            catch(InvalidDataException){throw new DomainException("The file is not a valid DOCX.");}
        }
        Directory.CreateDirectory(Root);var key=Guid.NewGuid().ToString("N")+ext;var path=Path.Combine(Root,key);
        await File.WriteAllBytesAsync(path,raw);
        var result=new PrivateFile{SubmissionId=submission,StorageKey=key,OriginalName=originalName,Length=raw.Length,ContentType=ext==".pdf"?"application/pdf":"application/vnd.openxmlformats-officedocument.wordprocessingml.document"};
        db.Add(result);try{await db.SaveChangesAsync();await tx.CommitAsync();}catch{File.Delete(path);throw;}return result;
    }
    public async Task<(PrivateFile File,string Path)> Download(AppUser actor,Guid id)
    {
        var file=await db.Set<PrivateFile>().SingleOrDefaultAsync(x=>x.Id==id)??throw new DomainException("File not found.",404);
        if(!await access.Submissions(actor).AnyAsync(s=>s.Id==file.SubmissionId))throw new DomainException("File not found.",404);
        var path=Path.Combine(Root,Path.GetFileName(file.StorageKey));if(!File.Exists(path))throw new DomainException("The file is temporarily unavailable. Contact support.",503);return(file,path);
    }
}
