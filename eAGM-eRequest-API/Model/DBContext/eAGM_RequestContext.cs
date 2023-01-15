using Microsoft.EntityFrameworkCore;

namespace Models.DBContext
{
    public class eAGM_RequestContext : DbContext
    {

        public eAGM_RequestContext(DbContextOptions<eAGM_RequestContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UploadFile>();

        }
 
        public DbSet<UploadFile> UploadFile { get; set; }
    }
}
