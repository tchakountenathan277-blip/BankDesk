using Microsoft.Maui.Controls.Shapes;
using static BankDesk.UI;
namespace BankDesk;

// ───────────── Messages : liste des conversations ─────────────
public class MessagesPage : Pg
{
    public MessagesPage()
    {
        bool adv = Store.Role == "advisor";
        string me = adv ? Store.Advisor : Store.Me;
        var list = new VerticalStackLayout { Padding = 16 };
        int nb = 0;

        // Une ligne par requête qui contient au moins un message
        foreach (var r in Store.Reqs.Where(r => adv || r.Client == Store.Me).ToList())
        {
            var ms = Store.Msgs.GetValueOrDefault(r.Id);
            if (ms == null || !ms.Any()) continue;
            nb++;
            var last = ms.Last();
            int unread = ms.Count(m => m.From != me && !m.Read);

            var titre = L(adv ? $"{r.Client} · {r.Title}" : r.Title, 16, true);
            var apercu = L($"{last.From} : {last.Text}", 14, false, Mute);
            apercu.LineBreakMode = LineBreakMode.TailTruncation; apercu.MaxLines = 1;
            var col = new VerticalStackLayout { Spacing = 4, Children = { titre, apercu, L($"{r.Id} · {last.At}", 12, false, Mute) } };

            var g = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
            g.Add(new Label { Text = "💬", FontSize = 24, VerticalOptions = LayoutOptions.Center }, 0);
            g.Add(col, 1);
            if (unread > 0)
            {
                var pastille = Box(L(unread.ToString(), 12, true, Colors.White), Teal, new Thickness(9, 3), 10, Teal, 0);
                pastille.Margin = 0; pastille.VerticalOptions = LayoutOptions.Center;
                g.Add(pastille, 2);
            }

            var carte = Box(g);
            carte.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Nav.Go(new ChatPage(r))) });
            list.Add(carte);
        }

        if (nb == 0) list.Add(L("Aucune conversation pour le moment.", 15, false, Mute));
        Build(Header("Messages", $"{nb} conversation{(nb > 1 ? "s" : "")}"), list, Tabs("msg"));
    }
}

// ───────────── Profil : informations + déconnexion ─────────────
public class ProfilePage : Pg
{
    public ProfilePage()
    {
        bool adv = Store.Role == "advisor";
        string nom = adv ? Store.Advisor : Store.Me;

        // Pastille ronde avec l'initiale
        var avatar = new Border
        {
            WidthRequest = 84,
            HeightRequest = 84,
            BackgroundColor = Teal,
            Stroke = Teal,
            StrokeShape = new RoundRectangle { CornerRadius = 42 },
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = nom.Length > 0 ? nom.Substring(0, 1) : "?",
                FontSize = 36,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        var nomLbl = L(nom, 22, true); nomLbl.HorizontalOptions = LayoutOptions.Center;
        var roleLbl = L(adv ? "Conseiller" : "Client", 14, false, Mute); roleLbl.HorizontalOptions = LayoutOptions.Center;

        // Statistiques (3 cases)
        var tout = adv ? Store.Reqs : Store.Reqs.Where(r => r.Client == Store.Me).ToList();
        var stats = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10, Margin = new Thickness(0, 16, 0, 8) };
        stats.Add(Stat(tout.Count().ToString(), "Requêtes"), 0);
        stats.Add(Stat(adv ? tout.Count(r => r.Status == "Reçue").ToString() : tout.Count(r => r.Status is "Reçue" or "En cours").ToString(), adv ? "Nouvelles" : "En cours"), 1);
        stats.Add(Stat(adv ? tout.Count(r => r.Late).ToString() : tout.Count(r => r.Status == "Résolue").ToString(), adv ? "En retard" : "Résolues"), 2);

        // Bouton rouge de déconnexion : retour à l'écran de connexion
        var logout = Btn("Se déconnecter", () => Nav.Go(new LoginPage()), true);
        logout.TextColor = Red; logout.BorderColor = Red;

        Build(Header("Profil", adv ? "Espace conseiller" : "Espace client"), new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 6,
            Children = { avatar, nomLbl, roleLbl, stats, new BoxView { HeightRequest = 16, Color = Colors.Transparent }, logout }
        }, Tabs("pro"));
    }

    static View Stat(string valeur, string libelle)
    {
        var v = L(valeur, 24, true, Teal); v.HorizontalOptions = LayoutOptions.Center;
        var l = L(libelle, 12, false, Mute); l.HorizontalOptions = LayoutOptions.Center;
        return Box(new VerticalStackLayout { Spacing = 2, Children = { v, l } });
    }
}