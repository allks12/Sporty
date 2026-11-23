using Microsoft.AspNetCore.Identity;

namespace Sporty.Data.Identity;

public class ApplicationUser : IdentityUser {
    public string Name {get; set;} = string.Empty;
    public bool IsDeleted {get; set;}
    public DateTime CreatedAtUtc {get; set;}
}
