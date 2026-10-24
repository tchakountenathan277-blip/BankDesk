using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using BankDesk.Api.Donnees;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BankDesk.Api.Securite;

/// <summary>
/// Mots de passe : on ne stocke jamais le mot de passe, seulement son empreinte PBKDF2
/// (algorithme lent, conçu pour cet usage) et un "sel" aléatoire propre à chaque utilisateur.
/// </summary>
public static class MotDePasse
{
    private const int TailleSel = 16, TailleHash = 32, Iterations = 100_000;

    public static (string Hash, string Sel) Hacher(string motDePasse)
    {
        byte[] sel = RandomNumberGenerator.GetBytes(TailleSel);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleHash);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(sel));
    }

    public static bool Verifier(string motDePasse, string hashStocke, string selStocke)
    {
        byte[] attendu = Convert.FromBase64String(hashStocke);
        byte[] calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, Convert.FromBase64String(selStocke), Iterations, HashAlgorithmName.SHA256, attendu.Length);
        // Comparaison en temps constant : ne révèle rien par le temps de réponse.
        return CryptographicOperations.FixedTimeEquals(calcule, attendu);
    }
}

/// <summary>Jetons de connexion : aléatoires (256 bits) ; seule leur empreinte SHA-256 est stockée.</summary>
public static class Jetons
{
    public static string Generer()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hacher(string jeton)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jeton)));
}

/// <summary>
/// AUTHENTIFICATION : lit l'en-tête "Authorization: Bearer &lt;jeton&gt;", retrouve la session en base
/// et, si elle est valide, identifie l'utilisateur. Les routes marquées [Authorize] exigent un jeton valide.
/// </summary>
public class JetonAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Schema = "Jeton";
    private readonly BankDeskDb _db;

    public JetonAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
                            UrlEncoder encoder, BankDeskDb db) : base(options, logger, encoder) => _db = db;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var valeur))
            return AuthenticateResult.NoResult();

        string texte = valeur.ToString();
        const string prefixe = "Bearer ";
        if (!texte.StartsWith(prefixe, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        string empreinte = Jetons.Hacher(texte[prefixe.Length..].Trim());
        var session = await _db.Sessions.Include(s => s.Utilisateur)
            .FirstOrDefaultAsync(s => s.JetonHash == empreinte && s.Expiration > DateTime.UtcNow);
        if (session is null)
            return AuthenticateResult.Fail("Jeton invalide ou expiré.");

        // "Claims" = informations sur l'utilisateur, relues ensuite dans les contrôleurs.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.Utilisateur.Id.ToString()),
            new Claim(ClaimTypes.Name, session.Utilisateur.NomComplet),
            new Claim(ClaimTypes.Role, session.Utilisateur.Role),
        };
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
