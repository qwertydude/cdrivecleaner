using System.Diagnostics;
using System.Drawing;
using CDriveCleaner.Models;
using CDriveCleaner.Services;

namespace CDriveCleaner;

internal sealed class MainForm : Form
{
    private readonly CleanupService _cleanupService = new();
    private readonly Dictionary<string, CleanupTargetState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TimeSpan> _scanDurationHistory = new(StringComparer.OrdinalIgnoreCase);

    private readonly Label _driveHeadline = new();
    private readonly Label _driveSubheadline = new();
    private readonly ProgressBar _driveProgressBar = new();
    private readonly Label _usedSpaceStatLabel = new();
    private readonly Label _freeSpaceStatLabel = new();
    private readonly Label _totalCapacityStatLabel = new();
    private readonly Label _freeShareStatLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ListView _targetsListView = new();
    private readonly ComboBox _sortComboBox = new();
    private readonly CheckBox _showIrrelevantCheckBox = new();
    private readonly Label _targetsSummaryLabel = new();
    private readonly TextBox _searchTextBox = new();
    private readonly Button _clearSearchButton = new();
    private readonly Label _selectedEstimateLabel = new();
    private readonly TextBox _detailsTextBox = new();
    private readonly RichTextBox _logTextBox = new();

    private readonly Button _selectRecommendedButton = new();
    private readonly Button _selectAllButton = new();
    private readonly Button _clearSelectionButton = new();
    private readonly Button _analyzeButton = new();
    private readonly Button _cleanButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _storageSettingsButton = new();
    private readonly Button _diskCleanupButton = new();
    private readonly Button _tempFolderButton = new();
    private readonly Button _clearLogButton = new();
    private readonly CheckBox _useAggressiveCleanupCheckBox = new();
    private bool _isPopulatingTargets;

    private enum TargetSortMode
    {
        Recommended,
        BiggestFirst,
        SmallestFirst,
        Name,
        Risk,
    }

    private enum ButtonTone
    {
        Neutral,
        Accent,
        Danger,
    }

    public MainForm()
    {
        Text = "C Drive Cleaner";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1280, 860);
        BackColor = Color.FromArgb(242, 246, 251);
        Font = new Font("Segoe UI", 9F);

        InitializeState();
        InitializeLayout();
        PopulateTargetList();

        Shown += async (_, _) =>
        {
            SelectRecommendedTargets();
            await RefreshDriveSummaryAsync();
            Log("Ready. Review the selected cleanup targets, then run Analyze Selected before cleaning.");
        };
    }

    private void InitializeState()
    {
        foreach (var definition in _cleanupService.Definitions)
        {
            var state = new CleanupTargetState(definition);
            state.SetRelevance(_cleanupService.IsRelevant(definition));
            _states.Add(definition.Id, state);
        }
    }

    private void InitializeLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = BackColor,
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));

        var headerPanel = BuildHeaderPanel();
        var targetsPanel = BuildTargetsPanel();
        var actionsPanel = BuildActionsPanel();
        var logPanel = BuildLogPanel();

        root.Controls.Add(headerPanel, 0, 0);
        root.SetColumnSpan(headerPanel, 2);
        root.Controls.Add(targetsPanel, 0, 1);
        root.Controls.Add(actionsPanel, 1, 1);
        root.Controls.Add(logPanel, 0, 2);
        root.SetColumnSpan(logPanel, 2);

        Controls.Add(root);
    }

    private Control BuildHeaderPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 16);
        panel.Padding = new Padding(26, 24, 26, 24);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));

        var introLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0, 0, 20, 0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        introLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        introLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        introLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        introLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var eyebrow = new Label
        {
            Text = "Storage Cleanup Dashboard",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(67, 109, 155),
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
        };

        var title = new Label
        {
            Text = "C Drive Cleaner",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(30, 48, 70),
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
        };

        var subtitle = CreateMutedLabel(
            "Analyze temp files, caches, browser data, update leftovers, and other reclaimable storage on C:.");
        subtitle.Dock = DockStyle.Fill;
        subtitle.TextAlign = ContentAlignment.MiddleLeft;

        var statusHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(240, 246, 252),
            Padding = new Padding(14, 10, 14, 10),
            Margin = new Padding(0, 8, 0, 0),
        };
        AttachSubtleBorder(statusHost, Color.FromArgb(216, 226, 237));

        _statusLabel.Text = "Ready";
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.ForeColor = Color.FromArgb(54, 82, 112);
        _statusLabel.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

        statusHost.Controls.Add(_statusLabel);
        introLayout.Controls.Add(eyebrow, 0, 0);
        introLayout.Controls.Add(title, 0, 1);
        introLayout.Controls.Add(subtitle, 0, 2);
        introLayout.Controls.Add(statusHost, 0, 3);

        var driveCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(247, 250, 252),
            Padding = new Padding(18, 16, 18, 16),
            Margin = new Padding(0),
        };
        AttachSubtleBorder(driveCard, Color.FromArgb(220, 229, 237));

        var driveLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = driveCard.BackColor,
        };
        driveLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        driveLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        driveLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        driveLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        driveLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var driveLabel = new Label
        {
            Text = "Drive Overview",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(54, 74, 98),
        };

        _driveHeadline.Text = "C: drive usage";
        _driveHeadline.Dock = DockStyle.Fill;
        _driveHeadline.TextAlign = ContentAlignment.MiddleLeft;
        _driveHeadline.ForeColor = Color.FromArgb(30, 48, 70);
        _driveHeadline.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);

        _driveProgressBar.Dock = DockStyle.Fill;
        _driveProgressBar.Style = ProgressBarStyle.Continuous;
        _driveProgressBar.Maximum = 100;
        _driveProgressBar.Margin = new Padding(0, 4, 0, 6);

        _driveSubheadline.Text = "Loading drive details...";
        _driveSubheadline.Dock = DockStyle.Fill;
        _driveSubheadline.TextAlign = ContentAlignment.TopLeft;
        _driveSubheadline.ForeColor = Color.FromArgb(88, 102, 119);
        _driveSubheadline.Font = new Font("Segoe UI", 10F);

        var statsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 0),
            Padding = new Padding(0),
            BackColor = driveCard.BackColor,
        };
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        statsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        statsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        statsGrid.Controls.Add(CreateDriveStatCard("Used space", _usedSpaceStatLabel), 0, 0);
        statsGrid.Controls.Add(CreateDriveStatCard("Free space", _freeSpaceStatLabel), 1, 0);
        statsGrid.Controls.Add(CreateDriveStatCard("Total size", _totalCapacityStatLabel), 0, 1);
        statsGrid.Controls.Add(CreateDriveStatCard("Free share", _freeShareStatLabel), 1, 1);

        driveLayout.Controls.Add(driveLabel, 0, 0);
        driveLayout.Controls.Add(_driveHeadline, 0, 1);
        driveLayout.Controls.Add(_driveProgressBar, 0, 2);
        driveLayout.Controls.Add(_driveSubheadline, 0, 3);
        driveLayout.Controls.Add(statsGrid, 0, 4);
        driveCard.Controls.Add(driveLayout);

        layout.Controls.Add(introLayout, 0, 0);
        layout.Controls.Add(driveCard, 1, 0);
        panel.Controls.Add(layout);

        return panel;
    }

    private Control BuildTargetsPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 16, 16);

        var title = CreateSectionLabel("Cleanup Targets");
        var subtitle = CreateMutedLabel("Use the checkboxes to choose which C: cleanup jobs to scan and run.");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 14, 10),
            BackColor = Color.FromArgb(248, 250, 252),
            Margin = new Padding(0, 8, 0, 12),
        };
        AttachSubtleBorder(toolbar, Color.FromArgb(220, 228, 237));

        var toolbarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = toolbar.BackColor,
        };
        toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        toolbarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = toolbar.BackColor,
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var sortLabel = new Label
        {
            Text = "Sort",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(88, 102, 119),
        };

        _sortComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sortComboBox.FlatStyle = FlatStyle.Flat;
        _sortComboBox.Font = new Font("Segoe UI", 9.25F);
        _sortComboBox.Items.AddRange(
        [
            "Recommended order",
            "Estimated biggest wins",
            "Estimated smallest first",
            "Name (A-Z)",
            "Highest risk first",
        ]);
        _sortComboBox.SelectedIndex = 0;
        _sortComboBox.Dock = DockStyle.Fill;
        _sortComboBox.Margin = new Padding(10, 0, 18, 0);
        _sortComboBox.SelectedIndexChanged += (_, _) => PopulateTargetList();

        _showIrrelevantCheckBox.Text = "Show irrelevant targets too";
        _showIrrelevantCheckBox.Dock = DockStyle.Fill;
        _showIrrelevantCheckBox.ForeColor = Color.FromArgb(88, 102, 119);
        _showIrrelevantCheckBox.Margin = new Padding(0, 4, 0, 0);
        _showIrrelevantCheckBox.CheckedChanged += (_, _) => PopulateTargetList();

        _targetsSummaryLabel.Dock = DockStyle.Fill;
        _targetsSummaryLabel.TextAlign = ContentAlignment.MiddleRight;
        _targetsSummaryLabel.ForeColor = Color.FromArgb(88, 102, 119);

        var searchLabel = new Label
        {
            Text = "Search",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(88, 102, 119),
        };

        _searchTextBox.Dock = DockStyle.Fill;
        _searchTextBox.Margin = new Padding(10, 0, 10, 0);
        _searchTextBox.Font = new Font("Segoe UI", 9.25F);
        _searchTextBox.PlaceholderText = "Search targets or use filters like risk:high category:browser -admin:yes \"node modules\"";
        _searchTextBox.TextChanged += (_, _) => PopulateTargetList();

        ConfigureInlineButton(_clearSearchButton, "Clear", (_, _) => _searchTextBox.Clear());

        var searchRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = toolbar.BackColor,
        };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        topRow.Controls.Add(sortLabel, 0, 0);
        topRow.Controls.Add(_sortComboBox, 1, 0);
        topRow.Controls.Add(_showIrrelevantCheckBox, 2, 0);
        topRow.Controls.Add(_targetsSummaryLabel, 3, 0);

        searchRow.Controls.Add(searchLabel, 0, 0);
        searchRow.Controls.Add(_searchTextBox, 1, 0);
        searchRow.Controls.Add(_clearSearchButton, 2, 0);

        toolbarLayout.Controls.Add(topRow, 0, 0);
        toolbarLayout.Controls.Add(searchRow, 0, 1);
        toolbar.Controls.Add(toolbarLayout);

        _targetsListView.Dock = DockStyle.Fill;
        _targetsListView.CheckBoxes = true;
        _targetsListView.BorderStyle = BorderStyle.None;
        _targetsListView.FullRowSelect = true;
        _targetsListView.MultiSelect = false;
        _targetsListView.GridLines = false;
        _targetsListView.HideSelection = false;
        _targetsListView.ShowItemToolTips = true;
        _targetsListView.View = View.Details;
        _targetsListView.Font = new Font("Segoe UI", 9.25F);
        _targetsListView.SelectedIndexChanged += (_, _) => RefreshDetailsPanel();
        _targetsListView.ItemChecked += TargetsListViewOnItemChecked;

        _targetsListView.Columns.Add("Target", 220);
        _targetsListView.Columns.Add("Category", 95);
        _targetsListView.Columns.Add("Risk", 65);
        _targetsListView.Columns.Add("Admin", 70);
        _targetsListView.Columns.Add("Estimated", 110);
        _targetsListView.Columns.Add("Status", 320);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(toolbar, 0, 2);
        layout.Controls.Add(CreateInsetHost(_targetsListView), 0, 3);
        panel.Controls.Add(layout);

        return panel;
    }

    private Control BuildActionsPanel()
    {
        var panel = CreateCardPanel();
        panel.Margin = new Padding(0, 0, 0, 16);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = CreateSectionLabel("Actions");
        var subtitle = CreateMutedLabel("Selection, cleanup, and built-in Windows shortcuts.");
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);

        var selectionGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 10, 0, 10),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        selectionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selectionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selectionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        selectionGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        ConfigureButton(_selectRecommendedButton, "Select Recommended", (_, _) => SelectRecommendedTargets());
        ConfigureButton(_selectAllButton, "Select All", (_, _) => SetSelectionForAllTargets(true));
        ConfigureButton(_clearSelectionButton, "Clear Selection", (_, _) => SetSelectionForAllTargets(false));
        ConfigureButton(_analyzeButton, "Analyze Selected", async (_, _) => await AnalyzeSelectedAsync(), ButtonTone.Accent);
        ConfigureButton(_cleanButton, "Clean Selected", async (_, _) => await CleanSelectedAsync(), ButtonTone.Danger);
        ConfigureButton(_refreshButton, "Refresh C Drive", async (_, _) => await RefreshDriveSummaryAsync());
        ConfigureButton(_storageSettingsButton, "Open Storage Settings", (_, _) => OpenShellTarget("ms-settings:storagesense"));
        ConfigureButton(_diskCleanupButton, "Launch Disk Cleanup", (_, _) => OpenShellTarget("cleanmgr.exe"));
        ConfigureButton(_tempFolderButton, "Open Temp Folder", (_, _) => OpenShellTarget(Path.GetTempPath()));
        ConfigureButton(_clearLogButton, "Clear Log", (_, _) => _logTextBox.Clear());

        selectionGrid.Controls.Add(_selectRecommendedButton, 0, 0);
        selectionGrid.Controls.Add(_selectAllButton, 1, 0);
        selectionGrid.Controls.Add(_clearSelectionButton, 0, 1);
        selectionGrid.SetColumnSpan(_clearSelectionButton, 2);
        layout.Controls.Add(selectionGrid, 0, 2);
        layout.Controls.Add(_analyzeButton, 0, 3);
        layout.Controls.Add(_cleanButton, 0, 4);

        var utilitiesGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, 10, 0, 8),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        utilitiesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        utilitiesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        utilitiesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        utilitiesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        utilitiesGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        utilitiesGrid.Controls.Add(_refreshButton, 0, 0);
        utilitiesGrid.Controls.Add(_clearLogButton, 1, 0);
        utilitiesGrid.Controls.Add(_storageSettingsButton, 0, 1);
        utilitiesGrid.Controls.Add(_diskCleanupButton, 1, 1);
        utilitiesGrid.Controls.Add(_tempFolderButton, 0, 2);
        utilitiesGrid.SetColumnSpan(_tempFolderButton, 2);
        layout.Controls.Add(utilitiesGrid, 0, 5);

        var optionsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(0, 4, 0, 12),
        };
        AttachSubtleBorder(optionsPanel, Color.FromArgb(220, 228, 237));

        var optionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = optionsPanel.BackColor,
        };
        optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        optionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _useAggressiveCleanupCheckBox.Text = "Use aggressive cleanup when supported";
        _useAggressiveCleanupCheckBox.AutoSize = true;
        _useAggressiveCleanupCheckBox.ForeColor = Color.FromArgb(88, 102, 119);
        _useAggressiveCleanupCheckBox.Dock = DockStyle.Fill;
        _useAggressiveCleanupCheckBox.Margin = new Padding(0);
        optionsLayout.Controls.Add(_useAggressiveCleanupCheckBox, 0, 0);

        _selectedEstimateLabel.Dock = DockStyle.Fill;
        _selectedEstimateLabel.TextAlign = ContentAlignment.TopLeft;
        _selectedEstimateLabel.Font = new Font("Segoe UI", 9F);
        _selectedEstimateLabel.ForeColor = Color.FromArgb(88, 102, 119);
        _selectedEstimateLabel.Text =
            "Selected estimate: 0 B. Analyze the checked targets to see how much space they may save.";
        optionsLayout.Controls.Add(_selectedEstimateLabel, 0, 1);
        optionsPanel.Controls.Add(optionsLayout);
        layout.Controls.Add(optionsPanel, 0, 6);

        var detailsSection = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        detailsSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        detailsSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var detailsLabel = new Label
        {
            Text = "Target Details",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(36, 52, 71),
        };

        _detailsTextBox.Dock = DockStyle.Fill;
        _detailsTextBox.Multiline = true;
        _detailsTextBox.ReadOnly = true;
        _detailsTextBox.ScrollBars = ScrollBars.Vertical;
        _detailsTextBox.BorderStyle = BorderStyle.None;
        _detailsTextBox.BackColor = Color.White;
        _detailsTextBox.Text =
            "Select a cleanup target to see what it removes, whether admin rights are likely needed, and any safety notes.";

        detailsSection.Controls.Add(detailsLabel, 0, 0);
        detailsSection.Controls.Add(CreateInsetHost(_detailsTextBox), 0, 1);
        layout.Controls.Add(detailsSection, 0, 7);
        panel.Controls.Add(layout);

        return panel;
    }

    private Control BuildLogPanel()
    {
        var panel = CreateCardPanel();

        var title = CreateSectionLabel("Activity Log");
        var subtitle = CreateMutedLabel("Each scan and cleanup action is recorded here, including any skipped locked files.");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = panel.BackColor,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.ReadOnly = true;
        _logTextBox.BackColor = Color.White;
        _logTextBox.BorderStyle = BorderStyle.None;
        _logTextBox.Font = new Font("Consolas", 8.75F);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        layout.Controls.Add(CreateInsetHost(_logTextBox), 0, 2);
        panel.Controls.Add(layout);

        return panel;
    }

    private static Panel CreateCardPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20),
        };
        AttachSubtleBorder(panel, Color.FromArgb(221, 229, 237));
        return panel;
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 48, 70),
        };
    }

    private static Label CreateMutedLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(88, 102, 119),
        };
    }

    private static Control CreateDriveStatCard(string caption, Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 8),
            Margin = new Padding(0, 0, 8, 8),
        };
        AttachSubtleBorder(card, Color.FromArgb(218, 227, 236));

        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 18,
            ForeColor = Color.FromArgb(102, 115, 131),
            Font = new Font("Segoe UI", 8.5F),
        };

        valueLabel.Text = "--";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.BottomLeft;
        valueLabel.ForeColor = Color.FromArgb(30, 48, 70);
        valueLabel.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);

        card.Controls.Add(valueLabel);
        card.Controls.Add(captionLabel);
        return card;
    }

    private static Control CreateInsetHost(Control content)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(1),
            Margin = new Padding(0),
        };
        AttachSubtleBorder(host, Color.FromArgb(218, 227, 236));
        content.Dock = DockStyle.Fill;
        host.Controls.Add(content);
        return host;
    }

    private static void AttachSubtleBorder(Control control, Color borderColor)
    {
        control.Paint += (_, e) =>
        {
            using var pen = new Pen(borderColor);
            var bounds = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
            e.Graphics.DrawRectangle(pen, bounds);
        };
    }

    private void ConfigureButton(Button button, string text, EventHandler onClick, ButtonTone tone = ButtonTone.Neutral)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 38;
        button.Margin = new Padding(0, 0, 8, 8);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = tone == ButtonTone.Neutral ? 1 : 0;
        button.FlatAppearance.BorderColor = Color.FromArgb(214, 224, 233);
        button.BackColor = tone switch
        {
            ButtonTone.Accent => Color.FromArgb(57, 107, 163),
            ButtonTone.Danger => Color.FromArgb(210, 82, 58),
            _ => Color.FromArgb(249, 251, 253),
        };
        button.ForeColor = tone == ButtonTone.Neutral ? Color.FromArgb(36, 52, 71) : Color.White;
        button.FlatAppearance.MouseOverBackColor = tone switch
        {
            ButtonTone.Accent => Color.FromArgb(48, 96, 149),
            ButtonTone.Danger => Color.FromArgb(192, 72, 48),
            _ => Color.FromArgb(242, 246, 250),
        };
        button.FlatAppearance.MouseDownBackColor = tone switch
        {
            ButtonTone.Accent => Color.FromArgb(42, 86, 136),
            ButtonTone.Danger => Color.FromArgb(176, 64, 42),
            _ => Color.FromArgb(234, 240, 246),
        };
        button.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        button.Click += onClick;
    }

    private void ConfigureInlineButton(Button button, string text, EventHandler onClick)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Size = new Size(80, 32);
        button.Margin = new Padding(0);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(214, 224, 233);
        button.BackColor = Color.White;
        button.ForeColor = Color.FromArgb(36, 52, 71);
        button.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
        button.Click += onClick;
    }

    private void PopulateTargetList()
    {
        _isPopulatingTargets = true;
        var selectedTargetId = _targetsListView.SelectedItems.Count > 0
            ? ((CleanupTargetState)_targetsListView.SelectedItems[0].Tag!).Definition.Id
            : null;

        _targetsListView.BeginUpdate();
        _targetsListView.Items.Clear();

        foreach (var state in GetVisibleStates())
        {
            _targetsListView.Items.Add(CreateListViewItem(state));
        }

        _targetsListView.EndUpdate();
        _isPopulatingTargets = false;
        UpdateTargetsSummary();
        UpdateSelectedEstimateSummary();

        if (selectedTargetId is not null)
        {
            foreach (ListViewItem item in _targetsListView.Items)
            {
                var state = (CleanupTargetState)item.Tag!;
                if (!string.Equals(state.Definition.Id, selectedTargetId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                item.Selected = true;
                item.EnsureVisible();
                RefreshDetailsPanel();
                return;
            }
        }

        if (_targetsListView.Items.Count > 0)
        {
            _targetsListView.Items[0].Selected = true;
        }
        else
        {
            RefreshDetailsPanel();
        }
    }

    private ListViewItem CreateListViewItem(CleanupTargetState state)
    {
        var item = new ListViewItem(state.Definition.Title)
        {
            Tag = state,
            Checked = state.IsSelected,
            ToolTipText = BuildTooltipText(state),
        };

        item.SubItems.Add(state.Definition.Category);
        item.SubItems.Add(state.Definition.RiskLevel);
        item.SubItems.Add(state.Definition.RequiresAdmin ? "Likely" : "No");
        item.SubItems.Add(FormatEstimatedValue(state));
        item.SubItems.Add(state.StatusText);
        ApplyItemVisuals(item, state);

        return item;
    }

    private void RefreshTargetItem(CleanupTargetState state)
    {
        if (ShouldRepopulateTargets())
        {
            PopulateTargetList();
            return;
        }

        foreach (ListViewItem item in _targetsListView.Items)
        {
            if (!ReferenceEquals(item.Tag, state))
            {
                continue;
            }

            item.SubItems[4].Text = FormatEstimatedValue(state);
            item.SubItems[5].Text = state.StatusText;
            item.ToolTipText = BuildTooltipText(state);
            ApplyItemVisuals(item, state);
            break;
        }

        RefreshDetailsPanel();
        UpdateSelectedEstimateSummary();
    }

    private void RefreshDetailsPanel()
    {
        if (_targetsListView.SelectedItems.Count == 0)
        {
            _detailsTextBox.Text = string.IsNullOrWhiteSpace(_searchTextBox.Text)
                ? "Select a cleanup target to see what it does."
                : "No cleanup targets match the current search.";
            return;
        }

        var state = (CleanupTargetState)_targetsListView.SelectedItems[0].Tag!;
        var definition = state.Definition;
        var candidatesText = state.Candidates.Count == 0
            ? "No per-location scan details are available yet."
            : string.Join(
                Environment.NewLine,
                state.Candidates
                    .Take(8)
                    .Select(candidate => $"- {candidate.Label}: {FormatBytes(candidate.Bytes)}"));
        if (state.Candidates.Count > 8)
        {
            candidatesText += $"{Environment.NewLine}- ...and {state.Candidates.Count - 8} more";
        }

        _detailsTextBox.Text =
            $"{definition.Description}{Environment.NewLine}{Environment.NewLine}" +
            $"Category: {definition.Category}{Environment.NewLine}" +
            $"Risk: {definition.RiskLevel}{Environment.NewLine}" +
            $"Needs admin: {(definition.RequiresAdmin ? "Likely" : "No")}{Environment.NewLine}" +
            $"Recommended: {(definition.Recommended ? "Yes" : "No")}{Environment.NewLine}" +
            $"Shown by default: {(state.IsRelevant ? "Yes" : "No, hidden unless you enable all targets")}{Environment.NewLine}" +
            $"Last estimate: {FormatEstimatedValue(state)}{Environment.NewLine}" +
            $"Status: {state.StatusText}{Environment.NewLine}{Environment.NewLine}" +
            $"{GetRiskBanner(definition)}{Environment.NewLine}{Environment.NewLine}" +
            $"Scan details:{Environment.NewLine}{candidatesText}{Environment.NewLine}{Environment.NewLine}" +
            $"Safety note:{Environment.NewLine}{definition.SafetyNote}{Environment.NewLine}{Environment.NewLine}" +
            $"Locations:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", definition.Paths)}";
    }

    private void SelectRecommendedTargets()
    {
        foreach (ListViewItem item in _targetsListView.Items)
        {
            var state = (CleanupTargetState)item.Tag!;
            var isChecked = state.Definition.Recommended;
            state.IsSelected = isChecked;
            item.Checked = isChecked;
        }

        UpdateSelectedEstimateSummary();
    }

    private void SetSelectionForAllTargets(bool isChecked)
    {
        foreach (ListViewItem item in _targetsListView.Items)
        {
            ((CleanupTargetState)item.Tag!).IsSelected = isChecked;
            item.Checked = isChecked;
        }

        UpdateSelectedEstimateSummary();
    }

    private List<CleanupTargetState> GetCheckedTargets()
    {
        return _states.Values
            .Where(state => _showIrrelevantCheckBox.Checked || state.IsRelevant)
            .Where(MatchesSearch)
            .Where(state => state.IsSelected)
            .ToList();
    }

    private IEnumerable<CleanupTargetState> GetVisibleStates()
    {
        var visibleStates = _states.Values
            .Where(state => _showIrrelevantCheckBox.Checked || state.IsRelevant)
            .Where(MatchesSearch);

        return GetSelectedSortMode() switch
        {
            TargetSortMode.BiggestFirst => visibleStates
                .OrderByDescending(state => state.EstimatedBytes)
                .ThenByDescending(state => state.Definition.Recommended)
                .ThenBy(state => state.Definition.Title, StringComparer.CurrentCultureIgnoreCase),
            TargetSortMode.SmallestFirst => visibleStates
                .OrderBy(state => state.EstimatedBytes)
                .ThenBy(state => state.Definition.Title, StringComparer.CurrentCultureIgnoreCase),
            TargetSortMode.Name => visibleStates
                .OrderBy(state => state.Definition.Title, StringComparer.CurrentCultureIgnoreCase),
            TargetSortMode.Risk => visibleStates
                .OrderByDescending(state => GetRiskPriority(state.Definition.RiskLevel))
                .ThenByDescending(state => state.EstimatedBytes)
                .ThenBy(state => state.Definition.Title, StringComparer.CurrentCultureIgnoreCase),
            _ => visibleStates
                .OrderByDescending(state => state.Definition.Recommended)
                .ThenByDescending(state => state.IsRelevant)
                .ThenByDescending(state => state.EstimatedBytes)
                .ThenBy(state => GetRiskPriority(state.Definition.RiskLevel))
                .ThenBy(state => state.Definition.Title, StringComparer.CurrentCultureIgnoreCase),
        };
    }

    private TargetSortMode GetSelectedSortMode() =>
        _sortComboBox.SelectedIndex switch
        {
            1 => TargetSortMode.BiggestFirst,
            2 => TargetSortMode.SmallestFirst,
            3 => TargetSortMode.Name,
            4 => TargetSortMode.Risk,
            _ => TargetSortMode.Recommended,
        };

    private bool MatchesSearch(CleanupTargetState state)
    {
        var query = _searchTextBox.Text;
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        foreach (var rawToken in TokenizeSearchQuery(query))
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                continue;
            }

            var isExcluded = rawToken.StartsWith('-');
            var token = isExcluded ? rawToken[1..] : rawToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            var matches = TokenMatches(state, token);
            if (!isExcluded && !matches)
            {
                return false;
            }

            if (isExcluded && matches)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> TokenizeSearchQuery(string query)
    {
        foreach (System.Text.RegularExpressions.Match match in
                 System.Text.RegularExpressions.Regex.Matches(query, "\"([^\"]+)\"|(\\S+)"))
        {
            if (match.Groups[1].Success)
            {
                yield return match.Groups[1].Value;
                continue;
            }

            if (match.Groups[2].Success)
            {
                yield return match.Groups[2].Value;
            }
        }
    }

    private static bool TokenMatches(CleanupTargetState state, string token)
    {
        var separatorIndex = token.IndexOf(':');
        if (separatorIndex > 0)
        {
            var field = token[..separatorIndex].Trim().ToLowerInvariant();
            var value = token[(separatorIndex + 1)..].Trim();
            return FieldMatches(state, field, value);
        }

        return SearchableText(state).Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FieldMatches(CleanupTargetState state, string field, string value)
    {
        var definition = state.Definition;
        return field switch
        {
            "title" or "name" => definition.Title.Contains(value, StringComparison.OrdinalIgnoreCase),
            "category" or "cat" => definition.Category.Contains(value, StringComparison.OrdinalIgnoreCase),
            "risk" => definition.RiskLevel.Contains(value, StringComparison.OrdinalIgnoreCase),
            "admin" => BooleanTermMatches(definition.RequiresAdmin, value),
            "recommended" or "rec" => BooleanTermMatches(definition.Recommended, value),
            "relevant" or "visible" => BooleanTermMatches(state.IsRelevant, value),
            "status" => state.StatusText.Contains(value, StringComparison.OrdinalIgnoreCase),
            "path" or "location" => definition.Paths.Any(path => path.Contains(value, StringComparison.OrdinalIgnoreCase)),
            "desc" or "description" => definition.Description.Contains(value, StringComparison.OrdinalIgnoreCase),
            "note" or "safety" => definition.SafetyNote.Contains(value, StringComparison.OrdinalIgnoreCase),
            _ => SearchableText(state).Contains(value, StringComparison.OrdinalIgnoreCase),
        };
    }

    private static bool BooleanTermMatches(bool actualValue, string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "yes" or "true" or "1" or "on" => actualValue,
            "no" or "false" or "0" or "off" => !actualValue,
            "likely" => actualValue,
            _ => false,
        };
    }

    private static string SearchableText(CleanupTargetState state)
    {
        var definition = state.Definition;
        return string.Join(
            ' ',
            definition.Title,
            definition.Category,
            definition.RiskLevel,
            definition.Description,
            definition.SafetyNote,
            state.StatusText,
            definition.RequiresAdmin ? "admin yes likely elevated" : "admin no",
            definition.Recommended ? "recommended yes" : "recommended no",
            state.IsRelevant ? "relevant yes visible yes" : "relevant no visible no",
            string.Join(' ', definition.Paths));
    }

    private void TargetsListViewOnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_isPopulatingTargets || e.Item.Tag is not CleanupTargetState state)
        {
            return;
        }

        state.IsSelected = e.Item.Checked;
        UpdateSelectedEstimateSummary();
    }

    private void UpdateTargetsSummary()
    {
        var visibleCount = _targetsListView.Items.Count;
        var totalCount = _states.Count;
        var hiddenCount = totalCount - visibleCount;
        var hasSearch = !string.IsNullOrWhiteSpace(_searchTextBox.Text);
        _targetsSummaryLabel.Text = hasSearch
            ? $"{visibleCount} match search"
            : hiddenCount > 0 && !_showIrrelevantCheckBox.Checked
                ? $"Showing {visibleCount} of {totalCount} targets"
                : $"{visibleCount} targets visible";
        PositionTargetsSummaryLabel();
    }

    private void UpdateSelectedEstimateSummary()
    {
        var selectedStates = _states.Values
            .Where(state => state.IsSelected)
            .Where(state => _showIrrelevantCheckBox.Checked || state.IsRelevant)
            .Where(MatchesSearch)
            .ToList();

        if (selectedStates.Count == 0)
        {
            _selectedEstimateLabel.Text = "Selected estimate: 0 B. Check one or more targets to estimate savings.";
            return;
        }

        var totalEstimatedBytes = selectedStates
            .Where(state => state.HasKnownEstimate)
            .Sum(state => state.EstimatedBytes);
        var analyzedCount = selectedStates.Count(state => state.LastUpdated.HasValue);
        var pendingCount = selectedStates.Count - analyzedCount;
        var unknownCount = selectedStates.Count(state => state.LastUpdated.HasValue && !state.HasKnownEstimate);
        _selectedEstimateLabel.Text =
            $"Selected estimate: {(unknownCount > 0 ? "at least " : string.Empty)}{FormatBytes(totalEstimatedBytes)} across {selectedStates.Count} target(s)." +
            (unknownCount > 0
                ? $" {unknownCount} selected target(s) use system-managed or informational estimates."
                : pendingCount > 0
                    ? $" Analyze {pendingCount} more selected target(s) for a fuller estimate."
                    : " All selected targets have been analyzed.");
    }

    private void PositionTargetsSummaryLabel()
    {
        // Layout is handled by the filter table in the modernized toolbar.
    }

    private bool ShouldRepopulateTargets() =>
        GetSelectedSortMode() is TargetSortMode.BiggestFirst or TargetSortMode.SmallestFirst or TargetSortMode.Risk;

    private string BuildTooltipText(CleanupTargetState state)
    {
        var definition = state.Definition;
        var relevanceText = state.IsRelevant
            ? "Shown by default"
            : "Hidden by default until 'Show irrelevant targets too' is enabled";

        return
            $"{definition.Title}\n" +
            $"Estimated reclaim: {FormatEstimatedValue(state)}\n" +
            $"Risk: {definition.RiskLevel} | Admin: {(definition.RequiresAdmin ? "Likely" : "No")}\n" +
            $"{relevanceText}\n" +
            $"{definition.Description}";
    }

    private void ApplyItemVisuals(ListViewItem item, CleanupTargetState state)
    {
        item.BackColor = state.Definition.RiskLevel switch
        {
            "High" => Color.FromArgb(255, 239, 236),
            "Medium" => Color.FromArgb(255, 248, 230),
            _ => Color.White,
        };

        item.ForeColor = state.IsRelevant
            ? Color.FromArgb(36, 52, 71)
            : Color.FromArgb(138, 149, 161);
    }

    private static int GetRiskPriority(string riskLevel) =>
        riskLevel switch
        {
            "High" => 3,
            "Medium" => 2,
            _ => 1,
        };

    private static string FormatEstimatedValue(CleanupTargetState state)
    {
        if (state.LastUpdated.HasValue && !state.HasKnownEstimate)
        {
            return "System-managed";
        }

        return FormatBytes(state.EstimatedBytes);
    }

    private static string GetRiskBanner(CleanupTargetDefinition definition) =>
        definition.Kind switch
        {
            CleanupTargetKind.WindowsComponentStore => "Warning: this runs DISM cleanup against the WinSxS component store. The aggressive toggle adds /ResetBase and permanently removes superseded component rollback baselines.",
            CleanupTargetKind.DownloadsFolder => "Warning: this removes personal downloaded files and folders, not just cache data.",
            CleanupTargetKind.VirtualMachineDisks => "Warning: this can permanently delete entire virtual machines or emulator images. Review the selected disk files carefully before cleaning.",
            CleanupTargetKind.WslVirtualDisks => "Informational target. Cleanup opens the VHDX locations for manual review rather than deleting disks automatically.",
            CleanupTargetKind.HibernationFile => "Warning: this disables Windows hibernation and Fast Startup by running 'powercfg /hibernate off'. Re-enable later with 'powercfg /hibernate on'.",
            CleanupTargetKind.SystemRestorePoints => "Warning: this removes all but the most recent System Restore shadow copy. You will lose the ability to roll back to older restore points.",
            _ => definition.RiskLevel switch
            {
                "High" => "Warning: high-risk cleanup target. Use this only if you understand the rollback, troubleshooting, or cache rebuild impact.",
                "Medium" => "Caution: medium-risk cleanup target. Review the safety note before cleaning.",
                _ => "Routine cleanup target. Locked files will still be skipped automatically.",
            },
        };

    private async Task AnalyzeSelectedAsync()
    {
        var targets = GetCheckedTargets();
        if (targets.Count == 0)
        {
            MessageBox.Show(this, "Select at least one cleanup target first.", "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await RunBusyAsync("Analyzing selected targets...", async () =>
        {
            for (var index = 0; index < targets.Count; index++)
            {
                var state = targets[index];
                using var statusTimer = new System.Windows.Forms.Timer
                {
                    Interval = 250,
                };
                var stopwatch = Stopwatch.StartNew();
                statusTimer.Tick += (_, _) =>
                {
                    var eta = EstimateScanTimeRemaining(targets, index, stopwatch.Elapsed);
                    _statusLabel.Text = BuildScanStatusText(index + 1, targets.Count, state.Definition.Title, eta);
                };
                _statusLabel.Text = BuildScanStatusText(index + 1, targets.Count, state.Definition.Title, EstimateScanTimeRemaining(targets, index, TimeSpan.Zero));
                statusTimer.Start();

                Log($"Analyzing {state.Definition.Title}...");
                var result = await _cleanupService.AnalyzeAsync(state.Definition.Id);
                stopwatch.Stop();
                statusTimer.Stop();
                RememberScanDuration(state.Definition.Id, stopwatch.Elapsed);
                state.ApplyAnalysis(result);
                RefreshTargetItem(state);
                Log($"{state.Definition.Title}: {result.Message}");
            }

            PopulateTargetList();
        });
    }

    private async Task CleanSelectedAsync()
    {
        var targets = GetCheckedTargets();
        if (targets.Count == 0)
        {
            MessageBox.Show(this, "Select at least one cleanup target first.", "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var highRiskTargets = targets
            .Where(state => string.Equals(state.Definition.RiskLevel, "High", StringComparison.OrdinalIgnoreCase))
            .Select(state => state.Definition.Title)
            .ToList();
        var totalEstimate = targets.Where(state => state.HasKnownEstimate).Sum(state => state.EstimatedBytes);
        var unknownEstimateTargets = targets.Count(state => state.LastUpdated.HasValue && !state.HasKnownEstimate);
        var confirmationMessage =
            $"Clean {targets.Count} visible selected target(s) on C:?{Environment.NewLine}{Environment.NewLine}" +
            $"Estimated reclaimable size: {(unknownEstimateTargets > 0 ? "at least " : string.Empty)}{FormatBytes(totalEstimate)}{Environment.NewLine}";

        if (unknownEstimateTargets > 0)
        {
            confirmationMessage += $"{unknownEstimateTargets} selected target(s) use system-managed or informational estimates.{Environment.NewLine}";
        }

        if (_useAggressiveCleanupCheckBox.Checked &&
            targets.Any(state => state.Definition.Kind == CleanupTargetKind.WindowsComponentStore))
        {
            confirmationMessage += $"Aggressive component cleanup is enabled and will add /ResetBase for Windows Component Store cleanup.{Environment.NewLine}";
        }

        if (highRiskTargets.Count > 0)
        {
            confirmationMessage +=
                $"{Environment.NewLine}High-risk targets selected:{Environment.NewLine}- " +
                string.Join($"{Environment.NewLine}- ", highRiskTargets) +
                $"{Environment.NewLine}{Environment.NewLine}These targets can remove rollback, troubleshooting, or advanced cache data. Continue only if you understand that impact.";
        }
        else
        {
            confirmationMessage +=
                $"{Environment.NewLine}This will remove cached and temporary files immediately. Locked items will be skipped.";
        }

        var confirmation = MessageBox.Show(
            this,
            confirmationMessage,
            highRiskTargets.Count > 0 ? "High-Risk Cleanup Confirmation" : "Confirm Cleanup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        if (!PromptForMathConfirmation())
        {
            Log("Cleanup canceled during math confirmation.");
            return;
        }

        await RunBusyAsync("Cleaning selected targets...", async () =>
        {
            foreach (var state in targets)
            {
                var executionOptions = await BuildExecutionOptionsAsync(state);
                if (executionOptions is null)
                {
                    Log($"{state.Definition.Title}: skipped.");
                    continue;
                }

                Log($"Cleaning {state.Definition.Title}...");
                var cleanupResult = await _cleanupService.CleanAsync(state.Definition.Id, executionOptions);
                state.ApplyCleanup(cleanupResult);
                RefreshTargetItem(state);
                Log($"{state.Definition.Title}: {cleanupResult.Message}");

                if (cleanupResult.Errors.Count > 0)
                {
                    foreach (var error in cleanupResult.Errors.Take(3))
                    {
                        Log($"  Skipped: {error}");
                    }

                    if (cleanupResult.Errors.Count > 3)
                    {
                        Log($"  ...and {cleanupResult.Errors.Count - 3} more skipped item(s).");
                    }
                }

                var rescanResult = await _cleanupService.AnalyzeAsync(state.Definition.Id);
                state.ApplyAnalysis(rescanResult);
                RefreshTargetItem(state);
            }

            RefreshDriveSummary(logUpdate: true);
            PopulateTargetList();
        });
    }

    private bool PromptForMathConfirmation()
    {
        var left = Random.Shared.Next(1, 10);
        var right = Random.Shared.Next(1, 10);
        var expectedAnswer = left + right;

        using var dialog = new Form
        {
            Text = "Math Confirmation",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(420, 180),
            BackColor = Color.White,
            Font = Font,
        };

        var instructionsLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 18),
            Size = new Size(384, 42),
            Text = "To continue with cleanup, solve this addition problem and press Enter or click Confirm.",
            ForeColor = Color.FromArgb(36, 52, 71),
        };

        var equationLabel = new Label
        {
            AutoSize = false,
            Location = new Point(18, 68),
            Size = new Size(384, 26),
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            Text = $"{left} + {right} = ?",
            ForeColor = Color.FromArgb(210, 82, 58),
        };

        var answerTextBox = new TextBox
        {
            Location = new Point(18, 102),
            Width = 120,
            TabIndex = 0,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            Location = new Point(236, 134),
            DialogResult = DialogResult.Cancel,
            TabIndex = 2,
        };

        var confirmButton = new Button
        {
            Text = "Confirm",
            AutoSize = true,
            Location = new Point(318, 134),
            DialogResult = DialogResult.None,
            TabIndex = 1,
        };

        void ConfirmMath()
        {
            if (!int.TryParse(answerTextBox.Text.Trim(), out var actualAnswer) || actualAnswer != expectedAnswer)
            {
                MessageBox.Show(
                    dialog,
                    "Incorrect answer. Cleanup was canceled.",
                    "Math Check Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                dialog.DialogResult = DialogResult.Cancel;
                dialog.Close();
                return;
            }

            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        }

        confirmButton.Click += (_, _) => ConfirmMath();
        answerTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            ConfirmMath();
        };

        dialog.AcceptButton = confirmButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.Add(instructionsLabel);
        dialog.Controls.Add(equationLabel);
        dialog.Controls.Add(answerTextBox);
        dialog.Controls.Add(cancelButton);
        dialog.Controls.Add(confirmButton);
        dialog.Shown += (_, _) => answerTextBox.Focus();

        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private async Task<CleanupExecutionOptions?> BuildExecutionOptionsAsync(CleanupTargetState state)
    {
        if (state.Definition.Kind is CleanupTargetKind.NodeModules or CleanupTargetKind.PythonVirtualEnvs or CleanupTargetKind.VirtualMachineDisks or CleanupTargetKind.RustTargetDirectories or CleanupTargetKind.DotNetBuildArtifacts)
        {
            if (!state.LastUpdated.HasValue || state.Candidates.Count == 0)
            {
                Log($"Analyzing {state.Definition.Title} to prepare project selections...");
                var analysisResult = await _cleanupService.AnalyzeAsync(state.Definition.Id);
                state.ApplyAnalysis(analysisResult);
                RefreshTargetItem(state);
            }

            if (state.Candidates.Count == 0)
            {
                MessageBox.Show(this, $"No {state.Definition.Title.ToLowerInvariant()} entries were found for cleanup.", "Nothing Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            var (dialogTitle, instructions) = GetCandidatePickerContent(state.Definition.Kind);
            var selectedPaths = PromptForCandidateSelection(
                dialogTitle,
                instructions,
                state.Candidates);

            return selectedPaths.Count == 0
                ? null
                : new CleanupExecutionOptions { SelectedPaths = selectedPaths };
        }

        return new CleanupExecutionOptions
        {
            UseAggressiveMode = state.Definition.Kind == CleanupTargetKind.WindowsComponentStore && _useAggressiveCleanupCheckBox.Checked,
        };
    }

    private static (string Title, string Instructions) GetCandidatePickerContent(CleanupTargetKind kind) =>
        kind switch
        {
            CleanupTargetKind.NodeModules => ("Select Node Modules Folders", "Choose which node_modules folders to remove."),
            CleanupTargetKind.PythonVirtualEnvs => ("Select Python Virtual Environments", "Choose which venv or .venv folders to remove."),
            CleanupTargetKind.VirtualMachineDisks => ("Select VM / Emulator Disk Files", "Choose which virtual machine or emulator disk files to remove. Review carefully before confirming."),
            CleanupTargetKind.RustTargetDirectories => ("Select Rust Target Directories", "Choose which Rust target build directories to remove. Projects will need to recompile."),
            CleanupTargetKind.DotNetBuildArtifacts => ("Select .NET Build Artifact Directories", "Choose which obj/bin build directories to remove. Projects will need to rebuild."),
            _ => ("Select Cleanup Candidates", "Choose which cleanup candidates to remove."),
        };

    private List<string> PromptForCandidateSelection(string title, string instructions, IReadOnlyList<CleanupCandidate> candidates)
    {
        using var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MinimumSize = new Size(760, 480),
            Size = new Size(860, 560),
            BackColor = Color.White,
            Font = Font,
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var instructionsLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = instructions,
            ForeColor = Color.FromArgb(88, 102, 119),
        };

        var checkedList = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true,
            HorizontalScrollbar = true,
        };

        foreach (var candidate in candidates)
        {
            checkedList.Items.Add($"{candidate.Label}  [{FormatBytes(candidate.Bytes)}]", isChecked: false);
        }

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
        };

        var confirmButton = new Button
        {
            Text = "Use Selected",
            AutoSize = true,
            DialogResult = DialogResult.OK,
        };

        var selectAllButton = new Button
        {
            Text = "Select All",
            AutoSize = true,
        };
        selectAllButton.Click += (_, _) =>
        {
            for (var i = 0; i < checkedList.Items.Count; i++)
            {
                checkedList.SetItemChecked(i, true);
            }
        };

        buttonsPanel.Controls.Add(cancelButton);
        buttonsPanel.Controls.Add(confirmButton);
        buttonsPanel.Controls.Add(selectAllButton);

        root.Controls.Add(instructionsLabel, 0, 0);
        root.Controls.Add(checkedList, 0, 1);
        root.Controls.Add(buttonsPanel, 0, 2);

        dialog.AcceptButton = confirmButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.Add(root);

        return dialog.ShowDialog(this) != DialogResult.OK
            ? []
            : checkedList.CheckedIndices
                .Cast<int>()
                .Select(index => candidates[index].Path)
                .ToList();
    }

    private TimeSpan EstimateScanTimeRemaining(IReadOnlyList<CleanupTargetState> targets, int currentIndex, TimeSpan currentElapsed)
    {
        var remaining = TimeSpan.Zero;

        for (var index = currentIndex; index < targets.Count; index++)
        {
            var expectedDuration = GetExpectedScanDuration(targets[index]);

            if (index == currentIndex)
            {
                var currentRemaining = expectedDuration - currentElapsed;
                if (currentRemaining > TimeSpan.Zero)
                {
                    remaining += currentRemaining;
                }

                continue;
            }

            remaining += expectedDuration;
        }

        return remaining;
    }

    private TimeSpan GetExpectedScanDuration(CleanupTargetState state)
    {
        if (_scanDurationHistory.TryGetValue(state.Definition.Id, out var historicalDuration))
        {
            return historicalDuration;
        }

        return state.Definition.Kind switch
        {
            CleanupTargetKind.NodeModules or CleanupTargetKind.PythonVirtualEnvs or CleanupTargetKind.VirtualMachineDisks or CleanupTargetKind.RustTargetDirectories or CleanupTargetKind.DotNetBuildArtifacts => TimeSpan.FromSeconds(18),
            CleanupTargetKind.DownloadsFolder or CleanupTargetKind.WindowsOldInstallation or CleanupTargetKind.MemoryDumps or CleanupTargetKind.OldLargeLogFiles => TimeSpan.FromSeconds(7),
            CleanupTargetKind.DockerData or CleanupTargetKind.WslVirtualDisks or CleanupTargetKind.WindowsComponentStore or CleanupTargetKind.SystemRestorePoints or CleanupTargetKind.WindowsEventLogs => TimeSpan.FromSeconds(4),
            CleanupTargetKind.WindowsTemp or CleanupTargetKind.UserTemp or CleanupTargetKind.WindowsUpdateDownloads => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromSeconds(2),
        };
    }

    private void RememberScanDuration(string targetId, TimeSpan duration)
    {
        if (_scanDurationHistory.TryGetValue(targetId, out var previousDuration))
        {
            _scanDurationHistory[targetId] = TimeSpan.FromMilliseconds(
                (previousDuration.TotalMilliseconds * 0.65) +
                (duration.TotalMilliseconds * 0.35));
            return;
        }

        _scanDurationHistory[targetId] = duration;
    }

    private static string BuildScanStatusText(int currentItem, int totalItems, string title, TimeSpan eta) =>
        $"Analyzing {currentItem}/{totalItems}: {title}  ETA {FormatDuration(eta)}";

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "under 1s";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes}m {duration.Seconds:D2}s";
        }

        return $"{Math.Max(1, (int)Math.Ceiling(duration.TotalSeconds))}s";
    }

    private async Task RefreshDriveSummaryAsync()
    {
        await RunBusyAsync("Refreshing C: drive summary...", () =>
        {
            RefreshDriveSummary(logUpdate: true);
            return Task.CompletedTask;
        }, suppressBusyLog: true);
    }

    private void RefreshDriveSummary(bool logUpdate)
    {
        var summary = _cleanupService.GetDriveSummary();
        UpdateDriveSummary(summary);

        if (logUpdate)
        {
            Log($"C: drive refreshed. Free space: {FormatBytes(summary.FreeBytes)} of {FormatBytes(summary.TotalBytes)}.");
        }
    }

    private void UpdateDriveSummary(DriveSummary summary)
    {
        var usedPercent = summary.TotalBytes == 0 ? 0 : (int)Math.Round(summary.UsedBytes * 100.0 / summary.TotalBytes);
        var freePercent = summary.TotalBytes == 0 ? 0 : (int)Math.Round(summary.FreeBytes * 100.0 / summary.TotalBytes);
        usedPercent = Math.Max(0, Math.Min(100, usedPercent));
        freePercent = Math.Max(0, Math.Min(100, freePercent));

        _driveHeadline.Text = $"C: drive usage: {usedPercent}% used";
        _driveSubheadline.Text =
            $"{FormatBytes(summary.FreeBytes)} free of {FormatBytes(summary.TotalBytes)} total. " +
            $"{FormatBytes(summary.UsedBytes)} currently in use.";
        _driveProgressBar.Value = usedPercent;
        _usedSpaceStatLabel.Text = FormatBytes(summary.UsedBytes);
        _freeSpaceStatLabel.Text = FormatBytes(summary.FreeBytes);
        _totalCapacityStatLabel.Text = FormatBytes(summary.TotalBytes);
        _freeShareStatLabel.Text = $"{freePercent}%";
    }

    private async Task RunBusyAsync(string statusText, Func<Task> action, bool suppressBusyLog = false)
    {
        SetBusy(true, statusText);

        if (!suppressBusyLog)
        {
            Log(statusText);
        }

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log($"Operation failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Operation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, "Ready");
        }
    }

    private void SetBusy(bool isBusy, string statusText)
    {
        UseWaitCursor = isBusy;
        _statusLabel.Text = statusText;
        _sortComboBox.Enabled = !isBusy;
        _searchTextBox.Enabled = !isBusy;
        _clearSearchButton.Enabled = !isBusy;
        _showIrrelevantCheckBox.Enabled = !isBusy;
        _useAggressiveCleanupCheckBox.Enabled = !isBusy;
        _targetsListView.Enabled = !isBusy;

        foreach (var button in GetManagedButtons())
        {
            button.Enabled = !isBusy;
        }
    }

    private IEnumerable<Button> GetManagedButtons()
    {
        yield return _selectRecommendedButton;
        yield return _selectAllButton;
        yield return _clearSelectionButton;
        yield return _analyzeButton;
        yield return _cleanButton;
        yield return _refreshButton;
        yield return _storageSettingsButton;
        yield return _diskCleanupButton;
        yield return _tempFolderButton;
        yield return _clearLogButton;
    }

    private void OpenShellTarget(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Unable to Open", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Log(string message)
    {
        _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _logTextBox.ScrollToCaret();
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
}
