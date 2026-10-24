using BankDesk.Api.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankDesk.Api.Controllers;

/// <summary>
/// Messagerie client ↔ conseiller, rattachée à une requête :
///   GET  /api/requetes/{reference}/messages       lire la conversation
///   POST /api/requetes/{reference}/messages       envoyer un message
///   POST /api/requetes/{reference}/messages/lus   marquer comme lus les messages de l'autre personne
/// </summary>
[Route("api/requetes/{reference}/messages")]
[Authorize]
public class MessagesController : ApiControllerBase
{
    public MessagesController(BankDeskDb db) : base(db) { }

    [HttpGet]
    public async Task<ActionResult<List<MessageDto>>> Lister(string reference)
    {
        var r = await TrouverRequeteAsync(reference);
        if (r is null) return Erreur(404, "Requête introuvable.");
        return r.Messages.OrderBy(m => m.Date).Select(m => m.VersDto()).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> Envoyer(string reference, EnvoyerMessageDemande d)
    {
        string texte = d.Texte.Trim();
        if (texte.Length == 0) return Erreur(400, "Le message est vide.");
        if (texte.Length > 1000) return Erreur(400, "Le message est trop long (1000 caractères maximum).");

        var r = await TrouverRequeteAsync(reference);
        if (r is null) return Erreur(404, "Requête introuvable.");

        // L'auteur vient du JETON, jamais de ce que l'application prétend : impossible d'écrire au nom d'un autre.
        var m = new Message { Requete = r, AuteurId = MonId, AuteurRole = MonRole, AuteurNom = MonNom, Texte = texte, Date = DateTime.UtcNow, Lu = false };
        Db.Messages.Add(m);
        await Db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, m.VersDto());
    }

    [HttpPost("lus")]
    public async Task<IActionResult> MarquerCommeLus(string reference)
    {
        var r = await TrouverRequeteAsync(reference);
        if (r is null) return Erreur(404, "Requête introuvable.");

        // Ouvrir la conversation = lire les messages écrits par l'AUTRE personne.
        foreach (var m in r.Messages.Where(m => m.AuteurRole != MonRole && !m.Lu))
            m.Lu = true;
        await Db.SaveChangesAsync();
        return NoContent();
    }
}
