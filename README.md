# BankDesk

Application mobile de dépôt et de suivi de demandes bancaires.

- **Front end** : application .NET MAUI (C#), interface construite en code.
- **Back end** : API REST ASP.NET Core (.NET 10) + base SQLite (dossier `BankDesk.Api`).

## Fonctionnalités

- Connexion / inscription (mots de passe hachés PBKDF2, session par jeton).
- Création d'une requête par type (réclamation, chéquier, carte, crédit, autre) avec pièces jointes (JPG, PNG, PDF, 10 Mo max).
- Suivi : étapes, délai de traitement restant, historique.
- Messagerie client ↔ conseiller (actualisation automatique).
- Espace conseiller : file (nouvelles, en cours, en retard), traitement (statut + réponse, rejet avec motif).
- Règles appliquées côté serveur : rôles, propriété des requêtes, validation, vérification du contenu des fichiers.

## Prérequis

- Visual Studio 2026 avec la charge de travail « Développement d'applications multiplateformes .NET »
- SDK .NET 10

## Lancer le serveur (à faire en premier)

```
cd BankDesk.Api
dotnet run
```

Le serveur écoute sur `http://localhost:5080`. Test : `http://localhost:5080/api/sante`.
La base `bankdesk.db` est créée au premier lancement avec des données de démonstration.

## Lancer l'application

Ouvrir `BankDesk.slnx` dans Visual Studio, choisir la cible **Windows Machine**, puis F5.

## Comptes de démonstration (mot de passe : `1234`)

| Compte | Rôle |
|---|---|
| pierre@bankdesk.com | client |
| etoa@bankdesk.com | conseiller |

## Android

L'émulateur joint le PC via `10.0.2.2` (déjà géré dans `Api.cs`). Pour le HTTP en clair, ajouter
`android:usesCleartextTraffic="true"` dans la balise `<application>` de `Platforms/Android/AndroidManifest.xml`.

## Auteur

Projet BankDesk — FEUPA / NATHAN.
