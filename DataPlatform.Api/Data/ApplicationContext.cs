using DataPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DataPlatform.Api.Data;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        
    }
    
    public DbSet<User> Users => Set<User>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<Stat> Stats => Set<Stat>();
}