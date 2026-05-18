using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OS.ViewModels; // Sau namespace-ul unde ai pus ViewModel-ul

namespace OS; // Asigură-te că acest namespace este IDENTIC cu cel din XAML (x:Class)

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Setăm contextul de date pentru a face legătura cu butoanele și listele
        DataContext = new MainWindowViewModel();
    }

    // Această metodă este necesară în unele versiuni de Avalonia pentru a lega XAML-ul
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
