using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Sporty.Data.Identity;
using Sporty.Data.Entities;

namespace Sporty.Data.Contexts;

public class SportyDbContext : IdentityDbContext<ApplicationUser> {
    public SportyDbContext(DbContextOptions<SportyDbContext> options)
        : base(options) {

    }

    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("btree_gist");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SportyDbContext).Assembly);
    }
}
