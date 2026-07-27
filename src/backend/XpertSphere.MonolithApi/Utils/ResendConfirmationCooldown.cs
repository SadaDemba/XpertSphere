using System.Collections.Concurrent;

namespace XpertSphere.MonolithApi.Utils;

/// <summary>
/// Cooldown applicatif par email, protection anti-bombardement complémentaire au rate limiting
/// par IP sur POST /api/auth/resend-confirmation (voir candidate-account-activation-email.md, §8).
/// Même principe que le ConcurrentDictionary statique déjà utilisé par
/// <see cref="XpertSphere.MonolithApi.Services.EntraIdRateLimitService"/> pour du throttling en
/// mémoire, process-local.
/// </summary>
/// <remarks>
/// Limite connue et assumée : ce cooldown est process-local (ConcurrentDictionary statique en
/// mémoire) - non partagé entre plusieurs instances derrière un load balancer. Non traité
/// différemment ici que pour EntraIdRateLimitService (aucun déploiement multi-instance aujourd'hui,
/// aucun cache distribué/Redis câblé dans ce projet).
/// </remarks>
internal static class ResendConfirmationCooldown
{
    private static readonly ConcurrentDictionary<string, DateTime> LastSentAt =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retourne true si aucun renvoi n'a eu lieu pour cet email dans la fenêtre de cooldown (et
    /// enregistre l'instant présent comme nouveau dernier envoi) ; false si un renvoi a déjà eu
    /// lieu récemment (aucun nouvel envoi ne doit avoir lieu).
    /// </summary>
    public static bool TryStart(string email, TimeSpan cooldown)
    {
        var now = DateTime.UtcNow;
        var recorded = LastSentAt.AddOrUpdate(
            email,
            now,
            (_, last) => now - last < cooldown ? last : now);

        return recorded == now;
    }
}
