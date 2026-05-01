using System.Diagnostics;
using System.Runtime.InteropServices;
using CDriveCleaner.Models;

namespace CDriveCleaner.Services;

internal sealed class CleanupService
{
    private readonly Dictionary<string, CleanupTargetDefinition> _definitionsById;

    public CleanupService()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var systemDrive = Path.GetPathRoot(windows) ?? @"C:\";

        Definitions = new List<CleanupTargetDefinition>
        {
            new(
                "user-temp",
                CleanupTargetKind.UserTemp,
                "User temp files",
                "Temporary",
                "Deletes temporary files from your user profile temp folder.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Usually safe. Files currently in use will be skipped automatically.",
                Path.GetTempPath()),
            new(
                "windows-temp",
                CleanupTargetKind.WindowsTemp,
                "Windows temp files",
                "System",
                "Clears the shared Windows temp directory used by installers and background tasks.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Safe in most cases, but some files may be locked or require administrator rights.",
                Path.Combine(windows, "Temp")),
            new(
                "recycle-bin",
                CleanupTargetKind.RecycleBin,
                "Recycle Bin",
                "Temporary",
                "Empties the Recycle Bin on the C drive.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Only items already sent to the Recycle Bin are affected.",
                @"C:\$Recycle.Bin"),
            new(
                "downloads-folder",
                CleanupTargetKind.DownloadsFolder,
                "Downloads folder",
                "Personal",
                "Deletes files and folders from your user Downloads folder.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "This removes personal downloaded files. Use it only if you have reviewed the Downloads folder and no longer need the contents.",
                Path.Combine(userProfile, "Downloads")),
            new(
                "thumbnail-cache",
                CleanupTargetKind.ThumbnailCache,
                "Thumbnail and icon cache",
                "Cache",
                "Removes thumbnail and icon database files so Windows can rebuild them.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Windows rebuilds these caches automatically after cleanup.",
                Path.Combine(localAppData, @"Microsoft\Windows\Explorer")),
            new(
                "shader-cache",
                CleanupTargetKind.DirectXShaderCache,
                "DirectX shader cache",
                "Cache",
                "Removes cached shader files created by games and graphics-heavy apps.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Games may rebuild this cache on the next launch.",
                Path.Combine(localAppData, "D3DSCache")),
            new(
                "crash-dumps",
                CleanupTargetKind.AppCrashDumps,
                "App crash dumps",
                "Diagnostics",
                "Deletes user-level crash dump files left behind by crashed apps.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Useful unless you are actively debugging application crashes.",
                Path.Combine(localAppData, "CrashDumps")),
            new(
                "memory-dumps",
                CleanupTargetKind.MemoryDumps,
                "Crash dumps",
                "Diagnostics",
                "Deletes Windows memory dump files created after blue-screen crashes.",
                "Low",
                requiresAdmin: true,
                recommended: false,
                "Do not remove these if you still need them for troubleshooting BSOD issues.",
                Path.Combine(windows, "MEMORY.DMP"),
                Path.Combine(windows, "Minidump")),
            new(
                "delivery-optimization",
                CleanupTargetKind.DeliveryOptimizationCache,
                "Delivery Optimization cache",
                "Update",
                "Removes Windows delivery optimization cache data used for update sharing.",
                "Low",
                requiresAdmin: true,
                recommended: true,
                "Windows will recreate the cache as needed later.",
                Path.Combine(commonAppData, @"Microsoft\Windows\DeliveryOptimization\Cache")),
            new(
                "windows-update-downloads",
                CleanupTargetKind.WindowsUpdateDownloads,
                "Windows Update downloads",
                "Update",
                "Clears downloaded update files stored in SoftwareDistribution.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Best used when updates are finished or when stale downloads are consuming space.",
                Path.Combine(windows, @"SoftwareDistribution\Download")),
            new(
                "windows-component-store",
                CleanupTargetKind.WindowsComponentStore,
                "Windows Component Store cleanup",
                "System",
                "Runs DISM component cleanup to remove old Windows update files from the WinSxS component store.",
                "High",
                requiresAdmin: true,
                recommended: false,
                "This uses DISM to remove old Windows update files. The exact reclaimed size is system-managed rather than precomputed.",
                Path.Combine(windows, "WinSxS")),
            new(
                "edge-cache",
                CleanupTargetKind.EdgeCache,
                "Microsoft Edge cache",
                "Browser",
                "Removes Edge cache folders from the default browser profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Edge first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Code Cache"),
                Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\GPUCache")),
            new(
                "chrome-cache",
                CleanupTargetKind.ChromeCache,
                "Google Chrome cache",
                "Browser",
                "Removes Chrome cache folders from the default browser profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Chrome first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Code Cache"),
                Path.Combine(localAppData, @"Google\Chrome\User Data\Default\GPUCache")),
            new(
                "firefox-cache",
                CleanupTargetKind.FirefoxCache,
                "Mozilla Firefox cache",
                "Browser",
                "Removes Firefox cache data and startup cache from every detected user profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Firefox first for the best cleanup result; startup cache and disk cache will rebuild automatically.",
                Path.Combine(localAppData, @"Mozilla\Firefox\Profiles"),
                Path.Combine(roamingAppData, @"Mozilla\Firefox\Profiles")),
            new(
                "brave-cache",
                CleanupTargetKind.BraveCache,
                "Brave browser cache",
                "Browser",
                "Removes Brave cache folders from the default browser profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Brave first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Cache"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Code Cache"),
                Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\GPUCache")),
            new(
                "opera-cache",
                CleanupTargetKind.OperaCache,
                "Opera cache",
                "Browser",
                "Removes Opera cache folders from the stable browser profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Opera first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"Opera Software\Opera Stable\Cache"),
                Path.Combine(localAppData, @"Opera Software\Opera Stable\Code Cache"),
                Path.Combine(localAppData, @"Opera Software\Opera Stable\GPUCache")),
            new(
                "webview2-cache",
                CleanupTargetKind.WebView2Cache,
                "WebView2 runtime cache",
                "Cache",
                "Removes cache folders created by the shared Microsoft Edge WebView2 runtime.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Apps that embed WebView2 may rebuild this cache the next time they open.",
                Path.Combine(localAppData, @"Microsoft\EdgeWebView\Default\Cache"),
                Path.Combine(localAppData, @"Microsoft\EdgeWebView\Default\Code Cache"),
                Path.Combine(localAppData, @"Microsoft\EdgeWebView\Default\GPUCache")),
            new(
                "store-cache",
                CleanupTargetKind.StoreCache,
                "Microsoft Store cache",
                "App",
                "Removes local cache data used by the Microsoft Store app.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; Store thumbnails and metadata will repopulate later.",
                Path.Combine(localAppData, @"Packages\Microsoft.WindowsStore_8wekyb3d8bbwe\LocalCache")),
            new(
                "spotify-cache",
                CleanupTargetKind.SpotifyCache,
                "Spotify cache",
                "App",
                "Removes Spotify local cache data and downloaded temporary media files.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Spotify may need to stream or recache some content again after cleanup.",
                Path.Combine(localAppData, @"Spotify\Data")),
            new(
                "slack-cache",
                CleanupTargetKind.SlackCache,
                "Slack cache",
                "App",
                "Removes Slack cache folders used by the desktop client.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Slack first for the best cleanup result; workspace data will sync back in.",
                Path.Combine(roamingAppData, @"Slack\Cache"),
                Path.Combine(roamingAppData, @"Slack\Code Cache"),
                Path.Combine(roamingAppData, @"Slack\GPUCache"),
                Path.Combine(roamingAppData, @"Slack\Service Worker\CacheStorage")),
            new(
                "zoom-cache",
                CleanupTargetKind.ZoomCache,
                "Zoom cache",
                "App",
                "Removes Zoom webview cache and temporary app data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Zoom first for the best cleanup result; cached meeting assets will be rebuilt later.",
                Path.Combine(roamingAppData, @"Zoom\data\Webview\Default\Cache"),
                Path.Combine(roamingAppData, @"Zoom\data\Webview\Default\Code Cache"),
                Path.Combine(roamingAppData, @"Zoom\data\Webview\Default\GPUCache"),
                Path.Combine(roamingAppData, @"Zoom\data\CEFCaches")),
            new(
                "epic-games-cache",
                CleanupTargetKind.EpicGamesLauncherCache,
                "Epic Games Launcher cache",
                "Gaming",
                "Removes Epic Games Launcher web cache folders that accumulate over time.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close the Epic Games Launcher first so its web cache files are not locked.",
                Path.Combine(localAppData, @"EpicGamesLauncher\Saved")),
            new(
                "battle-net-cache",
                CleanupTargetKind.BattleNetCache,
                "Battle.net cache",
                "Gaming",
                "Removes Battle.net desktop client cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Battle.net first for the best cleanup result; the client will rebuild cached assets later.",
                Path.Combine(roamingAppData, @"Battle.net\Cache"),
                Path.Combine(roamingAppData, @"Battle.net\GPUCache"),
                Path.Combine(roamingAppData, @"Battle.net\BrowserCache")),
            new(
                "ea-desktop-cache",
                CleanupTargetKind.EADesktopCache,
                "EA app cache",
                "Gaming",
                "Removes EA desktop client cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close the EA app first so locked cache files can be removed cleanly.",
                Path.Combine(localAppData, @"Electronic Arts\EA Desktop\CEF\Cache"),
                Path.Combine(localAppData, @"Electronic Arts\EA Desktop\CEF\Code Cache"),
                Path.Combine(localAppData, @"Electronic Arts\EA Desktop\CEF\GPUCache")),
            new(
                "ubisoft-connect-cache",
                CleanupTargetKind.UbisoftConnectCache,
                "Ubisoft Connect cache",
                "Gaming",
                "Removes Ubisoft Connect cache and downloaded temporary app data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Ubisoft Connect first so cached files are not locked.",
                Path.Combine(localAppData, @"Ubisoft Game Launcher\cache")),
            new(
                "teams-cache",
                CleanupTargetKind.TeamsCache,
                "Microsoft Teams cache",
                "App",
                "Removes cache folders used by classic Teams and the newer packaged Teams app.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Teams first so cache folders are not locked while the cleanup runs.",
                Path.Combine(roamingAppData, @"Microsoft\Teams"),
                Path.Combine(localAppData, @"Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams")),
            new(
                "discord-cache",
                CleanupTargetKind.DiscordCache,
                "Discord cache",
                "App",
                "Removes Discord cache folders used by the desktop app.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Discord first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"Discord\Cache"),
                Path.Combine(localAppData, @"Discord\Code Cache"),
                Path.Combine(localAppData, @"Discord\GPUCache")),
            new(
                "vscode-cache",
                CleanupTargetKind.VsCodeCache,
                "Visual Studio Code cache",
                "App",
                "Removes cache folders used by Visual Studio Code.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Visual Studio Code first for the best cleanup result; editor caches will rebuild automatically.",
                Path.Combine(roamingAppData, @"Code\Cache"),
                Path.Combine(roamingAppData, @"Code\Code Cache"),
                Path.Combine(roamingAppData, @"Code\CachedData"),
                Path.Combine(roamingAppData, @"Code\GPUCache")),
            new(
                "steam-html-cache",
                CleanupTargetKind.SteamHtmlCache,
                "Steam HTML cache",
                "Gaming",
                "Removes Steam browser and web UI cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Steam first so locked cache files can be removed cleanly.",
                Path.Combine(localAppData, @"Steam\htmlcache")),
            new(
                "github-desktop-cache",
                CleanupTargetKind.GitHubDesktopCache,
                "GitHub Desktop cache",
                "Developer",
                "Removes GitHub Desktop cache folders used by the Electron shell.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close GitHub Desktop first for the best cleanup result; repository data is not removed.",
                Path.Combine(roamingAppData, @"GitHub Desktop\Cache"),
                Path.Combine(roamingAppData, @"GitHub Desktop\Code Cache"),
                Path.Combine(roamingAppData, @"GitHub Desktop\GPUCache")),
            new(
                "postman-cache",
                CleanupTargetKind.PostmanCache,
                "Postman cache",
                "Developer",
                "Removes Postman cache folders and temporary Electron web data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Postman first so locked cache files can be removed cleanly.",
                Path.Combine(roamingAppData, @"Postman\Cache"),
                Path.Combine(roamingAppData, @"Postman\Code Cache"),
                Path.Combine(roamingAppData, @"Postman\GPUCache")),
            new(
                "notion-cache",
                CleanupTargetKind.NotionCache,
                "Notion cache",
                "App",
                "Removes Notion desktop app cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Notion first for the best cleanup result; workspace content will sync back in.",
                Path.Combine(roamingAppData, @"Notion\Cache"),
                Path.Combine(roamingAppData, @"Notion\Code Cache"),
                Path.Combine(roamingAppData, @"Notion\GPUCache")),
            new(
                "telegram-cache",
                CleanupTargetKind.TelegramCache,
                "Telegram Desktop cache",
                "App",
                "Removes Telegram Desktop cached media files.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Telegram may need to redownload some media previews after cleanup.",
                Path.Combine(roamingAppData, @"Telegram Desktop\tdata\user_data\cache")),
            new(
                "squirrel-temp",
                CleanupTargetKind.SquirrelTemp,
                "Squirrel temp files",
                "Temporary",
                "Deletes Squirrel updater temp files left by many Electron apps.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; active updaters may recreate these files later.",
                Path.Combine(localAppData, "SquirrelTemp")),
            new(
                "nuget-cache",
                CleanupTargetKind.NuGetCache,
                "NuGet HTTP cache",
                "Developer",
                "Removes NuGet HTTP and plugin caches used during package restore.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; package restores may be slower the next time they run.",
                Path.Combine(localAppData, @"NuGet\v3-cache"),
                Path.Combine(localAppData, @"NuGet\plugins-cache")),
            new(
                "nuget-global-packages",
                CleanupTargetKind.NuGetGlobalPackages,
                "NuGet global packages",
                "Developer",
                "Removes the shared NuGet package cache used by .NET tooling.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "This can reclaim a lot of space, but .NET restores will need to re-download packages and some local developer workflows may slow down temporarily.",
                Path.Combine(userProfile, @".nuget\packages")),
            new(
                "npm-cache",
                CleanupTargetKind.NpmCache,
                "npm cache",
                "Developer",
                "Removes npm content cache and cached log files.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; future installs may need to re-download packages.",
                Path.Combine(localAppData, @"npm-cache\_cacache"),
                Path.Combine(localAppData, @"npm-cache\_logs")),
            new(
                "pnpm-store",
                CleanupTargetKind.PnpmStore,
                "pnpm store",
                "Developer",
                "Removes the shared pnpm package store used across projects.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "This can reclaim a lot of space, but pnpm installs will need to repopulate the store afterward.",
                Path.Combine(localAppData, @"pnpm\store"),
                Path.Combine(userProfile, @".pnpm-store")),
            new(
                "yarn-cache",
                CleanupTargetKind.YarnCache,
                "Yarn cache",
                "Developer",
                "Removes Yarn classic and Berry package caches.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; future installs may need to re-download packages.",
                Path.Combine(localAppData, @"Yarn\Cache"),
                Path.Combine(localAppData, @"Yarn\Berry\cache")),
            new(
                "pip-cache",
                CleanupTargetKind.PipCache,
                "pip cache",
                "Developer",
                "Removes pip wheel and HTTP cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; future installs may need to rebuild or re-download packages.",
                Path.Combine(localAppData, @"pip\Cache")),
            new(
                "node-modules",
                CleanupTargetKind.NodeModules,
                "Node modules",
                "Developer",
                "Finds node_modules folders under your user profile and estimates their reclaimable size by project.",
                "Low",
                requiresAdmin: false,
                recommended: false,
                "This is safe for most projects, but packages will need to be restored later with npm, pnpm, or Yarn.",
                userProfile),
            new(
                "python-virtual-envs",
                CleanupTargetKind.PythonVirtualEnvs,
                "Python virtual environments",
                "Developer",
                "Finds venv and .venv folders under your user profile and estimates their reclaimable size by project.",
                "Low",
                requiresAdmin: false,
                recommended: false,
                "This is safe for most projects, but Python packages will need to be reinstalled into the environment later.",
                userProfile),
            new(
                "gradle-cache",
                CleanupTargetKind.GradleCache,
                "Gradle cache",
                "Developer",
                "Removes the shared Gradle dependency and transform cache.",
                "Medium",
                requiresAdmin: false,
                recommended: false,
                "This can reclaim a lot of space, but Java and Android builds will need to re-download dependencies.",
                Path.Combine(userProfile, @".gradle\caches")),
            new(
                "maven-cache",
                CleanupTargetKind.MavenRepositoryCache,
                "Maven local repository",
                "Developer",
                "Removes the local Maven repository used for Java dependencies.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "Only use this if you are comfortable forcing Maven-based tools to re-download dependencies.",
                Path.Combine(userProfile, @".m2\repository")),
            new(
                "cargo-cache",
                CleanupTargetKind.CargoCache,
                "Cargo crate cache",
                "Developer",
                "Removes Cargo registry cache files used by Rust tooling.",
                "Medium",
                requiresAdmin: false,
                recommended: false,
                "Rust builds may need to re-download crate archives after cleanup.",
                Path.Combine(userProfile, @".cargo\registry\cache")),
            new(
                "conda-package-cache",
                CleanupTargetKind.CondaPackageCache,
                "Conda package cache",
                "Developer",
                "Removes downloaded Conda package archives and extracted package cache folders.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "This can reclaim a lot of space, but Conda environments may need to re-download package content later.",
                Path.Combine(userProfile, @".conda\pkgs"),
                Path.Combine(localAppData, @"conda\conda\pkgs")),
            new(
                "docker-data",
                CleanupTargetKind.DockerData,
                "Docker data",
                "Containers",
                "Uses Docker's own cleanup commands to remove reclaimable images, containers, and build cache.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "This runs docker system prune and can remove unused images and caches. Active containers and referenced images are kept.",
                "docker"),
            new(
                "virtual-machine-disks",
                CleanupTargetKind.VirtualMachineDisks,
                "Old virtual machines / emulator disks",
                "Virtualization",
                "Finds common VM and emulator disk image files under your user profile and typical VM folders.",
                "High",
                requiresAdmin: false,
                recommended: false,
                "Only delete disk images that belong to virtual machines or emulators you no longer need. This can permanently remove whole VM or emulator instances.",
                userProfile),
            new(
                "wsl-virtual-disks",
                CleanupTargetKind.WslVirtualDisks,
                "WSL virtual disks",
                "Virtualization",
                "Finds WSL virtual disk files (.vhdx) under installed WSL app packages.",
                "Low",
                requiresAdmin: false,
                recommended: false,
                "This target is informational by default. Cleanup opens the disk location so you can review or compact it manually.",
                Path.Combine(localAppData, "Packages")),
            new(
                "unity-cache",
                CleanupTargetKind.UnityCache,
                "Unity cache",
                "Developer",
                "Removes Unity's shared local cache directory.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Unity rebuilds this cache automatically as needed after cleanup.",
                Path.Combine(localAppData, @"Unity\cache")),
            new(
                "unreal-ddc",
                CleanupTargetKind.UnrealDerivedDataCache,
                "Unreal Engine derived data cache",
                "Developer",
                "Removes Unreal Engine's shared derived data cache.",
                "Medium",
                requiresAdmin: false,
                recommended: false,
                "This can reclaim a lot of space, but Unreal projects will need time to rebuild shader and asset cache data.",
                Path.Combine(localAppData, @"UnrealEngine\Common\DerivedDataCache")),
            new(
                "adobe-media-cache",
                CleanupTargetKind.AdobeMediaCache,
                "Adobe media cache",
                "App",
                "Removes Adobe shared media cache files used by creative apps.",
                "Medium",
                requiresAdmin: false,
                recommended: false,
                "Only use this if Adobe apps are closed; they may need to regenerate media cache entries later.",
                Path.Combine(roamingAppData, @"Adobe\Common\Media Cache"),
                Path.Combine(roamingAppData, @"Adobe\Common\Media Cache Files")),
            new(
                "roblox-cache",
                CleanupTargetKind.RobloxCache,
                "Roblox cache and logs",
                "Gaming",
                "Removes Roblox logs, downloads, and temporary cache data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Roblox may need to redownload update or asset files after cleanup.",
                Path.Combine(localAppData, @"Roblox\logs"),
                Path.Combine(localAppData, @"Roblox\Downloads"),
                Path.Combine(localAppData, @"Roblox\rbx-storage")),
            new(
                "onedrive-logs",
                CleanupTargetKind.OneDriveLogs,
                "OneDrive logs",
                "Logs",
                "Removes OneDrive log folders that can grow over time after sync activity and updates.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Avoid this while troubleshooting sync issues, because recent logs may be useful for support.",
                Path.Combine(localAppData, @"Microsoft\OneDrive\logs"),
                Path.Combine(localAppData, @"Microsoft\OneDrive\setup\logs")),
            new(
                "defender-history",
                CleanupTargetKind.DefenderScanHistory,
                "Windows Defender scan history",
                "Diagnostics",
                "Deletes cached Windows Defender scan history and result files.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Skip this if you still want recent scan history visible in Windows Security.",
                Path.Combine(commonAppData, @"Microsoft\Windows Defender\Scans\History\Service"),
                Path.Combine(commonAppData, @"Microsoft\Windows Defender\Scans\History\Store"),
                Path.Combine(commonAppData, @"Microsoft\Windows Defender\Scans\History\Results")),
            new(
                "windows-error-reports",
                CleanupTargetKind.WindowsErrorReports,
                "Windows Error Reporting files",
                "Diagnostics",
                "Deletes archived Windows Error Reporting data stored for application and system crash reports.",
                "Low",
                requiresAdmin: true,
                recommended: true,
                "Safe unless you still need recent crash reports for troubleshooting or vendor support.",
                Path.Combine(commonAppData, @"Microsoft\Windows\WER"),
                Path.Combine(localAppData, @"Microsoft\Windows\WER")),
            new(
                "windows-update-logs",
                CleanupTargetKind.WindowsUpdateLogs,
                "Windows update logs",
                "System",
                "Deletes CBS, DISM, and Panther log folders left behind by Windows servicing and upgrade work.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Useful only when you no longer need servicing or upgrade logs for troubleshooting.",
                Path.Combine(windows, @"Logs\CBS"),
                Path.Combine(windows, @"Logs\DISM"),
                Path.Combine(windows, "Panther")),
            new(
                "windows-old",
                CleanupTargetKind.WindowsOldInstallation,
                "Previous Windows installation",
                "System",
                "Deletes the Windows.old folder left by a previous Windows installation or major upgrade.",
                "Low",
                requiresAdmin: true,
                recommended: false,
                "Only use this if you no longer need to recover files or roll back to the previous Windows installation.",
                Path.Combine(systemDrive, "Windows.old")),
            new(
                "windows-upgrade-leftovers",
                CleanupTargetKind.WindowsUpgradeLeftovers,
                "Windows upgrade leftovers",
                "System",
                "Deletes common Windows setup leftover folders such as $WINDOWS.~BT and ESD.",
                "High",
                requiresAdmin: true,
                recommended: false,
                "Only use this when you are sure you do not need rollback or setup leftover files anymore.",
                Path.Combine(systemDrive, "$WINDOWS.~BT"),
                Path.Combine(systemDrive, "$Windows.~WS"),
                Path.Combine(systemDrive, "ESD")),
            new(
                "iis-logs",
                CleanupTargetKind.IisLogs,
                "IIS logs",
                "Logs",
                "Deletes web server log files stored by IIS.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Only use this if the machine runs IIS and you no longer need the logs for auditing or troubleshooting.",
                Path.Combine(systemDrive, @"inetpub\logs\LogFiles")),
            new(
                "branch-cache",
                CleanupTargetKind.BranchCache,
                "BranchCache files",
                "Network",
                "Removes BranchCache content storage used for Windows peer content caching.",
                "Low",
                requiresAdmin: true,
                recommended: false,
                "Safe if you do not rely on BranchCache performance; Windows can regenerate the cache later.",
                Path.Combine(windows, @"ServiceProfiles\NetworkService\AppData\Local\PeerDistRepub")),
            new(
                "intel-shader-cache",
                CleanupTargetKind.IntelShaderCache,
                "Intel shader cache",
                "Cache",
                "Removes Intel graphics shader caches created for apps and games.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Shaders will be rebuilt automatically by apps or the graphics driver after cleanup.",
                Path.Combine(localAppData, @"Intel\ShaderCache")),
            new(
                "amd-shader-cache",
                CleanupTargetKind.AmdShaderCache,
                "AMD shader cache",
                "Cache",
                "Removes AMD graphics shader caches created for apps and games.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Shaders will be rebuilt automatically by apps or the graphics driver after cleanup.",
                Path.Combine(localAppData, @"AMD\DxCache"),
                Path.Combine(localAppData, @"AMD\GLCache"),
                Path.Combine(localAppData, @"AMD\VkCache")),
            new(
                "nvidia-shader-cache",
                CleanupTargetKind.NvidiaShaderCache,
                "NVIDIA shader cache",
                "Cache",
                "Removes NVIDIA driver shader caches created for games and GPU-accelerated apps.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Shaders will be rebuilt automatically by the driver or apps after cleanup.",
                Path.Combine(localAppData, @"NVIDIA\DXCache"),
                Path.Combine(localAppData, @"NVIDIA\GLCache"),
                Path.Combine(localAppData, @"NVIDIA Corporation\NV_Cache")),
            new(
                "prefetch",
                CleanupTargetKind.Prefetch,
                "Prefetch files",
                "Advanced",
                "Deletes Windows prefetch files, which may reclaim some space but are usually rebuilt.",
                "High",
                requiresAdmin: true,
                recommended: false,
                "This is an advanced option and usually not needed for routine cleanup.",
                Path.Combine(windows, "Prefetch")),
            new(
                "hibernation-file",
                CleanupTargetKind.HibernationFile,
                "Hibernation file (hiberfil.sys)",
                "System",
                "Disables hibernation to delete the hiberfil.sys file, which is typically as large as your installed RAM.",
                "High",
                requiresAdmin: true,
                recommended: false,
                "This disables hibernation and Fast Startup. You can re-enable hibernation later with 'powercfg /hibernate on' from an elevated prompt.",
                Path.Combine(systemDrive, "hiberfil.sys")),
            new(
                "system-restore-points",
                CleanupTargetKind.SystemRestorePoints,
                "System Restore shadow copies",
                "System",
                "Removes all but the most recent System Restore shadow copies using vssadmin.",
                "High",
                requiresAdmin: true,
                recommended: false,
                "Older restore points are deleted. The most recent restore point is kept. You will not be able to roll back to deleted restore points.",
                "vssadmin"),
            new(
                "go-module-cache",
                CleanupTargetKind.GoModuleCache,
                "Go module cache",
                "Developer",
                "Removes the shared Go module download cache.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; Go builds will re-download modules as needed.",
                Path.Combine(userProfile, @"go\pkg\mod\cache\download")),
            new(
                "rust-target-directories",
                CleanupTargetKind.RustTargetDirectories,
                "Rust target directories",
                "Developer",
                "Finds Rust project 'target' build directories under your user profile and estimates their reclaimable size.",
                "Low",
                requiresAdmin: false,
                recommended: false,
                "This is safe but Rust projects will need to recompile from scratch after their target directory is removed.",
                userProfile),
            new(
                "dotnet-build-artifacts",
                CleanupTargetKind.DotNetBuildArtifacts,
                ".NET build artifacts (obj/bin)",
                "Developer",
                "Finds obj and bin build output directories under your user profile and estimates their reclaimable size.",
                "Low",
                requiresAdmin: false,
                recommended: false,
                "This is safe but .NET projects will need to rebuild after their build output directories are removed.",
                userProfile),
            new(
                "jetbrains-cache",
                CleanupTargetKind.JetBrainsCache,
                "JetBrains IDE caches",
                "Developer",
                "Removes caches from JetBrains IDEs like IntelliJ, Rider, WebStorm, PyCharm, GoLand, etc.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close JetBrains IDEs first for the best cleanup result; caches rebuild automatically on next launch.",
                Path.Combine(localAppData, "JetBrains")),
            new(
                "visual-studio-cache",
                CleanupTargetKind.VisualStudioCache,
                "Visual Studio cache",
                "Developer",
                "Removes Visual Studio ComponentModelCache and MEF cache data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Visual Studio first; caches rebuild automatically on next launch.",
                Path.Combine(localAppData, @"Microsoft\VisualStudio"),
                Path.Combine(localAppData, @"Microsoft\VSCommon")),
            new(
                "windows-font-cache",
                CleanupTargetKind.WindowsFontCache,
                "Windows font cache",
                "System",
                "Removes font cache files that Windows rebuilds automatically on next reboot.",
                "Low",
                requiresAdmin: true,
                recommended: false,
                "Font rendering may be briefly slow after cleanup until the cache is rebuilt on next reboot.",
                Path.Combine(windows, @"ServiceProfiles\LocalService\AppData\Local\FontCache")),
            new(
                "windows-installer-patch-cache",
                CleanupTargetKind.WindowsInstallerPatchCache,
                "Windows Installer patch cache",
                "System",
                "Removes orphaned installer patch files from the Windows Installer cache.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Only removes orphaned patches. Active installer patches are not touched. Some apps may need to be repaired or reinstalled from original media.",
                Path.Combine(windows, @"Installer\$PatchCache$")),
            new(
                "windows-event-logs",
                CleanupTargetKind.WindowsEventLogs,
                "Windows event logs",
                "Logs",
                "Clears all Windows event logs (Application, System, Security, etc.) using wevtutil.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Only use this if you no longer need the event logs for auditing, troubleshooting, or compliance.",
                Path.Combine(windows, @"System32\winevt\Logs")),
            new(
                "chocolatey-cache",
                CleanupTargetKind.ChocolateyCache,
                "Chocolatey cache",
                "Developer",
                "Removes cached package downloads from the Chocolatey package manager.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; future installs will re-download packages.",
                Path.Combine(commonAppData, @"chocolatey\cache"),
                Path.Combine(localAppData, @"Temp\chocolatey")),
            new(
                "winget-cache",
                CleanupTargetKind.WingetCache,
                "Winget cache",
                "Developer",
                "Removes cached downloads from the Windows Package Manager (winget).",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; future installs will re-download packages.",
                Path.Combine(localAppData, @"Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir"),
                Path.Combine(localAppData, @"Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalCache")),
            new(
                "windows-search-index",
                CleanupTargetKind.WindowsSearchIndex,
                "Windows Search index",
                "System",
                "Deletes the Windows Search index database file. Windows will rebuild the index automatically.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Search results will be unavailable until the index is rebuilt, which may take some time. The Windows Search service must be stopped first.",
                Path.Combine(commonAppData, @"Microsoft\Search\Data\Applications\Windows\Windows.edb")),
            new(
                "temporary-internet-files",
                CleanupTargetKind.TemporaryInternetFiles,
                "Temporary Internet Files",
                "Cache",
                "Removes legacy Temporary Internet Files and INetCache content used by IE, Edge, and system components.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; cached web content will be re-downloaded as needed.",
                Path.Combine(localAppData, @"Microsoft\Windows\INetCache"),
                Path.Combine(localAppData, @"Microsoft\Windows\Temporary Internet Files")),
            new(
                "gog-galaxy-cache",
                CleanupTargetKind.GogGalaxyCache,
                "GOG Galaxy cache",
                "Gaming",
                "Removes GOG Galaxy launcher cache and web data.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close GOG Galaxy first so locked cache files can be removed cleanly.",
                Path.Combine(localAppData, @"GOG.com\Galaxy\webcache"),
                Path.Combine(commonAppData, @"GOG.com\Galaxy\webcache")),
            new(
                "vivaldi-cache",
                CleanupTargetKind.VivaldiCache,
                "Vivaldi browser cache",
                "Browser",
                "Removes Vivaldi browser cache folders from the default profile.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Vivaldi first for the best cleanup result; locked files will be skipped.",
                Path.Combine(localAppData, @"Vivaldi\User Data\Default\Cache\Cache_Data"),
                Path.Combine(localAppData, @"Vivaldi\User Data\Default\Code Cache"),
                Path.Combine(localAppData, @"Vivaldi\User Data\Default\GPUCache")),
            new(
                "whatsapp-cache",
                CleanupTargetKind.WhatsAppCache,
                "WhatsApp Desktop cache",
                "App",
                "Removes WhatsApp Desktop cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close WhatsApp first for the best cleanup result; media will sync back as needed.",
                Path.Combine(roamingAppData, @"WhatsApp\Cache"),
                Path.Combine(roamingAppData, @"WhatsApp\Code Cache"),
                Path.Combine(roamingAppData, @"WhatsApp\GPUCache")),
            new(
                "signal-cache",
                CleanupTargetKind.SignalCache,
                "Signal Desktop cache",
                "App",
                "Removes Signal Desktop cache folders.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Close Signal first for the best cleanup result; messages are not removed.",
                Path.Combine(roamingAppData, @"Signal\Cache"),
                Path.Combine(roamingAppData, @"Signal\Code Cache"),
                Path.Combine(roamingAppData, @"Signal\GPUCache")),
            new(
                "google-drive-cache",
                CleanupTargetKind.GoogleDriveCache,
                "Google Drive cache",
                "App",
                "Removes Google Drive offline sync cache files.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Google Drive may need to re-sync some content after cleanup.",
                Path.Combine(localAppData, @"Google\DriveFS\cef_cache"),
                Path.Combine(localAppData, @"Google\DriveFS\tmp_cef")),
            new(
                "dropbox-cache",
                CleanupTargetKind.DropboxCache,
                "Dropbox cache",
                "App",
                "Removes Dropbox sync cache files that accumulate over time.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Dropbox may need to re-sync some content after cleanup. Close Dropbox first.",
                Path.Combine(userProfile, @"Dropbox\.dropbox.cache"),
                Path.Combine(localAppData, @"Dropbox\Crashpad"),
                Path.Combine(roamingAppData, @"Dropbox\cache")),
            new(
                "xbox-app-cache",
                CleanupTargetKind.XboxAppCache,
                "Xbox app cache",
                "Gaming",
                "Removes cached data from the Xbox and Game Pass apps.",
                "Low",
                requiresAdmin: false,
                recommended: true,
                "Safe for routine cleanup; the Xbox app will rebuild its cache.",
                Path.Combine(localAppData, @"Packages\Microsoft.XboxApp_8wekyb3d8bbwe\LocalCache"),
                Path.Combine(localAppData, @"Packages\Microsoft.GamingApp_8wekyb3d8bbwe\LocalCache")),
            new(
                "windows-installer-temp",
                CleanupTargetKind.WindowsInstallerTemp,
                "Windows Installer temp files",
                "Temporary",
                "Removes temporary files left behind by Windows Installer operations.",
                "Low",
                requiresAdmin: true,
                recommended: true,
                "Safe for routine cleanup; only stale temporary files are removed.",
                Path.Combine(windows, @"Installer\$PatchCache$"),
                Path.Combine(windows, @"Temp\msiredist")),
            new(
                "old-large-log-files",
                CleanupTargetKind.OldLargeLogFiles,
                "Old large log files",
                "Logs",
                "Finds .log, .etl, and .evtx files larger than 10 MB across common log directories.",
                "Medium",
                requiresAdmin: true,
                recommended: false,
                "Only removes old log files. Use this if you no longer need these logs for troubleshooting.",
                Path.Combine(windows, "Logs"),
                Path.Combine(commonAppData, @"Microsoft\Windows\WER"),
                Path.Combine(windows, @"System32\LogFiles")),
        };

        _definitionsById = Definitions.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CleanupTargetDefinition> Definitions { get; }

    public bool IsRelevant(CleanupTargetDefinition definition)
    {
        if (!ShouldHideWhenIrrelevant(definition))
        {
            return true;
        }

        if (definition.Kind == CleanupTargetKind.DockerData)
        {
            return RunProcess("docker", "--version").ExitCode == 0;
        }

        if (definition.Kind == CleanupTargetKind.WslVirtualDisks)
        {
            return GetWslVirtualDiskCandidates(CancellationToken.None).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.NodeModules)
        {
            return FindNodeModules(CancellationToken.None, maxResults: 1).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.PythonVirtualEnvs)
        {
            return FindPythonVirtualEnvs(CancellationToken.None, maxResults: 1).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.VirtualMachineDisks)
        {
            return FindVirtualMachineDiskCandidates(CancellationToken.None, maxResults: 1).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.RustTargetDirectories)
        {
            return FindCargoTargetDirectories(CancellationToken.None, maxResults: 1).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.DotNetBuildArtifacts)
        {
            return FindDotNetBuildArtifacts(CancellationToken.None, maxResults: 1).Count > 0;
        }

        if (definition.Kind == CleanupTargetKind.ChocolateyCache)
        {
            return RunProcess("choco", "--version").ExitCode == 0;
        }

        return GetRelevantPaths(definition).Any(PathExists);
    }

    public DriveSummary GetDriveSummary()
    {
        var drive = new DriveInfo(@"C:\");
        var total = drive.TotalSize;
        var free = drive.AvailableFreeSpace;
        return new DriveSummary("C:", total, total - free, free);
    }

    public Task<CleanupAnalysisResult> AnalyzeAsync(string targetId, CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(targetId);
        return Task.Run(() => AnalyzeCore(definition, cancellationToken), cancellationToken);
    }

    public Task<CleanupActionResult> CleanAsync(string targetId, CancellationToken cancellationToken = default)
    {
        return CleanAsync(targetId, options: null, cancellationToken);
    }

    public Task<CleanupActionResult> CleanAsync(string targetId, CleanupExecutionOptions? options, CancellationToken cancellationToken = default)
    {
        var definition = GetDefinition(targetId);
        return Task.Run(() => CleanCore(definition, options ?? new CleanupExecutionOptions(), cancellationToken), cancellationToken);
    }

    private CleanupTargetDefinition GetDefinition(string targetId)
    {
        if (_definitionsById.TryGetValue(targetId, out var definition))
        {
            return definition;
        }

        throw new InvalidOperationException($"Unknown cleanup target: {targetId}");
    }

    private CleanupAnalysisResult AnalyzeCore(CleanupTargetDefinition definition, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = definition.Kind switch
            {
                CleanupTargetKind.UserTemp => MeasureDirectoryContents(Path.GetTempPath(), cancellationToken),
                CleanupTargetKind.WindowsTemp => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), cancellationToken),
                CleanupTargetKind.RecycleBin => MeasureRecycleBin(),
                CleanupTargetKind.DownloadsFolder => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), cancellationToken),
                CleanupTargetKind.ThumbnailCache => MeasureMatchingFiles(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer"),
                    cancellationToken,
                    "thumbcache*.db",
                    "iconcache*.db"),
                CleanupTargetKind.DirectXShaderCache => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"), cancellationToken),
                CleanupTargetKind.AppCrashDumps => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps"), cancellationToken),
                CleanupTargetKind.MemoryDumps => Combine(
                    MeasureSingleFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "MEMORY.DMP")),
                    MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Minidump"), cancellationToken)),
                CleanupTargetKind.DeliveryOptimizationCache => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\DeliveryOptimization\Cache"), cancellationToken),
                CleanupTargetKind.WindowsUpdateDownloads => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download"), cancellationToken),
                CleanupTargetKind.WindowsComponentStore => MeasureWindowsComponentStoreAvailability(),
                CleanupTargetKind.EdgeCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.ChromeCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.FirefoxCache => MeasurePathSet(GetFirefoxCachePaths(), cancellationToken),
                CleanupTargetKind.BraveCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OperaCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WebView2Cache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.StoreCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SpotifyCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SlackCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.ZoomCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.EpicGamesLauncherCache => MeasurePathSet(GetEpicGamesLauncherCachePaths(), cancellationToken),
                CleanupTargetKind.BattleNetCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.EADesktopCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.UbisoftConnectCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.TeamsCache => MeasurePathSet(GetTeamsCachePaths(), cancellationToken),
                CleanupTargetKind.DiscordCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.VsCodeCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SteamHtmlCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GitHubDesktopCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PostmanCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NotionCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.TelegramCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SquirrelTemp => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NuGetCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NuGetGlobalPackages => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NpmCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PnpmStore => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.YarnCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PipCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NodeModules => MeasureNodeModules(cancellationToken),
                CleanupTargetKind.PythonVirtualEnvs => MeasurePythonVirtualEnvs(cancellationToken),
                CleanupTargetKind.GradleCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.MavenRepositoryCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.CargoCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.CondaPackageCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DockerData => MeasureDockerData(),
                CleanupTargetKind.VirtualMachineDisks => MeasureVirtualMachineDisks(cancellationToken),
                CleanupTargetKind.WslVirtualDisks => MeasureWslVirtualDisks(cancellationToken),
                CleanupTargetKind.UnityCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.UnrealDerivedDataCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.AdobeMediaCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.RobloxCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OneDriveLogs => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DefenderScanHistory => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsErrorReports => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsUpdateLogs => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsOldInstallation => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsUpgradeLeftovers => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.IisLogs => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.BranchCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.IntelShaderCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.AmdShaderCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NvidiaShaderCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.Prefetch => MeasureDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"), cancellationToken),
                CleanupTargetKind.HibernationFile => MeasureHibernationFile(),
                CleanupTargetKind.SystemRestorePoints => MeasureSystemRestorePoints(),
                CleanupTargetKind.GoModuleCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.RustTargetDirectories => MeasureRustTargetDirectories(cancellationToken),
                CleanupTargetKind.DotNetBuildArtifacts => MeasureDotNetBuildArtifacts(cancellationToken),
                CleanupTargetKind.JetBrainsCache => MeasureJetBrainsCachePaths(cancellationToken),
                CleanupTargetKind.VisualStudioCache => MeasureVisualStudioCachePaths(cancellationToken),
                CleanupTargetKind.WindowsFontCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsInstallerPatchCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsEventLogs => MeasureWindowsEventLogs(cancellationToken),
                CleanupTargetKind.ChocolateyCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WingetCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsSearchIndex => MeasureSingleFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Search\Data\Applications\Windows\Windows.edb")),
                CleanupTargetKind.TemporaryInternetFiles => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GogGalaxyCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.VivaldiCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WhatsAppCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SignalCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GoogleDriveCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DropboxCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.XboxAppCache => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsInstallerTemp => MeasurePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OldLargeLogFiles => MeasureOldLargeLogFiles(cancellationToken),
                _ => default,
            };

            return BuildAnalysisResult(definition.Id, metrics);
        }
        catch (Exception ex)
        {
            return new CleanupAnalysisResult
            {
                TargetId = definition.Id,
                Success = false,
                EstimatedBytes = 0,
                ItemCount = 0,
                Message = $"Scan failed: {ex.Message}",
            };
        }
    }

    private CleanupActionResult CleanCore(CleanupTargetDefinition definition, CleanupExecutionOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = definition.Kind switch
            {
                CleanupTargetKind.UserTemp => DeleteDirectoryContents(Path.GetTempPath(), cancellationToken),
                CleanupTargetKind.WindowsTemp => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), cancellationToken),
                CleanupTargetKind.RecycleBin => EmptyRecycleBin(),
                CleanupTargetKind.DownloadsFolder => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"), cancellationToken),
                CleanupTargetKind.ThumbnailCache => DeleteMatchingFiles(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer"),
                    cancellationToken,
                    "thumbcache*.db",
                    "iconcache*.db"),
                CleanupTargetKind.DirectXShaderCache => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"), cancellationToken),
                CleanupTargetKind.AppCrashDumps => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrashDumps"), cancellationToken),
                CleanupTargetKind.MemoryDumps => DeleteSystemMemoryDumps(cancellationToken),
                CleanupTargetKind.DeliveryOptimizationCache => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\DeliveryOptimization\Cache"), cancellationToken),
                CleanupTargetKind.WindowsUpdateDownloads => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download"), cancellationToken),
                CleanupTargetKind.WindowsComponentStore => RunWindowsComponentStoreCleanup(options.UseAggressiveMode),
                CleanupTargetKind.EdgeCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.ChromeCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.FirefoxCache => DeletePathSet(GetFirefoxCachePaths(), cancellationToken),
                CleanupTargetKind.BraveCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OperaCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WebView2Cache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.StoreCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SpotifyCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SlackCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.ZoomCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.EpicGamesLauncherCache => DeletePathSet(GetEpicGamesLauncherCachePaths(), cancellationToken),
                CleanupTargetKind.BattleNetCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.EADesktopCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.UbisoftConnectCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.TeamsCache => DeletePathSet(GetTeamsCachePaths(), cancellationToken),
                CleanupTargetKind.DiscordCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.VsCodeCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SteamHtmlCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GitHubDesktopCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PostmanCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NotionCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.TelegramCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SquirrelTemp => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NuGetCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NuGetGlobalPackages => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NpmCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PnpmStore => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.YarnCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.PipCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NodeModules => DeletePathSet(options.SelectedPaths, cancellationToken),
                CleanupTargetKind.PythonVirtualEnvs => DeletePathSet(options.SelectedPaths, cancellationToken),
                CleanupTargetKind.GradleCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.MavenRepositoryCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.CargoCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.CondaPackageCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DockerData => RunDockerCleanup(),
                CleanupTargetKind.VirtualMachineDisks => DeletePathSet(options.SelectedPaths, cancellationToken),
                CleanupTargetKind.WslVirtualDisks => OpenWslDiskLocations(),
                CleanupTargetKind.UnityCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.UnrealDerivedDataCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.AdobeMediaCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.RobloxCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OneDriveLogs => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DefenderScanHistory => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsErrorReports => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsUpdateLogs => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsOldInstallation => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsUpgradeLeftovers => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.IisLogs => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.BranchCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.IntelShaderCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.AmdShaderCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.NvidiaShaderCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.Prefetch => DeleteDirectoryContents(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"), cancellationToken),
                CleanupTargetKind.HibernationFile => RunDisableHibernation(),
                CleanupTargetKind.SystemRestorePoints => RunSystemRestoreCleanup(),
                CleanupTargetKind.GoModuleCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.RustTargetDirectories => DeletePathSet(options.SelectedPaths, cancellationToken),
                CleanupTargetKind.DotNetBuildArtifacts => DeletePathSet(options.SelectedPaths, cancellationToken),
                CleanupTargetKind.JetBrainsCache => DeletePathSet(GetJetBrainsCachePaths(), cancellationToken),
                CleanupTargetKind.VisualStudioCache => DeletePathSet(GetVisualStudioCachePaths(), cancellationToken),
                CleanupTargetKind.WindowsFontCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsInstallerPatchCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsEventLogs => RunClearWindowsEventLogs(),
                CleanupTargetKind.ChocolateyCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WingetCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsSearchIndex => RunDeleteSearchIndex(),
                CleanupTargetKind.TemporaryInternetFiles => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GogGalaxyCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.VivaldiCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WhatsAppCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.SignalCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.GoogleDriveCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.DropboxCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.XboxAppCache => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.WindowsInstallerTemp => DeletePathSet(definition.Paths, cancellationToken),
                CleanupTargetKind.OldLargeLogFiles => DeleteOldLargeLogFiles(cancellationToken),
                _ => new DeleteAccumulator(),
            };

            var skipped = result.Errors.Count;
            var message = !string.IsNullOrWhiteSpace(result.CustomMessage)
                ? result.CustomMessage
                : result.Bytes == 0 && result.Items == 0 && skipped == 0
                    ? "Nothing was removed."
                    : $"Freed {FormatBytes(result.Bytes)} by removing {result.Items:N0} item(s).";

            if (skipped > 0)
            {
                message += $" Skipped {skipped:N0} locked or protected item(s).";
            }

            return new CleanupActionResult
            {
                TargetId = definition.Id,
                Success = skipped == 0,
                BytesFreed = result.Bytes,
                ItemsRemoved = result.Items,
                Message = message,
                Errors = result.Errors,
            };
        }
        catch (Exception ex)
        {
            return new CleanupActionResult
            {
                TargetId = definition.Id,
                Success = false,
                BytesFreed = 0,
                ItemsRemoved = 0,
                Message = $"Cleanup failed: {ex.Message}",
                Errors = new[] { ex.Message },
            };
        }
    }

    private static ScanMetrics MeasurePathSet(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        var total = new ScanMetrics();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            total = Combine(total, MeasurePath(path, cancellationToken));
        }

        return total;
    }

    private static DeleteAccumulator DeletePathSet(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        var total = new DeleteAccumulator();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Merge(total, DeletePath(path, cancellationToken));
        }

        return total;
    }

    private static ScanMetrics MeasurePath(string path, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
        {
            return MeasureSingleFile(path);
        }

        if (Directory.Exists(path))
        {
            return MeasureDirectoryContents(path, cancellationToken);
        }

        return default;
    }

    private static DeleteAccumulator DeletePath(string path, CancellationToken cancellationToken)
    {
        if (File.Exists(path))
        {
            return DeleteSingleFile(path);
        }

        if (Directory.Exists(path))
        {
            return DeleteDirectoryContents(path, cancellationToken);
        }

        return new DeleteAccumulator();
    }

    private static ScanMetrics MeasureDirectoryContents(string path, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
        {
            return default;
        }

        var bytes = 0L;
        var items = 0;
        var directories = new Stack<DirectoryInfo>();
        directories.Push(new DirectoryInfo(path));

        while (directories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = directories.Pop();
            FileSystemInfo[] entries;

            try
            {
                entries = current.GetFileSystemInfos();
            }
            catch
            {
                continue;
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry is FileInfo file)
                {
                    try
                    {
                        bytes += file.Length;
                        items++;
                    }
                    catch
                    {
                    }

                    continue;
                }

                if (entry is DirectoryInfo directory)
                {
                    try
                    {
                        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                    }

                    directories.Push(directory);
                }
            }
        }

        return new ScanMetrics(bytes, items);
    }

    private static DeleteAccumulator DeleteDirectoryContents(string path, CancellationToken cancellationToken)
    {
        var accumulator = new DeleteAccumulator();

        if (!Directory.Exists(path))
        {
            return accumulator;
        }

        DeleteDirectoryChildren(new DirectoryInfo(path), deleteSelf: false, accumulator, cancellationToken);
        return accumulator;
    }

    private static void DeleteDirectoryChildren(DirectoryInfo directory, bool deleteSelf, DeleteAccumulator accumulator, CancellationToken cancellationToken)
    {
        FileSystemInfo[] entries;

        try
        {
            entries = directory.GetFileSystemInfos();
        }
        catch (Exception ex)
        {
            accumulator.Errors.Add($"{directory.FullName}: {ex.Message}");
            return;
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry is FileInfo file)
            {
                Merge(accumulator, DeleteSingleFile(file.FullName));
                continue;
            }

            if (entry is DirectoryInfo childDirectory)
            {
                try
                {
                    if ((childDirectory.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                }
                catch
                {
                }

                DeleteDirectoryChildren(childDirectory, deleteSelf: true, accumulator, cancellationToken);
            }
        }

        if (!deleteSelf)
        {
            return;
        }

        try
        {
            directory.Attributes = FileAttributes.Normal;
            directory.Delete(false);
            accumulator.Items++;
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException ex)
        {
            accumulator.Errors.Add($"{directory.FullName}: {ex.Message}");
        }
    }

    private static ScanMetrics MeasureMatchingFiles(string directoryPath, CancellationToken cancellationToken, params string[] patterns)
    {
        if (!Directory.Exists(directoryPath))
        {
            return default;
        }

        var bytes = 0L;
        var items = 0;
        var directory = new DirectoryInfo(directoryPath);

        foreach (var pattern in patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<FileInfo> files;

            try
            {
                files = directory.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    bytes += file.Length;
                    items++;
                }
                catch
                {
                }
            }
        }

        return new ScanMetrics(bytes, items);
    }

    private static DeleteAccumulator DeleteMatchingFiles(string directoryPath, CancellationToken cancellationToken, params string[] patterns)
    {
        var accumulator = new DeleteAccumulator();

        if (!Directory.Exists(directoryPath))
        {
            return accumulator;
        }

        var directory = new DirectoryInfo(directoryPath);
        foreach (var pattern in patterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<FileInfo> files;

            try
            {
                files = directory.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                accumulator.Errors.Add($"{directoryPath}: {ex.Message}");
                continue;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Merge(accumulator, DeleteSingleFile(file.FullName));
            }
        }

        return accumulator;
    }

    private static ScanMetrics MeasureSingleFile(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            var info = new FileInfo(path);
            return new ScanMetrics(info.Length, 1);
        }
        catch
        {
            return default;
        }
    }

    private static DeleteAccumulator DeleteSingleFile(string path)
    {
        var accumulator = new DeleteAccumulator();

        if (!File.Exists(path))
        {
            return accumulator;
        }

        long length = 0;
        try
        {
            length = new FileInfo(path).Length;
        }
        catch
        {
        }

        try
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
            accumulator.Bytes += length;
            accumulator.Items++;
        }
        catch (Exception ex)
        {
            accumulator.Errors.Add($"{path}: {ex.Message}");
        }

        return accumulator;
    }

    private static DeleteAccumulator DeleteSystemMemoryDumps(CancellationToken cancellationToken)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var accumulator = DeleteSingleFile(Path.Combine(windows, "MEMORY.DMP"));
        Merge(accumulator, DeleteDirectoryContents(Path.Combine(windows, "Minidump"), cancellationToken));
        return accumulator;
    }

    private static CleanupAnalysisResult BuildAnalysisResult(string targetId, ScanMetrics metrics)
    {
        var message = metrics.MessageOverride;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = metrics.Bytes == 0
                ? "Nothing to reclaim right now."
                : $"Found {FormatBytes(metrics.Bytes)} across {metrics.Items:N0} item(s).";
        }

        return new CleanupAnalysisResult
        {
            TargetId = targetId,
            Success = true,
            EstimatedBytes = metrics.Bytes,
            ItemCount = metrics.Items,
            Message = message,
            HasKnownEstimate = GetHasKnownEstimate(metrics),
            Candidates = GetCandidates(metrics),
        };
    }

    private static ScanMetrics MeasureWindowsComponentStoreAvailability()
    {
        var winSxS = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS");
        var exists = Directory.Exists(winSxS);

        return exists
            ? new ScanMetrics(
                0,
                1,
                hasKnownEstimate: false,
                messageOverride: "Available. DISM can remove old Windows update files from the component store.")
            : new ScanMetrics(
                0,
                0,
                hasKnownEstimate: false,
                messageOverride: "WinSxS component store cleanup is not available on this system.");
    }

    private static ScanMetrics MeasureNodeModules(CancellationToken cancellationToken)
    {
        var candidates = FindNodeModules(cancellationToken);
        var totalBytes = candidates.Sum(candidate => candidate.Bytes);
        var totalItems = candidates.Count;

        var message = totalItems == 0
            ? "No node_modules folders were found under the scanned user directories."
            : $"Found {totalItems:N0} node_modules folder(s) totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, totalItems, candidates: candidates, messageOverride: message);
    }

    private static ScanMetrics MeasurePythonVirtualEnvs(CancellationToken cancellationToken)
    {
        var candidates = FindPythonVirtualEnvs(cancellationToken);
        var totalBytes = candidates.Sum(candidate => candidate.Bytes);
        var totalItems = candidates.Count;

        var message = totalItems == 0
            ? "No venv or .venv folders were found under the scanned user directories."
            : $"Found {totalItems:N0} Python virtual environment folder(s) totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, totalItems, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> FindNodeModules(CancellationToken cancellationToken, int maxResults = int.MaxValue)
    {
        return FindNamedDirectories(
            cancellationToken,
            ["node_modules"],
            maxResults,
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}",
            GetNodeModuleSearchRoots(),
            "No node_modules folders were found under the scanned user directories.");
    }

    private static List<CleanupCandidate> FindPythonVirtualEnvs(CancellationToken cancellationToken, int maxResults = int.MaxValue)
    {
        return FindNamedDirectories(
            cancellationToken,
            ["venv", ".venv"],
            maxResults,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            GetNodeModuleSearchRoots(),
            "No venv or .venv folders were found under the scanned user directories.");
    }

    private static List<CleanupCandidate> FindNamedDirectories(
        CancellationToken cancellationToken,
        IReadOnlyCollection<string> names,
        int maxResults,
        string broadRoot,
        IEnumerable<string> targetedRoots,
        string emptyMessage)
    {
        var candidates = new List<CleanupCandidate>();
        var searchRoots = maxResults == 1
            ? targetedRoots
            : [broadRoot];
        var pending = new Stack<DirectoryInfo>(searchRoots.Select(path => new DirectoryInfo(path)));

        while (pending.Count > 0 && candidates.Count < maxResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            DirectoryInfo[] subdirectories;

            try
            {
                subdirectories = current.GetDirectories();
            }
            catch
            {
                continue;
            }

            foreach (var directory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                }
                catch
                {
                }

                if (IsSkippableSearchDirectory(directory))
                {
                    continue;
                }

                if (names.Contains(directory.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var metrics = MeasureDirectoryContents(directory.FullName, cancellationToken);
                    var label = directory.Parent is null
                        ? directory.FullName
                        : $"{directory.Parent.Name} ({directory.Parent.FullName})";

                    candidates.Add(new CleanupCandidate(label, directory.FullName, metrics.Bytes));
                    continue;
                }

                pending.Push(directory);
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Bytes)
            .ThenBy(candidate => candidate.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> GetNodeModuleSearchRoots()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string[] candidates =
        [
            Path.Combine(userProfile, "Desktop"),
            Path.Combine(userProfile, "Documents"),
            Path.Combine(userProfile, "source"),
            Path.Combine(userProfile, "source", "repos"),
            Path.Combine(userProfile, "projects"),
            Path.Combine(userProfile, "dev"),
            Path.Combine(userProfile, "code"),
        ];

        return candidates.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSkippableSearchDirectory(DirectoryInfo directory) =>
        directory.Name is "AppData" or ".git" or ".svn" or "Library" or "obj" or "bin";

    private static ScanMetrics MeasureVirtualMachineDisks(CancellationToken cancellationToken)
    {
        var candidates = FindVirtualMachineDiskCandidates(cancellationToken);
        var totalBytes = candidates.Sum(candidate => candidate.Bytes);
        var totalItems = candidates.Count;

        var message = totalItems == 0
            ? "No virtual machine or emulator disk image files were found in the scanned locations."
            : $"Found {totalItems:N0} VM or emulator disk file(s) totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, totalItems, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> FindVirtualMachineDiskCandidates(CancellationToken cancellationToken, int maxResults = int.MaxValue)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var publicDocuments = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        string[] searchRoots =
        [
            userProfile,
            documents,
            publicDocuments,
            Path.Combine(userProfile, "VirtualBox VMs"),
            Path.Combine(userProfile, "VMware"),
            Path.Combine(userProfile, "Hyper-V"),
            Path.Combine(userProfile, ".android", "avd"),
        ];

        string[] extensions = [".vhd", ".vhdx", ".avhdx", ".vdi", ".vmdk", ".qcow", ".qcow2", ".img", ".hdd"];
        var candidates = new List<CleanupCandidate>();

        foreach (var root in searchRoots.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pending = new Stack<DirectoryInfo>([new DirectoryInfo(root)]);

            while (pending.Count > 0 && candidates.Count < maxResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();

                FileInfo[] files;
                DirectoryInfo[] directories;
                try
                {
                    files = current.GetFiles();
                    directories = current.GetDirectories();
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        var label = $"{file.Name} ({file.DirectoryName})";
                        candidates.Add(new CleanupCandidate(label, file.FullName, file.Length));
                    }
                    catch
                    {
                    }
                }

                foreach (var directory in directories)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                    }

                    if (IsSkippableSearchDirectory(directory))
                    {
                        continue;
                    }

                    pending.Push(directory);
                }
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Bytes)
            .ThenBy(candidate => candidate.Label, StringComparer.CurrentCultureIgnoreCase)
            .Take(maxResults)
            .ToList();
    }

    private static ScanMetrics MeasureDockerData()
    {
        var versionResult = RunProcess("docker", "--version");
        if (versionResult.ExitCode != 0)
        {
            return new ScanMetrics(0, 0, messageOverride: "Docker was not detected.");
        }

        var dfResult = RunProcess("docker", "system df");
        if (dfResult.ExitCode != 0)
        {
            return new ScanMetrics(
                0,
                0,
                hasKnownEstimate: false,
                messageOverride: $"Docker was detected, but reclaimable size could not be read: {FirstNonEmpty(dfResult.StandardError, dfResult.StandardOutput)}");
        }

        var reclaimableBytes = ParseDockerReclaimableBytes(dfResult.StandardOutput);
        return new ScanMetrics(
            reclaimableBytes,
            reclaimableBytes > 0 ? 1 : 0,
            messageOverride: reclaimableBytes == 0
                ? "Docker reports no reclaimable data right now."
                : $"Docker reports about {FormatBytes(reclaimableBytes)} reclaimable via docker system prune.");
    }

    private static long ParseDockerReclaimableBytes(string output)
    {
        var total = 0L;
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("TYPE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+(?:\.\d+)?\s*[KMGTP]?B)\s+\(");
            if (!match.Success)
            {
                continue;
            }

            total += ParseHumanSize(match.Groups[1].Value);
        }

        return total;
    }

    private static ScanMetrics MeasureWslVirtualDisks(CancellationToken cancellationToken)
    {
        var candidates = GetWslVirtualDiskCandidates(cancellationToken);
        var totalBytes = candidates.Sum(candidate => candidate.Bytes);
        var message = candidates.Count == 0
            ? "No WSL .vhdx files were found."
            : $"Found {candidates.Count:N0} WSL virtual disk file(s) totaling {FormatBytes(totalBytes)}. Cleanup opens their locations for manual review.";

        return new ScanMetrics(totalBytes, candidates.Count, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> GetWslVirtualDiskCandidates(CancellationToken cancellationToken)
    {
        var packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
        var candidates = new List<CleanupCandidate>();

        if (!Directory.Exists(packagesRoot))
        {
            return candidates;
        }

        foreach (var packageDir in EnumerateDirectories(packagesRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var localState = Path.Combine(packageDir, "LocalState");
            if (!Directory.Exists(localState))
            {
                continue;
            }

            IEnumerable<string> vhdxFiles;
            try
            {
                vhdxFiles = Directory.EnumerateFiles(localState, "*.vhdx", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var filePath in vhdxFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var info = new FileInfo(filePath);
                    candidates.Add(new CleanupCandidate($"{Path.GetFileName(packageDir)} ({info.Name})", filePath, info.Length));
                }
                catch
                {
                }
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Bytes)
            .ThenBy(candidate => candidate.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static DeleteAccumulator RunWindowsComponentStoreCleanup(bool useResetBase)
    {
        var args = useResetBase
            ? "/Online /Cleanup-Image /StartComponentCleanup /ResetBase"
            : "/Online /Cleanup-Image /StartComponentCleanup";
        var processResult = RunProcess("DISM.exe", args, timeoutMilliseconds: 600000);

        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmpty(processResult.StandardError, processResult.StandardOutput));
        }

        return new DeleteAccumulator
        {
            CustomMessage = useResetBase
                ? "Windows Component Store cleanup completed with /ResetBase. DISM removed old Windows update files and reset superseded component baselines."
                : "Windows Component Store cleanup completed. DISM removed old Windows update files.",
        };
    }

    private static DeleteAccumulator RunDockerCleanup()
    {
        var processResult = RunProcess("docker", "system prune -a -f", timeoutMilliseconds: 600000);
        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmpty(processResult.StandardError, processResult.StandardOutput));
        }

        return new DeleteAccumulator
        {
            CustomMessage = "Docker cleanup completed. Re-run Analyze Selected to see the updated reclaimable size.",
        };
    }

    private static ScanMetrics MeasureHibernationFile()
    {
        var hiberPath = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? @"C:\", "hiberfil.sys");
        if (!File.Exists(hiberPath))
        {
            return new ScanMetrics(0, 0, messageOverride: "Hibernation is not enabled or hiberfil.sys was not found.");
        }

        try
        {
            var info = new FileInfo(hiberPath);
            return new ScanMetrics(info.Length, 1, messageOverride: $"hiberfil.sys is {FormatBytes(info.Length)}. Cleanup will disable hibernation to remove it.");
        }
        catch
        {
            return new ScanMetrics(0, 0, hasKnownEstimate: false, messageOverride: "hiberfil.sys exists but its size could not be read (requires administrator).");
        }
    }

    private static DeleteAccumulator RunDisableHibernation()
    {
        var processResult = RunProcess("powercfg", "/hibernate off", timeoutMilliseconds: 30000);
        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException(FirstNonEmpty(processResult.StandardError, processResult.StandardOutput));
        }

        return new DeleteAccumulator
        {
            CustomMessage = "Hibernation has been disabled and hiberfil.sys will be removed. To re-enable, run 'powercfg /hibernate on' from an elevated prompt.",
        };
    }

    private static ScanMetrics MeasureSystemRestorePoints()
    {
        var processResult = RunProcess("vssadmin", "list shadows", timeoutMilliseconds: 30000);
        if (processResult.ExitCode != 0)
        {
            return new ScanMetrics(0, 0, hasKnownEstimate: false, messageOverride: "Could not query shadow copies. Requires administrator privileges.");
        }

        var shadowCount = 0;
        foreach (var line in processResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.TrimStart().StartsWith("Shadow Copy ID:", StringComparison.OrdinalIgnoreCase))
            {
                shadowCount++;
            }
        }

        if (shadowCount <= 1)
        {
            return new ScanMetrics(0, 0, messageOverride: shadowCount == 0
                ? "No shadow copies found."
                : "Only one shadow copy exists; nothing to remove.");
        }

        return new ScanMetrics(0, shadowCount - 1, hasKnownEstimate: false,
            messageOverride: $"Found {shadowCount} shadow copies. Cleanup will remove all but the most recent, which can reclaim significant space.");
    }

    private static DeleteAccumulator RunSystemRestoreCleanup()
    {
        var processResult = RunProcess("vssadmin", "delete shadows /for=C: /oldest /quiet", timeoutMilliseconds: 120000);

        return new DeleteAccumulator
        {
            CustomMessage = processResult.ExitCode == 0
                ? "System Restore shadow copies have been cleaned up. Re-run Analyze to verify."
                : $"Shadow copy cleanup returned: {FirstNonEmpty(processResult.StandardError, processResult.StandardOutput)}",
        };
    }

    private static ScanMetrics MeasureRustTargetDirectories(CancellationToken cancellationToken)
    {
        var candidates = FindRustTargetDirectories(cancellationToken);
        var totalBytes = candidates.Sum(c => c.Bytes);
        var message = candidates.Count == 0
            ? "No Rust target directories were found under the scanned user directories."
            : $"Found {candidates.Count:N0} Rust target director{(candidates.Count == 1 ? "y" : "ies")} totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, candidates.Count, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> FindRustTargetDirectories(CancellationToken cancellationToken, int maxResults = int.MaxValue)
    {
        return FindCargoTargetDirectories(cancellationToken, maxResults);
    }

    private static List<CleanupCandidate> FindCargoTargetDirectories(CancellationToken cancellationToken, int maxResults)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<CleanupCandidate>();
        var searchRoots = maxResults == 1
            ? GetNodeModuleSearchRoots()
            : [userProfile];
        var pending = new Stack<DirectoryInfo>(searchRoots.Select(p => new DirectoryInfo(p)));

        while (pending.Count > 0 && candidates.Count < maxResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            DirectoryInfo[] subdirectories;

            try { subdirectories = current.GetDirectories(); }
            catch { continue; }

            foreach (var directory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try { if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                catch { }

                if (IsSkippableSearchDirectory(directory)) continue;

                if (string.Equals(directory.Name, "target", StringComparison.OrdinalIgnoreCase))
                {
                    var cargoToml = Path.Combine(directory.Parent?.FullName ?? "", "Cargo.toml");
                    if (File.Exists(cargoToml))
                    {
                        var metrics = MeasureDirectoryContents(directory.FullName, cancellationToken);
                        var label = directory.Parent is null
                            ? directory.FullName
                            : $"{directory.Parent.Name} ({directory.Parent.FullName})";
                        candidates.Add(new CleanupCandidate(label, directory.FullName, metrics.Bytes));
                    }
                    continue;
                }

                pending.Push(directory);
            }
        }

        return candidates.OrderByDescending(c => c.Bytes).ThenBy(c => c.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static ScanMetrics MeasureDotNetBuildArtifacts(CancellationToken cancellationToken)
    {
        var candidates = FindDotNetBuildArtifacts(cancellationToken);
        var totalBytes = candidates.Sum(c => c.Bytes);
        var message = candidates.Count == 0
            ? "No .NET build artifact directories (obj/bin) were found under the scanned user directories."
            : $"Found {candidates.Count:N0} build artifact director{(candidates.Count == 1 ? "y" : "ies")} totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, candidates.Count, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> FindDotNetBuildArtifacts(CancellationToken cancellationToken, int maxResults = int.MaxValue)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<CleanupCandidate>();
        var searchRoots = maxResults == 1
            ? GetNodeModuleSearchRoots()
            : [userProfile];
        var pending = new Stack<DirectoryInfo>(searchRoots.Select(p => new DirectoryInfo(p)));

        while (pending.Count > 0 && candidates.Count < maxResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();
            DirectoryInfo[] subdirectories;

            try { subdirectories = current.GetDirectories(); }
            catch { continue; }

            foreach (var directory in subdirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try { if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                catch { }

                if (IsSkippableSearchDirectory(directory)) continue;
                if (string.Equals(directory.Name, "node_modules", StringComparison.OrdinalIgnoreCase)) continue;

                if (string.Equals(directory.Name, "obj", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(directory.Name, "bin", StringComparison.OrdinalIgnoreCase))
                {
                    var parent = directory.Parent;
                    if (parent != null)
                    {
                        var hasCsproj = false;
                        try { hasCsproj = parent.GetFiles("*.csproj").Length > 0 || parent.GetFiles("*.fsproj").Length > 0 || parent.GetFiles("*.vbproj").Length > 0; }
                        catch { }

                        if (hasCsproj)
                        {
                            var metrics = MeasureDirectoryContents(directory.FullName, cancellationToken);
                            if (metrics.Bytes > 0)
                            {
                                var label = $"{directory.Name} in {parent.Name} ({parent.FullName})";
                                candidates.Add(new CleanupCandidate(label, directory.FullName, metrics.Bytes));
                            }
                        }
                    }
                    continue;
                }

                pending.Push(directory);
            }
        }

        return candidates.OrderByDescending(c => c.Bytes).ThenBy(c => c.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static ScanMetrics MeasureJetBrainsCachePaths(CancellationToken cancellationToken)
    {
        var paths = GetJetBrainsCachePaths();
        return MeasurePathSet(paths, cancellationToken);
    }

    private static IReadOnlyList<string> GetJetBrainsCachePaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var jetBrainsRoot = Path.Combine(localAppData, "JetBrains");
        var paths = new List<string>();

        foreach (var productDir in EnumerateDirectories(jetBrainsRoot))
        {
            paths.Add(Path.Combine(productDir, "caches"));
            paths.Add(Path.Combine(productDir, "index"));
            paths.Add(Path.Combine(productDir, "tmp"));
            paths.Add(Path.Combine(productDir, "log"));
        }

        return paths;
    }

    private static ScanMetrics MeasureVisualStudioCachePaths(CancellationToken cancellationToken)
    {
        var paths = GetVisualStudioCachePaths();
        return MeasurePathSet(paths, cancellationToken);
    }

    private static IReadOnlyList<string> GetVisualStudioCachePaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var vsRoot = Path.Combine(localAppData, @"Microsoft\VisualStudio");
        var vsCommon = Path.Combine(localAppData, @"Microsoft\VSCommon");
        var paths = new List<string>();

        foreach (var versionDir in EnumerateDirectories(vsRoot))
        {
            paths.Add(Path.Combine(versionDir, "ComponentModelCache"));
            paths.Add(Path.Combine(versionDir, "Extensions", "MefCache"));
            paths.Add(Path.Combine(versionDir, "Designer", "ShadowCache"));
        }

        foreach (var versionDir in EnumerateDirectories(vsCommon))
        {
            paths.Add(Path.Combine(versionDir, "Extensions"));
        }

        return paths;
    }

    private static ScanMetrics MeasureWindowsEventLogs(CancellationToken cancellationToken)
    {
        var logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\winevt\Logs");
        return MeasureDirectoryContents(logsPath, cancellationToken);
    }

    private static DeleteAccumulator RunClearWindowsEventLogs()
    {
        var processResult = RunProcess("wevtutil", "el", timeoutMilliseconds: 30000);
        if (processResult.ExitCode != 0)
        {
            throw new InvalidOperationException("Failed to enumerate event logs. Requires administrator privileges.");
        }

        var logs = processResult.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var cleared = 0;
        var errors = new List<string>();

        foreach (var log in logs)
        {
            var clearResult = RunProcess("wevtutil", $"cl \"{log.Trim()}\"", timeoutMilliseconds: 10000);
            if (clearResult.ExitCode == 0)
            {
                cleared++;
            }
            else if (errors.Count < 3)
            {
                errors.Add($"{log}: {FirstNonEmpty(clearResult.StandardError, clearResult.StandardOutput)}");
            }
        }

        var accumulator = new DeleteAccumulator
        {
            Items = cleared,
            CustomMessage = $"Cleared {cleared:N0} event log(s)." + (errors.Count > 0 ? $" {errors.Count} could not be cleared." : ""),
        };
        accumulator.Errors.AddRange(errors);
        return accumulator;
    }

    private static DeleteAccumulator RunDeleteSearchIndex()
    {
        var stopResult = RunProcess("net", "stop WSearch", timeoutMilliseconds: 60000);

        var edbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"Microsoft\Search\Data\Applications\Windows\Windows.edb");

        var accumulator = DeleteSingleFile(edbPath);

        RunProcess("net", "start WSearch", timeoutMilliseconds: 60000);

        accumulator.CustomMessage = accumulator.Bytes > 0
            ? $"Deleted Windows Search index ({FormatBytes(accumulator.Bytes)}). The index will rebuild automatically."
            : "Windows Search index could not be deleted (may be locked or not found).";

        return accumulator;
    }

    private static ScanMetrics MeasureOldLargeLogFiles(CancellationToken cancellationToken)
    {
        var candidates = FindOldLargeLogFiles(cancellationToken);
        var totalBytes = candidates.Sum(c => c.Bytes);
        var message = candidates.Count == 0
            ? "No large log files (>10 MB) were found in the scanned locations."
            : $"Found {candidates.Count:N0} large log file(s) totaling {FormatBytes(totalBytes)}.";

        return new ScanMetrics(totalBytes, candidates.Count, candidates: candidates, messageOverride: message);
    }

    private static List<CleanupCandidate> FindOldLargeLogFiles(CancellationToken cancellationToken)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        string[] searchRoots =
        [
            Path.Combine(windows, "Logs"),
            Path.Combine(windows, @"System32\LogFiles"),
            Path.Combine(commonAppData, @"Microsoft\Windows\WER"),
        ];

        string[] extensions = [".log", ".etl", ".evtx", ".dmp", ".cab"];
        const long minSize = 10 * 1024 * 1024; // 10 MB
        var candidates = new List<CleanupCandidate>();

        foreach (var root in searchRoots.Where(Directory.Exists))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pending = new Stack<DirectoryInfo>([new DirectoryInfo(root)]);

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();

                FileInfo[] files;
                DirectoryInfo[] directories;
                try
                {
                    files = current.GetFiles();
                    directories = current.GetDirectories();
                }
                catch { continue; }

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!extensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)) continue;

                    try
                    {
                        if (file.Length >= minSize)
                        {
                            candidates.Add(new CleanupCandidate($"{file.Name} ({FormatBytes(file.Length)})", file.FullName, file.Length));
                        }
                    }
                    catch { }
                }

                foreach (var dir in directories)
                {
                    try { if ((dir.Attributes & FileAttributes.ReparsePoint) != 0) continue; }
                    catch { }
                    pending.Push(dir);
                }
            }
        }

        return candidates.OrderByDescending(c => c.Bytes).ThenBy(c => c.Label, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private static DeleteAccumulator DeleteOldLargeLogFiles(CancellationToken cancellationToken)
    {
        var candidates = FindOldLargeLogFiles(cancellationToken);
        var accumulator = new DeleteAccumulator();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Merge(accumulator, DeleteSingleFile(candidate.Path));
        }

        return accumulator;
    }

    private static DeleteAccumulator OpenWslDiskLocations()
    {
        var candidates = GetWslVirtualDiskCandidates(CancellationToken.None);
        var directories = candidates
            .Select(candidate => Path.GetDirectoryName(candidate.Path))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (directories.Count == 0)
        {
            return new DeleteAccumulator
            {
                CustomMessage = "No WSL virtual disk locations were found to open.",
            };
        }

        foreach (var directory in directories.Take(3))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directory!,
                UseShellExecute = true,
            });
        }

        return new DeleteAccumulator
        {
            CustomMessage = "Opened WSL virtual disk location(s) for manual review. Shut WSL down before compacting or editing any VHDX files.",
        };
    }

    private static ProcessResult RunProcess(string fileName, string arguments, int timeoutMilliseconds = 15000)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return new ProcessResult(-1, string.Empty, $"{fileName} timed out.");
        }

        Task.WaitAll(new Task[] { standardOutputTask, standardErrorTask }, timeoutMilliseconds);
        var standardOutput = standardOutputTask.Result;
        var standardError = standardErrorTask.Result;
        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static long ParseHumanSize(string sizeText)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sizeText.Trim(), @"^(?<value>\d+(?:\.\d+)?)\s*(?<unit>[KMGTP]?B)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return 0;
        }

        var value = double.Parse(match.Groups["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value.ToUpperInvariant();
        var multiplier = unit switch
        {
            "KB" => 1024d,
            "MB" => 1024d * 1024,
            "GB" => 1024d * 1024 * 1024,
            "TB" => 1024d * 1024 * 1024 * 1024,
            "PB" => 1024d * 1024 * 1024 * 1024 * 1024,
            _ => 1d,
        };

        return (long)Math.Round(value * multiplier);
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim()
        ?? "No output was returned.";

    private static IReadOnlyList<string> GetFirefoxCachePaths()
    {
        var localProfilesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Mozilla\Firefox\Profiles");
        var roamingProfilesRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Mozilla\Firefox\Profiles");

        var paths = new List<string>();

        foreach (var profile in EnumerateDirectories(localProfilesRoot))
        {
            paths.Add(Path.Combine(profile, "cache2"));
            paths.Add(Path.Combine(profile, "jumpListCache"));
            paths.Add(Path.Combine(profile, "thumbnails"));
        }

        foreach (var profile in EnumerateDirectories(roamingProfilesRoot))
        {
            paths.Add(Path.Combine(profile, "startupCache"));
        }

        return paths;
    }

    private static IReadOnlyList<string> GetTeamsCachePaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return
        [
            Path.Combine(roamingAppData, @"Microsoft\Teams\application cache\cache"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\blob_storage"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\Cache"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\Code Cache"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\databases"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\GPUCache"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\IndexedDB"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\Local Storage"),
            Path.Combine(roamingAppData, @"Microsoft\Teams\tmp"),
            Path.Combine(localAppData, @"Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams\EBWebView\Default\Cache"),
            Path.Combine(localAppData, @"Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams\EBWebView\Default\Code Cache"),
            Path.Combine(localAppData, @"Packages\MSTeams_8wekyb3d8bbwe\LocalCache\Microsoft\MSTeams\EBWebView\Default\GPUCache"),
        ];
    }

    private static IReadOnlyList<string> GetEpicGamesLauncherCachePaths()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"EpicGamesLauncher\Saved");

        var paths = new List<string>();
        foreach (var directory in EnumerateDirectories(root, "webcache*"))
        {
            paths.Add(directory);
        }

        return paths;
    }

    private static bool ShouldHideWhenIrrelevant(CleanupTargetDefinition definition) =>
        definition.Category is "Browser" or "App" or "Gaming" or "Developer" or "Containers" or "Virtualization";

    private static IEnumerable<string> GetRelevantPaths(CleanupTargetDefinition definition) =>
        definition.Kind switch
        {
            CleanupTargetKind.FirefoxCache => GetFirefoxCachePaths().Concat(definition.Paths),
            CleanupTargetKind.TeamsCache => GetTeamsCachePaths().Concat(definition.Paths),
            CleanupTargetKind.EpicGamesLauncherCache => GetEpicGamesLauncherCachePaths().Concat(definition.Paths),
            CleanupTargetKind.JetBrainsCache => GetJetBrainsCachePaths().Concat(definition.Paths),
            CleanupTargetKind.VisualStudioCache => GetVisualStudioCachePaths().Concat(definition.Paths),
            _ => definition.Paths,
        };

    private static bool PathExists(string path) =>
        File.Exists(path) || Directory.Exists(path);

    private static IEnumerable<string> EnumerateDirectories(string rootPath)
    {
        return EnumerateDirectories(rootPath, "*");
    }

    private static IEnumerable<string> EnumerateDirectories(string rootPath, string searchPattern)
    {
        if (!Directory.Exists(rootPath))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory.EnumerateDirectories(rootPath, searchPattern, SearchOption.TopDirectoryOnly).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static ScanMetrics MeasureRecycleBin()
    {
        var info = new RecycleBinInfo
        {
            cbSize = (uint)Marshal.SizeOf<RecycleBinInfo>(),
        };

        var hr = SHQueryRecycleBin(@"C:\", ref info);
        if (hr != 0)
        {
            throw new InvalidOperationException($"Shell reported error 0x{hr:X8} while reading the Recycle Bin.");
        }

        return new ScanMetrics(info.i64Size, (int)Math.Min(int.MaxValue, info.i64NumItems));
    }

    private static DeleteAccumulator EmptyRecycleBin()
    {
        var before = MeasureRecycleBin();
        var hr = SHEmptyRecycleBin(
            IntPtr.Zero,
            @"C:\",
            RecycleBinFlags.NoConfirmation | RecycleBinFlags.NoProgressUi | RecycleBinFlags.NoSound);

        if (hr != 0)
        {
            throw new InvalidOperationException($"Shell reported error 0x{hr:X8} while emptying the Recycle Bin.");
        }

        return new DeleteAccumulator
        {
            Bytes = before.Bytes,
            Items = before.Items,
        };
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.##} {units[unitIndex]}";
    }

    private static ScanMetrics Combine(ScanMetrics left, ScanMetrics right) =>
        new(
            left.Bytes + right.Bytes,
            left.Items + right.Items,
            hasKnownEstimate: GetHasKnownEstimate(left) && GetHasKnownEstimate(right),
            candidates: GetCandidates(left).Concat(GetCandidates(right)).ToList(),
            messageOverride: left.MessageOverride ?? right.MessageOverride);

    private static void Merge(DeleteAccumulator target, DeleteAccumulator source)
    {
        target.Bytes += source.Bytes;
        target.Items += source.Items;
        target.Errors.AddRange(source.Errors);
        target.CustomMessage ??= source.CustomMessage;
    }

    private readonly record struct ScanMetrics
    {
        public ScanMetrics(
            long bytes,
            int items,
            bool hasKnownEstimate = true,
            IReadOnlyList<CleanupCandidate>? candidates = null,
            string? messageOverride = null)
        {
            Bytes = bytes;
            Items = items;
            HasKnownEstimate = hasKnownEstimate;
            Candidates = candidates ?? Array.Empty<CleanupCandidate>();
            MessageOverride = messageOverride;
        }

        public long Bytes { get; }

        public int Items { get; }

        public bool HasKnownEstimate { get; } = true;

        public IReadOnlyList<CleanupCandidate> Candidates { get; } = Array.Empty<CleanupCandidate>();

        public string? MessageOverride { get; }
    }

    private sealed class DeleteAccumulator
    {
        public long Bytes { get; set; }

        public int Items { get; set; }

        public List<string> Errors { get; } = new();

        public string? CustomMessage { get; set; }
    }

    private readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private static bool GetHasKnownEstimate(ScanMetrics metrics) =>
        metrics.Candidates is null && metrics.MessageOverride is null && metrics.Bytes == 0 && metrics.Items == 0
            ? true
            : metrics.HasKnownEstimate;

    private static IReadOnlyList<CleanupCandidate> GetCandidates(ScanMetrics metrics) =>
        metrics.Candidates ?? Array.Empty<CleanupCandidate>();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RecycleBinInfo
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [Flags]
    private enum RecycleBinFlags : uint
    {
        NoConfirmation = 0x00000001,
        NoProgressUi = 0x00000002,
        NoSound = 0x00000004,
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? rootPath, ref RecycleBinInfo info);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, RecycleBinFlags flags);
}
