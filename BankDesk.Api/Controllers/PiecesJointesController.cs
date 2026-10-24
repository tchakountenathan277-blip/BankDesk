using BankDesk.Api.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankDesk.Api.Controllers;

/// <summary>
///   POST /api/requetes/{reference}/pieces-jointes        envoyer un fichier (multipart, champ "fichier")
///   GET  /api/requetes/{reference}/pieces-jointes/{id}   télécharger un fichier
/// Les fichiers sont enregistrés sur le disque du serveur (dossier "uploads") ; la base garde leurs informations.
/// </summary>
[Route("api/requetes/{reference}/pieces-jointes")]
[Authorize]
public class PiecesJointesController : ApiControllerBase
{
    private const long TailleMax = 10 * 1024 * 1024;   // 10 Mo par fichier (comme l'écran)
    private const int NombreMax = 5;

    private static readonly Dictionary<string, string> TypesAutorises = new()
    { [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".pdf"] = "application/pdf" };

    private readonly IWebHostEnvironment _env;
    public PiecesJointesController(BankDeskDb db, IWebHostEnvironment env) : base(db) => _env = env;

    [HttpPost]
    [RequestSizeLimit(TailleMax + 1_048_576)]
    public async Task<ActionResult<PieceJointeDto>> Ajouter(string reference, IFormFile fichier)
    {
        if (EstConseiller) return Erreur(403, "Seul le client peut ajouter des pièces jointes à sa requête.");
        var r = await TrouverRequeteAsync(reference);
        if (r is null) return Erreur(404, "Requête introuvable.");
        if (r.PiecesJointes.Count >= NombreMax) return Erreur(400, $"{NombreMax} fichiers maximum par requête.");
        if (fichier.Length == 0) return Erreur(400, "Le fichier est vide.");
        if (fichier.Length > TailleMax) return Erreur(400, "Le fichier dépasse 10 Mo.");

        // Path.GetFileName retire tout chemin ("..\..\x.jpg" -> "x.jpg") : protection contre les attaques par chemin.
        string nom = Path.GetFileName(fichier.FileName);
        string ext = Path.GetExtension(nom).ToLowerInvariant();
        if (!TypesAutorises.TryGetValue(ext, out string? mime)) return Erreur(400, "Format non accepté : JPG, PNG ou PDF uniquement.");

        // On vérifie aussi le CONTENU (premiers octets) : renommer un .exe en .jpg ne suffit pas.
        var debut = new byte[8]; int lus;
        await using (var flux = fichier.OpenReadStream()) lus = await flux.ReadAsync(debut.AsMemory(0, debut.Length));
        if (!SignatureValide(ext, debut.AsSpan(0, lus))) return Erreur(400, "Le contenu du fichier ne correspond pas à son format.");

        // Enregistrement sur disque sous un nom aléatoire.
        string dossier = Path.Combine(_env.ContentRootPath, "uploads", r.Id.ToString());
        Directory.CreateDirectory(dossier);
        string chemin = Path.Combine(dossier, Guid.NewGuid().ToString("N") + ext);
        await using (var sortie = System.IO.File.Create(chemin)) await fichier.CopyToAsync(sortie);

        var p = new PieceJointe { Requete = r, NomFichier = nom, TypeMime = mime, TailleOctets = fichier.Length, CheminStockage = chemin };
        Db.PiecesJointes.Add(p);
        await Db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new PieceJointeDto(p.Id, p.NomFichier, p.TailleOctets, p.TypeMime));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Telecharger(string reference, int id)
    {
        var r = await TrouverRequeteAsync(reference);   // vérifie aussi les droits d'accès
        var p = r?.PiecesJointes.FirstOrDefault(x => x.Id == id);
        if (p is null || !System.IO.File.Exists(p.CheminStockage)) return Erreur(404, "Pièce jointe introuvable.");
        return PhysicalFile(p.CheminStockage, p.TypeMime, p.NomFichier);
    }

    /// <summary>Compare les premiers octets du fichier à la "signature" attendue pour son format.</summary>
    private static bool SignatureValide(string ext, ReadOnlySpan<byte> d) => ext switch
    {
        ".jpg" or ".jpeg" => d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF,
        ".png" => d.Length >= 8 && d.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".pdf" => d.Length >= 4 && d[0] == 0x25 && d[1] == 0x50 && d[2] == 0x44 && d[3] == 0x46,   // "%PDF"
        _ => false
    };
}
