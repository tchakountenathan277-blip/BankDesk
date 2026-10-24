using BankDesk.Api.Securite;

namespace BankDesk.Api.Donnees;

/// <summary>
/// Données de démonstration insérées au premier lancement (base vide). Ce sont les mêmes que celles
/// de l'application en mode simulation. Mot de passe de TOUS les comptes : 1234 (démo uniquement !).
///   clients     : pierre@bankdesk.com, amina@bankdesk.com, lucas@bankdesk.com, marie@bankdesk.com
///   conseiller  : etoa@bankdesk.com
/// </summary>
public static class DonneesDemo
{
    public static void Remplir(BankDeskDb db)
    {
        if (db.Utilisateurs.Any()) return;   // déjà rempli : on ne touche à rien

        Utilisateur Creer(string prenom, string nom, string contact, string role)
        {
            var (hash, sel) = MotDePasse.Hacher("1234");
            var u = new Utilisateur { Prenom = prenom, Nom = nom, Contact = contact, MotDePasseHash = hash, Sel = sel, Role = role };
            db.Utilisateurs.Add(u);
            return u;
        }

        var pierre = Creer("Pierre", "Nathan", "pierre@bankdesk.com", Valeurs.Client);
        var amina = Creer("Amina", "Benali", "amina@bankdesk.com", Valeurs.Client);
        var lucas = Creer("Lucas", "Fontaine", "lucas@bankdesk.com", Valeurs.Client);
        var marie = Creer("Marie", "Dupont", "marie@bankdesk.com", Valeurs.Client);
        var etoa = Creer("M.", "Etoa", "etoa@bankdesk.com", Valeurs.Conseiller);
        db.SaveChanges();

        var maintenant = DateTime.UtcNow;

        // "ecoule" = temps écoulé depuis la création : il fixe le délai restant (délai annoncé - écoulé).
        Requete Req(string reference, Utilisateur client, string type, string objet, string description, string statut, TimeSpan ecoule)
        {
            var r = new Requete
            {
                Reference = reference, Client = client, ClientId = client.Id, Type = type, Objet = objet,
                Description = description, Statut = statut, DateCreation = maintenant - ecoule,
                DelaiEstimeHeures = Valeurs.DelaiHeures(type)
            };
            db.Requetes.Add(r);
            return r;
        }
        void Evt(Requete r, TimeSpan apresCreation, string titre, string detail)
            => db.Evenements.Add(new EvenementHistorique { Requete = r, Date = r.DateCreation + apresCreation, Titre = titre, Detail = detail });
        void Msg(Requete r, TimeSpan apresCreation, Utilisateur auteur, string texte, bool lu)
            => db.Messages.Add(new Message { Requete = r, AuteurId = auteur.Id, AuteurRole = auteur.Role, AuteurNom = auteur.NomComplet, Texte = texte, Date = r.DateCreation + apresCreation, Lu = lu });
        string recu = "Votre demande a bien été enregistrée.";

        // REQ-0042 : chéquier en cours (environ 28 h restantes), conversation avec le conseiller.
        var r42 = Req("REQ-0042", pierre, "Chéquier", "Demande de chéquier",
            "Bonjour, je souhaite commander un nouveau chéquier de 25 chèques, à retirer dans mon agence habituelle. Merci.", "En cours", TimeSpan.FromHours(20));
        Evt(r42, TimeSpan.Zero, "Requête reçue", recu);
        Evt(r42, TimeSpan.FromHours(3), "Prise en charge", "Dossier vérifié par votre conseiller.");
        Evt(r42, TimeSpan.FromHours(10), "En cours", "Votre chéquier est en préparation.");
        Msg(r42, TimeSpan.FromHours(3), etoa, "Bonjour Monsieur Nathan, votre demande est prise en charge. Le délai estimé est de 2 jours ouvrés.", true);
        Msg(r42, TimeSpan.FromHours(4), pierre, "Merci. Je le retirerai à mon agence habituelle.", true);
        Msg(r42, TimeSpan.FromHours(10), etoa, "Votre chéquier est en préparation. Il reste 1 j 4 h de traitement. Je vous préviendrai dès sa disponibilité.", false);
        Msg(r42, TimeSpan.FromHours(10.1), pierre, "Très bien, merci pour votre suivi.", true);
        db.PiecesJointes.Add(new PieceJointe { Requete = r42, NomFichier = "CNI.jpg", TypeMime = "image/jpeg", TailleOctets = 1258291, CheminStockage = "" });

        // REQ-0041 : réclamation en retard (3 h de dépassement).
        var r41 = Req("REQ-0041", amina, "Réclamation", "Réclamation virement", "Un virement n'a pas été crédité.", "En cours", TimeSpan.FromHours(51));
        Evt(r41, TimeSpan.Zero, "Requête reçue", recu);

        var r40 = Req("REQ-0040", lucas, "Carte", "Remplacement de carte", "Ma carte est endommagée.", "Reçue", TimeSpan.FromHours(32));
        Evt(r40, TimeSpan.Zero, "Requête reçue", recu);

        var r39 = Req("REQ-0039", pierre, "Réclamation", "Réclamation prélèvement", "Prélèvement non autorisé.", "Reçue", TimeSpan.FromHours(18));
        Evt(r39, TimeSpan.Zero, "Requête reçue", recu);

        var r38 = Req("REQ-0038", marie, "Crédit", "Demande de crédit", "Crédit consommation.", "Reçue", TimeSpan.FromHours(30));
        Evt(r38, TimeSpan.Zero, "Requête reçue", recu);

        var r31 = Req("REQ-0031", pierre, "Carte", "Remplacement de carte", "Carte perdue.", "Résolue", TimeSpan.FromDays(22));
        Evt(r31, TimeSpan.Zero, "Requête reçue", recu);
        Evt(r31, TimeSpan.FromDays(2), "Résolue", "Votre nouvelle carte est disponible.");

        var r27 = Req("REQ-0027", pierre, "Crédit", "Demande de crédit", "Crédit immobilier.", "Rejetée", TimeSpan.FromDays(26));
        Evt(r27, TimeSpan.Zero, "Requête reçue", recu);
        Evt(r27, TimeSpan.FromDays(2), "Rejetée", "Dossier incomplet.");

        db.SaveChanges();
    }
}
