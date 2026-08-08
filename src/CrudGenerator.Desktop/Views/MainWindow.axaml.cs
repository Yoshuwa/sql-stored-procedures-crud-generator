using Avalonia.Controls;
using Avalonia.Interactivity;
using CrudGenerator.Desktop.ViewModels;

namespace CrudGenerator.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnInstallGeneratorClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.CanInstall)
            return;

        var database = viewModel.SelectedDatabase!;
        var prompt = new DatabaseChangePrompt(
            "Install or update sp_CRUDGen?",
            "This executes the bundled generator script and may replace dbo.sp_CRUDGen in the target database.",
            database,
            null,
            "Install generator");

        if (await prompt.ShowDialog<bool>(this))
            await viewModel.InstallGeneratorCommand.ExecuteAsync(null);
    }

    private async void OnCreateProceduresClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || !viewModel.CanCreate)
            return;

        var prompt = new DatabaseChangePrompt(
            "Create stored procedures?",
            "sp_CRUDGen will create or replace the selected procedure types for this table.",
            viewModel.SelectedDatabase!,
            viewModel.SelectedTable!.DisplayName,
            "Create procedures");

        if (await prompt.ShowDialog<bool>(this))
            await viewModel.CreateProceduresCommand.ExecuteAsync(null);
    }
}
