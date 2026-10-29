using Microsoft.EntityFrameworkCore;
using Server.Data;

namespace Server.Storage;

public class GameDbContext : DbContext
{
    public DbSet<GameMatch> Matches {get; set;}
    public DbSet<User> Users {get; set;}
    
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }
}
