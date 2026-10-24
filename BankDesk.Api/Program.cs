using System.Text.Json;
using BankDesk.Api.Donnees;
using BankDesk.Api.Securite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

// =====================================================================
//  BankDesk.Api : le serveur (back end). Il démarre le serveur web, branche la base SQLite,
//  l'authentification par jeton et les contrôleurs.
// =====================================================================

var builder = WebApplication.CreateBuilder(args);

// Le serveur écoute sur toutes les interfaces réseau, port 5080 :
//   le même PC : http://localhost:5080   |   émulateur Android : http://10.0.2.2:5080
//   un téléphone sur le même Wi-Fi : http://<IP-du-PC>:5080
builder.WebHost.UseUrls("http://0.0.0.0:5080");

// Base de données : un simple fichier "bankdesk.db" créé à côté du programme.
builder.Services.AddDbContext<BankDeskDb>(o => o.UseSqlite("Data Source=bankdesk.db"));

// JSON en camelCase ("heuresRestantes") : c'est ce que lit l'application.
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.AddAuthentication(JetonAuthHandler.Schema)
    .AddScheme<AuthenticationSchemeOptions, JetonAuthHandler>(JetonAuthHandler.Schema, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();   // erreurs au format standard : l'application lit le champ "detail"

var app = builder.Build();

app.UseExceptionHandler();   // toute erreur inattendue devient une réponse 500 propre
app.UseStatusCodePages();    // 401/404 sans corps -> format standard
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Point de contrôle : http://localhost:5080/api/sante répond "ok" si le serveur tourne.
app.MapGet("/api/sante", () => Results.Ok(new { etat = "ok", heure = DateTime.UtcNow }));

// Création de la base au premier lancement + données de démonstration.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDeskDb>();
    db.Database.EnsureCreated();
    DonneesDemo.Remplir(db);
}

app.Run();
