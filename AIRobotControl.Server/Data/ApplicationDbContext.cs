using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Modules.RobotManagement.Domain;

namespace AIRobotControl.Server.Data;

public class ApplicationDbContext : DbContext
{
    private readonly bool _enableSeeding = true;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHostEnvironment? env = null)
        : base(options)
    {
        // Disable seeding when running tests (environment set by TestWebApplicationFactory)
        if (env != null && env.IsEnvironment("Testing"))
        {
            _enableSeeding = false;
        }
    }

    public DbSet<Persona> Personas => Set<Persona>();
    public DbSet<RobotPreset> RobotPresets => Set<RobotPreset>();
    public DbSet<RobotGroup> RobotGroups => Set<RobotGroup>();
    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Battery> Batteries => Set<Battery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    // Owned type configuration for Robot.State (optional)
    modelBuilder.Entity<Robot>().OwnsOne(r => r.State);
    modelBuilder.Entity<Robot>().Navigation(r => r.State).IsRequired(false);

        // Configure Robot relationships
        modelBuilder.Entity<Robot>()
            .HasOne(r => r.RobotGroup)
            .WithMany()
            .HasForeignKey(r => r.RobotGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Robot>()
            .HasOne(r => r.Persona)
            .WithMany(p => p.Robots)
            .HasForeignKey(r => r.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Robot>()
            .HasOne(r => r.RobotPreset)
            .WithMany()
            .HasForeignKey(r => r.RobotPresetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional king/strategist robots within the group
        modelBuilder.Entity<RobotGroup>()
            .HasOne<Robot>()
            .WithMany()
            .HasForeignKey(g => g.RobotKingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RobotGroup>()
            .HasOne<Robot>()
            .WithMany()
            .HasForeignKey(g => g.GroupStrategistId)
            .OnDelete(DeleteBehavior.SetNull);

        // Seed data (simple baseline), unless disabled (e.g., in tests)
        if (_enableSeeding)
        {
            var now = DateTimeOffset.UtcNow;

            modelBuilder.Entity<Persona>().HasData(
                new Persona { Id = 1, Name = "Curious Explorer", Instructions = "Explore and learn.", Tags = "curious,explorer", CreatedAt = now, UpdatedAt = now },
                new Persona { Id = 2, Name = "Helper", Instructions = "Assist users.", Tags = "helpful", CreatedAt = now, UpdatedAt = now }
            );

            modelBuilder.Entity<RobotPreset>().HasData(
                new RobotPreset { Id = 1, Name = "Basic Bot", Instructions = "Default behavior.", MeshScale = 1.0f, Tags = "basic", CreatedAt = now, UpdatedAt = now },
                new RobotPreset { Id = 2, Name = "Heavy Bot", Instructions = "Strong and sturdy.", MeshScale = 1.5f, Tags = "heavy", CreatedAt = now, UpdatedAt = now }
            );

            modelBuilder.Entity<RobotGroup>().HasData(
                new RobotGroup { Id = 1, Name = "Alpha Squad", Instructions = "Coordinate tasks.", RobotKingId = null, GroupStrategistId = null, CreatedAt = now, UpdatedAt = now }
            );

            // Seed some batteries
            modelBuilder.Entity<Battery>().HasData(
                new Battery { Id = 1, X = 10f, Y = 0f, Z = 5f, Energy = 50f, LastRespawnTime = DateTime.UtcNow },
                new Battery { Id = 2, X = -10f, Y = 0f, Z = -5f, Energy = 75f, LastRespawnTime = DateTime.UtcNow },
                new Battery { Id = 3, X = 0f, Y = 0f, Z = 15f, Energy = 100f, LastRespawnTime = DateTime.UtcNow }
            );

            // Seed some robots
            modelBuilder.Entity<Robot>().HasData(
                new Robot 
                { 
                    Id = 1, 
                    PersonaId = 1, 
                    RobotPresetId = 1, 
                    RobotGroupId = 1, 
                    Instructions = "Explorer robot for testing", 
                    Length = 1.5f, 
                    CreatedAt = now, 
                    UpdatedAt = now 
                },
                new Robot 
                { 
                    Id = 2, 
                    PersonaId = 2, 
                    RobotPresetId = 2, 
                    RobotGroupId = 1, 
                    Instructions = "Helper robot for testing", 
                    Length = 1.8f, 
                    CreatedAt = now, 
                    UpdatedAt = now 
                }
            );
            
            // Configure owned entity data for robot states
            modelBuilder.Entity<Robot>().OwnsOne(r => r.State, state =>
            {
                state.HasData(
                    new { RobotId = 1, Health = 100f, Energy = 80f, MaxEnergy = 100f, Happiness = 75, X = 0f, Y = 0f, Z = 0f },
                    new { RobotId = 2, Health = 100f, Energy = 90f, MaxEnergy = 100f, Happiness = 85, X = 5f, Y = 0f, Z = 5f }
                );
            });
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case Persona p when entry.State is EntityState.Added or EntityState.Modified:
                    p.UpdatedAt = now;
                    if (entry.State == EntityState.Added) p.CreatedAt = now;
                    break;
                case RobotPreset rp when entry.State is EntityState.Added or EntityState.Modified:
                    rp.UpdatedAt = now;
                    if (entry.State == EntityState.Added) rp.CreatedAt = now;
                    break;
                case RobotGroup rg when entry.State is EntityState.Added or EntityState.Modified:
                    rg.UpdatedAt = now;
                    if (entry.State == EntityState.Added) rg.CreatedAt = now;
                    break;
                case Robot r when entry.State is EntityState.Added or EntityState.Modified:
                    r.UpdatedAt = now;
                    if (entry.State == EntityState.Added) r.CreatedAt = now;
                    break;
            }
        }
    }
}