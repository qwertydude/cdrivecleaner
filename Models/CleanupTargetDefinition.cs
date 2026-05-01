namespace CDriveCleaner.Models;

internal sealed class CleanupTargetDefinition
{
    public CleanupTargetDefinition(
        string id,
        CleanupTargetKind kind,
        string title,
        string category,
        string description,
        string riskLevel,
        bool requiresAdmin,
        bool recommended,
        string safetyNote,
        params string[] paths)
    {
        Id = id;
        Kind = kind;
        Title = title;
        Category = category;
        Description = description;
        RiskLevel = riskLevel;
        RequiresAdmin = requiresAdmin;
        Recommended = recommended;
        SafetyNote = safetyNote;
        Paths = paths;
    }

    public string Id { get; }

    public CleanupTargetKind Kind { get; }

    public string Title { get; }

    public string Category { get; }

    public string Description { get; }

    public string RiskLevel { get; }

    public bool RequiresAdmin { get; }

    public bool Recommended { get; }

    public string SafetyNote { get; }

    public IReadOnlyList<string> Paths { get; }
}

internal enum CleanupTargetKind
{
    UserTemp,
    WindowsTemp,
    RecycleBin,
    DownloadsFolder,
    ThumbnailCache,
    DirectXShaderCache,
    AppCrashDumps,
    MemoryDumps,
    DeliveryOptimizationCache,
    WindowsUpdateDownloads,
    WindowsComponentStore,
    EdgeCache,
    ChromeCache,
    FirefoxCache,
    BraveCache,
    OperaCache,
    WebView2Cache,
    StoreCache,
    SpotifyCache,
    SlackCache,
    ZoomCache,
    EpicGamesLauncherCache,
    BattleNetCache,
    EADesktopCache,
    UbisoftConnectCache,
    TeamsCache,
    DiscordCache,
    VsCodeCache,
    SteamHtmlCache,
    GitHubDesktopCache,
    PostmanCache,
    NotionCache,
    TelegramCache,
    SquirrelTemp,
    NuGetCache,
    NuGetGlobalPackages,
    NpmCache,
    PnpmStore,
    YarnCache,
    PipCache,
    NodeModules,
    PythonVirtualEnvs,
    GradleCache,
    MavenRepositoryCache,
    CargoCache,
    CondaPackageCache,
    DockerData,
    VirtualMachineDisks,
    WslVirtualDisks,
    UnityCache,
    UnrealDerivedDataCache,
    AdobeMediaCache,
    RobloxCache,
    OneDriveLogs,
    DefenderScanHistory,
    WindowsErrorReports,
    WindowsUpdateLogs,
    WindowsOldInstallation,
    WindowsUpgradeLeftovers,
    IisLogs,
    BranchCache,
    IntelShaderCache,
    AmdShaderCache,
    NvidiaShaderCache,
    Prefetch,
    HibernationFile,
    SystemRestorePoints,
    GoModuleCache,
    RustTargetDirectories,
    DotNetBuildArtifacts,
    JetBrainsCache,
    VisualStudioCache,
    WindowsFontCache,
    WindowsInstallerPatchCache,
    WindowsEventLogs,
    ChocolateyCache,
    WingetCache,
    WindowsSearchIndex,
    TemporaryInternetFiles,
    GogGalaxyCache,
    VivaldiCache,
    WhatsAppCache,
    SignalCache,
    GoogleDriveCache,
    DropboxCache,
    XboxAppCache,
    WindowsInstallerTemp,
    OldLargeLogFiles,
}
