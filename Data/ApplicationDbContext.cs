﻿
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
        public DbSet<Genre> Genres { get; set; }

        public DbSet<FavoriteEntry> FavoriteEntries { get; set; }

        public DbSet<Comment> Comments { get; set; }
        


    }
}