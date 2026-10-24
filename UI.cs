using Microsoft.Maui.Controls.Shapes;
namespace BankDesk;

// Thème et composants réutilisables (reprend la charte de la maquette)
public static class UI
{
    public static readonly Color Teal = Color.FromArgb("#0F8B8D"), Navy = Color.FromArgb("#12243F"), Bg = Color.FromArgb("#F5F7FA"),
        Line = Color.FromArgb("#E3E8EF"), Mute = Color.FromArgb("#6B7A90"), Red = Color.FromArgb("#C0392B");
    public static readonly Dictionary<string, (string Bg, string Fg)> Status = new()
    {
        ["Reçue"] = ("#E3E9F3", "#34476B"), ["En cours"] = ("#FDEBC8", "#8A5A00"),
        ["Résolue"] = ("#D5EFE0", "#1E6B3F"), ["Rejetée"] = ("#FADBD8", "#A3261C"),
    };
    public static readonly Dictionary<string, string> Icons = new()
    { ["Réclamation"] = "⚠️", ["Chéquier"] = "📖", ["Carte"] = "💳", ["Crédit"] = "🏛️", ["Autre"] = "📄" };

    public static string Delay(int h) => h < 0 ? $"Délai dépassé de {-h} h" : $"{h / 24} j {h % 24} h restant";

    public static Label L(string t, double size = 15, bool bold = false, Color? c = null) => new()
    { Text = t, FontSize = size, TextColor = c ?? Navy, FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None };

    public static Border Box(View v, Color? stroke = null, Thickness? pad = null, double r = 14, Color? fill = null, double sw = 1) => new()
    {
        Content = v, Padding = pad ?? new Thickness(14), BackgroundColor = fill ?? Colors.White, Stroke = stroke ?? Line,
        StrokeThickness = sw, StrokeShape = new RoundRectangle { CornerRadius = r }, Margin = new Thickness(0, 0, 0, 12)
    };

    public static Border Badge(string status, string? text = null)
    {
        var (bg, fg) = Status[status];
        var b = Box(L(text ?? status, 12, true, Color.FromArgb(fg)), Colors.Transparent, new Thickness(10, 4), 8, Color.FromArgb(bg), 0);
        b.Margin = 0; b.HorizontalOptions = LayoutOptions.End; return b;
    }

    public static Button Btn(string text, Action tap, bool outline = false)
    {
        var b = new Button { Text = text, CornerRadius = 12, HeightRequest = 50, FontAttributes = FontAttributes.Bold, FontSize = 16,
            BackgroundColor = outline ? Colors.White : Teal, TextColor = outline ? Teal : Colors.White, BorderColor = Teal, BorderWidth = 1 };
        b.Clicked += (_, _) => tap(); return b;
    }

    public static Button Chip(string text, bool on, Action tap)
    {
        var b = new Button { Text = text, CornerRadius = 20, HeightRequest = 38, Padding = new Thickness(16, 0), FontSize = 14, Margin = new Thickness(0, 0, 8, 8),
            BackgroundColor = on ? Teal : Colors.White, TextColor = on ? Colors.White : Mute, BorderColor = on ? Teal : Line, BorderWidth = 1 };
        b.Clicked += (_, _) => tap(); return b;
    }

    public static View Field(string label, string placeholder, out Entry entry, bool secure = false, string icon = "👤")
    {
        entry = new Entry { Placeholder = placeholder, IsPassword = secure, TextColor = Navy, PlaceholderColor = Mute, HorizontalOptions = LayoutOptions.Fill };
        var g = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 8 };
        g.Add(new Label { Text = icon, VerticalOptions = LayoutOptions.Center }, 0); g.Add(entry, 1);
        return new VerticalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 0, 14), Children = { L(label, 15, true), Box(g, pad: new Thickness(14, 0), r: 12) } };
    }

    public static Grid Header(string title, string? sub = null, Action? back = null)
    {
        var s = new VerticalStackLayout { Children = { L(title, 24, true, Colors.White) } };
        if (sub != null) s.Add(L(sub, 14, false, Color.FromArgb("#B8C4D6")));
        var g = new Grid { BackgroundColor = Navy, Padding = new Thickness(20, 50, 20, 18), ColumnSpacing = 14,
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) } };
        if (back != null) { var bl = L("←", 26, true, Colors.White); bl.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(back) }); g.Add(bl, 0); }
        g.Add(s, 1); return g;
    }

    public static Grid Tabs(string active)
    {
        var items = new[] { ("req", "Requêtes", "📑"), ("msg", "Messages", "💬"), ("pro", "Profil", "👤") };
        var g = new Grid { BackgroundColor = Colors.White, Padding = new Thickness(0, 10, 0, 18) };
        for (int i = 0; i < 3; i++)
        {
            g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var (k, t, ic) = items[i]; bool on = k == active;
            var lbl = L(t, 12, on, on ? Teal : Mute); lbl.HorizontalOptions = LayoutOptions.Center;
            var s = new VerticalStackLayout { Children = { new Label { Text = ic, FontSize = 22, HorizontalOptions = LayoutOptions.Center }, lbl } };
            if (!on) s.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() =>
            {
                if (k == "req") Nav.Go(Store.Role == "advisor" ? new QueuePage() : new MyRequestsPage());
                else if (k == "msg") Nav.Go(new MessagesPage());
                else Nav.Go(new ProfilePage());
            }) });
            g.Add(s, i);
        }
        return g;
    }
}

public static class Nav
{
    public static void Go(Page p) { if (Application.Current?.Windows.Count > 0) Application.Current.Windows[0].Page = new NavigationPage(p); }
}

// Page de base : en-tête fixe + contenu défilant + pied optionnel
public abstract class Pg : ContentPage
{
    protected Pg() { NavigationPage.SetHasNavigationBar(this, false); BackgroundColor = UI.Bg; }

    // À chaque affichage : on recharge les données depuis le serveur, puis la page se redessine (Reload).
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!Store.LoggedIn) return;
        try { await Store.LoadAsync(); Reload(); } catch (ApiException) { /* serveur éteint : on garde les données déjà affichées */ }
    }
    protected virtual void Reload() { }
    protected void Build(View? header, View body, View? footer = null, bool scroll = true)
    {
        var g = new Grid { RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        if (header != null) g.Add(header, 0, 0);
        g.Add(scroll ? new ScrollView { Content = body } : body, 0, 1);
        if (footer != null) g.Add(footer, 0, 2);
        Content = g;
    }
}