using Microsoft.AspNetCore.Identity;

namespace XpertSphere.MonolithApi.Utils;

/// <summary>
/// Traduction en français des messages natifs d'ASP.NET Core Identity (<see cref="IdentityErrorDescriber"/>).
/// ASP.NET Core Identity ne fournit pas de ressources satellites françaises pour ces textes (câblés
/// en dur en anglais dans le framework) : cette classe surcharge chaque méthode virtuelle de la
/// classe de base pour renvoyer un texte français en dur, indépendamment de la culture de la requête
/// (voir .claude/specifications/localize-identity-error-messages.md).
/// Enregistrée via <c>.AddErrorDescriber&lt;FrenchIdentityErrorDescriber&gt;()</c> dans
/// <see cref="XpertSphere.MonolithApi.Extensions.SecurityExtensions.AddSecurity"/>.
/// </summary>
/// <remarks>
/// Exhaustivité vérifiée par réflexion contre la version du SDK réellement utilisée (9.0.7) :
/// 22 méthodes virtuelles publiques déclarées par <see cref="IdentityErrorDescriber"/>, toutes
/// surchargées ci-dessous (voir <c>FrenchIdentityErrorDescriberTests</c>).
/// </remarks>
public class FrenchIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError()
    {
        return new IdentityError
        {
            Code = nameof(DefaultError),
            Description = "Une erreur inconnue est survenue."
        };
    }

    public override IdentityError ConcurrencyFailure()
    {
        return new IdentityError
        {
            Code = nameof(ConcurrencyFailure),
            Description = "Échec de concurrence : cet élément a été modifié entre-temps."
        };
    }

    public override IdentityError PasswordMismatch()
    {
        return new IdentityError
        {
            Code = nameof(PasswordMismatch),
            Description = "Mot de passe incorrect."
        };
    }

    public override IdentityError InvalidToken()
    {
        return new IdentityError
        {
            Code = nameof(InvalidToken),
            Description = "Jeton invalide."
        };
    }

    public override IdentityError LoginAlreadyAssociated()
    {
        return new IdentityError
        {
            Code = nameof(LoginAlreadyAssociated),
            Description = "Un utilisateur avec cette connexion existe déjà."
        };
    }

    public override IdentityError InvalidUserName(string? userName)
    {
        return new IdentityError
        {
            Code = nameof(InvalidUserName),
            Description = $"Le nom d'utilisateur « {userName} » n'est pas valide : il ne peut contenir que des lettres ou des chiffres."
        };
    }

    public override IdentityError InvalidEmail(string? email)
    {
        return new IdentityError
        {
            Code = nameof(InvalidEmail),
            Description = $"L'email « {email} » n'est pas valide."
        };
    }

    public override IdentityError DuplicateUserName(string userName)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateUserName),
            Description = $"Le nom d'utilisateur « {userName} » est déjà utilisé."
        };
    }

    public override IdentityError DuplicateEmail(string email)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateEmail),
            Description = $"L'email « {email} » est déjà utilisé."
        };
    }

    public override IdentityError InvalidRoleName(string? role)
    {
        return new IdentityError
        {
            Code = nameof(InvalidRoleName),
            Description = $"Le nom de rôle « {role} » n'est pas valide."
        };
    }

    public override IdentityError DuplicateRoleName(string role)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateRoleName),
            Description = $"Le nom de rôle « {role} » est déjà utilisé."
        };
    }

    public override IdentityError UserAlreadyHasPassword()
    {
        return new IdentityError
        {
            Code = nameof(UserAlreadyHasPassword),
            Description = "Cet utilisateur possède déjà un mot de passe."
        };
    }

    public override IdentityError UserLockoutNotEnabled()
    {
        return new IdentityError
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = "Le verrouillage n'est pas activé pour cet utilisateur."
        };
    }

    public override IdentityError UserAlreadyInRole(string role)
    {
        return new IdentityError
        {
            Code = nameof(UserAlreadyInRole),
            Description = $"L'utilisateur possède déjà le rôle « {role} »."
        };
    }

    public override IdentityError UserNotInRole(string role)
    {
        return new IdentityError
        {
            Code = nameof(UserNotInRole),
            Description = $"L'utilisateur ne possède pas le rôle « {role} »."
        };
    }

    public override IdentityError PasswordTooShort(int length)
    {
        return new IdentityError
        {
            Code = nameof(PasswordTooShort),
            Description = $"Le mot de passe doit contenir au moins {length} caractères."
        };
    }

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = $"Le mot de passe doit contenir au moins {uniqueChars} caractère(s) distinct(s)."
        };
    }

    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "Le mot de passe doit contenir au moins un caractère spécial (non alphanumérique)."
        };
    }

    public override IdentityError PasswordRequiresDigit()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "Le mot de passe doit contenir au moins un chiffre (0-9)."
        };
    }

    public override IdentityError PasswordRequiresLower()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresLower),
            Description = "Le mot de passe doit contenir au moins une lettre minuscule (a-z)."
        };
    }

    public override IdentityError PasswordRequiresUpper()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "Le mot de passe doit contenir au moins une lettre majuscule (A-Z)."
        };
    }

    public override IdentityError RecoveryCodeRedemptionFailed()
    {
        return new IdentityError
        {
            Code = nameof(RecoveryCodeRedemptionFailed),
            Description = "Échec de l'utilisation du code de récupération."
        };
    }
}
