using System.Security.Claims;
using BankDesk.Api.Donnees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankDesk.Api.Controllers;

/// <summary>
/// Base commune des contrôleurs : accès à la base, identité de l'utilisateur connecté,
/// chargement SÉCURISÉ d'une requête. [ApiController] valide automatiquement le JSON reçu.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly BankDeskDb Db;
    protected ApiControllerBase(BankDeskDb db) => Db = db;

    protected int MonId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string MonNom => User.Identity?.Name ?? "";
    protected bool EstConseiller => User.IsInRole(Valeurs.Conseiller);
    protected string MonRole => EstConseiller ? Valeurs.Conseiller : Valeurs.Client;

    /// <summary>Réponse d'erreur standard : {"status":400,"detail":"message"}. L'application lit "detail".</summary>
    protected ActionResult Erreur(int statut, string message) => Problem(detail: message, statusCode: statut);

    /// <summary>
    /// Charge une requête avec tout son contenu, EN VÉRIFIANT LES DROITS :
    /// un conseiller voit tout ; un client ne voit que les siennes.
    /// Retourne null si elle n'existe pas OU appartient à un autre client (même réponse "introuvable").
    /// </summary>
    protected async Task<Requete?> TrouverRequeteAsync(string reference)
    {
        var requete = await Db.Requetes
            .Include(r => r.Client).Include(r => r.PiecesJointes).Include(r => r.Historique).Include(r => r.Messages)
            .FirstOrDefaultAsync(r => r.Reference == reference);

        if (requete is null) return null;
        if (!EstConseiller && requete.ClientId != MonId) return null;
        return requete;
    }
}
