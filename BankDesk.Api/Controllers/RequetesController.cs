using BankDesk.Api.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankDesk.Api.Controllers;

/// <summary>
///   GET  /api/requetes                     liste (le client voit les siennes, le conseiller toutes), contenu complet
///   GET  /api/requetes/{reference}         une requête
///   POST /api/requetes                     créer une requête (client)
///   PUT  /api/requetes/{reference}/statut  changer le statut + réponse (conseiller)
/// </summary>
[Route("api/requetes")]
[Authorize]   // toutes les routes exigent un jeton valide
public class RequetesController : ApiControllerBase
{
    // Verrou : deux clients ne peuvent pas obtenir en même temps la même référence.
    private static readonly SemaphoreSlim VerrouCreation = new(1, 1);

    public RequetesController(BankDeskDb db) : base(db) { }

    [HttpGet]
    public async Task<ActionResult<List<RequeteDto>>> Lister()
    {
        int moi = MonId;
        IQueryable<Requete> q = Db.Requetes;
        if (!EstConseiller) q = q.Where(r => r.ClientId == moi);   // règle de sécurité : un client ne voit QUE les siennes

        var liste = await q.Include(r => r.Client).Include(r => r.PiecesJointes).Include(r => r.Historique).Include(r => r.Messages)
            .OrderByDescending(r => r.DateCreation)   // "les plus récentes" d'abord
            .ToListAsync();
        return liste.Select(r => r.VersDto()).ToList();
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<RequeteDto>> Detail(string reference)
    {
        var r = await TrouverRequeteAsync(reference);
        return r is null ? Erreur(404, "Requête introuvable.") : r.VersDto();
    }

    [HttpPost]
    [Authorize(Roles = Valeurs.Client)]   // seuls les clients déposent des requêtes
    public async Task<ActionResult<RequeteDto>> Creer(CreerRequeteDemande d)
    {
        string objet = d.Objet.Trim(), description = d.Description.Trim();
        if (objet.Length == 0) return Erreur(400, "L'objet de la requête est obligatoire.");
        if (objet.Length > 120) return Erreur(400, "L'objet est trop long (120 caractères maximum).");
        if (description.Length == 0) return Erreur(400, "La description est obligatoire.");
        if (description.Length > 2000) return Erreur(400, "La description est trop longue (2000 caractères maximum).");
        if (!Valeurs.Types.Contains(d.Type)) return Erreur(400, "Type de requête inconnu.");

        int moi = MonId;
        var client = await Db.Utilisateurs.FirstAsync(u => u.Id == moi);

        await VerrouCreation.WaitAsync();
        try
        {
            // Référence suivante : plus grand numéro existant + 1 (REQ-0042 -> REQ-0043).
            var references = await Db.Requetes.Select(r => r.Reference).ToListAsync();
            int dernier = references.Select(x => int.TryParse(x.Replace("REQ-", ""), out int n) ? n : 0).DefaultIfEmpty(0).Max();

            var r = new Requete
            {
                Reference = $"REQ-{dernier + 1:D4}",   // D4 = 4 chiffres avec zéros devant
                Client = client, ClientId = client.Id,
                Type = d.Type, Objet = objet, Description = description, Statut = "Reçue",
                DateCreation = DateTime.UtcNow, DelaiEstimeHeures = Valeurs.DelaiHeures(d.Type)
            };
            Db.Requetes.Add(r);
            Db.Evenements.Add(new EvenementHistorique { Requete = r, Date = DateTime.UtcNow, Titre = "Requête reçue", Detail = "Votre demande a bien été enregistrée." });
            await Db.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, r.VersDto());
        }
        finally { VerrouCreation.Release(); }   // toujours libérer le verrou, même en cas d'erreur
    }

    [HttpPut("{reference}/statut")]
    [Authorize(Roles = Valeurs.Conseiller)]   // seuls les conseillers changent le statut
    public async Task<ActionResult<RequeteDto>> ChangerStatut(string reference, ChangerStatutDemande d)
    {
        if (!Valeurs.Statuts.Contains(d.Statut)) return Erreur(400, "Statut inconnu.");
        var r = await TrouverRequeteAsync(reference);
        if (r is null) return Erreur(404, "Requête introuvable.");

        string? reponse = string.IsNullOrWhiteSpace(d.Reponse) ? null : d.Reponse.Trim();
        if (reponse is { Length: > 1000 }) return Erreur(400, "La réponse est trop longue (1000 caractères maximum).");
        if (d.Statut == "Rejetée" && reponse is null) return Erreur(400, "Un motif est obligatoire pour rejeter une requête.");

        // Passage "Reçue" -> "En cours" : on ajoute d'abord l'étape "Prise en charge" (2e étape de la frise).
        if (r.Statut == "Reçue" && d.Statut == "En cours")
            Db.Evenements.Add(new EvenementHistorique { Requete = r, Date = DateTime.UtcNow, Titre = "Prise en charge", Detail = "Dossier vérifié par votre conseiller." });

        string defaut = d.Statut switch
        {
            "Reçue" => "Votre demande a bien été enregistrée.",
            "En cours" => "Votre demande est en cours de traitement.",
            "Résolue" => "Votre demande a été traitée.",
            _ => "Votre demande n'a pas pu être acceptée."
        };
        Db.Evenements.Add(new EvenementHistorique
        {
            Requete = r, Date = DateTime.UtcNow.AddSeconds(1),   // +1 s : garde l'ordre après "Prise en charge"
            Titre = d.Statut == "Reçue" ? "Requête reçue" : d.Statut, Detail = reponse ?? defaut
        });

        // Une réponse du conseiller part aussi dans la messagerie.
        if (reponse is not null)
            Db.Messages.Add(new Message { Requete = r, AuteurId = MonId, AuteurRole = Valeurs.Conseiller, AuteurNom = MonNom, Texte = reponse, Date = DateTime.UtcNow, Lu = false });

        r.Statut = d.Statut;
        await Db.SaveChangesAsync();
        return r.VersDto();
    }
}
