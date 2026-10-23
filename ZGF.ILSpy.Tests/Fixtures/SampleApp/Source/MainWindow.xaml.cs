using System.Windows;

namespace SampleApp;

public partial class MainWindow : Window
{
    private readonly Counter _counter = new();

    public MainWindow()
    {
        InitializeComponent();
        _counter.Changed += ShowCount;
    }

    private void OnCount(object sender, RoutedEventArgs e) => _counter.Increment();

    private void ShowCount() => CountText.Text = _counter.Count.ToString();
}
