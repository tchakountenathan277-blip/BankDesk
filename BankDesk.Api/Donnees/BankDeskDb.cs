using Microsoft.EntityFrameworkCore;

namespace BankDesk.Api.Donnees;

/// <summary>
/// Porte d'entrée vers la base de données : chaque DbSet est une table interrogée avec LINQ
/// (ex. : db.Requetes.Where(r => r.Statut == "Reçue")).
/// </summary>
public class BankDeskDb : DbContext
{
    public BankDeskDb(DbContextOptions<BankDeskDb> options) : base(options) { }

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Requete> Requetes => Set<Requete>();
    public DbSet<PieceJointe> PiecesJointes => Set<PieceJointe>();
    public DbSet<EvenementHistorique> Evenements => Set<EvenementHistorique>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Deux comptes ne peuvent pas avoir le même identifiant ; deux requêtes la même référence.
        modelBuilder.Entity<Utilisateur>().HasIndex(u => u.Contact).IsUnique();
        modelBuilder.Entity<Requete>().HasIndex(r => r.Reference).IsUnique();
        // Les sessions sont cherchées par l'empreinte du jeton : un index accélère la recherche.
        modelBuilder.Entity<Session>().HasIndex(s => s.JetonHash);
    }
}
