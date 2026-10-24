namespace BankDesk.Api.Donnees;

// ============================================================================
//  DTO : la forme EXACTE des données échangées en JSON avec l'application mobile.
//  On n'expose jamais directement les entités (elles contiennent des champs sensibles).
// ============================================================================

public record InscriptionDemande(string Nom, string Prenom, string Contact, string MotDePasse);
public record ConnexionDemande(string Contact, string MotDePasse);
public record UtilisateurDto(int Id, string Nom, string Prenom, string NomComplet, string Contact, string Role);
public record ConnexionReponse(string Jeton, UtilisateurDto Utilisateur);

public record CreerRequeteDemande(string Type, string Objet, string Description);
public record ChangerStatutDemande(string Statut, string? Reponse);
public record EnvoyerMessageDemande(string Texte);

public record PieceJointeDto(int Id, string Nom, long TailleOctets, string TypeMime);
public record EvenementDto(DateTime Date, string Titre, string Detail);
public record MessageDto(string Auteur, string AuteurRole, DateTime Date, string Texte, bool Lu);

/// <summary>Une requête complète (liste, suivi, messagerie et traitement utilisent la même forme).</summary>
public record RequeteDto(
    string Reference, string Client, string Type, string Objet, string Description, string Statut,
    DateTime DateCreation, int DelaiEstimeHeures, int HeuresRestantes, string Estimation,
    List<PieceJointeDto> PiecesJointes, List<EvenementDto> Historique, List<MessageDto> Messages);

/// <summary>Conversion entités → DTO (méthodes d'extension : requete.VersDto()).</summary>
public static class Mappage
{
    // SQLite renvoie des dates sans fuseau : on précise "UTC" pour que le JSON se termine par "Z".
    private static DateTime Utc(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    public static UtilisateurDto VersDto(this Utilisateur u) => new(u.Id, u.Nom, u.Prenom, u.NomComplet, u.Contact, u.Role);

    public static MessageDto VersDto(this Message m) => new(m.AuteurNom, m.AuteurRole, Utc(m.Date), m.Texte, m.Lu);

    public static RequeteDto VersDto(this Requete r)
    {
        // Heures restantes = délai annoncé - temps déjà écoulé (négatif = en retard). 0 si la requête est clôturée.
        bool terminee = r.Statut is "Résolue" or "Rejetée";
        int restantes = terminee ? 0 : (int)Math.Floor(r.DelaiEstimeHeures - (DateTime.UtcNow - Utc(r.DateCreation)).TotalHours);

        return new RequeteDto(
            r.Reference, r.Client.NomComplet, r.Type, r.Objet, r.Description, r.Statut,
            Utc(r.DateCreation), r.DelaiEstimeHeures, restantes, $"{r.DelaiEstimeHeures / 24} jours ouvrés",
            r.PiecesJointes.Select(p => new PieceJointeDto(p.Id, p.NomFichier, p.TailleOctets, p.TypeMime)).ToList(),
            r.Historique.OrderBy(e => e.Date).Select(e => new EvenementDto(Utc(e.Date), e.Titre, e.Detail)).ToList(),
            r.Messages.OrderBy(m => m.Date).Select(m => m.VersDto()).ToList());
    }
}
