using eAGM_eRequest_API.Model.DBContext;
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
            builder.Entity<FileUploadModel>()
                   .Property(c => c.CreatedDate)
                   .HasColumnType("datetime");

        }
        public DbSet<FileUploadModel> FileUploadModel { get; set; }
    }
}
