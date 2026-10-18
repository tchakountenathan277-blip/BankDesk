namespace BankDesk;

public partial class App : Application
{
    public App() { InitializeComponent(); }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Même contenu qu'avant : la page de connexion dans une NavigationPage.
        var fenetre = new Window(new NavigationPage(new LoginPage()));

#if WINDOWS
        // Taille d'un téléphone (portrait), pour que les écrans tiennent en entier
        // et pour des captures proches des maquettes. Ignoré sur Android et iOS.
        fenetre.Width = 390;
        fenetre.Height = 710;
        fenetre.X = 100;   // position de la fenêtre sur l'écran
        fenetre.Y = 10;
#endif

        return fenetre;
    }
}