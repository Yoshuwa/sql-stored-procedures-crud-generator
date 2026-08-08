using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrudGenerator.Core;

namespace CrudGenerator.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly string[] KnownProcedureSuffixes =
        ["Create", "CreateMultiple", "Read", "ReadEager", "Update", "UpdateMultiple", "Upsert", "Indate", "Delete", "DeleteMultiple", "Search"];
    private readonly ICrudGeneratorService _service;
    private readonly List<StoredProcedureInfo> _allProcedures = [];
    private bool _suppressDatabaseSelection;

    public MainWindowViewModel() : this(null) { }

    public MainWindowViewModel(ICrudGeneratorService? service)
    {
        _service = service ?? new DesignTimeCrudGeneratorService();
    }

    public ObservableCollection<string> Databases { get; } = [];
    public ObservableCollection<DatabaseTable> Tables { get; } = [];
    public ObservableCollection<StoredProcedureInfo> Procedures { get; } = [];
    public ObservableCollection<ProcedureTestResult> TestResults { get; } = [];
    public IReadOnlyList<string> TimeFunctions { get; } =
        ["SYSDATETIMEOFFSET()", "SYSUTCDATETIME()", "SYSDATETIME()", "GETUTCDATE()", "GETDATE()", "CURRENT_TIMESTAMP"];

    [ObservableProperty] private string _server = @"(localdb)\MSSQLLocalDB";
    [ObservableProperty] private string? _selectedDatabase;
    [ObservableProperty] private DatabaseTable? _selectedTable;
    [ObservableProperty] private bool _useIntegratedSecurity = true;
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private StoredProcedureInfo? _selectedProcedure;
    [ObservableProperty] private string _status = "Enter a SQL Server instance, then load databases.";
    [ObservableProperty] private string _generatedSql = "-- Generated SQL will appear here.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isGeneratorInstalled;
    [ObservableProperty] private int _selectedOutputTabIndex;

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

    [ObservableProperty] private string _searchSeparatorString = " to ";
    [ObservableProperty] private string _createPersonColumnName = "CreatePersonId";
    [ObservableProperty] private bool _createPersonInclude;
    [ObservableProperty] private string _createTimeColumnName = "CreateTime";
    [ObservableProperty] private string _createTimeFunction = "SYSDATETIMEOFFSET()";
    [ObservableProperty] private string _modifyPersonColumnName = "ModifyPersonId";
    [ObservableProperty] private bool _modifyPersonInclude;
    [ObservableProperty] private string _modifyTimeColumnName = "ModifyTime";
    [ObservableProperty] private string _modifyTimeFunction = "SYSDATETIMEOFFSET()";
    [ObservableProperty] private string _versionStampColumnName = "VersionStamp";
    [ObservableProperty] private string _validFromTimeColumnName = "ValidFromTime";
    [ObservableProperty] private string _validToTimeColumnName = "ValidToTime";

    public string GeneratorStatus => string.IsNullOrWhiteSpace(SelectedDatabase)
        ? "No database selected"
        : IsGeneratorInstalled ? "sp_CRUDGen installed" : "sp_CRUDGen not found";

    public string TargetSummary => SelectedTable is null
        ? "Choose a database and table to begin"
        : $"{SelectedDatabase}  /  {SelectedTable.DisplayName}";

    public string ProcedureCountText => Procedures.Count == 1
        ? "1 generated procedure"
        : $"{Procedures.Count} generated procedures";

    public bool HasSelectedTable => SelectedTable is not null;
    public bool HasGeneratedProcedures => SelectedTable is not null && _allProcedures.Any(IsForSelectedTable);
    public bool HasVisibleProcedures => Procedures.Count > 0;
    public bool CanGenerate => !IsBusy && IsGeneratorInstalled && HasSelectedTable;
    public bool CanCreate => CanGenerate;
    public bool CanTest => CanGenerate && HasGeneratedProcedures;
    public bool CanInstall => !IsBusy && !string.IsNullOrWhiteSpace(SelectedDatabase);

    partial void OnIsGeneratorInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(GeneratorStatus));
        NotifyActionState();
    }

    partial void OnIsBusyChanged(bool value) => NotifyActionState();

    partial void OnSelectedDatabaseChanged(string? value)
    {
        OnPropertyChanged(nameof(GeneratorStatus));
        OnPropertyChanged(nameof(TargetSummary));
        NotifyActionState();
        if (!_suppressDatabaseSelection && !string.IsNullOrWhiteSpace(value))
            _ = LoadSelectedDatabaseAsync();
    }

    partial void OnSelectedTableChanged(DatabaseTable? value)
    {
        SelectedProcedure = null;
        TestResults.Clear();
        ApplyFilter();
        OnPropertyChanged(nameof(TargetSummary));
        OnPropertyChanged(nameof(HasSelectedTable));
        NotifyActionState();
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
            Status = $"Generating a preview for {SelectedTable!.DisplayName}…";
            var result = await _service.GenerateAsync(CreateProfile(), SelectedTable, CreateOptions(), false);
            GeneratedSql = result.HasSql ? result.Sql : "-- sp_CRUDGen completed without returning preview text.";
            SelectedOutputTabIndex = 0;
            Status = $"Preview generated for {SelectedTable.DisplayName}. No procedures were changed.";
        });
    }

    [RelayCommand]
    private async Task CreateProceduresAsync()
    {
        await RunAsync(async () =>
        {
            EnsureReady();
            var target = SelectedTable!;
            Status = $"Creating procedures for {target.DisplayName}…";
            var result = await _service.GenerateAsync(CreateProfile(), target, CreateOptions(), true);
            if (result.HasSql) GeneratedSql = result.Sql;
            SelectedOutputTabIndex = 0;
            await LoadDatabaseDetailsCoreAsync();
            SelectedTable = Tables.FirstOrDefault(item => item.DisplayName == target.DisplayName);
            Status = $"Stored procedures created for {target.DisplayName}. Use Test generated procedures to validate them.";
        });
    }

    [RelayCommand]
    private async Task TestProceduresAsync()
    {
        await RunAsync(async () =>
        {
            EnsureReady();
            if (!HasGeneratedProcedures)
                throw new InvalidOperationException("No generated procedures were found for this table. Preview and create them first.");
            Status = $"Testing generated procedures for {SelectedTable!.DisplayName}…";
            var results = await _service.TestGeneratedProceduresAsync(CreateProfile(), SelectedTable, CreateOptions());
            var passed = results.Count(item => item.Passed);
            TestResults.Clear();
            foreach (var result in results) TestResults.Add(result);
            GeneratedSql = string.Join(Environment.NewLine, results.Select(item =>
                $"[{(item.Passed ? "PASS" : "FAIL")}] {item.ProcedureName}{Environment.NewLine}       {item.Message}"));
            SelectedOutputTabIndex = 1;
            Status = $"Procedure test completed for {SelectedTable.DisplayName}: {passed}/{results.Count} passed.";
        });
    }

    [RelayCommand]
    private async Task InstallGeneratorAsync()
    {
        await RunAsync(async () =>
        {
            var path = Path.Combine(AppContext.BaseDirectory, "sql", "sp_CRUDGen.sql");
            if (!File.Exists(path)) throw new FileNotFoundException("The bundled sp_CRUDGen.sql file was not found.", path);
            Status = $"Installing sp_CRUDGen in {SelectedDatabase}…";
            await _service.InstallGeneratorAsync(CreateProfile(), await File.ReadAllTextAsync(path));
            IsGeneratorInstalled = true;
            Status = $"sp_CRUDGen installed in {SelectedDatabase}.";
        });
    }

    [RelayCommand]
    private void ClearOutput()
    {
        GeneratedSql = "-- Generated SQL will appear here.";
        TestResults.Clear();
        SelectedOutputTabIndex = 0;
    }

    [RelayCommand]
    private void SelectAllProcedureTypes()
    {
        GenerateCreate = GenerateCreateMultiple = GenerateRead = GenerateReadEager = true;
        GenerateUpdate = GenerateUpdateMultiple = GenerateUpsert = GenerateIndate = true;
        GenerateDelete = GenerateDeleteMultiple = GenerateSearch = true;
    }

    [RelayCommand]
    private void ClearProcedureTypes()
    {
        GenerateCreate = GenerateCreateMultiple = GenerateRead = GenerateReadEager = false;
        GenerateUpdate = GenerateUpdateMultiple = GenerateUpsert = GenerateIndate = false;
        GenerateDelete = GenerateDeleteMultiple = GenerateSearch = false;
    }

    [RelayCommand]
    private void ResetAdvancedParameters()
    {
        SearchSeparatorString = " to ";
        CreatePersonColumnName = "CreatePersonId";
        CreatePersonInclude = false;
        CreateTimeColumnName = "CreateTime";
        CreateTimeFunction = "SYSDATETIMEOFFSET()";
        ModifyPersonColumnName = "ModifyPersonId";
        ModifyPersonInclude = false;
        ModifyTimeColumnName = "ModifyTime";
        ModifyTimeFunction = "SYSDATETIMEOFFSET()";
        VersionStampColumnName = "VersionStamp";
        ValidFromTimeColumnName = "ValidFromTime";
        ValidToTimeColumnName = "ValidToTime";
    }

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
        GenerateSearch = GenerateSearch,
        SearchSeparatorString = SearchSeparatorString,
        CreatePersonColumnName = CreatePersonColumnName,
        CreatePersonInclude = CreatePersonInclude,
        CreateTimeColumnName = CreateTimeColumnName,
        CreateTimeFunction = CreateTimeFunction,
        ModifyPersonColumnName = ModifyPersonColumnName,
        ModifyPersonInclude = ModifyPersonInclude,
        ModifyTimeColumnName = ModifyTimeColumnName,
        ModifyTimeFunction = ModifyTimeFunction,
        VersionStampColumnName = VersionStampColumnName,
        ValidFromTimeColumnName = ValidFromTimeColumnName,
        ValidToTimeColumnName = ValidToTimeColumnName
    };

    private void EnsureReady()
    {
        if (!IsGeneratorInstalled) throw new InvalidOperationException("Install sp_CRUDGen in the target database first.");
        if (string.IsNullOrWhiteSpace(SelectedDatabase)) throw new InvalidOperationException("Select a database first.");
        if (SelectedTable is null) throw new InvalidOperationException("Select a table first.");
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
        Status = $"Loading tables and sp_CRUDGen procedures from {profile.Database}…";
        IsGeneratorInstalled = await _service.IsGeneratorInstalledAsync(profile);
        var tables = await _service.GetTablesAsync(profile);
        var procedures = await _service.GetGeneratedProceduresAsync(profile);
        Tables.Clear();
        foreach (var table in tables) Tables.Add(table);
        SelectedTable = Tables.FirstOrDefault();
        _allProcedures.Clear();
        _allProcedures.AddRange(procedures);
        ApplyFilter();
        Status = $"Connected to {profile.Database}. Found {tables.Count} table(s) and {procedures.Count} generated procedure(s).";
    }

    private void ClearDatabaseDetails()
    {
        IsGeneratorInstalled = false;
        Tables.Clear();
        SelectedTable = null;
        _allProcedures.Clear();
        Procedures.Clear();
        TestResults.Clear();
        SelectedProcedure = null;
    }

    private void ApplyFilter()
    {
        var selected = SelectedProcedure;
        var matches = _allProcedures.Where(IsForSelectedTable);
        if (!string.IsNullOrWhiteSpace(SearchText))
            matches = matches.Where(item => item.DisplayName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));
        Procedures.Clear();
        foreach (var item in matches) Procedures.Add(item);
        if (selected is not null && Procedures.Contains(selected)) SelectedProcedure = selected;
        OnPropertyChanged(nameof(ProcedureCountText));
        OnPropertyChanged(nameof(HasGeneratedProcedures));
        OnPropertyChanged(nameof(HasVisibleProcedures));
        NotifyActionState();
    }

    private bool IsForSelectedTable(StoredProcedureInfo item) =>
        SelectedTable is not null &&
        item.Schema.Equals(SelectedTable.Schema, StringComparison.OrdinalIgnoreCase) &&
        KnownProcedureSuffixes.Any(suffix => item.Name.Equals(SelectedTable.Name + suffix, StringComparison.OrdinalIgnoreCase));

    private void NotifyActionState()
    {
        OnPropertyChanged(nameof(CanGenerate));
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanTest));
        OnPropertyChanged(nameof(CanInstall));
    }

    private sealed class DesignTimeCrudGeneratorService : ICrudGeneratorService
    {
        public Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(["AdventureWorks", "Inventory"]);
        public Task<IReadOnlyList<DatabaseTable>> GetTablesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseTable>>([new("dbo", "Customer"), new("sales", "Order")]);
        public Task<IReadOnlyList<StoredProcedureInfo>> GetGeneratedProceduresAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredProcedureInfo>>([new("dbo", "CustomerRead", DateTime.Now), new("sales", "OrderUpdate", DateTime.Now)]);
        public Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseTable table, GeneratorOptions options, bool createProcedures, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GenerationResult("-- Preview", []));
        public Task<IReadOnlyList<ProcedureTestResult>> TestGeneratedProceduresAsync(ConnectionProfile profile, DatabaseTable table, GeneratorOptions options, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProcedureTestResult>>([new($"{table.DisplayName}Create", true, "Validation passed.")]);
    }
}
