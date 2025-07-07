using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace XpertSphere.MonolithApi.Models.Base;

[ComplexType]
public class Address
{
    [MaxLength(10)]
    public string? StreetNumber { get; set; }

    [MaxLength(200)]
    public string? StreetName { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Region { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; } = "France";

    [MaxLength(100)]
    public string? AddressLine2 { get; set; } // Complément d'adresse (Apt, Étage, etc.)

    // Propriété calculée pour l'adresse complète
    [NotMapped]
    public string FullAddress
    {
        get
        {
            var parts = new List<string>();

            // Numéro et nom de rue
            if (!string.IsNullOrWhiteSpace(StreetNumber) && !string.IsNullOrWhiteSpace(StreetName))
            {
                parts.Add($"{StreetNumber} {StreetName}");
            }
            else if (!string.IsNullOrWhiteSpace(StreetName))
            {
                parts.Add(StreetName);
            }

            // Complément d'adresse
            if (!string.IsNullOrWhiteSpace(AddressLine2))
            {
                parts.Add(AddressLine2);
            }

            // Code postal et ville
            if (!string.IsNullOrWhiteSpace(PostalCode) && !string.IsNullOrWhiteSpace(City))
            {
                parts.Add($"{PostalCode} {City}");
            }
            else if (!string.IsNullOrWhiteSpace(City))
            {
                parts.Add(City);
            }

            // Région (si différente du pays)
            if (!string.IsNullOrWhiteSpace(Region) && Region != Country)
            {
                parts.Add(Region);
            }

            // Pays
            if (!string.IsNullOrWhiteSpace(Country))
            {
                parts.Add(Country);
            }

            return string.Join(", ", parts);
        }
    }

    // Méthode pour vérifier si l'adresse est vide
    [NotMapped]
    public bool IsEmpty => string.IsNullOrWhiteSpace(StreetName) && 
                          string.IsNullOrWhiteSpace(City) && 
                          string.IsNullOrWhiteSpace(PostalCode);

    // Méthode pour formater l'adresse sur plusieurs lignes
    [NotMapped]
    public string MultiLineAddress
    {
        get
        {
            var lines = new List<string>();

            // Ligne 1: Numéro et nom de rue
            if (!string.IsNullOrWhiteSpace(StreetNumber) && !string.IsNullOrWhiteSpace(StreetName))
            {
                lines.Add($"{StreetNumber} {StreetName}");
            }
            else if (!string.IsNullOrWhiteSpace(StreetName))
            {
                lines.Add(StreetName);
            }

            // Ligne 2: Complément d'adresse
            if (!string.IsNullOrWhiteSpace(AddressLine2))
            {
                lines.Add(AddressLine2);
            }

            // Ligne 3: Code postal, ville, région
            var cityLine = new List<string>();
            if (!string.IsNullOrWhiteSpace(PostalCode))
                cityLine.Add(PostalCode);
            if (!string.IsNullOrWhiteSpace(City))
                cityLine.Add(City);
            if (!string.IsNullOrWhiteSpace(Region) && Region != Country)
                cityLine.Add(Region);

            if (cityLine.Any())
                lines.Add(string.Join(" ", cityLine));

            // Ligne 4: Pays
            if (!string.IsNullOrWhiteSpace(Country))
                lines.Add(Country);

            return string.Join(Environment.NewLine, lines);
        }
    }

    public override string ToString() => FullAddress;
}
