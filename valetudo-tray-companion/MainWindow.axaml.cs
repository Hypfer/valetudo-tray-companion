using Avalonia.Controls;

namespace valetudo_tray_companion;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded()
    {
        Hide();
        base.OnLoaded();
    }
}