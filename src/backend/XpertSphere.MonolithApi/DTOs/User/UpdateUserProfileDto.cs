using System.ComponentModel.DataAnnotations;

namespace XpertSphere.MonolithApi.DTOs.User;

public class UpdateUserProfileDto
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    // Address Information
    public string? StreetNumber { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? AddressLine2 { get; set; }
    
    // Professional Information
    public int? YearsOfExperience { get; set; }
    public decimal? DesiredSalary { get; set; }
    public DateTime? Availability { get; set; }
    public string? LinkedInProfile { get; set; }
}