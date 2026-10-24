namespace BankDesk;

public record Hist(string D, string S, string T);
public record Msg(string From, string At, string Text, bool Read = false);

public class Req
{
    public string Id = "", Client = "", Type = "", Title = "", Date = "", Status = "Reçue", Est = "", Desc = "";
    public int Hours;                                   // heures restantes (négatif = en retard)
    public List<(string Name, string Size)> Files = new();
    public List<Hist> History = new();
    public bool Late => Hours < 0 && Status is "Reçue" or "En cours" && Hours < 0;
}

// Données de l'application : désormais LUES depuis le serveur (voir Api.cs) puis gardées en mémoire (cache).
// Les pages continuent de lire Store.Reqs / Store.Msgs comme avant ; ce sont les modifications qui passent par l'API.
public static class Store
{
    public static string Me = "";                       // nom du client connecté (était une constante en mode simulation)
    public static string Advisor = "M. Etoa";           // nom du conseiller (le sien s'il est connecté)
    public static string Role = "client";               // "client" ou "advisor"
    public static bool LoggedIn => Api.Token != null;
    public static string Now() => DateTime.Now.ToString("dd/MM HH:mm");

    public static List<Req> Reqs = new();
    public static Dictionary<string, List<Msg>> Msgs = new();

    /// <summary>Mémorise qui vient de se connecter (appelé après une connexion réussie).</summary>
    public static void SetUser(UserDto u)
    {
        Reqs = new(); Msgs = new();
        if (u.Role == "conseiller") { Role = "advisor"; Advisor = u.NomComplet; }
        else { Role = "client"; Me = u.NomComplet; Advisor = "M. Etoa"; }
    }

    /// <summary>Recharge toutes les requêtes (et leurs messages) depuis le serveur.</summary>
    public static async Task LoadAsync()
    {
        var liste = await Api.GetRequestsAsync();
        var reqs = new List<Req>(); var msgs = new Dictionary<string, List<Msg>>();
        foreach (var d in liste)
        {
            reqs.Add(VersReq(d));
            if (d.Messages.Count > 0) msgs[d.Reference] = d.Messages.Select(m => VersMsg(m)).ToList();
        }
        Reqs = reqs; Msgs = msgs;
    }

    /// <summary>Recharge uniquement les messages d'une requête (rafraîchissement léger de la messagerie).</summary>
    public static async Task LoadMessagesAsync(string reference)
    {
        var ms = await Api.GetMessagesAsync(reference);
        if (ms.Count > 0) Msgs[reference] = ms.Select(m => VersMsg(m)).ToList(); else Msgs.Remove(reference);
    }

    /// <summary>Déconnexion : on oublie les données de l'utilisateur.</summary>
    public static void Clear() { Reqs = new(); Msgs = new(); Me = ""; Advisor = "M. Etoa"; Role = "client"; }

    // ---- Conversions : formes du serveur (DTO) -> formes utilisées par les pages ----

    static Req VersReq(ReqDto d)
    {
        var r = new Req
        {
            Id = d.Reference, Client = d.Client, Type = d.Type, Title = d.Objet, Desc = d.Description, Status = d.Statut,
            Date = d.DateCreation.ToLocalTime().ToString("dd/MM/yyyy"),   // le serveur travaille en UTC : conversion en heure locale
            Hours = d.HeuresRestantes, Est = d.Estimation
        };
        foreach (var f in d.PiecesJointes)
            r.Files.Add((f.Nom, $"{System.IO.Path.GetExtension(f.Nom).TrimStart('.').ToUpper()} · {Taille(f.TailleOctets)}"));
        // L'historique s'affiche du plus récent au plus ancien.
        foreach (var e in d.Historique.OrderByDescending(e => e.Date))
            r.History.Add(new Hist(e.Date.ToLocalTime().ToString("dd/MM HH:mm"), e.Titre, e.Detail));
        return r;
    }

    static Msg VersMsg(MsgDto m) => new(m.Auteur, m.Date.ToLocalTime().ToString("dd/MM HH:mm"), m.Texte, m.Lu);

    static string Taille(long octets) => octets >= 1048576 ? $"{octets / 1048576.0:0.0} Mo" : $"{Math.Max(1, octets / 1024)} Ko";
}
