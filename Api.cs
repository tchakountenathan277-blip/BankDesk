using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace BankDesk;

// ============================================================================
//  CLIENT DE L'API : toutes les communications avec le serveur (back end) passent ici.
//  Les pages ne manipulent jamais HttpClient : elles appellent Api.XxxAsync(...).
// ============================================================================

/// <summary>Erreur "lisible" : son message est affiché tel quel à l'utilisateur dans une alerte.</summary>
public class ApiException : Exception { public ApiException(string message) : base(message) { } }

// ---- Formes des réponses JSON du serveur (reprennent les DTO de BankDesk.Api) ----
public record UserDto(int Id, string Nom, string Prenom, string NomComplet, string Contact, string Role);
public record LoginReply(string Jeton, UserDto Utilisateur);
public record FileDto(int Id, string Nom, long TailleOctets, string TypeMime);
public record EventDto(DateTime Date, string Titre, string Detail);
public record MsgDto(string Auteur, string AuteurRole, DateTime Date, string Texte, bool Lu);
public record ReqDto(string Reference, string Client, string Type, string Objet, string Description, string Statut,
                     DateTime DateCreation, int DelaiEstimeHeures, int HeuresRestantes, string Estimation,
                     List<FileDto> PiecesJointes, List<EventDto> Historique, List<MsgDto> Messages);

public static class Api
{
    // ⚠ ADRESSE DU SERVEUR : à adapter selon l'endroit où tourne l'application.
    //   - Windows (même PC que le serveur)  : http://localhost:5080
    //   - Émulateur Android                 : http://10.0.2.2:5080  (10.0.2.2 = le PC vu depuis l'émulateur)
    //   - Vrai téléphone (même Wi-Fi)       : http://<adresse IP du PC>:5080   (ex. http://192.168.1.20:5080)
#if ANDROID
    public static string BaseUrl = "http://10.0.2.2:5080";
#else
    public static string BaseUrl = "http://localhost:5080";
#endif

    /// <summary>Jeton de connexion reçu du serveur (null = personne n'est connecté).</summary>
    public static string? Token { get; private set; }

    static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(15) };
    static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);   // camelCase, insensible à la casse
    const string CleJeton = "bankdesk_jeton";

    // ------------------------------------------------------------------
    //  Cœur : envoi d'une requête HTTP + traduction des erreurs
    // ------------------------------------------------------------------

    /// <summary>Envoie la requête et renvoie le texte de la réponse. Lève ApiException avec un message clair en cas d'erreur.</summary>
    static async Task<string> Send(HttpMethod method, string path, HttpContent? body = null)
    {
        using var req = new HttpRequestMessage(method, BaseUrl + path) { Content = body };
        if (Token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);   // identifie l'utilisateur

        HttpResponseMessage rep;
        try { rep = await http.SendAsync(req); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Serveur éteint, mauvaise adresse, pas de réseau...
            throw new ApiException($"Impossible de joindre le serveur ({BaseUrl}). Vérifiez qu'il est lancé.");
        }

        using (rep)
        {
            string texte = await rep.Content.ReadAsStringAsync();
            if (rep.IsSuccessStatusCode) return texte;
            throw new ApiException(MessageErreur((int)rep.StatusCode, texte));
        }
    }

    /// <summary>Extrait le message d'erreur du serveur (champ "detail"), sinon un message adapté au code HTTP.</summary>
    static string MessageErreur(int code, string texte)
    {
        try
        {
            using var doc = JsonDocument.Parse(texte);
            if (doc.RootElement.TryGetProperty("detail", out var d) && d.GetString() is { Length: > 0 } detail)
                return detail;
        }
        catch (JsonException) { /* réponse non JSON : on passe au message par défaut */ }

        return code switch
        {
            401 => Token != null ? "Session expirée : reconnectez-vous." : "Identifiants incorrects.",
            403 => "Action non autorisée pour votre profil.",
            404 => "Élément introuvable.",
            413 => "Fichier trop volumineux.",
            _ => $"Erreur du serveur ({code})."
        };
    }

    static StringContent Corps(object o) => new(JsonSerializer.Serialize(o, json), Encoding.UTF8, "application/json");
    static T Lire<T>(string texte) => JsonSerializer.Deserialize<T>(texte, json) ?? throw new ApiException("Réponse du serveur invalide.");

    // ------------------------------------------------------------------
    //  Authentification
    // ------------------------------------------------------------------

    /// <summary>Connexion : mémorise le jeton (en mémoire et dans le stockage sécurisé de l'appareil).</summary>
    public static async Task<UserDto> LoginAsync(string contact, string motDePasse)
    {
        var rep = Lire<LoginReply>(await Send(HttpMethod.Post, "/api/auth/connexion", Corps(new { contact = contact.Trim(), motDePasse })));
        Token = rep.Jeton;
        try { await SecureStorage.Default.SetAsync(CleJeton, rep.Jeton); } catch { /* stockage indisponible : sans gravité */ }
        return rep.Utilisateur;
    }

    /// <summary>Reprend la session précédente si un jeton valide est mémorisé (utilisé par "Connexion par empreinte").</summary>
    public static async Task<UserDto?> RestoreAsync()
    {
        try
        {
            string? jeton = await SecureStorage.Default.GetAsync(CleJeton);
            if (string.IsNullOrEmpty(jeton)) return null;
            Token = jeton;
            return Lire<UserDto>(await Send(HttpMethod.Get, "/api/auth/moi"));
        }
        catch { Token = null; return null; }   // jeton expiré, serveur éteint, stockage indisponible...
    }

    public static Task RegisterAsync(string nom, string prenom, string contact, string motDePasse)
        => Send(HttpMethod.Post, "/api/auth/inscription", Corps(new { nom, prenom, contact, motDePasse }));

    /// <summary>Déconnexion : ferme la session côté serveur (sans erreur si le serveur est injoignable) et oublie le jeton.</summary>
    public static async Task LogoutAsync()
    {
        try { if (Token != null) await Send(HttpMethod.Post, "/api/auth/deconnexion"); } catch (ApiException) { }
        Token = null;
        try { SecureStorage.Default.Remove(CleJeton); } catch { }
    }

    // ------------------------------------------------------------------
    //  Requêtes
    // ------------------------------------------------------------------

    /// <summary>Toutes les requêtes visibles par l'utilisateur, avec historique, pièces jointes et messages.</summary>
    public static async Task<List<ReqDto>> GetRequestsAsync()
        => Lire<List<ReqDto>>(await Send(HttpMethod.Get, "/api/requetes"));

    public static async Task<ReqDto> CreateRequestAsync(string type, string objet, string description)
        => Lire<ReqDto>(await Send(HttpMethod.Post, "/api/requetes", Corps(new { type, objet = objet.Trim(), description = description.Trim() })));

    /// <summary>Le conseiller change le statut ; la réponse (facultative) part dans l'historique et la messagerie.</summary>
    public static Task ChangeStatusAsync(string reference, string statut, string? reponse)
        => Send(HttpMethod.Put, $"/api/requetes/{reference}/statut", Corps(new { statut, reponse }));

    /// <summary>Envoie un fichier (JPG, PNG, PDF ; 10 Mo max) en pièce jointe.</summary>
    public static async Task UploadAsync(string reference, FileResult fichier)
    {
        using var contenu = new MultipartFormDataContent();
        await using var flux = await fichier.OpenReadAsync();
        var partie = new StreamContent(flux);
        partie.Headers.ContentType = new MediaTypeHeaderValue(Path.GetExtension(fichier.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        });
        contenu.Add(partie, "fichier", fichier.FileName);   // "fichier" = nom du champ attendu par le serveur
        await Send(HttpMethod.Post, $"/api/requetes/{reference}/pieces-jointes", contenu);
    }

    // ------------------------------------------------------------------
    //  Messagerie
    // ------------------------------------------------------------------

    public static async Task<List<MsgDto>> GetMessagesAsync(string reference)
        => Lire<List<MsgDto>>(await Send(HttpMethod.Get, $"/api/requetes/{reference}/messages"));

    public static Task SendMessageAsync(string reference, string texte)
        => Send(HttpMethod.Post, $"/api/requetes/{reference}/messages", Corps(new { texte = texte.Trim() }));

    /// <summary>Marque comme lus les messages de l'autre personne (sans erreur visible si ça échoue).</summary>
    public static async Task MarkReadAsync(string reference)
    {
        try { await Send(HttpMethod.Post, $"/api/requetes/{reference}/messages/lus"); } catch (ApiException) { }
    }
}
