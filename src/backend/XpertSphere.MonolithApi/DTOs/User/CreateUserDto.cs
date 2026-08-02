using System.ComponentModel.DataAnnotations;
using XpertSphere.MonolithApi.Models;

namespace XpertSphere.MonolithApi.DTOs.User;

public class CreateUserDto
{
    [Required(ErrorMessage = "Le prénom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [MaxLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    [MaxLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
    public required string Email { get; set; }

    [MaxLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
    public string? PhoneNumber { get; set; }

    // For internal users
    public Guid? OrganizationId { get; set; }

    [MaxLength(50, ErrorMessage = "Le matricule ne peut pas dépasser 50 caractères")]
    public string? EmployeeId { get; set; }

    [MaxLength(100, ErrorMessage = "Le département ne peut pas dépasser 100 caractères")]
    public string? Department { get; set; }

    public DateTime? HireDate { get; set; }

    // For candidates
    [MaxLength(255, ErrorMessage = "Le profil LinkedIn ne peut pas dépasser 255 caractères")]
    public string? LinkedInProfile { get; set; }

    public string? Skills { get; set; }

    public int? YearsOfExperience { get; set; }

    public List<Training>? Trainings { get; set; } = [];

    public List<Experience>? Experiences { get; set; } = [];

    public decimal? DesiredSalary { get; set; }

    public DateTime? Availability { get; set; }

    [MaxLength(500, ErrorMessage = "Le chemin du CV ne peut pas dépasser 500 caractères")]
    public string? CvPath { get; set; }

    // Communication preferences
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;

    [MaxLength(20, ErrorMessage = "La langue préférée ne peut pas dépasser 20 caractères")]
    public string? PreferredLanguage { get; set; } = "fr";

    [MaxLength(50, ErrorMessage = "Le fuseau horaire ne peut pas dépasser 50 caractères")]
    public string? TimeZone { get; set; } = "UTC";

    // Consent
    public DateTime? ConsentGivenAt { get; set; }

    // Authentication & Security
    [MaxLength(255, ErrorMessage = "L'identifiant externe ne peut pas dépasser 255 caractères")]
    public string? ExternalId { get; set; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    public string? Password { get; set; }

    public bool EmailConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;

    // Address
    public AddressDto? Address { get; set; }
}

public class AddressDto
{
    [MaxLength(10, ErrorMessage = "Le numéro de rue ne peut pas dépasser 10 caractères")]
    public string? StreetNumber { get; set; }

    [MaxLength(200, ErrorMessage = "Le nom de rue ne peut pas dépasser 200 caractères")]
    public string? StreetName { get; set; }

    [MaxLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
    public string? City { get; set; }

    [MaxLength(20, ErrorMessage = "Le code postal ne peut pas dépasser 20 caractères")]
    public string? PostalCode { get; set; }

    [MaxLength(100, ErrorMessage = "La région ne peut pas dépasser 100 caractères")]
    public string? Region { get; set; }

    [MaxLength(100, ErrorMessage = "Le pays ne peut pas dépasser 100 caractères")]
    public string? Country { get; set; } = "France";

    [MaxLength(100, ErrorMessage = "Le complément d'adresse ne peut pas dépasser 100 caractères")]
    public string? AddressLine2 { get; set; }
}