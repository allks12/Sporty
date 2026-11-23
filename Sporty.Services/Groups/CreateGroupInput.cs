using System.ComponentModel.DataAnnotations;

namespace Sporty.Services.Groups;

//The fields from the "Create group" form that are saved
public class CreateGroupInput {
    [Required(ErrorMessage = "Избери спорт.")]
    public int? SportId {get; set;}

    [Required(ErrorMessage = "Въведи локация.")]
    [StringLength(300)]
    public string? CustomLocation {get; set;}

    [Required(ErrorMessage = "Избери район.")]
    public int? DistrictId {get; set;}

    [Required(ErrorMessage = "Избери дата.")]
    public DateOnly? Date {get; set;}

    [Required(ErrorMessage = "Избери начален час.")]
    public TimeOnly? StartTime {get; set;}

    [Required(ErrorMessage = "Въведи минимален брой.")]
    [Range(2, 100)]
    public int? MinMembers {get; set;}

    [Required(ErrorMessage = "Въведи максимален брой.")]
    [Range(2, 100)]
    public int? MaxMembers {get; set;}
}
