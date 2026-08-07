using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrudGenerator.Core;

namespace CrudGenerator.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ICrudGeneratorService _service;
    private readonly List<StoredProcedureInfo> _allProcedures = [];
    private bool _suppressDatabaseSelection;

    public MainWindowViewModel() : this(null) { }

    public MainWindowViewModel(ICrudGeneratorService? service)
    {
        _service = service ?? new DesignTimeCrudGeneratorService();
    }

    public ObservableCollection<string> Databases { get; } = [];
    public ObservableCollection<StoredProcedureInfo> Procedures { get; } = [];

    [ObservableProperty] private string _server = @"(localdb)\MSSQLLocalDB";
    [ObservableProperty] private string? _selectedDatabase;
    [ObservableProperty] private bool _useIntegratedSecurity = true;
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private StoredProcedureInfo? _selectedProcedure;
    [ObservableProperty] private string _status = "Enter a SQL Server instance, then load databases.";
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

    public string GeneratorStatus => string.IsNullOrWhiteSpace(SelectedDatabase)
        ? "No database selected"
        : IsGeneratorInstalled ? "sp_CRUDGen installed" : "sp_CRUDGen not found";

    partial void OnIsGeneratorInstalledChanged(bool value) => OnPropertyChanged(nameof(GeneratorStatus));

    partial void OnSelectedDatabaseChanged(string? value)
    {
        OnPropertyChanged(nameof(GeneratorStatus));
        if (!_suppressDatabaseSelection && !string.IsNullOrWhiteSpace(value))
            _ = LoadSelectedDatabaseAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task ConnectAsync()
    {
        await RunAsync(async () =>
        {
            Status = $"Connecting to {Server} and loading databases…";
            var databases = await _service.GetDatabasesAsync(CreateServerProfile());
            _suppressDatabaseSelection = true;
            SelectedDatabase = null;
            Databases.Clear();
            foreach (var database in databases) Databases.Add(database);
            SelectedDatabase = Databases.FirstOrDefault();
            _suppressDatabaseSelection = false;
            if (SelectedDatabase is null)
            {
                ClearDatabaseDetails();
                Status = $"Connected to {Server}, but no accessible online databases were found.";
                return;
            }
            await LoadDatabaseDetailsCoreAsync();
        });
    }

    private Task LoadSelectedDatabaseAsync() => RunAsync(LoadDatabaseDetailsCoreAsync);

    [RelayCommand]
    private Task RefreshDatabaseAsync() => LoadSelectedDatabaseAsync();

    [RelayCommand]
    private async Task GeneratePreviewAsync()
    {
        await RunAsync(async () =>
        {
            EnsureReady();
            Status = $"Generating a database-wide preview for {SelectedDatabase}…";
            var result = await _service.GenerateAsync(CreateProfile(), CreateOptions(), false);
            GeneratedSql = result.HasSql ? result.Sql : "-- sp_CRUDGen completed without returning preview text.";
            Status = $"Preview generated for {SelectedDatabase}. No database procedures were changed.";
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
            Status = $"Creating procedures for all eligible objects in {SelectedDatabase}…";
            var result = await _service.GenerateAsync(CreateProfile(), CreateOptions(), true);
            if (result.HasSql) GeneratedSql = result.Sql;
            ConfirmDatabaseChanges = false;
            await LoadDatabaseDetailsCoreAsync();
            Status = $"Stored procedures created and refreshed for {SelectedDatabase}.";
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
            Status = $"Installing sp_CRUDGen in {SelectedDatabase}…";
            await _service.InstallGeneratorAsync(CreateProfile(), await File.ReadAllTextAsync(path));
            IsGeneratorInstalled = true;
            ConfirmDatabaseChanges = false;
            Status = $"sp_CRUDGen installed in {SelectedDatabase}.";
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
        new(Server, SelectedDatabase ?? "", UseIntegratedSecurity, UserName, Password);

    private ConnectionProfile CreateServerProfile() =>
        new(Server, "", UseIntegratedSecurity, UserName, Password);

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
        if (string.IsNullOrWhiteSpace(SelectedDatabase)) throw new InvalidOperationException("Select a database first.");
        if (!CreateOptions().HasSelection) throw new InvalidOperationException("Select at least one procedure type.");
    }

    private async Task LoadDatabaseDetailsCoreAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedDatabase))
        {
            ClearDatabaseDetails();
            return;
        }

        var profile = CreateProfile();
        Status = $"Loading sp_CRUDGen procedures from {profile.Database}…";
        IsGeneratorInstalled = await _service.IsGeneratorInstalledAsync(profile);
        var procedures = await _service.GetGeneratedProceduresAsync(profile);
        _allProcedures.Clear();
        _allProcedures.AddRange(procedures);
        ApplyFilter();
        Status = $"Connected to {profile.Database}. Found {procedures.Count} procedure(s) created by sp_CRUDGen.";
    }

    private void ClearDatabaseDetails()
    {
        IsGeneratorInstalled = false;
        _allProcedures.Clear();
        Procedures.Clear();
        SelectedProcedure = null;
    }

    private void ApplyFilter()
    {
        var selected = SelectedProcedure;
        var matches = string.IsNullOrWhiteSpace(SearchText)
            ? _allProcedures
            : _allProcedures.Where(item => item.DisplayName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        Procedures.Clear();
        foreach (var item in matches) Procedures.Add(item);
        if (selected is not null && Procedures.Contains(selected)) SelectedProcedure = selected;
    }

    private sealed class DesignTimeCrudGeneratorService : ICrudGeneratorService
    {
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["AdventureWorks", "Inventory"]);
        public Task<IReadOnlyList<StoredProcedureInfo>> GetGeneratedProceduresAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredProcedureInfo>>([new("dbo", "CustomerRead", DateTime.Now), new("sales", "OrderUpdate", DateTime.Now)]);
        public Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GenerationResult> GenerateAsync(ConnectionProfile profile, GeneratorOptions options, bool createProcedures, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GenerationResult("-- Preview", []));
    }
}
