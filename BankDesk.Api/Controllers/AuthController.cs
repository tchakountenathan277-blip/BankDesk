using BankDesk.Api.Donnees;
using BankDesk.Api.Securite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankDesk.Api.Controllers;

/// <summary>
///   POST /api/auth/inscription   créer un compte client
///   POST /api/auth/connexion     se connecter → renvoie un jeton
///   POST /api/auth/deconnexion   fermer la session
///   GET  /api/auth/moi           qui suis-je ?
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private static readonly TimeSpan DureeSession = TimeSpan.FromDays(7);
    public AuthController(BankDeskDb db) : base(db) { }

    [HttpPost("inscription")]
    [AllowAnonymous]
    public async Task<ActionResult<UtilisateurDto>> Inscription(InscriptionDemande d)
    {
        // On ne fait JAMAIS confiance aux données reçues : validation côté serveur.
        string nom = d.Nom.Trim(), prenom = d.Prenom.Trim(), contact = d.Contact.Trim().ToLower();
        if (nom.Length == 0 || prenom.Length == 0 || contact.Length == 0) return Erreur(400, "Tous les champs sont obligatoires.");
        if (nom.Length > 60 || prenom.Length > 60 || contact.Length > 100) return Erreur(400, "Un des champs est trop long.");
        if (d.MotDePasse.Length < 8 || d.MotDePasse.Length > 100) return Erreur(400, "Le mot de passe doit contenir entre 8 et 100 caractères.");
        if (await Db.Utilisateurs.AnyAsync(u => u.Contact == contact)) return Erreur(409, "Un compte existe déjà avec ce téléphone ou cet e-mail.");

        var (hash, sel) = MotDePasse.Hacher(d.MotDePasse);
        var u = new Utilisateur
        {
            Nom = nom, Prenom = prenom, Contact = contact, MotDePasseHash = hash, Sel = sel,
            Role = Valeurs.Client   // une inscription crée TOUJOURS un client, jamais un conseiller
        };
        Db.Utilisateurs.Add(u);
        await Db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, u.VersDto());
    }

    [HttpPost("connexion")]
    [AllowAnonymous]
    public async Task<ActionResult<ConnexionReponse>> Connexion(ConnexionDemande d)
    {
        string contact = d.Contact.Trim().ToLower();
        var u = await Db.Utilisateurs.FirstOrDefaultAsync(x => x.Contact == contact);

        // Même message pour "compte inconnu" et "mauvais mot de passe" : on ne révèle pas quels comptes existent.
        if (u is null || !MotDePasse.Verifier(d.MotDePasse, u.MotDePasseHash, u.Sel))
            return Erreur(401, "Identifiants incorrects.");

        string jeton = Jetons.Generer();   // envoyé à l'application ; seule son empreinte va en base
        Db.Sessions.Add(new Session { JetonHash = Jetons.Hacher(jeton), UtilisateurId = u.Id, Utilisateur = u, Expiration = DateTime.UtcNow.Add(DureeSession) });
        await Db.SaveChangesAsync();
        return new ConnexionReponse(jeton, u.VersDto());
    }

    [HttpPost("deconnexion")]
    [Authorize]
    public async Task<IActionResult> Deconnexion()
    {
        string empreinte = Jetons.Hacher(Request.Headers.Authorization.ToString()["Bearer ".Length..].Trim());
        Db.Sessions.RemoveRange(await Db.Sessions.Where(s => s.JetonHash == empreinte).ToListAsync());
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("moi")]
    [Authorize]
    public async Task<ActionResult<UtilisateurDto>> Moi()
    {
        int id = MonId;
        var u = await Db.Utilisateurs.FirstOrDefaultAsync(x => x.Id == id);
        return u is null ? Erreur(404, "Utilisateur introuvable.") : u.VersDto();
    }
}
