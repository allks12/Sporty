using Microsoft.EntityFrameworkCore;

namespace Sporty.Data.Contexts;

public class SportyDbContext : DbContext {
    public SportyDbContext(DbContextOptions<SportyDbContext> options)
        : base(options) {

    }

    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
}
