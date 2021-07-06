using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStream.Models
{
    public class MSContext : IdentityDbContext<User>
    {
        public MSContext(DbContextOptions<MSContext> options) : base(options) { }
        public DbSet<User> Users;
    }
}
