namespace XpertSphere.MonolithApi.Config;

/// <summary>
/// URLs des applications frontend, utilisées côté backend pour construire des liens absolus
/// (ex. lien d'activation de compte envoyé par email) — voir
/// candidate-account-activation-email.md.
/// </summary>
public class FrontendSettings
{
    /// <summary>
    /// Adresse navigateur exposée de candidate-app (jamais un nom de service Docker interne, sous
    /// peine de liens cassés pour le candidat qui ouvre l'email depuis son propre navigateur).
    /// </summary>
    public string CandidateAppBaseUrl { get; set; } = string.Empty;
}
