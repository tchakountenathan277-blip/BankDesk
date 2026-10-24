using static BankDesk.UI;
namespace BankDesk;

// ───────────── Onglet « Messages » : liste des conversations ─────────────
public class MessagesPage : Pg
{
    readonly VerticalStackLayout list = new() { Padding = 16 };
    public MessagesPage()
    {
        bool adv = Store.Role == "advisor";
        Build(Header("Messages", adv ? "Conversations avec vos clients" : "Vos conversations"), list, Tabs("msg"));
        Refresh();
    }
    protected override void Reload() => Refresh();
    void Refresh()
    {
        list.Clear();
        bool adv = Store.Role == "advisor";
        var convs = Store.Reqs.Where(r => (adv || r.Client == Store.Me) && Store.Msgs.GetValueOrDefault(r.Id)?.Count > 0).ToList();
        if (convs.Count == 0) { list.Add(L("Aucune conversation pour le moment.", 15, false, Mute)); return; }
        foreach (var r in convs)
        {
            var last = Store.Msgs[r.Id].Last();
            var col = new VerticalStackLayout { Spacing = 4, Children = {
                L(adv ? $"{r.Client} · {r.Title}" : r.Title, 16, true),
                L($"{last.From} : {last.Text}", 14, false, Mute),
                L($"{r.Id} · {last.At}", 12, false, Mute) } };
            var card = Box(col);
            card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Nav.Go(new ChatPage(r))) });
            list.Add(card);
        }
    }
}

// ───────────── Onglet « Profil » + déconnexion ─────────────
public class ProfilePage : Pg
{
    public ProfilePage()
    {
        bool adv = Store.Role == "advisor";
        string name = adv ? Store.Advisor : Store.Me;
        var info = Box(new VerticalStackLayout { Spacing = 6, Children = {
            L(name, 20, true), L(adv ? "Conseiller bancaire" : "Client BankDesk", 14, false, Mute) } });
        var logout = Btn("Se déconnecter", async () =>
        {
            await Api.LogoutAsync();
            Store.Clear();
            Nav.Go(new LoginPage());
        }, true);
        Build(Header("Profil", name), new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { info, logout } }, Tabs("pro"));
    }
}
