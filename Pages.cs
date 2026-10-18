using RoundRectangle = Microsoft.Maui.Controls.Shapes.RoundRectangle;
using static BankDesk.UI;
namespace BankDesk;

// ───────────── Connexion ─────────────
public class LoginPage : Pg
{
    public LoginPage()
    {
        var id = Field("Téléphone ou e-mail", "Votre téléphone ou e-mail", out var eId);
        var pw = Field("Mot de passe", "Saisissez votre mot de passe", out var ePw, true, "🔒");
        void Enter(string role) { Store.Role = role; Nav.Go(role == "advisor" ? new QueuePage() : new MyRequestsPage()); }
        var forgot = L("Mot de passe oublié ?", 14, true, Teal); forgot.HorizontalOptions = LayoutOptions.End; forgot.Margin = new Thickness(0, 0, 0, 14);
        var create = L("Créer un compte", 16, true, Teal); create.HorizontalOptions = LayoutOptions.Center;
        create.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Nav.Go(new RegisterPage())) });
        Build(null, new VerticalStackLayout
        {
            Padding = new Thickness(24, 60, 24, 24), Spacing = 4,
            Children = { Logo("Vos demandes bancaires, sans file d'attente"), id, pw, forgot,
                Btn("Se connecter", () => {
                    if (string.IsNullOrWhiteSpace(eId.Text) || (ePw.Text?.Length ?? 0) < 4) { DisplayAlert("Connexion", "Identifiant et mot de passe (4 car. min) requis.", "OK"); return; }
                    Enter(eId.Text.ToLower().Contains("conseiller") ? "advisor" : "client"); }),
                new BoxView { HeightRequest = 8, Color = Colors.Transparent },
                Btn("🖐  Connexion par empreinte", () => Enter("client"), true),
                L("Démo : un identifiant contenant « conseiller » ouvre l'espace conseiller", 12, false, Mute),
                new BoxView { HeightRequest = 30, Color = Colors.Transparent }, create, Secure() }
        });
    }
    public static View Logo(string sub)
    {
        var b = new Border { WidthRequest = 70, HeightRequest = 70, BackgroundColor = Teal, Stroke = Teal, StrokeShape = new RoundRectangle { CornerRadius = 18 },
            HorizontalOptions = LayoutOptions.Center, Content = new Label { Text = "B", FontSize = 40, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
        var t = L("BankDesk", 36, true); t.HorizontalOptions = LayoutOptions.Center;
        var s = L(sub, 15, false, Mute); s.HorizontalOptions = LayoutOptions.Center; s.Margin = new Thickness(0, 0, 0, 24);
        return new VerticalStackLayout { Spacing = 8, Children = { b, t, s } };
    }
    public static View Secure() { var l = L("🛡  Vos données sont protégées et confidentielles", 12, false, Mute); l.HorizontalOptions = LayoutOptions.Center; l.Margin = new Thickness(0, 12); return l; }
}

// ───────────── Inscription ─────────────
public class RegisterPage : Pg
{
    public RegisterPage()
    {
        var nom = Field("Nom", "Votre nom", out var eN); var pre = Field("Prénom", "Votre prénom", out var eP);
        var id = Field("Téléphone ou e-mail", "Votre téléphone ou e-mail", out var eI);
        var p1 = Field("Mot de passe", "Saisissez votre mot de passe", out var e1, true, "🔒");
        var p2 = Field("Confirmer le mot de passe", "Confirmez votre mot de passe", out var e2, true, "🔒");
        var login = L("J'ai déjà un compte", 16, true, Teal); login.HorizontalOptions = LayoutOptions.Center; login.Margin = new Thickness(0, 12);
        login.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Nav.Go(new LoginPage())) });
        Build(null, new VerticalStackLayout
        {
            Padding = new Thickness(24, 40, 24, 24),
            Children = { LoginPage.Logo("Créez votre compte en quelques étapes"), nom, pre, id, p1, p2,
                Btn("Créer mon compte", async () => {
                    if (string.IsNullOrWhiteSpace(eN.Text) || string.IsNullOrWhiteSpace(eP.Text) || string.IsNullOrWhiteSpace(eI.Text)) { await DisplayAlert("Champs requis", "Renseignez tous les champs.", "OK"); return; }
                    if ((e1.Text?.Length ?? 0) < 8) { await DisplayAlert("Mot de passe", "8 caractères minimum.", "OK"); return; }
                    if (e1.Text != e2.Text) { await DisplayAlert("Mot de passe", "Les mots de passe ne correspondent pas.", "OK"); return; }
                    await DisplayAlert("Compte créé", "Vous pouvez vous connecter.", "OK"); Nav.Go(new LoginPage()); }),
                login, LoginPage.Secure() }
        });
    }
}

// ───────────── Carte de requête (client + conseiller) ─────────────
static class Rows
{
    public static View Row(Req r, bool advisor, Action tap)
    {
        bool late = advisor && r.Late;
        var title = new VerticalStackLayout();
        if (advisor) title.Add(L(r.Client, 14, false, Mute));
        title.Add(L(r.Title, 17, true));
        var top = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
        top.Add(new Label { Text = late ? "⚠️" : Icons[r.Type], FontSize = 22, VerticalOptions = LayoutOptions.Center }, 0);
        top.Add(title, 1); top.Add(L("›", 24, false, Mute), 2);
        var meta = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) }, ColumnSpacing = 6, Margin = new Thickness(0, 10, 0, 0) };
        meta.Add(L($"{r.Id} · {r.Date}", 14, false, Mute), 0);
        if (late) meta.Add(Badge("Rejetée", $"Retard {-r.Hours} h"), 1);
        meta.Add(Badge(r.Status), 2);
        var col = new VerticalStackLayout { Children = { top, meta } };
        if (r.Status == "En cours" || late) col.Add(L("🕓 " + (late ? Delay(r.Hours) : $"délai estimé {r.Est}"), 13, false, late ? Red : Mute));
        var card = Box(col, late ? Red : null, sw: late ? 2 : 1);
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(tap) });
        return card;
    }
}

// ───────────── Mes requêtes (client) ─────────────
public class MyRequestsPage : Pg
{
    string filter = "Toutes"; readonly VerticalStackLayout list = new() { Padding = new Thickness(16, 0) }; readonly FlexLayout chips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
    public MyRequestsPage()
    {
        var head = Header("Mes requêtes", $"Bonjour, {Store.Me}");
        var fab = Btn("+ Nouvelle requête", () => Nav.Go(new NewRequestPage())); fab.HorizontalOptions = LayoutOptions.End; fab.Padding = new Thickness(24, 0); fab.Margin = new Thickness(16, 0, 16, 16);
        var body = new VerticalStackLayout { Children = { new VerticalStackLayout { Padding = new Thickness(16, 14, 16, 0), Children = { chips } }, list, fab } };
        Build(head, body, Tabs("req")); Refresh();
    }
    void Refresh()
    {
        chips.Clear();
        foreach (var f in new[] { "Toutes", "En cours", "Résolues" }) chips.Add(Chip(f, filter == f, () => { filter = f; Refresh(); }));
        var mine = Store.Reqs.Where(r => r.Client == Store.Me && (filter == "Toutes" || (filter == "En cours" ? r.Status is "Reçue" or "En cours" : r.Status == "Résolue"))).ToList();
        list.Clear();
        list.Add(new Label { Text = $"{mine.Count} requêtes", TextColor = Mute, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });
        foreach (var r in mine) list.Add(Rows.Row(r, false, () => Nav.Go(new TrackingPage(r))));
    }
}

// ───────────── Nouvelle requête ─────────────
public class NewRequestPage : Pg
{
    string type = "Chéquier"; readonly List<(string, string)> files = new();
    readonly FlexLayout chips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap }; readonly VerticalStackLayout fileBox = new();
    readonly Label count = L("0 fichier", 14, false, Mute);
    public NewRequestPage()
    {
        var obj = Field("Objet", "Objet de votre demande", out var eObj, false, "✏️");
        var desc = new Editor { HeightRequest = 120, Placeholder = "Décrivez votre demande…", TextColor = Navy, PlaceholderColor = Mute };
        async Task Add(FileResult? f)
        {
            if (f == null) return;
            using var s = await f.OpenReadAsync(); double mb = s.Length / 1048576.0;
            if (mb > 10) { await DisplayAlert("Fichier trop lourd", "10 Mo maximum par fichier.", "OK"); return; }
            files.Add((f.FileName, $"{Path.GetExtension(f.FileName).TrimStart('.').ToUpper()} · {mb:0.0} Mo")); RefreshFiles();
        }
        var photo = Btn("+ Photo", async () => await Add(await MediaPicker.Default.PickPhotoAsync()), true);
        var file = Btn("+ Fichier", async () => await Add(await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "JPG, PNG ou PDF" })), true);
        var pics = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) }, ColumnSpacing = 10 }; pics.Add(photo, 0); pics.Add(file, 1);
        var head = new Grid { ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } }; head.Add(L("Pièces jointes", 15, true), 0); head.Add(count, 1);
        var send = Btn("Envoyer la requête", async () => {
            if (string.IsNullOrWhiteSpace(eObj.Text) || string.IsNullOrWhiteSpace(desc.Text)) { await DisplayAlert("Champs requis", "Renseignez l'objet et la description.", "OK"); return; }
            var est = type switch { "Carte" or "Autre" => "3 jours ouvrés", "Crédit" => "5 jours ouvrés", _ => "2 jours ouvrés" };
            var r = new Req { Id = $"REQ-{43 + Store.Reqs.Count - 7:0000}", Client = Store.Me, Type = type, Title = eObj.Text, Desc = desc.Text, Date = DateTime.Now.ToString("dd/MM/yyyy"), Hours = 48, Est = est };
            r.Files.AddRange(files); r.History.Add(new Hist(Store.Now(), "Requête reçue", "Votre demande a bien été enregistrée."));
            Store.Reqs.Insert(0, r); Nav.Go(new MyRequestsPage()); });
        RefreshChips();
        Build(Header("Nouvelle requête", null, () => Nav.Go(new MyRequestsPage())), new VerticalStackLayout
        {
            Padding = 18, BackgroundColor = Colors.White,
            Children = { L("Type de requête", 15, true), chips, obj, L("Description", 15, true), Box(desc, pad: new Thickness(8), r: 12), head, fileBox, pics,
                L("JPG, PNG ou PDF · 10 Mo maximum par fichier", 12, false, Mute), new BoxView { HeightRequest = 8, Color = Colors.Transparent }, send }
        });
    }
    void RefreshChips() { chips.Clear(); foreach (var t in Icons.Keys) chips.Add(Chip(t, t == type, () => { type = t; RefreshChips(); })); }
    void RefreshFiles()
    {
        fileBox.Clear(); count.Text = $"{files.Count} fichier{(files.Count > 1 ? "s" : "")}";
        foreach (var (n, s) in files.ToList())
        {
            var g = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) }, ColumnSpacing = 12 };
            g.Add(new Label { Text = "🖼️", FontSize = 22 }, 0); g.Add(new VerticalStackLayout { Children = { L(n, 15, true), L(s, 12, false, Mute) } }, 1);
            var x = L("✕", 18, false, Mute); x.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { files.Remove((n, s)); RefreshFiles(); }) }); g.Add(x, 2);
            fileBox.Add(Box(g, pad: new Thickness(12, 8)));
        }
    }
}

// ───────────── Suivi ─────────────
public class TrackingPage : Pg
{
    public TrackingPage(Req r)
    {
        int cur = r.Status switch { "Reçue" => 1, "En cours" => 3, _ => 4 };
        string[] steps = { "Reçue", "Prise en charge", "En cours", "Résolue" };
        var grid = new Grid { Margin = new Thickness(0, 14) };
        grid.Add(new BoxView { HeightRequest = 2, Color = Line, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(40, 14) }, 0, 0);
        Grid.SetColumnSpan(grid.Children[0] as BindableObject, 4);
        for (int i = 0; i < 4; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            bool done = i + 1 < cur || (i + 1 == cur && r.Status is "Résolue" or "Rejetée"), now = i + 1 == cur && !done;
            var dot = new Border { WidthRequest = 30, HeightRequest = 30, StrokeShape = new RoundRectangle { CornerRadius = 15 }, StrokeThickness = 2, HorizontalOptions = LayoutOptions.Center,
                BackgroundColor = done ? Teal : Colors.White, Stroke = done || now ? Teal : Line,
                Content = new Label { Text = done ? "✓" : $"{i + 1}", TextColor = done ? Colors.White : now ? Teal : Mute, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center } };
            var lb = L(steps[i], 11, now, now ? Teal : Mute); lb.HorizontalOptions = LayoutOptions.Center;
            grid.Add(new VerticalStackLayout { Spacing = 6, Children = { dot, lb } }, i, 0);
        }
        var delayTxt = r.Status is "Résolue" or "Rejetée" ? "Requête clôturée" : "Délai de traitement: " + Delay(r.Hours);
        var delay = Box(new VerticalStackLayout { Spacing = 8, Children = { L("🕓  " + delayTxt, 16, true), L("Délai estimé : " + r.Est, 14, false, Mute) } });
        var hist = new VerticalStackLayout { Spacing = 14 };
        for (int i = 0; i < r.History.Count; i++)
        {
            var h = r.History[i];
            var dot = new BoxView { WidthRequest = 10, HeightRequest = 10, CornerRadius = 5, Color = i == 0 ? Teal : Color.FromArgb("#2B3F66"), VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) };
            var txt = new VerticalStackLayout { Children = { new HorizontalStackLayout { Spacing = 8, Children = { L(h.D, 13, false, Mute), L(h.S, 15, true) } }, L(h.T, 14, false, Mute) } };
            var row = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 12 }; row.Add(dot, 0); row.Add(txt, 1); hist.Add(row);
        }
        var badges = new HorizontalStackLayout { Spacing = 10, Children = { Badge(r.Status), L($"Créée le {r.Date}", 14, false, Mute) } };
        Build(Header("Suivi", r.Id, () => Nav.Go(new MyRequestsPage())), new VerticalStackLayout
        {
            Padding = 18, Spacing = 6,
            Children = { L(r.Title, 22, true), badges, grid, delay, L("Historique", 19, true), hist,
                new BoxView { HeightRequest = 6, Color = Colors.Transparent }, Btn("💬  Écrire au conseiller", () => Nav.Go(new ChatPage(r))) }
        });
    }
}

// ───────────── Messagerie ─────────────
public class ChatPage : Pg
{
    readonly Req r; readonly VerticalStackLayout box = new() { Padding = 16, Spacing = 12 }; readonly ScrollView sv = new();
    public ChatPage(Req req)
    {
        r = req; bool adv = Store.Role == "advisor"; string me = adv ? Store.Advisor : Store.Me;
        var bar = new HorizontalStackLayout { Padding = 14, Spacing = 10, Children = { Badge(r.Status), L("Délai estimé : " + r.Est, 14, false, Mute) } };
        var field = Field("", "Votre message…", out var e, false, "✏️"); 
        void Send() { if (string.IsNullOrWhiteSpace(e.Text)) return; if (!Store.Msgs.ContainsKey(r.Id)) Store.Msgs[r.Id] = new(); Store.Msgs[r.Id].Add(new Msg(me, Store.Now(), e.Text.Trim())); e.Text = ""; Draw(me); }
        sv.Content = box;
        var foot = new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { field, Btn("💬  Envoyer", Send) } };
        var g = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        g.Add(Header(adv ? r.Client : $"{Store.Advisor}, conseiller", $"{r.Id} · {r.Title}", () => Nav.Go(adv ? new ProcessPage(r) : new TrackingPage(r))), 0, 0);
        g.Add(bar, 0, 1); g.Add(sv, 0, 2); g.Add(foot, 0, 3); Content = g; Draw(me);
    }
    async void Draw(string me)
    {
        box.Clear();
        foreach (var m in Store.Msgs.GetValueOrDefault(r.Id) ?? new())
        {
            bool mine = m.From == me;
            var meta = L($"{m.From} · {m.At}{(mine && m.Read ? " · Lu" : "")}", 12, false, mine ? Color.FromArgb("#D6F0F0") : Mute); meta.HorizontalTextAlignment = TextAlignment.End;
            var col = new VerticalStackLayout { Children = { L(m.Text, 16, false, mine ? Colors.White : Navy), meta } };
            var b = Box(col, mine ? Teal : Line, null, 14, mine ? Teal : Colors.White); b.Margin = 0; b.MaximumWidthRequest = 300;
            b.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start; box.Add(b);
        }
        await Task.Delay(50); await sv.ScrollToAsync(0, box.Height, false);
    }
}

// ───────────── Conseiller : file ─────────────
public class QueuePage : Pg
{
    string tab = "Nouvelles"; readonly FlexLayout chips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap }; readonly VerticalStackLayout list = new();
    public QueuePage()
    {
        Build(Header("Requêtes à traiter", $"Bonjour, {Store.Advisor}"), new VerticalStackLayout { Padding = 16, Children = { chips, list } }, Tabs("req")); Refresh();
    }
    void Refresh()
    {
        var all = Store.Reqs; var nw = all.Where(r => r.Status == "Reçue").ToList(); var cur = all.Where(r => r.Status == "En cours").ToList(); var late = all.Where(r => r.Late).ToList();
        chips.Clear();
        foreach (var (k, n) in new[] { ("Nouvelles", nw.Count), ("En cours", cur.Count), ("En retard", late.Count) }) chips.Add(Chip($"{k} {n}", tab == k, () => { tab = k; Refresh(); }));
        var src = tab == "Nouvelles" ? nw : tab == "En cours" ? cur : late;
        list.Clear(); list.Add(new Label { Text = $"{src.Count} requêtes", TextColor = Mute, FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });
        foreach (var r in src.OrderBy(r => r.Hours)) list.Add(Rows.Row(r, true, () => Nav.Go(new ProcessPage(r))));
    }
}

// ───────────── Conseiller : traitement ─────────────
public class ProcessPage : Pg
{
    string sel; readonly FlexLayout chips = new() { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
    public ProcessPage(Req r)
    {
        sel = r.Status;
        var head = Box(new VerticalStackLayout { Spacing = 8, Children = { L(r.Title, 22, true), new HorizontalStackLayout { Spacing = 10, Children = { Badge(r.Status), L($"Créée le {r.Date}", 14, false, Mute) } } } });
        var det = new VerticalStackLayout { Spacing = 8, Children = { L("Description et pièce jointe", 17, true), L(r.Desc, 16) } };
        foreach (var (n, s) in r.Files) det.Add(Box(new HorizontalStackLayout { Spacing = 12, Children = { new Label { Text = "🖼️", FontSize = 22 }, new VerticalStackLayout { Children = { L(n, 15, true), L(s, 12, false, Mute) } } } }, pad: new Thickness(12, 8)));
        var reply = new Editor { HeightRequest = 120, Placeholder = "Votre réponse au client…", TextColor = Navy, PlaceholderColor = Mute };
        void Apply(string st) { r.Status = st; r.History.Insert(0, new Hist(Store.Now(), st, string.IsNullOrWhiteSpace(reply.Text) ? $"Statut mis à jour : {st}." : reply.Text.Trim()));
            if (!string.IsNullOrWhiteSpace(reply.Text)) { if (!Store.Msgs.ContainsKey(r.Id)) Store.Msgs[r.Id] = new(); Store.Msgs[r.Id].Add(new Msg(Store.Advisor, Store.Now(), reply.Text.Trim())); }
            Nav.Go(new QueuePage()); }
        var btns = new Grid { ColumnDefinitions = { new(GridLength.Star), new(new GridLength(1.3, GridUnitType.Star)) }, ColumnSpacing = 12 };
        btns.Add(Btn("Rejeter", () => Apply("Rejetée"), true), 0);
        btns.Add(Btn("Marquer résolue", () => Apply(sel is "Reçue" or "En cours" ? sel : "Résolue")), 1);
        void Chips() { chips.Clear(); foreach (var s in new[] { "Reçue", "En cours", "Résolue", "Rejetée" }) { var b = Badge(s, $"  {s}  "); b.Margin = new Thickness(0, 0, 8, 8); b.Opacity = s == sel ? 1 : .55; b.Stroke = s == sel ? Navy : Colors.Transparent; b.StrokeThickness = 2;
            b.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => { sel = s; Chips(); }) }); chips.Add(b); } }
        Chips();
        var chat = L("💬", 24, false, Colors.White); chat.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => Nav.Go(new ChatPage(r))) });
        var h = Header(r.Client, $"{r.Id} · {r.Title}", () => Nav.Go(new QueuePage())); h.Add(chat, 2); h.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        Build(h, new VerticalStackLayout { Padding = 16, Spacing = 8, Children = { head, L("Statuts", 17, true), chips, Box(det), L("Réponse au client", 17, true), Box(reply, pad: new Thickness(8), r: 12), btns } });
    }
}
