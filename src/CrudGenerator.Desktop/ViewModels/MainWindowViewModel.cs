using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrudGenerator.Core;

namespace CrudGenerator.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICrudGeneratorService _service;
    private readonly List<DatabaseObject> _allObjects = [];

    public MainWindowViewModel() : this(null) { }

    public MainWindowViewModel(ICrudGeneratorService? service)
    {
        _service = service ?? new DesignTimeCrudGeneratorService();
    }

    public ObservableCollection<DatabaseObject> Objects { get; } = [];

    [ObservableProperty] private string _server = @"(localdb)\MSSQLLocalDB";
    [ObservableProperty] private string _database = "";
    [ObservableProperty] private bool _useIntegratedSecurity = true;
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private DatabaseObject? _selectedObject;
    [ObservableProperty] private string _status = "Enter a target database, then connect.";
    [ObservableProperty] private string _generatedSql = "-- Generated SQL will appear here.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isGeneratorInstalled;
    [ObservableProperty] private bool _confirmDatabaseChanges;

    [ObservableProperty] private bool _generateCreate = true;
    [ObservableProperty] private bool _generateCreateMultiple = true;
    [ObservableProperty] private bool _generateRead = true;
    [ObservableProperty] private bool _generateReadEager = true;
    [ObservableProperty] private bool _generateUpdate = true;
    [ObservableProperty] private bool _generateUpdateMultiple = true;
    [ObservableProperty] private bool _generateUpsert = true;
    [ObservableProperty] private bool _generateIndate;
    [ObservableProperty] private bool _generateDelete = true;
    [ObservableProperty] private bool _generateDeleteMultiple = true;
    [ObservableProperty] private bool _generateSearch = true;

    public string GeneratorStatus => IsGeneratorInstalled ? "sp_CRUDGen installed" : "sp_CRUDGen not found";

    partial void OnIsGeneratorInstalledChanged(bool value) => OnPropertyChanged(nameof(GeneratorStatus));

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task ConnectAsync()
    {
        await RunAsync(async () =>
        {
            var profile = CreateProfile();
            Status = "Connecting…";
            await _service.TestConnectionAsync(profile);
            var objects = await _service.GetObjectsAsync(profile);
            IsGeneratorInstalled = await _service.IsGeneratorInstalledAsync(profile);
            _allObjects.Clear();
            _allObjects.AddRange(objects);
            ApplyFilter();
            Status = $"Connected to {profile.Database}. Found {objects.Count} tables and views.";
        });
    }

    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        await RunAsync(async () =>
        {
            EnsureReady();
            Status = $"Generating preview for {SelectedObject!.DisplayName}…";
            var result = await _service.GenerateAsync(CreateProfile(), SelectedObject, CreateOptions(), false);
            GeneratedSql = result.HasSql ? result.Sql : "-- sp_CRUDGen completed without returning preview text.";
            Status = $"Preview generated for {SelectedObject.DisplayName}. No database procedures were changed.";
        });
    }

    [RelayCommand]
    private async Task CreateProceduresAsync()
    {
        await RunAsync(async () =>
        {
            EnsureReady();
            if (!ConfirmDatabaseChanges)
                throw new InvalidOperationException("Confirm the database change before creating procedures.");
            Status = $"Creating procedures for {SelectedObject!.DisplayName}…";
            var result = await _service.GenerateAsync(CreateProfile(), SelectedObject, CreateOptions(), true);
            if (result.HasSql) GeneratedSql = result.Sql;
            ConfirmDatabaseChanges = false;
            Status = $"Stored procedures created for {SelectedObject.DisplayName}.";
        });
    }

    [RelayCommand]
    private async Task InstallGeneratorAsync()
    {
        await RunAsync(async () =>
        {
            if (!ConfirmDatabaseChanges)
                throw new InvalidOperationException("Confirm the database change before installing sp_CRUDGen.");
            var path = Path.Combine(AppContext.BaseDirectory, "sql", "sp_CRUDGen.sql");
            if (!File.Exists(path)) throw new FileNotFoundException("The bundled sp_CRUDGen.sql file was not found.", path);
            Status = $"Installing sp_CRUDGen in {Database}…";
            await _service.InstallGeneratorAsync(CreateProfile(), await File.ReadAllTextAsync(path));
            IsGeneratorInstalled = true;
            ConfirmDatabaseChanges = false;
            Status = $"sp_CRUDGen installed in {Database}.";
        });
    }

    [RelayCommand]
    private void ClearOutput() => GeneratedSql = "-- Generated SQL will appear here.";

    private async Task RunAsync(Func<Task> operation)
    {
        if (IsBusy) return;
        try { IsBusy = true; await operation(); }
        catch (Exception exception) { Status = exception.Message; }
        finally { IsBusy = false; }
    }

    private ConnectionProfile CreateProfile() =>
        new(Server, Database, UseIntegratedSecurity, UserName, Password);

    private GeneratorOptions CreateOptions() => new()
    {
        GenerateCreate = GenerateCreate,
        GenerateCreateMultiple = GenerateCreateMultiple,
        GenerateRead = GenerateRead,
        GenerateReadEager = GenerateReadEager,
        GenerateUpdate = GenerateUpdate,
        GenerateUpdateMultiple = GenerateUpdateMultiple,
        GenerateUpsert = GenerateUpsert,
        GenerateIndate = GenerateIndate,
        GenerateDelete = GenerateDelete,
        GenerateDeleteMultiple = GenerateDeleteMultiple,
        GenerateSearch = GenerateSearch
    };

    private void EnsureReady()
    {
        if (!IsGeneratorInstalled) throw new InvalidOperationException("Install sp_CRUDGen in the target database first.");
        if (SelectedObject is null) throw new InvalidOperationException("Select a table or view first.");
        if (!CreateOptions().HasSelection) throw new InvalidOperationException("Select at least one procedure type.");
    }

    private void ApplyFilter()
    {
        var selected = SelectedObject;
        var matches = string.IsNullOrWhiteSpace(SearchText)
            ? _allObjects
            : _allObjects.Where(item => item.DisplayName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        Objects.Clear();
        foreach (var item in matches) Objects.Add(item);
        if (selected is not null && Objects.Contains(selected)) SelectedObject = selected;
    }

    private sealed class DesignTimeCrudGeneratorService : ICrudGeneratorService
    {
        public Task TestConnectionAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<DatabaseObject>> GetObjectsAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseObject>>([new("dbo", "Customers", DatabaseObjectType.Table), new("sales", "Orders", DatabaseObjectType.View)]);
        public Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseObject databaseObject, GeneratorOptions options, bool createProcedures, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GenerationResult("-- Preview", []));
    }
}
