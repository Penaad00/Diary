
using Microsoft.EntityFrameworkCore;
using Diary.Models; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Diary.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DiaryEntry> DiaryEntries { get; set; }
    }
}