using Microsoft.EntityFrameworkCore;
using NBomber.Sinks.Timescale.Models;

namespace NBomber.Sinks.Timescale.Structures;

public class TimescaleDbContext(string connectionString) : DbContext
{

    public DbSet<PointData> Points { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PointData>().HasIndex(e => e.StatusCodeStatus);
        modelBuilder.Entity<PointData>().HasIndex(e => e.CurrentOperation);
        modelBuilder.Entity<PointData>().HasIndex(e => e.NodeType);
        modelBuilder.Entity<PointData>().HasIndex(e => e.TestSuite);
        modelBuilder.Entity<PointData>().HasIndex(e => e.TestName);
        modelBuilder.Entity<PointData>().HasIndex(e => e.ClusterId);
        modelBuilder.Entity<PointData>().HasIndex(e => e.Scenario);
        modelBuilder.Entity<PointData>().HasIndex(e => e.Step);
        
        base.OnModelCreating(modelBuilder);
    }
}