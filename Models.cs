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

// Données simulées : à remplacer plus tard par les appels à l'API (HttpClient).
public static class Store
{
    public const string Me = "Pierre Nathan", Advisor = "M. Etoa";
    public static string Role = "client";
    public static string Now() => DateTime.Now.ToString("dd/MM HH:mm");
    static Hist Rec(string d) => new(d, "Requête reçue", "Votre demande a bien été enregistrée.");

    public static List<Req> Reqs = new()
    {
        new Req { Id="REQ-0042", Client=Me, Type="Chéquier", Title="Demande de chéquier", Date="03/10/2026", Status="En cours", Hours=28, Est="2 jours ouvrés",
            Desc="Bonjour, je souhaite commander un nouveau chéquier de 25 chèques, à retirer dans mon agence habituelle. Merci.",
            Files={("CNI.jpg","JPG · 1,2 Mo")},
            History={ new("05/10 14:20","En cours","Votre chéquier est en préparation."), new("04/10 09:05","Prise en charge","Dossier vérifié par votre conseiller."), Rec("03/10 17:42") } },
        new Req { Id="REQ-0041", Client="Amina Benali", Type="Réclamation", Title="Réclamation virement", Date="03/10/2026", Status="En cours", Hours=-3, Est="2 jours ouvrés", Desc="Un virement n'a pas été crédité.", History={ Rec("03/10 10:00") } },
        new Req { Id="REQ-0040", Client="Lucas Fontaine", Type="Carte", Title="Remplacement de carte", Date="02/10/2026", Hours=40, Est="3 jours ouvrés", Desc="Ma carte est endommagée.", History={ Rec("02/10 11:15") } },
        new Req { Id="REQ-0039", Client=Me, Type="Réclamation", Title="Réclamation prélèvement", Date="02/10/2026", Hours=30, Est="2 jours ouvrés", Desc="Prélèvement non autorisé.", History={ Rec("02/10 16:00") } },
        new Req { Id="REQ-0038", Client="Marie Dupont", Type="Crédit", Title="Demande de crédit", Date="01/10/2026", Hours=90, Est="5 jours ouvrés", Desc="Crédit consommation.", History={ Rec("01/10 09:30") } },
        new Req { Id="REQ-0031", Client=Me, Type="Carte", Title="Remplacement de carte", Date="28/09/2026", Status="Résolue", Est="3 jours ouvrés", Desc="Carte perdue.", History={ new("30/09 15:00","Résolue","Votre nouvelle carte est disponible."), Rec("28/09 10:00") } },
        new Req { Id="REQ-0027", Client=Me, Type="Crédit", Title="Demande de crédit", Date="24/09/2026", Status="Rejetée", Est="5 jours ouvrés", Desc="Crédit immobilier.", History={ new("26/09 11:00","Rejetée","Dossier incomplet."), Rec("24/09 08:50") } },
    };

    public static Dictionary<string, List<Msg>> Msgs = new()
    {
        ["REQ-0042"] = new()
        {
            new(Advisor,"04/10 09:05","Bonjour Monsieur Nathan, votre demande est prise en charge. Le délai estimé est de 2 jours ouvrés."),
            new(Me,"04/10 09:18","Merci. Je le retirerai à mon agence habituelle.", true),
            new(Advisor,"05/10 14:20","Votre chéquier est en préparation. Il reste 1 j 4 h de traitement. Je vous préviendrai dès sa disponibilité."),
            new(Me,"05/10 14:26","Très bien, merci pour votre suivi.", true),
        }
    };
}
