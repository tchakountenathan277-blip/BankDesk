namespace BankDesk.Api.Donnees;

// ============================================================================
//  ENTITÉS : chaque classe = une TABLE de la base SQLite (une propriété = une colonne).
//  Les valeurs "métier" (rôle, statut, type) sont stockées en texte, avec les MÊMES mots
//  que l'application mobile : "Reçue", "En cours", "Résolue", "Rejetée", "Chéquier"...
// ============================================================================

/// <summary>Listes de valeurs autorisées (le serveur refuse tout le reste).</summary>
public static class Valeurs
{
    public const string Client = "client", Conseiller = "conseiller";
    public static readonly string[] Statuts = { "Reçue", "En cours", "Résolue", "Rejetée" };
    public static readonly string[] Types = { "Réclamation", "Chéquier", "Carte", "Crédit", "Autre" };

    /// <summary>Délai de traitement annoncé selon le type (en heures) : 2, 3 ou 5 jours ouvrés.</summary>
    public static int DelaiHeures(string type) => type switch
    {
        "Carte" or "Autre" => 72,
        "Crédit" => 120,
        _ => 48
    };
}

/// <summary>Table Utilisateurs : clients et conseillers.</summary>
public class Utilisateur
{
    public int Id { get; set; }
    public string Nom { get; set; } = "";
    public string Prenom { get; set; } = "";

    /// <summary>Téléphone ou e-mail : identifiant de connexion (unique).</summary>
    public string Contact { get; set; } = "";

    /// <summary>Le mot de passe n'est JAMAIS stocké : seulement son empreinte PBKDF2 + un "sel" aléatoire.</summary>
    public string MotDePasseHash { get; set; } = "";
    public string Sel { get; set; } = "";

    public string Role { get; set; } = Valeurs.Client;
    public string NomComplet => $"{Prenom} {Nom}".Trim();
}

/// <summary>Table Sessions : une ligne par connexion. On stocke l'EMPREINTE du jeton, pas le jeton.</summary>
public class Session
{
    public int Id { get; set; }
    public string JetonHash { get; set; } = "";
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public DateTime Expiration { get; set; }
}

/// <summary>Table Requetes : une requête bancaire déposée par un client.</summary>
public class Requete
{
    public int Id { get; set; }
    public string Reference { get; set; } = "";          // "REQ-0042" (unique)

    public int ClientId { get; set; }
    public Utilisateur Client { get; set; } = null!;

    public string Type { get; set; } = "Autre";
    public string Objet { get; set; } = "";
    public string Description { get; set; } = "";
    public string Statut { get; set; } = "Reçue";

    /// <summary>Dates stockées en UTC ; l'application les convertit à l'heure locale.</summary>
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public int DelaiEstimeHeures { get; set; } = 48;

    public List<PieceJointe> PiecesJointes { get; set; } = new();
    public List<EvenementHistorique> Historique { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

/// <summary>Table PiecesJointes : informations ; le fichier est sur le disque (dossier "uploads").</summary>
public class PieceJointe
{
    public int Id { get; set; }
    public int RequeteId { get; set; }
    public Requete Requete { get; set; } = null!;
    public string NomFichier { get; set; } = "";
    public string TypeMime { get; set; } = "";
    public long TailleOctets { get; set; }
    public string CheminStockage { get; set; } = "";
}

/// <summary>Table Evenements : historique d'une requête (écran Suivi).</summary>
public class EvenementHistorique
{
    public int Id { get; set; }
    public int RequeteId { get; set; }
    public Requete Requete { get; set; } = null!;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Titre { get; set; } = "";
    public string Detail { get; set; } = "";
}

/// <summary>Table Messages : discussion client ↔ conseiller, liée à une requête.</summary>
public class Message
{
    public int Id { get; set; }
    public int RequeteId { get; set; }
    public Requete Requete { get; set; } = null!;
    public int AuteurId { get; set; }
    public string AuteurRole { get; set; } = Valeurs.Client;
    public string AuteurNom { get; set; } = "";
    public string Texte { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.UtcNow;

    /// <summary>true quand l'AUTRE personne a ouvert la conversation.</summary>
    public bool Lu { get; set; }
}
