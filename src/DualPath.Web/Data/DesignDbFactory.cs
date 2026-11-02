using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace DualPath.Web.Data;
public sealed class DesignDbFactory:IDesignTimeDbContextFactory<AppDb>
{
    public AppDb CreateDbContext(string[] args)=>new(new DbContextOptionsBuilder<AppDb>().UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Database")??"Host=localhost;Database=dualpath_design;Username=dualpath").Options);
}
