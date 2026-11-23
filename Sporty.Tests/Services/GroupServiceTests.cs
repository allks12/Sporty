using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Sporty.Data.Contexts;
using Sporty.Data.Entities;
using Sporty.Services.Common;
using Sporty.Services.Groups;
using Sporty.Tests.Infrastructure;

namespace Sporty.Tests.Services;

public class GroupServiceTests {
    private const string Organizer = "organizer-id";
    private const string OtherUser = "other-id";

    private static readonly DateOnly Today = DateOnly.FromDateTime(SportyWebApplicationFactory.Now.DateTime);

    private static SportyDbContext CreateDb() {
        var options = new DbContextOptionsBuilder<SportyDbContext>()
            .UseInMemoryDatabase($"group-service-tests-{Guid.NewGuid()}")
            .Options;

        var db = new SportyDbContext(options);
        db.Database.EnsureCreated(); //adds the seeded sports and districts
        return db;
    }

    private static GroupService CreateService(SportyDbContext db) =>
        new(db, new FakeTimeProvider(SportyWebApplicationFactory.Now));

    private static CreateGroupInput ValidInput() => new() {
        SportId = 1,
        CustomLocation = "  South Park, basketball court ",
        DistrictId = 9,
        Date = Today.AddDays(2),
        StartTime = new TimeOnly(18, 30),
        MinMembers = 4,
        MaxMembers = 10
    };

    [Fact]
    public async Task Create_SavesOpenGroup_WithTheOrganizerAsMember() {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.CreateAsync(Organizer, ValidInput());

        Assert.True(result.Succeeded);
        var group = await db.Groups.Include(g => g.Members).SingleAsync();
        Assert.Equal(Organizer, group.OrganizerId);
        Assert.Equal(GroupStatus.Open, group.Status);
        Assert.Equal("South Park, basketball court", group.CustomLocation);
        Assert.Equal(Today.AddDays(2).ToDateTime(new TimeOnly(18, 30)), group.StartAt);
        var member = Assert.Single(group.Members);
        Assert.Equal(Organizer, member.UserId);
        Assert.Equal(ParticipantStatus.Registered, member.ParticipantStatus);
    }

    [Fact]
    public async Task Create_MaxBelowMin_IsRejected() {
        using var db = CreateDb();
        var service = CreateService(db);
        var input = ValidInput();
        input.MinMembers = 8;
        input.MaxMembers = 5;

        var result = await service.CreateAsync(Organizer, input);

        Assert.Equal(ErrorCode.Validation, result.ErrorCode);
    }

    [Fact]
    public async Task ListOrganizedBy_ReturnsOnlyTheUsersGroups() {
        using var db = CreateDb();
        var service = CreateService(db);
        await service.CreateAsync(Organizer, ValidInput());
        await service.CreateAsync(OtherUser, ValidInput());

        var groups = await service.ListByOrganizerAsync(Organizer);

        Assert.Equal(Organizer, Assert.Single(groups).OrganizerId);
    }

    [Fact]
    public async Task Delete_ByOrganizer_RemovesGroupAndMembers() {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateAsync(Organizer, ValidInput());

        var result = await service.DeleteAsync(created.Value, Organizer, isAdmin: false);

        Assert.True(result.Succeeded);
        Assert.Empty(db.Groups);
        Assert.Empty(db.GroupMembers);
    }

    [Fact]
    public async Task Delete_ByAnotherUser_IsForbidden() {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateAsync(Organizer, ValidInput());

        var result = await service.DeleteAsync(created.Value, OtherUser, isAdmin: false);

        Assert.Equal(ErrorCode.Forbidden, result.ErrorCode);
        Assert.Single(db.Groups);
    }

    [Fact]
    public async Task Delete_ByAdmin_RemovesAnyGroup() {
        using var db = CreateDb();
        var service = CreateService(db);
        var created = await service.CreateAsync(Organizer, ValidInput());

        var result = await service.DeleteAsync(created.Value, "admin-id", isAdmin: true);

        Assert.True(result.Succeeded);
        Assert.Empty(db.Groups);
    }

    [Fact]
    public async Task Delete_UnknownGroup_IsNotFound() {
        using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.DeleteAsync(42, Organizer, isAdmin: false);

        Assert.Equal(ErrorCode.NotFound, result.ErrorCode);
    }
}
