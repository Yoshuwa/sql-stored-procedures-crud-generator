using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CrudGenerator.Desktop.Views;

public partial class DatabaseChangePrompt : Window
{
    public DatabaseChangePrompt()
    {
        InitializeComponent();
    }

    public DatabaseChangePrompt(
        string title,
        string message,
        string database,
        string? table,
        string confirmLabel) : this()
    {
        PromptTitle.Text = title;
        PromptMessage.Text = message;
        DatabaseName.Text = database;
        TableName.Text = table ?? string.Empty;
        TableLabel.IsVisible = table is not null;
        TableName.IsVisible = table is not null;
        ConfirmButton.Content = confirmLabel;
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);

    private void OnConfirmClicked(object? sender, RoutedEventArgs e) => Close(true);
}
