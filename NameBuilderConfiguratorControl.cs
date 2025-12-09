using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using McTools.Xrm.Connection;

namespace NameBuilderConfigurator
{
    public partial class NameBuilderConfiguratorControl : PluginControlBase
    {
        private ComboBox solutionDropdown;
        private ComboBox entityDropdown;
        private ComboBox viewDropdown;
        private ComboBox sampleRecordDropdown;
        private ListBox attributeListBox;
        private FlowLayoutPanel fieldsPanel;
        private TextBox jsonOutputTextBox;
        private TextBox previewTextBox;
        private ToolStripButton copyJsonToolButton;
        private ToolStripButton exportJsonToolButton;
        private ToolStripButton importJsonToolButton;
        private ToolStripButton retrieveConfigToolButton;
        private ToolStripButton registerPluginToolButton;
        private ToolStripButton publishToolButton;
        private System.Windows.Forms.Label statusLabel;
        private NumericUpDown maxLengthNumeric;
        private CheckBox enableTracingCheckBox;
        private TextBox targetFieldTextBox;
        private Panel propertiesPanel;
        private System.Windows.Forms.Label propertiesTitleLabel;
        private FieldBlockControl selectedBlock = null;
        private FieldBlockControl entityHeaderBlock = null;
        private readonly ToolTip helpToolTip;
        private SplitContainer leftRightSplitter;
        private SplitContainer middleRightSplitter;
        private SplitContainer rightTopBottomSplitter;
        private readonly Dictionary<Guid, HashSet<Guid>> solutionEntityCache = new Dictionary<Guid, HashSet<Guid>>();
        private readonly HashSet<Guid> solutionEntityLoadsInProgress = new HashSet<Guid>();
        private List<SolutionItem> solutions = new List<SolutionItem>();
        private Guid? currentSolutionId;
        private bool solutionsLoading;
        private bool suppressSolutionSelectionChanged;
        private bool suppressEntitySelectionChanged;
        private bool suppressViewSelectionChanged;
        private const string DefaultSolutionUniqueName = "Default";
        
        private List<EntityMetadata> entities = new List<EntityMetadata>();
        private List<AttributeMetadata> currentAttributes = new List<AttributeMetadata>();
        private List<AttributeMetadata> allAttributes = new List<AttributeMetadata>();
        private List<FieldBlockControl> fieldBlocks = new List<FieldBlockControl>();
        private PluginConfiguration currentConfig = new PluginConfiguration();
        private Entity sampleRecord = null;
        private string currentEntityLogicalName = null;
        private string currentEntityDisplayName = null;
        private string currentPrimaryNameAttribute = null;
        private string committedConfigJson;
        private string committedEntityLogicalName;
        private EntityItem lastSelectedEntityItem;
        private PluginConfiguration pendingConfigFromPlugin;
        private string pendingConfigTargetEntity;
        private PluginStepInfo pendingConfigSourceStep;
        private PluginStepInfo activeRegistryStep;
        private PluginTypeInfo activePluginType;
        private string pendingAutoLoadEntity;
        private bool autoLoadInProgress;
        private readonly HashSet<string> autoLoadAttemptedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<PluginStepInfo> cachedPluginSteps = new List<PluginStepInfo>();
        private bool suppressBlockSelection;
        private readonly Dictionary<string, Dictionary<Guid, Entity>> sampleRecordCache = new Dictionary<string, Dictionary<Guid, Entity>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Guid> sdkMessageCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Guid> messageFilterCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, (string Primary, string Secondary)> messageFilterDetailsCache = new Dictionary<Guid, (string, string)>();
        private readonly Dictionary<Guid, string> currencySymbolCache = new Dictionary<Guid, string>();
        private readonly HashSet<string> publishStepResolutionWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ConnectionDetail lastConnectionDetail;
        private bool pluginPresenceCheckRunning;
        private bool pluginPresenceVerified;
        private bool pluginInstallRunning;
        private PluginPresenceCheckResult lastPluginCheckResult;
        private string cachedPluginAssemblyPath;
        private string cachedLocalPluginHash;
        private Action pendingPostInstallAction;
        private bool pendingEntityRefreshAfterSolutions;
        private static readonly Font SpacePreviewInputFont = new Font("Consolas", 9F);
        private static readonly Font SpacePreviewLabelFont = new Font("Consolas", 8F);
        private const string EmbeddedNameBuilderMonoline32Png = "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAAEEfUpiAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAAZdEVYdFNvZnR3YXJlAFBhaW50Lk5FVCA1LjEuMTGKCBbOAAAAuGVYSWZJSSoACAAAAAUAGgEFAAEAAABKAAAAGwEFAAEAAABSAAAAKAEDAAEAAAACAAAAMQECABEAAABaAAAAaYcEAAEAAABsAAAAAAAAAPJ2AQDoAwAA8nYBAOgDAABQYWludC5ORVQgNS4xLjExAAADAACQBwAEAAAAMDIzMAGgAwABAAAAAQAAAAWgBAABAAAAlgAAAAAAAAACAAEAAgAEAAAAUjk4AAIABwAEAAAAMDEwMAAAAABc7WH6CeiquwAABUxJREFUSEu1l29sk1Ubxn9DjUvriJHUBa3LjAV5y3CpCy4EwRpjYyYJ9kWStyzwfpl+WMhK/NI0mhANNgsSAiYNIQQ2CVlAkxltyFz6QnUZwjsWGN2HOf44ATMXtjDtUhqt8/LD+pT2rJsT9fepz7mv677POc95zumBHDabbdr6DfCkJAEsyjU8YEXKAKxoIa8WPSkHVo6pqSnKysrGAGhsbJQkORwOASsKnRW3bt2y3LOKADA9PS1JqqyszJgxZTIZ7d27V263W2NjY5qYmBDwZl4RCoUkSX6/X06nU5FIpGSZy0Cn2QjAlStXCjs4M6RC8lFJq1atyppxrBnOZShJYk5BLBaTJNXU1Gjt2rWSpMbGxrtCyylJo6OjkqShoaGiTP+fmJhQMpmUx+PR6tWrBTxWKDDpOXXqVGFiffzxCQGLTWEpGkZGRorMFjt37pw9wFJUVVVlTbNmZudFUzsnnZ2dpvlDU1OKr9PptOrr64vMktTf36+BgQE1NzcLuFLwxcwQjUYlSVNTU2pra5PH49GGDRuUSOTfr7Zt26br169LMz1qKkoAOGOxmOrq6hQIBJRMJvNGSWpubpYkeb1eVVVVLWgyNwBf/ScQ0PDwsFV13BTNx4mKiooscA34txmcj/KOjg7F4/GiIQAXTOFcfOdwOIrMkpTJZAQ8YopnEQ6HdfPmTdMvzfTidVM/i6VLl6ZNowXgNvWlWG0aNbNNLOjVAWC321NmAuBfpm4+3i80x+PxhVcHPgLGCxOk02kBj5tCk/pgMChJ6uvrK/RLklwul9rb21VbW3sHeM80v9zU1KRvhr+RJKVSKXm9Xrndbrndbu3atSufyOfzKRwOzxpSkyVIJpNqamrS8uXL1dramt8k72TuqLa2VpIUjUZnJXgtX6IEvb296ujoyD8DP5oJADItLS1yu906dOiQ0uni9RSPx3X79m1rP3/GNAN4VqxY8XN3d7ckKZvNKhqNyuVyKRQKyWazWXv9adNYiieA1kX3Lxpvb2+XJCUSCQGbTOGf4RHgU7/fr76+PqVSdxdrKpXSuXPntHnzZgFfAEtN81/lnUgkou7ublVWVuratWv54qXYs2ePgN1mknvlucCWgI4ePSproS+EQCAg4AUzmUnx0VKaxRUPVbB161b27dtnxubE6XQCPGy23yuff9XTYw5yTi5cuLDg1fpniForej6OHz+u3Ib2j/DZwMCAWTPP8PCwgP+ZpvlYyBoo5Pvx8bmP9snJSYAfzPZ75T7gLeCnrq4u+f1+7d+/3xz0LA4fPizgW+AEEAYagKeAB80CpSgD3nW73b8ODg4WJe7p6ZHdblckEtHg4KBSqZR+yWaVzWaVSqU0NDSk1tZW2Ww29ZRYsLt37xYg4BWzaCFvbN++XSdPnlRLS4vWrFkjl8ul9evXKxwOq6urS2NjY/mk2WxWmUxG1iXGJBaLqby8XG1tbZKkgwcPyvxTeH/hA8CyZctoaGigoaHBDOWZnJzk8uXLnD9/nt7eXi5dugSAx+PB6XRy7NgxgsEgoVCITObuxSqdTpObhTkpB05UV1crGAyqs7NTIyMjc46wFMFgUOb94sCBA9b0/9csWGY25Galr7+/31NXVwe5K+fFixdJJBKcPn2a0dFRqqur8Xq9rFu3jpUrV7JkyRIAduzYwaZNM4eez+fjyJEj2O12Nm7c+CXwEvBbYbFSHbBYBLiAZ4HngeccDsfTfr9/sc/no76+3tpuAbhx4wZnz57l6tWreDweampq2LJlC2fOnPkg9zVMF2X/m3g0d3F6GzgJjAKTwCe5zv8hvwOi70cqFJ6k9QAAAABJRU5ErkJggg==";
        private string FormatFieldLabel(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return "field";
            }

            var friendly = GetAttributeDisplayName(logicalName);
            if (string.IsNullOrWhiteSpace(friendly) || friendly.Equals(logicalName, StringComparison.OrdinalIgnoreCase))
            {
                return logicalName;
            }

            return $"{friendly} ({logicalName})";
        }

        private string GetAttributeDisplayName(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return null;
            }

            var meta = allAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase)) ??
                       currentAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

            return meta?.DisplayName?.UserLocalizedLabel?.Label ?? logicalName;
        }

        private string FormatOptionValue(string logicalName, string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return rawValue;
            }

            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
            {
                return rawValue;
            }

            var meta = allAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase)) ??
                       currentAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

            if (meta is EnumAttributeMetadata enumMeta)
            {
                var option = enumMeta.OptionSet?.Options?.FirstOrDefault(o => o.Value == numeric);
                var label = option?.Label?.UserLocalizedLabel?.Label;
                if (!string.IsNullOrWhiteSpace(label))
                {
                    return $"{label} ({numeric})";
                }
            }

            return rawValue;
        }

        public NameBuilderConfiguratorControl()
        {
            helpToolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 300,
                ReshowDelay = 100
            };
            helpToolTip.ShowAlways = true;

            InitializeComponent();
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (!ReferenceEquals(detail, lastConnectionDetail))
            {
                lastConnectionDetail = detail;
                MigrateLegacyPreferenceKey();
                pluginPresenceVerified = false;
                cachedPluginSteps.Clear();
                activePluginType = null;
                activeRegistryStep = null;
                lastPluginCheckResult = null;
                ResetConnectionScopedSelections();
            }

            UpdatePluginInstallButtonState();

            if (newService != null)
            {
                CheckUserPermissions();
                EnsureNameBuilderPluginPresence();
                EnsureSolutionsLoaded(ensureEntities: true);
            }
        }

        /// <summary>Gets a cached font instance to avoid creating multiple font objects.</summary>
        private Font GetCachedFont(string fontName, float fontSize, FontStyle style = FontStyle.Regular)
        {
            // Map to cached field based on parameters
            if (fontName.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase))
            {
                if (fontSize == 8F && style == FontStyle.Regular)
                    return _fontSegoeUI8 ?? (_fontSegoeUI8 = new Font("Segoe UI", 8F));
                if (fontSize == 8.5F && style == FontStyle.Regular)
                    return _fontSegoeUI85 ?? (_fontSegoeUI85 = new Font("Segoe UI", 8.5F));
                if (fontSize == 9F && style == FontStyle.Regular)
                    return _fontSegoeUI9 ?? (_fontSegoeUI9 = new Font("Segoe UI", 9F));
                if (fontSize == 10F && style == FontStyle.Regular)
                    return _fontSegoeUI10 ?? (_fontSegoeUI10 = new Font("Segoe UI", 10F));
                if (fontSize == 10F && style == FontStyle.Bold)
                    return _fontSegoeUI10Bold ?? (_fontSegoeUI10Bold = new Font("Segoe UI", 10F, FontStyle.Bold));
                if (fontSize == 10F && style == FontStyle.Italic)
                    return _fontSegoeUI10Italic ?? (_fontSegoeUI10Italic = new Font("Segoe UI", 10F, FontStyle.Italic));
                if (fontSize == 11F && style == FontStyle.Regular)
                    return _fontSegoeUI11 ?? (_fontSegoeUI11 = new Font("Segoe UI", 11F));
                if (fontSize == 11F && style == FontStyle.Bold)
                    return _fontSegoeUI11Bold ?? (_fontSegoeUI11Bold = new Font("Segoe UI", 11F, FontStyle.Bold));
                if (fontSize == 12F && style == FontStyle.Bold)
                    return _fontSegoeUI12Bold ?? (_fontSegoeUI12Bold = new Font("Segoe UI", 12F, FontStyle.Bold));
                if (fontSize == 7.5F && style == FontStyle.Regular)
                    return _fontSegoeUI75 ?? (_fontSegoeUI75 = new Font("Segoe UI", 7.5F));
            }
            else if (fontName.Equals("Consolas", StringComparison.OrdinalIgnoreCase))
            {
                if (fontSize == 9F && style == FontStyle.Regular)
                    return _fontConsolas9 ?? (_fontConsolas9 = new Font("Consolas", 9F));
            }

            // Fallback: create new instance if not cached (shouldn't happen in normal usage)
            return new Font(fontName, fontSize, style);
        }

        private void CheckUserPermissions()
        {
            if (Service == null)
            {
                return;
            }

            try
            {
                // Try to query pluginassembly to check if user has customizer permissions
                var testQuery = new QueryExpression("pluginassembly")
                {
                    ColumnSet = new ColumnSet("pluginassemblyid"),
                    TopCount = 1
                };

                Service.RetrieveMultiple(testQuery);
            }
            catch (Exception ex)
            {
                // Check if this is a permission-related error
                if (ex.Message.Contains("privilege") || ex.Message.Contains("permission") || 
                    ex.Message.Contains("Access Denied") || ex.Message.Contains("UNSPECIFIED") ||
                    ex is FaultException)
                {
                    DiagnosticLog.LogWarning("Check User Permissions", $"User lacks permissions: {ex.Message}");
                    statusLabel.Text = "Insufficient Permissions";
                    statusLabel.ForeColor = Color.Firebrick;
                    MessageBox.Show(
                        "You do not have the necessary permissions to use the NameBuilder Configurator.\n\n" +
                        "This tool requires Customizer or System Administrator permissions in this Dataverse environment.\n\n" +
                        "Please contact your system administrator to grant you the required permissions.",
                        "Insufficient Permissions",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    
                    // Disable all UI controls
                    solutionDropdown.Enabled = false;
                    entityDropdown.Enabled = false;
                    viewDropdown.Enabled = false;
                    sampleRecordDropdown.Enabled = false;
                    attributeListBox.Enabled = false;
                    copyJsonToolButton.Enabled = false;
                    exportJsonToolButton.Enabled = false;
                    importJsonToolButton.Enabled = false;
                    retrieveConfigToolButton.Enabled = false;
                    registerPluginToolButton.Enabled = false;
                    publishToolButton.Enabled = false;
                    targetFieldTextBox.Enabled = false;
                    enableTracingCheckBox.Enabled = false;
                    maxLengthNumeric.Enabled = false;
                }
            }
        }

        private void ResetConnectionScopedSelections()
        {
            solutions = new List<SolutionItem>();
            solutionEntityCache.Clear();
            solutionEntityLoadsInProgress.Clear();
            currentSolutionId = null;
            solutionsLoading = false;
            pendingEntityRefreshAfterSolutions = false;
            pendingAutoLoadEntity = null;
            autoLoadAttemptedEntities.Clear();
            autoLoadInProgress = false;
            committedConfigJson = null;
            committedEntityLogicalName = null;
            lastSelectedEntityItem = null;
            ClearSampleRecordCache();

            if (solutionDropdown != null)
            {
                suppressSolutionSelectionChanged = true;
                solutionDropdown.Items.Clear();
                solutionDropdown.Text = string.Empty;
                solutionDropdown.Enabled = false;
                suppressSolutionSelectionChanged = false;
            }

            if (entityDropdown != null)
            {
                suppressEntitySelectionChanged = true;
                entityDropdown.Items.Clear();
                entityDropdown.Text = string.Empty;
                entityDropdown.Enabled = false;
                suppressEntitySelectionChanged = false;
            }

            if (viewDropdown != null)
            {
                suppressViewSelectionChanged = true;
                viewDropdown.Items.Clear();
                viewDropdown.Text = string.Empty;
                viewDropdown.Enabled = false;
                suppressViewSelectionChanged = false;
            }

            sampleRecordDropdown?.Items.Clear();
            attributeListBox?.Items.Clear();
        }

        private void ClearSampleRecordCache()
        {
            sampleRecordCache.Clear();
        }

        private void EnsureSolutionsLoaded(bool ensureEntities = false)
        {
            if (Service == null || solutionDropdown == null)
            {
                return;
            }

            if (solutionsLoading)
            {
                if (ensureEntities)
                {
                    pendingEntityRefreshAfterSolutions = true;
                }
                return;
            }

            if (solutionDropdown.Items.Count == 0)
            {
                if (ensureEntities)
                {
                    pendingEntityRefreshAfterSolutions = true;
                }
                ExecuteMethod(LoadSolutions);
                return;
            }

            if (ensureEntities)
            {
                pendingEntityRefreshAfterSolutions = false;
                ExecuteMethod(LoadEntities);
            }
        }

        private void LoadSolutions()
        {
            if (Service == null)
            {
                return;
            }

            solutionsLoading = true;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading solutions...",
                Work = (worker, args) =>
                {
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename", "ismanaged", "isvisible"),
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);

                    args.Result = Service.RetrieveMultiple(query).Entities;
                },
                PostWorkCallBack = (args) =>
                {
                    solutionsLoading = false;

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Unable to load solutions: {args.Error.Message}", "Solution Load Failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var solutionEntities = (args.Result as IEnumerable<Entity>) ?? Enumerable.Empty<Entity>();
                    solutions = solutionEntities
                        .Select(e => new SolutionItem
                        {
                            SolutionId = e.Id,
                            FriendlyName = e.GetAttributeValue<string>("friendlyname") ?? e.GetAttributeValue<string>("uniquename") ?? e.Id.ToString(),
                            UniqueName = e.GetAttributeValue<string>("uniquename"),
                            IsManaged = e.GetAttributeValue<bool?>("ismanaged") ?? false
                        })
                        .ToList();

                    if (solutions.Count == 0)
                    {
                        solutions.Add(new SolutionItem
                        {
                            SolutionId = Guid.Empty,
                            FriendlyName = "(Default Solution)",
                            UniqueName = DefaultSolutionUniqueName,
                            IsManaged = false
                        });
                    }

                    // Sort with Default Solution first, then alphabetically
                    var defaultIndex = solutions.FindIndex(IsDefaultSolution);
                    if (defaultIndex >= 0)
                    {
                        var defaultSolution = solutions[defaultIndex];
                        solutions.RemoveAt(defaultIndex);
                        solutions.Insert(0, defaultSolution);
                    }
                    else
                    {
                        solutions.Sort((a, b) => a.FriendlyName.CompareTo(b.FriendlyName));
                    }

                    var defaultIndex2 = solutions.FindIndex(IsDefaultSolution);
                    if (defaultIndex2 > 0)
                    {
                        var defaults = solutions[defaultIndex2];
                        solutions.RemoveAt(defaultIndex);
                        solutions.Insert(0, defaults);
                    }

                    suppressSolutionSelectionChanged = true;
                    solutionDropdown.Items.Clear();
                    foreach (var solution in solutions)
                    {
                        solutionDropdown.Items.Add(solution);
                    }
                    solutionDropdown.Enabled = true;
                    suppressSolutionSelectionChanged = false;

                    if (solutionDropdown.Items.Count > 0)
                    {
                        RestoreSolutionSelectionFromPreferences();
                    }

                    if (pendingEntityRefreshAfterSolutions)
                    {
                        pendingEntityRefreshAfterSolutions = false;
                        ExecuteMethod(LoadEntities);
                    }
                }
            });
        }

        private void RestoreSolutionSelectionFromPreferences()
        {
            if (solutionDropdown == null || solutionDropdown.Items.Count == 0)
            {
                return;
            }

            var preference = GetConnectionPreference();
            SolutionItem target = null;

            if (preference?.LastSolutionId != null)
            {
                target = solutionDropdown.Items.Cast<SolutionItem>()
                    .FirstOrDefault(s => s.SolutionId == preference.LastSolutionId.Value);
            }

            if (target == null && !string.IsNullOrWhiteSpace(preference?.LastSolutionUniqueName))
            {
                target = solutionDropdown.Items.Cast<SolutionItem>()
                    .FirstOrDefault(s => string.Equals(s.UniqueName, preference.LastSolutionUniqueName, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = solutionDropdown.Items.Cast<SolutionItem>()
                    .FirstOrDefault(IsDefaultSolution) ?? solutionDropdown.Items.Cast<SolutionItem>().FirstOrDefault();
            }

            if (target == null)
            {
                return;
            }

            suppressSolutionSelectionChanged = true;
            solutionDropdown.SelectedItem = target;
            suppressSolutionSelectionChanged = false;

            HandleSolutionSelectionChanged();
        }

        private void SolutionDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressSolutionSelectionChanged)
            {
                return;
            }

            HandleSolutionSelectionChanged();
        }

        private void HandleSolutionSelectionChanged()
        {
            var selectedSolution = solutionDropdown?.SelectedItem as SolutionItem;
            currentSolutionId = selectedSolution?.SolutionId;

            if (selectedSolution != null)
            {
                PersistConnectionPreference(pref =>
                {
                    pref.LastSolutionId = selectedSolution.SolutionId;
                    pref.LastSolutionUniqueName = selectedSolution.UniqueName;
                });
            }

            if (selectedSolution == null)
            {
                ApplySolutionFilterIfReady();
                return;
            }

            if (IsDefaultSolution(selectedSolution))
            {
                ApplySolutionFilterIfReady();
                return;
            }

            if (solutionEntityCache.ContainsKey(selectedSolution.SolutionId))
            {
                ApplySolutionFilterIfReady();
                return;
            }

            if (!solutionEntityLoadsInProgress.Contains(selectedSolution.SolutionId))
            {
                LoadSolutionEntities(selectedSolution);
            }
        }

        private static bool IsDefaultSolution(SolutionItem item)
        {
            return item != null &&
                !string.IsNullOrWhiteSpace(item.UniqueName) &&
                item.UniqueName.Equals(DefaultSolutionUniqueName, StringComparison.OrdinalIgnoreCase);
        }

        private void LoadSolutionEntities(SolutionItem solution)
        {
            if (Service == null || solution == null)
            {
                return;
            }

            solutionEntityLoadsInProgress.Add(solution.SolutionId);

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Loading entities for {solution.FriendlyName}...",
                AsyncArgument = solution,
                Work = (worker, args) =>
                {
                    var selected = (SolutionItem)args.Argument;
                    var metadataIds = new HashSet<Guid>();
                    var query = new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet("objectid"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("solutionid", ConditionOperator.Equal, selected.SolutionId),
                                new ConditionExpression("componenttype", ConditionOperator.Equal, 1)
                            }
                        },
                        PageInfo = new PagingInfo { Count = 500, PageNumber = 1 }
                    };

                    EntityCollection page;
                    do
                    {
                        page = Service.RetrieveMultiple(query);
                        foreach (var component in page.Entities)
                        {
                            var objectId = component.GetAttributeValue<Guid?>("objectid");
                            if (objectId.HasValue)
                            {
                                metadataIds.Add(objectId.Value);
                            }
                        }

                        if (page.MoreRecords)
                        {
                            query.PageInfo.PageNumber++;
                            query.PageInfo.PagingCookie = page.PagingCookie;
                        }
                    }
                    while (page.MoreRecords);

                    args.Result = new SolutionComponentLoadResult
                    {
                        SolutionId = selected.SolutionId,
                        EntityMetadataIds = metadataIds
                    };
                },
                PostWorkCallBack = (args) =>
                {
                    var solutionId = solution.SolutionId;
                    solutionEntityLoadsInProgress.Remove(solutionId);

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Unable to load entities for {solution.FriendlyName}: {args.Error.Message}",
                            "Solution Filter Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        statusLabel.Text = $"Failed to load entities for {solution.FriendlyName}";
                        statusLabel.ForeColor = Color.Firebrick;
                        return;
                    }

                    if (args.Result is SolutionComponentLoadResult result)
                    {
                        solutionEntityCache[result.SolutionId] = result.EntityMetadataIds;
                    }

                    ApplySolutionFilterIfReady();
                }
            });
        }

        private void ApplySolutionFilterIfReady()
        {
            if (entityDropdown == null)
            {
                return;
            }

            if (entities == null || entities.Count == 0)
            {
                suppressEntitySelectionChanged = true;
                entityDropdown.Items.Clear();
                suppressEntitySelectionChanged = false;
                entityDropdown.Enabled = false;
                return;
            }

            var selectedSolution = solutionDropdown?.SelectedItem as SolutionItem;
            IEnumerable<EntityMetadata> source = entities;

            if (selectedSolution != null && !IsDefaultSolution(selectedSolution))
            {
                if (!solutionEntityCache.TryGetValue(selectedSolution.SolutionId, out var ids))
                {
                    suppressEntitySelectionChanged = true;
                    entityDropdown.Items.Clear();
                    suppressEntitySelectionChanged = false;
                    entityDropdown.Enabled = false;
                    statusLabel.Text = $"Loading entities for {selectedSolution.FriendlyName}...";
                    statusLabel.ForeColor = Color.DarkGoldenrod;
                    return;
                }

                source = entities
                    .Where(e => e.MetadataId.HasValue && ids.Contains(e.MetadataId.Value))
                    .ToList();
            }

            PopulateEntityDropdown(source);
        }

        private void PopulateEntityDropdown(IEnumerable<EntityMetadata> source)
        {
            suppressEntitySelectionChanged = true;
            entityDropdown.Items.Clear();
            suppressEntitySelectionChanged = false;

            var entityItems = source?
                .Select(entity => new EntityItem
                {
                    DisplayName = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName,
                    LogicalName = entity.LogicalName,
                    Metadata = entity
                })
                .ToList() ?? new List<EntityItem>();

            foreach (var item in entityItems)
            {
                entityDropdown.Items.Add(item);
            }

            entityDropdown.Enabled = entityItems.Count > 0;

            if (entityItems.Count == 0)
            {
                statusLabel.Text = "No entities match this solution filter.";
                statusLabel.ForeColor = Color.Firebrick;
                return;
            }

            var solution = solutionDropdown?.SelectedItem as SolutionItem;
            if (solution == null || IsDefaultSolution(solution))
            {
                statusLabel.Text = $"Loaded {entityItems.Count} entities.";
            }
            else
            {
                statusLabel.Text = $"Filtered to {entityItems.Count} entities for {solution.FriendlyName}.";
            }
            statusLabel.ForeColor = Color.ForestGreen;

            RestoreEntitySelectionFromPreferences();
        }

        private void RestoreEntitySelectionFromPreferences()
        {
            if (entityDropdown.Items.Count == 0)
            {
                return;
            }

            var preference = GetConnectionPreference();
            EntityItem target = null;

            if (!string.IsNullOrWhiteSpace(preference?.LastEntityLogicalName))
            {
                target = entityDropdown.Items.Cast<EntityItem>()
                    .FirstOrDefault(i => i.LogicalName.Equals(preference.LastEntityLogicalName, StringComparison.OrdinalIgnoreCase));
            }

            if (target == null)
            {
                target = entityDropdown.Items[0] as EntityItem;
            }

            if (target == null)
            {
                return;
            }

            suppressEntitySelectionChanged = true;
            entityDropdown.SelectedItem = target;
            suppressEntitySelectionChanged = false;

            HandleEntitySelectionChanged();
        }

        private void RestoreViewSelectionFromPreferences()
        {
            if (viewDropdown.Items.Count == 0)
            {
                return;
            }

            var preference = GetConnectionPreference();
            ViewItem target = null;

            if (preference?.LastViewId != null)
            {
                target = viewDropdown.Items.Cast<ViewItem>()
                    .FirstOrDefault(v => v.ViewId == preference.LastViewId.Value && !v.IsSeparator);
            }

            if (target == null)
            {
                target = viewDropdown.Items.Cast<ViewItem>().FirstOrDefault(v => !v.IsSeparator);
            }

            if (target == null)
            {
                return;
            }

            suppressViewSelectionChanged = true;
            viewDropdown.SelectedItem = target;
            suppressViewSelectionChanged = false;

            HandleViewSelectionChanged();
        }

        private ConnectionPreference GetConnectionPreference()
        {
            var settings = PluginUserSettings.Load();
            var candidates = GetPreferenceKeyCandidates();

            string foundKey = null;
            ConnectionPreference found = null;

            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                var pref = settings.GetConnectionPreference(candidate);
                if (pref != null)
                {
                    found = pref;
                    foundKey = candidate;
                    break;
                }
            }

            var primaryKey = GetConnectionPreferenceKey();
            if (found != null && !string.IsNullOrWhiteSpace(primaryKey) && !string.Equals(foundKey, primaryKey, StringComparison.Ordinal))
            {
                // Migrate to the primary key for future lookups
                settings.ConnectionPreferences[primaryKey] = found;
                settings.ConnectionPreferences.Remove(foundKey);
                settings.Save();
            }

            return found;
        }

        private void PersistConnectionPreference(Action<ConnectionPreference> apply)
        {
            if (apply == null)
            {
                return;
            }

            var primaryKey = GetConnectionPreferenceKey();
            if (string.IsNullOrWhiteSpace(primaryKey))
            {
                return;
            }

            var settings = PluginUserSettings.Load();
            var preference = settings.GetOrCreateConnectionPreference(primaryKey);
            apply(preference);
            settings.Save();
        }

        private string GetConnectionPreferenceKey()
        {
            if (lastConnectionDetail == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(lastConnectionDetail.ConnectionName))
            {
                return lastConnectionDetail.ConnectionName;
            }

            // Fall back to stable environment identifiers so preferences persist across reconnects
            var candidates = new[]
            {
                lastConnectionDetail.WebApplicationUrl,
                lastConnectionDetail.OrganizationFriendlyName,
                lastConnectionDetail.OrganizationServiceUrl
            };

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }

            return lastConnectionDetail.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        private IEnumerable<string> GetPreferenceKeyCandidates()
        {
            if (lastConnectionDetail == null)
            {
                yield break;
            }

            var primary = GetConnectionPreferenceKey();
            if (!string.IsNullOrWhiteSpace(primary))
            {
                yield return primary;
            }

            // Additional fallbacks for environments saved under different identifiers
            if (!string.IsNullOrWhiteSpace(lastConnectionDetail.ConnectionName))
            {
                yield return lastConnectionDetail.ConnectionName;
            }

            if (!string.IsNullOrWhiteSpace(lastConnectionDetail.WebApplicationUrl))
            {
                yield return lastConnectionDetail.WebApplicationUrl;
            }

            if (!string.IsNullOrWhiteSpace(lastConnectionDetail.OrganizationFriendlyName))
            {
                yield return lastConnectionDetail.OrganizationFriendlyName;
            }

            if (!string.IsNullOrWhiteSpace(lastConnectionDetail.OrganizationServiceUrl))
            {
                yield return lastConnectionDetail.OrganizationServiceUrl;
            }

            // Legacy hash key
            yield return lastConnectionDetail.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }

        private void MigrateLegacyPreferenceKey()
        {
            if (lastConnectionDetail == null)
            {
                return;
            }

            var newKey = GetConnectionPreferenceKey();
            if (string.IsNullOrWhiteSpace(newKey))
            {
                return;
            }

            // Legacy hash-based key
            var oldKey = lastConnectionDetail.GetHashCode().ToString(CultureInfo.InvariantCulture);

            var settings = PluginUserSettings.Load();
            var oldPref = settings.GetConnectionPreference(oldKey);
            if (oldPref == null || string.Equals(oldKey, newKey, StringComparison.Ordinal))
            {
                return;
            }

            var newPref = settings.GetOrCreateConnectionPreference(newKey);
            // Only overwrite when the new key is empty to avoid clobbering real data
            if (newPref.LastSolutionId == null &&
                string.IsNullOrWhiteSpace(newPref.LastSolutionUniqueName) &&
                string.IsNullOrWhiteSpace(newPref.LastEntityLogicalName) &&
                newPref.LastViewId == null &&
                newPref.PluginSolutionId == null &&
                string.IsNullOrWhiteSpace(newPref.PluginSolutionUniqueName))
            {
                newPref.LastSolutionId = oldPref.LastSolutionId;
                newPref.LastSolutionUniqueName = oldPref.LastSolutionUniqueName;
                newPref.LastEntityLogicalName = oldPref.LastEntityLogicalName;
                newPref.LastViewId = oldPref.LastViewId;
                newPref.PluginSolutionId = oldPref.PluginSolutionId;
                newPref.PluginSolutionUniqueName = oldPref.PluginSolutionUniqueName;
                settings.ConnectionPreferences[newKey] = newPref;
            }

            // Remove the old key to avoid stale entries
            settings.ConnectionPreferences.Remove(oldKey);
            settings.Save();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Create main layout with ribbon and content
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                Padding = new Padding(0)
            };
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // Ribbon
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Content
            
            // RIBBON
            var ribbon = new ToolStrip
            {
                Dock = DockStyle.Fill,
                GripStyle = ToolStripGripStyle.Hidden,
                Padding = new Padding(5, 0, 5, 0),
                ImageScalingSize = new Size(18, 18)
            };
            
            var loadEntitiesToolButton = new ToolStripButton
            {
                Text = "Load Metadata",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("LoadEntities.png", SystemIcons.Application)
            };
            loadEntitiesToolButton.Click += LoadEntitiesButton_Click;
            loadEntitiesToolButton.ToolTipText = "Reload entity metadata and refresh the Available Attributes list.";
            ribbon.Items.Add(loadEntitiesToolButton);
            
            retrieveConfigToolButton = new ToolStripButton
            {
                Text = "Retrieve Configured Entity",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("RetrieveConfiguration.png", SystemIcons.Information)
            };
            retrieveConfigToolButton.Click += RetrieveConfigurationToolButton_Click;
            retrieveConfigToolButton.ToolTipText = "Pull an existing NameBuilder plug-in step configuration from Dataverse.";
            ribbon.Items.Add(retrieveConfigToolButton);

            registerPluginToolButton = new ToolStripButton
            {
                Text = "Register Plug-in",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("NameBuilder_Monoline_32x32.png", SystemIcons.Shield)
            };
            registerPluginToolButton.Click += RegisterPluginToolButton_Click;
            registerPluginToolButton.ToolTipText = "Verify or deploy the NameBuilder plug-in assembly into the connected environment.";
            ribbon.Items.Add(registerPluginToolButton);

            ribbon.Items.Add(new ToolStripSeparator());
            
            importJsonToolButton = new ToolStripButton
            {
                Text = "Import file",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("ImportJSON.png", SystemIcons.Shield)
            };
            importJsonToolButton.Click += ImportJsonToolButton_Click;
            importJsonToolButton.ToolTipText = "Load a NameBuilder JSON file from disk and rebuild the designer.";
            ribbon.Items.Add(importJsonToolButton);

            exportJsonToolButton = new ToolStripButton
            {
                Text = "Export file",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("ExportJSON.png", SystemIcons.Asterisk)
            };
            exportJsonToolButton.Click += ExportJsonButton_Click;
            exportJsonToolButton.ToolTipText = "Save the current NameBuilder payload to a JSON file.";
            ribbon.Items.Add(exportJsonToolButton);

            copyJsonToolButton = new ToolStripButton
            {
                Text = "Copy to clipboard",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("CopyJSON.png", SystemIcons.Question)
            };
            copyJsonToolButton.Click += CopyJsonButton_Click;
            copyJsonToolButton.ToolTipText = "Copy the generated JSON to the clipboard.";
            ribbon.Items.Add(copyJsonToolButton);

            ribbon.Items.Add(new ToolStripSeparator());

            publishToolButton = new ToolStripButton
            {
                Text = "Publish Configuration",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("PublishConfiguration.png", SystemIcons.Warning)
            };
            publishToolButton.Click += PublishToolButton_Click;
            publishToolButton.ToolTipText = "Push the JSON back to the selected NameBuilder steps (Create/Update).";
            ribbon.Items.Add(publishToolButton);
            
            // Store references for enabling/disabling
            SetActiveRegistryStep(null);
            
            // Initialize global config fields
            targetFieldTextBox = new TextBox { Text = "name" };
            maxLengthNumeric = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0 };
            enableTracingCheckBox = new CheckBox();
            
            mainContainer.Controls.Add(ribbon, 0, 0);
            
            // CONTENT AREA - Split containers for resizable IDE-style layout
            // Left panel goes to top; preview spans only middle+right via a nested top panel on the right side
            // Load settings early to calculate proper initial distances
            var initialSettings = PluginUserSettings.Load();
            var estimatedWidth = 1400; // Typical initial width
            var leftInitial = Math.Max(250, (int)(estimatedWidth * initialSettings.LeftPanelProportion));
            var rightRemaining = estimatedWidth - leftInitial;
            var rightPanelInitial = Math.Max(300, (int)(estimatedWidth * initialSettings.RightPanelProportion));
            var middleInitial = Math.Max(200, rightRemaining - rightPanelInitial);
            
            leftRightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = leftInitial
            };

            // Right side is split vertically: top preview and bottom content
            rightTopBottomSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = Math.Max(30, Math.Min(initialSettings.PreviewHeight, 100))
            };

            // Bottom of right side: split between middle and right panels
            middleRightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = middleInitial
            };
            
            // LEFT PANEL - Solution/Entity/View/Sample/Attributes
            var leftPanel = new Panel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(6)
            };

            // Solution
            var solutionLabel = new System.Windows.Forms.Label
            {
                Text = "Solution:",
                Location = new Point(5, 5),
                AutoSize = true
            };
            leftPanel.Controls.Add(solutionLabel);

            solutionDropdown = new ComboBox
            {
                Location = new Point(5, 25),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            solutionDropdown.SelectedIndexChanged += SolutionDropdown_SelectedIndexChanged;
            helpToolTip.SetToolTip(solutionDropdown, "Filter entities by Dataverse solution. Display names are shown while the solutionId is stored.");
            leftPanel.Controls.Add(solutionDropdown);
            
            // Entity
            var entityLabel = new System.Windows.Forms.Label {
                Text = "Entity:",
                Location = new Point(5, 65),
                AutoSize = true
            };
            leftPanel.Controls.Add(entityLabel);
            
            entityDropdown = new ComboBox
            {
                Location = new Point(5, 85),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            entityDropdown.SelectedIndexChanged += EntityDropdown_SelectedIndexChanged;
            helpToolTip.SetToolTip(entityDropdown, "Choose the Dataverse entity whose NameBuilder pattern you want to edit.");
            leftPanel.Controls.Add(entityDropdown);
            
            // View
            var viewLabel = new System.Windows.Forms.Label {
                Text = "View (optional):",
                Location = new Point(5, 120),
                AutoSize = true
            };
            leftPanel.Controls.Add(viewLabel);
            
            viewDropdown = new ComboBox
            {
                Location = new Point(5, 140),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            viewDropdown.SelectedIndexChanged += ViewDropdown_SelectedIndexChanged;
            helpToolTip.SetToolTip(viewDropdown, "Optional system/personal view used to scope sample records.");
            leftPanel.Controls.Add(viewDropdown);
            
            // Sample Record
            var sampleLabel = new System.Windows.Forms.Label {
                Text = "Sample Record:",
                Location = new Point(5, 175),
                AutoSize = true
            };
            leftPanel.Controls.Add(sampleLabel);
            
            sampleRecordDropdown = new ComboBox
            {
                Location = new Point(5, 195),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            sampleRecordDropdown.SelectedIndexChanged += SampleRecordDropdown_SelectedIndexChanged;
            helpToolTip.SetToolTip(sampleRecordDropdown, "Pick a row to feed the live preview (records come from the selected view).");
            leftPanel.Controls.Add(sampleRecordDropdown);
            
            // Attributes
            var attributeLabel = new System.Windows.Forms.Label {
                Text = "Available Attributes:(double-click to add)",
                Location = new Point(5, 230),
                AutoSize = true
            };
            leftPanel.Controls.Add(attributeLabel);
            
            // Attributes listbox - scales between label above and status below
            attributeListBox = new ListBox
            {
                Location = new Point(5, 250),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 200,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            attributeListBox.DoubleClick += AttributeListBox_DoubleClick;
            helpToolTip.SetToolTip(attributeListBox, "Double-click an attribute to append it as a field block; logical name is shown in parentheses.");
            leftPanel.Controls.Add(attributeListBox);
            
            // Status label pinned to bottom (60px from bottom to leave room for button)
            statusLabel = new System.Windows.Forms.Label {
                Text = "Not connected",
                Location = new Point(5, leftPanel.ClientSize.Height - 60),
                AutoSize = false,
                Size = new Size(leftPanel.ClientSize.Width - 16, 20),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            helpToolTip.SetToolTip(statusLabel, "Shows progress for metadata loads, publishes, or plugin checks.");
            leftPanel.Controls.Add(statusLabel);
            
            leftPanel.Resize += (s, e) =>
            {
                solutionDropdown.Width = leftPanel.ClientSize.Width - 16;
                entityDropdown.Width = leftPanel.ClientSize.Width - 16;
                viewDropdown.Width = leftPanel.ClientSize.Width - 16;
                sampleRecordDropdown.Width = leftPanel.ClientSize.Width - 16;
                attributeListBox.Width = leftPanel.ClientSize.Width - 16;
                statusLabel.Width = leftPanel.ClientSize.Width - 16;
                
                // Calculate positions based on actual panel height
                statusLabel.Top = leftPanel.ClientSize.Height - 33;
                
                // Scale listbox height: from current position to above Status label (with 10px gap)
                var bottomEdge = statusLabel.Top - 10;
                attributeListBox.Height = Math.Max(100, bottomEdge - attributeListBox.Top);
            };

            leftRightSplitter.Panel1.Controls.Add(leftPanel);
            
            // MIDDLE PANEL - Field Blocks and JSON
            var middlePanel = new Panel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(6)
            };
            
            var fieldsLabel = new System.Windows.Forms.Label {
                Text = "Field Blocks:",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            middlePanel.Controls.Add(fieldsLabel);
            
            fieldsPanel = new FlowLayoutPanel
            {
                Location = new Point(5, 30),
                Size = new Size(400, 340),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                Margin = new Padding(0),
                Padding = new Padding(2)
            };
            fieldsPanel.Resize += (s, e) => {
                foreach (Control c in fieldsPanel.Controls)
                {
                    if (c is FieldBlockControl block)
                    {
                        block.Width = fieldsPanel.ClientSize.Width - 6;
                    }
                }
            };
            
            middlePanel.Controls.Add(fieldsPanel);
            
            // Consolidated resize handler after all controls are created
            middlePanel.Resize += (s, e) => {
                var horizontalPadding = 10;
                var verticalPadding = 10;
                var newWidth = middlePanel.ClientSize.Width - fieldsPanel.Left - horizontalPadding;
                fieldsPanel.Width = Math.Max(100, newWidth);

                var availableHeight = middlePanel.ClientSize.Height - fieldsPanel.Top - verticalPadding;
                fieldsPanel.Height = Math.Max(100, availableHeight);
                // Update all block widths when middle panel resizes
                foreach (Control c in fieldsPanel.Controls)
                {
                    if (c is FieldBlockControl block)
                    {
                        block.Width = fieldsPanel.ClientSize.Width - 6;
                    }
                }
            };
            
            middleRightSplitter.Panel1.Controls.Add(middlePanel);
            
            // RIGHT PANEL - Tabbed interface with Properties and JSON tabs
            var rightPanel = new Panel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(6)
            };
            
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            
            // Properties Tab
            var propertiesTab = new TabPage("Properties");
            
            propertiesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                Padding = new Padding(8),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            
            propertiesTitleLabel = new System.Windows.Forms.Label {
                Text = "Properties",
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = true
            };
            propertiesPanel.Controls.Add(propertiesTitleLabel);
            
            propertiesTab.Controls.Add(propertiesPanel);
            tabControl.TabPages.Add(propertiesTab);
            
            // JSON Tab
            var jsonTab = new TabPage("JSON");
            
            var jsonTabPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            
            jsonOutputTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9F)
            };
            helpToolTip.SetToolTip(jsonOutputTextBox, "Read-only view of the generated JSON payload (copy or export via the ribbon).");
            jsonTabPanel.Controls.Add(jsonOutputTextBox);
            
            jsonTab.Controls.Add(jsonTabPanel);
            tabControl.TabPages.Add(jsonTab);
            
            rightPanel.Controls.Add(tabControl);
            
            ShowGlobalProperties();

            middleRightSplitter.Panel2.Controls.Add(rightPanel);

            // Preview spanning middle+right, but only at the top of the right side
            var previewPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var previewLabel = new System.Windows.Forms.Label {
                Text = "Live Preview:",
                Location = new Point(5, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            previewPanel.Controls.Add(previewLabel);
            previewTextBox = new TextBox
            {
                Location = new Point(5, 30),
                Width = previewPanel.ClientSize.Width - 16,
                Height = 28,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.LightYellow,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WordWrap = true
            };
            previewPanel.Resize += (s, e) => previewTextBox.Width = previewPanel.ClientSize.Width - 16;
            helpToolTip.SetToolTip(previewTextBox, "Live example of the assembled name for the selected sample record.");
            previewPanel.Controls.Add(previewTextBox);

            rightTopBottomSplitter.Panel1.Controls.Add(previewPanel);
            rightTopBottomSplitter.Panel2.Controls.Add(middleRightSplitter);

            leftRightSplitter.Panel2.Controls.Add(rightTopBottomSplitter);

            // Add splitters to main container
            mainContainer.Controls.Add(leftRightSplitter, 0, 1);
            
            // Add main container to control
            this.Controls.Add(mainContainer);
            
            this.Name = "NameBuilderConfiguratorControl";
            this.MinimumSize = new Size(1000, 600);

            // Wire up splitter persistence handlers
            bool isRestoringLayout = true; // Start as true to block initial layout changes
            bool allowPersistence = false;
            
            leftRightSplitter.SplitterMoved += (s, e) => {
                if (isRestoringLayout || !allowPersistence) return;
                var st = PluginUserSettings.Load();
                var total = this.ClientSize.Width;
                if (total > 0) st.LeftPanelProportion = (double)leftRightSplitter.SplitterDistance / total;
                st.Save();
            };
            middleRightSplitter.SplitterMoved += (s, e) => {
                if (isRestoringLayout || !allowPersistence) return;
                var st = PluginUserSettings.Load();
                var total = this.ClientSize.Width;
                if (total > 0)
                {
                    var rightPanelActualWidth = Math.Max(0, (total - leftRightSplitter.SplitterDistance) - middleRightSplitter.SplitterDistance);
                    st.RightPanelProportion = total > 0 ? (double)rightPanelActualWidth / total : st.RightPanelProportion;
                }
                st.Save();
            };
            rightTopBottomSplitter.SplitterMoved += (s, e) =>
            {
                if (isRestoringLayout || !allowPersistence) return;
                var st = PluginUserSettings.Load();
                st.PreviewHeight = Math.Max(20, Math.Min(rightTopBottomSplitter.SplitterDistance, 100));
                st.Save();
            };

            // Restore saved splitter positions after control is properly sized
            bool initialLayoutApplied = false;
            EventHandler applySavedLayout = null;
            applySavedLayout = (s, e) =>
            {
                if (initialLayoutApplied) return;
                
                var totalWidth = this.ClientSize.Width;
                if (totalWidth < 800) return; // Wait for proper sizing
                
                initialLayoutApplied = true;
                isRestoringLayout = true;
                
                try
                {
                    var settings = PluginUserSettings.Load();
                    
                    // Default proportions: Left 22%, Middle 48%, Right 30%
                    var leftProportion = settings.LeftPanelProportion;
                    var rightProportion = settings.RightPanelProportion;
                    
                    // Calculate left panel width (ensure minimum 250px, maximum 50%)
                    var leftWidth = (int)(totalWidth * leftProportion);
                    leftRightSplitter.SplitterDistance = Math.Max(250, Math.Min(leftWidth, (int)(totalWidth * 0.5)));
                    
                    // Calculate right panel splits based on proportions
                    var rightWidth = totalWidth - leftRightSplitter.SplitterDistance;
                    var rightPanelWidth = (int)(totalWidth * rightProportion);
                    var middleWidth = rightWidth - Math.Max(300, rightPanelWidth);
                    middleRightSplitter.SplitterDistance = Math.Max(200, middleWidth);
                    
                    // Preview height (persisted)
                    rightTopBottomSplitter.SplitterDistance = Math.Max(20, Math.Min(settings.PreviewHeight, 100));
                }
                finally
                {
                    isRestoringLayout = false;
                    // Delay enabling persistence to let layout fully settle
                    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ => {
                        allowPersistence = true;
                    });
                }
            };
            
            this.Load += (s, e) =>
            {
                applySavedLayout(s, e);
                
                // Auto-load entities if already connected
                if (Service != null)
                {
                    EnsureSolutionsLoaded(ensureEntities: true);
                    EnsureNameBuilderPluginPresence();
                }
            };
            
            this.Resize += applySavedLayout;

            UpdatePluginInstallButtonState();
            this.ResumeLayout();
        }

        private void EnsureNameBuilderPluginPresence()
        {
            if (Service == null || pluginPresenceCheckRunning || pluginPresenceVerified)
            {
                return;
            }

            pluginPresenceCheckRunning = true;
            UpdatePluginInstallButtonState();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Checking NameBuilder plug-in registration...",
                Work = (worker, args) =>
                {
                    args.Result = PerformNameBuilderPluginPresenceCheck();
                },
                PostWorkCallBack = (args) =>
                {
                    pluginPresenceCheckRunning = false;
                    UpdatePluginInstallButtonState();

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Unable to verify the NameBuilder plug-in: {args.Error.Message}",
                            "Plugin Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (args.Result is PluginPresenceCheckResult result)
                    {
                        lastPluginCheckResult = result;
                        if (!result.IsInstalled)
                        {
                            pluginPresenceVerified = false;
                            statusLabel.Text = result.Message;
                            statusLabel.ForeColor = Color.Firebrick;
                            MessageBox.Show(this, result.Message, "NameBuilder Plug-in Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            pendingPostInstallAction = null;
                        }
                        else
                        {
                            pluginPresenceVerified = true;
                            var hashPreview = FormatHashPreview(result.InstalledHash);
                            statusLabel.Text = $"NameBuilder plug-in registration verified (hash {hashPreview}).";
                            statusLabel.ForeColor = Color.ForestGreen;

                            if (result.ResolvedPluginType != null)
                            {
                                activePluginType = result.ResolvedPluginType;
                            }

                            if (pendingPostInstallAction != null)
                            {
                                var continuation = pendingPostInstallAction;
                                pendingPostInstallAction = null;
                                continuation?.Invoke();
                            }
                        }

                        UpdatePluginInstallButtonState();
                        SetActiveRegistryStep(activeRegistryStep);
                        TryAutoLoadPublishedConfiguration();
                    }
                }
            });
        }

        private PluginPresenceCheckResult PerformNameBuilderPluginPresenceCheck()
        {
            var assemblyQuery = new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("pluginassemblyid", "name", "version", "modifiedon", "content"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, "NameBuilder")
                    }
                }
            };

            var assembly = Service.RetrieveMultiple(assemblyQuery).Entities.FirstOrDefault();
            if (assembly == null)
            {
                return new PluginPresenceCheckResult
                {
                    IsInstalled = false,
                    Message = "The NameBuilder plug-in assembly is missing in this Dataverse environment. Use Register NameBuilder Plug-in to install it before continuing.",
                    AssemblyName = "NameBuilder"
                };
            }

            var pluginTypeQuery = new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("plugintypeid", "name", "typename"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assembly.Id)
                    }
                }
            };

            var pluginTypeEntities = Service.RetrieveMultiple(pluginTypeQuery).Entities;
            var pluginTypes = pluginTypeEntities.Select(e => new PluginTypeInfo
            {
                PluginTypeId = e.Id,
                Name = e.GetAttributeValue<string>("name"),
                TypeName = e.GetAttributeValue<string>("typename")
            }).ToList();

            if (pluginTypes.Count == 0)
            {
                return new PluginPresenceCheckResult
                {
                    IsInstalled = false,
                    Message = "No plug-in types were found under the NameBuilder assembly. Use Register NameBuilder Plug-in to reinstall the plug-in.",
                    PluginAssemblyId = assembly.Id,
                    InstalledVersion = assembly.GetAttributeValue<string>("version"),
                    AssemblyName = assembly.GetAttributeValue<string>("name") ?? "NameBuilder",
                    RegisteredPluginTypes = pluginTypes,
                    InstalledHash = ComputeSha256HexFromBase64(assembly.GetAttributeValue<string>("content"))
                };
            }

            return new PluginPresenceCheckResult
            {
                IsInstalled = true,
                ResolvedPluginType = ResolvePluginType(pluginTypes) ?? pluginTypes.FirstOrDefault(),
                InstalledVersion = assembly.GetAttributeValue<string>("version"),
                PluginAssemblyId = assembly.Id,
                AssemblyName = assembly.GetAttributeValue<string>("name") ?? "NameBuilder",
                LastUpdatedOn = assembly.GetAttributeValue<DateTime?>("modifiedon"),
                RegisteredPluginTypes = pluginTypes,
                InstalledHash = ComputeSha256HexFromBase64(assembly.GetAttributeValue<string>("content"))
            };
        }

        private void RegisterPluginToolButton_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to a Dataverse environment before installing the plug-in.",
                    "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var defaultPath = ResolveLocalPluginAssemblyPath();
            var statusInfo = BuildPluginRegistrationStatusInfo();

            using (var dialog = new PluginRegistrationDialog(defaultPath, statusInfo))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedAssemblyPath))
                {
                    var preference = GetConnectionPreference();
                    var solutionId = preference?.PluginSolutionId;

                    using (var solutionDialog = new PluginSolutionSelectionDialog(solutions, solutionId))
                    {
                        if (solutionDialog.ShowDialog(this) == DialogResult.OK)
                        {
                            PersistConnectionPreference(pref =>
                            {
                                pref.PluginSolutionId = solutionDialog.SelectedSolutionId;
                                pref.PluginSolutionUniqueName = solutionDialog.SelectedSolutionUniqueName;
                            });

                            StartPluginInstallation(dialog.SelectedAssemblyPath, solutionDialog.SelectedSolutionId);
                        }
                    }
                }
            }
        }

        private void StartPluginInstallation(string assemblyPath, Guid? solutionId, Action postInstallContinuation = null)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            {
                MessageBox.Show(this, "Select a valid NameBuilder.dll file before continuing.",
                    "Assembly Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            pluginInstallRunning = true;
            UpdatePluginInstallButtonState();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Registering NameBuilder plug-in...",
                AsyncArgument = new { Path = assemblyPath, SolutionId = solutionId },
                Work = (worker, args) =>
                {
                    dynamic arg = args.Argument;
                    var installer = new PluginAssemblyInstaller(Service);
                    var result = installer.InstallOrUpdate(arg.Path);
                    
                    if (arg.SolutionId != null && arg.SolutionId != Guid.Empty)
                    {
                        AddPluginComponentsToSolution(result.AssemblyId, arg.SolutionId);
                    }
                    
                    args.Result = result;
                },
                PostWorkCallBack = (args) =>
                {
                    pluginInstallRunning = false;
                    UpdatePluginInstallButtonState();

                    if (args.Error != null)
                    {
                        MessageBox.Show(this, $"Failed to register the NameBuilder plug-in: {args.Error.Message}",
                            "Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (args.Result is PluginAssemblyInstallResult installResult)
                    {
                        cachedPluginAssemblyPath = installResult.AssemblyPath;
                        cachedLocalPluginHash = installResult.AssemblyHash ?? null;
                        var action = installResult.CreatedAssembly ? "installed" : "updated";
                        var hashPreview = FormatHashPreview(installResult.AssemblyHash);
                        statusLabel.Text = $"NameBuilder plug-in {action} (hash {hashPreview}).";
                        statusLabel.ForeColor = Color.ForestGreen;
                    }

                    pluginPresenceVerified = false;
                    pendingPostInstallAction = postInstallContinuation;
                    EnsureNameBuilderPluginPresence();
                }
            });
        }

        private PluginRegistrationStatusInfo BuildPluginRegistrationStatusInfo()
        {
            var status = new PluginRegistrationStatusInfo
            {
                IsInstalled = lastPluginCheckResult?.IsInstalled ?? false,
                InstalledVersion = lastPluginCheckResult?.InstalledVersion,
                StatusMessage = lastPluginCheckResult?.Message,
                InstalledHash = lastPluginCheckResult?.InstalledHash
            };

            if (lastPluginCheckResult?.RegisteredPluginTypes != null && lastPluginCheckResult.RegisteredPluginTypes.Count > 0)
            {
                status.RegisteredTypes = lastPluginCheckResult.RegisteredPluginTypes
                    .Select(t => t?.Name ?? t?.TypeName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return status;
        }

        private void UpdatePluginInstallButtonState()
        {
            if (registerPluginToolButton == null)
            {
                return;
            }

            var hasConnection = Service != null;
            registerPluginToolButton.Enabled = hasConnection && !pluginPresenceCheckRunning && !pluginInstallRunning;
            registerPluginToolButton.Text = pluginPresenceVerified
                ? "Update NameBuilder Plug-in"
                : "Register NameBuilder Plug-in";

            if (lastPluginCheckResult != null && lastPluginCheckResult.IsInstalled &&
                !string.IsNullOrWhiteSpace(lastPluginCheckResult.InstalledHash))
            {
                registerPluginToolButton.ToolTipText =
                    $"Current NameBuilder hash: {FormatHashPreview(lastPluginCheckResult.InstalledHash)}. Click to update or repair.";
            }
            else
            {
                registerPluginToolButton.ToolTipText =
                    "Deploy or update the NameBuilder plug-in assembly in the connected Dataverse environment.";
            }
        }

        private string ResolveLocalPluginAssemblyPath(bool refresh = false)
        {
            if (!refresh && !string.IsNullOrWhiteSpace(cachedPluginAssemblyPath) && File.Exists(cachedPluginAssemblyPath))
            {
                return cachedPluginAssemblyPath;
            }

            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var searchRoots = new List<string>();

                void AddCandidate(string path)
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        searchRoots.Add(path);
                    }
                }

                if (!string.IsNullOrWhiteSpace(assemblyLocation))
                {
                    var baseDirectory = Path.GetDirectoryName(assemblyLocation);
                    var assemblyName = Path.GetFileNameWithoutExtension(assemblyLocation);

                    AddCandidate(baseDirectory);
                    if (!string.IsNullOrWhiteSpace(baseDirectory) && !string.IsNullOrWhiteSpace(assemblyName))
                    {
                        AddCandidate(Path.Combine(baseDirectory, assemblyName));
                    }

                    if (!string.IsNullOrWhiteSpace(baseDirectory))
                    {
                        var parent = Directory.GetParent(baseDirectory);
                        if (parent != null)
                        {
                            AddCandidate(parent.FullName);
                            if (!string.IsNullOrWhiteSpace(assemblyName))
                            {
                                AddCandidate(Path.Combine(parent.FullName, assemblyName));
                            }
                        }
                    }
                }

                foreach (var root in searchRoots.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var candidate = Path.Combine(root, "Assets", "DataversePlugin", "NameBuilder.dll");
                    if (File.Exists(candidate))
                    {
                        cachedPluginAssemblyPath = candidate;
                        return cachedPluginAssemblyPath;
                    }
                }
            }
            catch
            {
                // Ignore path resolution failures and fall back to browse.
            }

            cachedPluginAssemblyPath = null;
            return null;
        }

        private void AddPluginComponentsToSolution(Guid assemblyId, Guid solutionId)
        {
            if (assemblyId == Guid.Empty || solutionId == Guid.Empty)
            {
                return;
            }

            // Check if assembly is already in solution
            var checkQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("solutioncomponentid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                        new ConditionExpression("objectid", ConditionOperator.Equal, assemblyId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 91) // PluginAssembly
                    }
                },
                TopCount = 1
            };

            var existingComponent = Service.RetrieveMultiple(checkQuery).Entities.FirstOrDefault();
            if (existingComponent != null)
            {
                return; // Already in solution
            }

            // Add assembly to solution
            var addRequest = new OrganizationRequest("AddSolutionComponent")
            {
                ["ComponentId"] = assemblyId,
                ["ComponentType"] = 91, // PluginAssembly
                ["SolutionUniqueName"] = GetSolutionUniqueName(solutionId),
                ["AddRequiredComponents"] = false
            };

            Service.Execute(addRequest);
        }

        private void AddStepToSolution(Guid stepId, Guid? solutionId)
        {
            if (stepId == Guid.Empty || !solutionId.HasValue || solutionId.Value == Guid.Empty)
            {
                return;
            }

            // Check if step is already in solution
            var checkQuery = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("solutioncomponentid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId.Value),
                        new ConditionExpression("objectid", ConditionOperator.Equal, stepId),
                        new ConditionExpression("componenttype", ConditionOperator.Equal, 92) // SDKMessageProcessingStep
                    }
                },
                TopCount = 1
            };

            var existingComponent = Service.RetrieveMultiple(checkQuery).Entities.FirstOrDefault();
            if (existingComponent != null)
            {
                return; // Already in solution
            }

            // Add step to solution
            var addRequest = new OrganizationRequest("AddSolutionComponent")
            {
                ["ComponentId"] = stepId,
                ["ComponentType"] = 92, // SDKMessageProcessingStep
                ["SolutionUniqueName"] = GetSolutionUniqueName(solutionId.Value),
                ["AddRequiredComponents"] = false
            };

            Service.Execute(addRequest);
        }

        private string GetSolutionUniqueName(Guid solutionId)
        {
            var solution = solutions?.FirstOrDefault(s => s.SolutionId == solutionId);
            if (solution != null && !string.IsNullOrWhiteSpace(solution.UniqueName))
            {
                return solution.UniqueName;
            }

            // Fallback: query from Dataverse
            var query = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("uniquename"),
                Criteria = new FilterExpression
                {
                    Conditions = { new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId) }
                },
                TopCount = 1
            };

            var solutionEntity = Service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return solutionEntity?.GetAttributeValue<string>("uniquename");
        }

        private void LoadEntitiesButton_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to a Dataverse environment first.", "Not Connected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetActiveRegistryStep(null);
            EnsureSolutionsLoaded(ensureEntities: true);
        }

        private void RetrieveConfigurationToolButton_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to a Dataverse environment first.", "Not Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExecuteMethod(StartRetrieveConfigurationFlow);
        }

        private void PublishToolButton_Click(object sender, EventArgs e)
        {
            if (Service == null)
            {
                MessageBox.Show("Please connect to a Dataverse environment first.", "Not Connected",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!EnsureActivePluginTypeLoaded())
            {
                return;
            }

            if (fieldBlocks.Count == 0)
            {
                MessageBox.Show("Add at least one field block before publishing.",
                    "No Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(currentEntityLogicalName))
            {
                MessageBox.Show("Select an entity before publishing the configuration.",
                    "Missing Entity", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GenerateJson();
            var json = jsonOutputTextBox.Text;
            if (string.IsNullOrWhiteSpace(json))
            {
                MessageBox.Show("Unable to build the JSON payload for this configuration.",
                    "Serialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var attributeSet = GetReferencedAttributesFromConfiguration();
            if (attributeSet.Count == 0)
            {
                MessageBox.Show("No attributes were detected in the generated configuration.",
                    "Missing Attributes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var entityDisplayName = currentEntityDisplayName ?? currentEntityLogicalName;
            var cachedSteps = GetCachedEntitySteps(currentEntityLogicalName);

            if ((cachedSteps.insertStep == null || cachedSteps.updateStep == null) && activePluginType != null)
            {
                cachedSteps = (
                    cachedSteps.insertStep ?? ResolveStepFromDataverse(currentEntityLogicalName, "Create"),
                    cachedSteps.updateStep ?? ResolveStepFromDataverse(currentEntityLogicalName, "Update"));
            }

            using (var dialog = new PublishTargetsDialog(entityDisplayName, cachedSteps.insertStep != null, cachedSteps.updateStep != null))
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                if (!dialog.PublishInsert && !dialog.PublishUpdate)
                {
                    return;
                }

                var preference = GetConnectionPreference();
                var solutionId = preference?.PluginSolutionId;
                var selectedSolutionItem = preference != null
                    ? solutions?.FirstOrDefault(s => s.SolutionId == solutionId)
                    : null;

                Guid? finalSolutionId = solutionId;
                string finalSolutionUniqueName = preference?.PluginSolutionUniqueName;

                // Only prompt if Default Solution is selected or no solution is set yet
                if (!solutionId.HasValue || IsDefaultSolution(selectedSolutionItem))
                {
                    using (var solutionDialog = new PluginSolutionSelectionDialog(solutions, solutionId))
                    {
                        if (solutionDialog.ShowDialog(this) != DialogResult.OK)
                        {
                            return;
                        }

                        finalSolutionId = solutionDialog.SelectedSolutionId;
                        finalSolutionUniqueName = solutionDialog.SelectedSolutionUniqueName;

                        PersistConnectionPreference(pref =>
                        {
                            pref.PluginSolutionId = finalSolutionId;
                            pref.PluginSolutionUniqueName = finalSolutionUniqueName;
                        });
                    }
                }

                var context = new PublishContext
                {
                    PluginTypeId = activePluginType.PluginTypeId,
                    PluginTypeName = activePluginType.Name ?? activePluginType.TypeName,
                    EntityLogicalName = currentEntityLogicalName,
                    EntityDisplayName = entityDisplayName,
                    JsonPayload = json,
                    AttributeNames = attributeSet.ToList(),
                    PublishInsert = dialog.PublishInsert,
                    PublishUpdate = dialog.PublishUpdate,
                    SolutionId = finalSolutionId
                };

                publishToolButton.Enabled = false;

                var publishContext = context;

                WorkAsync(new WorkAsyncInfo
                    {
                        Message = "Publishing configuration...",
                        AsyncArgument = publishContext,
                        Work = (worker, args) =>
                        {
                            var ctx = (PublishContext)args.Argument;
                            args.Result = ExecutePublish(ctx);
                        },
                        PostWorkCallBack = (args) =>
                        {
                            publishToolButton.Enabled = true;
                            SetActiveRegistryStep(activeRegistryStep);

                            if (args.Error != null)
                            {
                                ShowPublishError(args.Error);
                                return;
                            }

                            var ctx = publishContext;
                            if (args.Result is PublishResult publishResult)
                            {
                                UpdateCachedStepsAfterPublish(publishResult);

                                if (activeRegistryStep != null)
                                {
                                    activeRegistryStep.UnsecureConfiguration = ctx.JsonPayload;
                                    if (publishResult.StepMetadata != null)
                                    {
                                        var updated = publishResult.StepMetadata
                                            .FirstOrDefault(s => s.StepId == activeRegistryStep.StepId);
                                        if (updated != null)
                                        {
                                            activeRegistryStep.FilteringAttributes = updated.FilteringAttributes;
                                        }
                                    }
                                }

                                if (publishResult.UpdatedSteps.Count > 0)
                                {
                                    statusLabel.Text = $"Published to: {string.Join(", ", publishResult.UpdatedSteps)}";
                                }
                                else
                                {
                                    statusLabel.Text = "Configuration published.";
                                }
                                statusLabel.ForeColor = Color.ForestGreen;

                                CaptureCommittedSnapshot();
                            }
                        }
                    });
            }
        }

        private void StartRetrieveConfigurationFlow()
        {
            retrieveConfigToolButton.Enabled = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Locating NameBuilder types...",
                Work = (worker, args) =>
                {
                    var assemblyQuery = new QueryExpression("pluginassembly")
                    {
                        ColumnSet = new ColumnSet("pluginassemblyid", "name"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("name", ConditionOperator.Equal, "NameBuilder")
                            }
                        }
                    };

                    var assembly = Service.RetrieveMultiple(assemblyQuery).Entities.FirstOrDefault();
                    if (assembly == null)
                    {
                        throw new InvalidOperationException("NameBuilder assembly was not found in this environment.");
                    }

                    var pluginTypeQuery = new QueryExpression("plugintype")
                    {
                        ColumnSet = new ColumnSet("plugintypeid", "name", "typename"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assembly.Id)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };

                    var pluginTypeEntities = Service.RetrieveMultiple(pluginTypeQuery).Entities;
                    var pluginTypes = pluginTypeEntities.Select(e => new PluginTypeInfo
                    {
                        PluginTypeId = e.Id,
                        Name = e.GetAttributeValue<string>("name"),
                        TypeName = e.GetAttributeValue<string>("typename")
                    }).ToList();

                    args.Result = pluginTypes;
                },
                PostWorkCallBack = (args) =>
                {
                    retrieveConfigToolButton.Enabled = true;

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error retrieving plugin types: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var pluginTypes = args.Result as List<PluginTypeInfo>;
                    if (pluginTypes == null || pluginTypes.Count == 0)
                    {
                        MessageBox.Show("No plugin types were found under the NameBuilder assembly.",
                            "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var selectedPluginType = ResolvePluginType(pluginTypes);

                    if (selectedPluginType == null)
                    {
                        using (var dialog = new PluginTypeSelectionDialog(pluginTypes))
                        {
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                selectedPluginType = dialog.SelectedType;
                            }
                        }
                    }

                    if (selectedPluginType == null)
                    {
                        return;
                    }

                    activePluginType = selectedPluginType;
                    LoadStepsForPluginType(selectedPluginType);
                }
            });
        }

        private PluginTypeInfo ResolvePluginType(List<PluginTypeInfo> pluginTypes)
        {
            if (pluginTypes == null || pluginTypes.Count == 0)
                return null;

            var exactName = pluginTypes.FirstOrDefault(t =>
                string.Equals(t.Name, "NameBuilderPlugin", StringComparison.OrdinalIgnoreCase));
            if (exactName != null)
                return exactName;

            var typeNameMatch = pluginTypes.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.TypeName) &&
                t.TypeName.IndexOf("NameBuilder", StringComparison.OrdinalIgnoreCase) >= 0);
            if (typeNameMatch != null)
                return typeNameMatch;

            var displayMatch = pluginTypes.FirstOrDefault(t =>
                !string.IsNullOrWhiteSpace(t.Name) &&
                t.Name.IndexOf("NameBuilder", StringComparison.OrdinalIgnoreCase) >= 0);
            if (displayMatch != null)
                return displayMatch;

            return pluginTypes.Count == 1 ? pluginTypes[0] : null;
        }

        private void LoadStepsForPluginType(PluginTypeInfo pluginType)
        {
            if (pluginType == null)
                return;

            retrieveConfigToolButton.Enabled = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Retrieving steps for {pluginType.Name ?? pluginType.TypeName}...",
                Work = (worker, args) =>
                {
                    var stepQuery = new QueryExpression("sdkmessageprocessingstep")
                    {
                        ColumnSet = new ColumnSet("sdkmessageprocessingstepid", "name", "description", "configuration", "sdkmessagefilterid", "sdkmessageid", "stage", "mode", "filteringattributes"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("eventhandler", ConditionOperator.Equal, pluginType.PluginTypeId)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };

                    var stepEntities = Service.RetrieveMultiple(stepQuery).Entities;

                    var filterIds = stepEntities
                        .Select(e => e.GetAttributeValue<EntityReference>("sdkmessagefilterid")?.Id)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .Distinct()
                        .ToList();

                    var filterMap = new Dictionary<Guid, Entity>();
                    if (filterIds.Count > 0)
                    {
                        var filterQuery = new QueryExpression("sdkmessagefilter")
                        {
                            ColumnSet = new ColumnSet("sdkmessagefilterid", "primaryobjecttypecode", "secondaryobjecttypecode"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("sdkmessagefilterid", ConditionOperator.In, filterIds.Cast<object>().ToArray())
                                }
                            }
                        };

                        var filterEntities = Service.RetrieveMultiple(filterQuery).Entities;
                        foreach (var filter in filterEntities)
                        {
                            filterMap[filter.Id] = filter;
                            messageFilterDetailsCache[filter.Id] = (
                                filter.GetAttributeValue<string>("primaryobjecttypecode"),
                                filter.GetAttributeValue<string>("secondaryobjecttypecode"));
                        }
                    }

                    var messageIds = stepEntities
                        .Select(e => e.GetAttributeValue<EntityReference>("sdkmessageid")?.Id)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .Distinct()
                        .ToList();

                    var messageMap = new Dictionary<Guid, string>();
                    if (messageIds.Count > 0)
                    {
                        var messageQuery = new QueryExpression("sdkmessage")
                        {
                            ColumnSet = new ColumnSet("sdkmessageid", "name"),
                            Criteria = new FilterExpression
                            {
                                Conditions =
                                {
                                    new ConditionExpression("sdkmessageid", ConditionOperator.In, messageIds.Cast<object>().ToArray())
                                }
                            }
                        };

                        foreach (var message in Service.RetrieveMultiple(messageQuery).Entities)
                        {
                            messageMap[message.Id] = message.GetAttributeValue<string>("name");
                        }
                    }

                    var steps = stepEntities.Select(e =>
                    {
                        string primary = null;
                        string secondary = null;
                        var filterRef = e.GetAttributeValue<EntityReference>("sdkmessagefilterid");
                        if (filterRef != null && filterMap.TryGetValue(filterRef.Id, out var filterEntity))
                        {
                            if (primary == null)
                            {
                                primary = filterEntity.GetAttributeValue<string>("primaryobjecttypecode");
                            }
                            secondary = filterEntity.GetAttributeValue<string>("secondaryobjecttypecode");
                        }

                        var messageRef = e.GetAttributeValue<EntityReference>("sdkmessageid");
                        var messageId = messageRef?.Id ?? Guid.Empty;
                        messageMap.TryGetValue(messageId, out var messageName);

                        return new PluginStepInfo
                        {
                            StepId = e.Id,
                            Name = e.GetAttributeValue<string>("name") ?? "(Unnamed Step)",
                            Description = e.GetAttributeValue<string>("description") ?? string.Empty,
                            UnsecureConfiguration = e.GetAttributeValue<string>("configuration") ?? string.Empty,
                            PrimaryEntity = primary,
                            SecondaryEntity = secondary,
                            MessageName = messageName,
                            MessageId = messageId == Guid.Empty ? (Guid?)null : messageId,
                            FilteringAttributes = e.GetAttributeValue<string>("filteringattributes"),
                            Stage = e.GetAttributeValue<OptionSetValue>("stage")?.Value,
                            Mode = e.GetAttributeValue<OptionSetValue>("mode")?.Value,
                            MessageFilterId = filterRef?.Id
                        };
                    }).ToList();

                    args.Result = steps;
                },
                PostWorkCallBack = (args) =>
                {
                    retrieveConfigToolButton.Enabled = true;

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error retrieving plugin steps: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var steps = args.Result as List<PluginStepInfo>;
                    if (steps == null || steps.Count == 0)
                    {
                        MessageBox.Show("No plugin steps were found for the selected plugin type.",
                            "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    cachedPluginSteps = steps;

                    using (var dialog = new StepSelectionDialog(steps))
                    {
                        if (dialog.ShowDialog() == DialogResult.OK && dialog.SelectedStep != null)
                        {
                            TryLoadConfigurationFromStep(dialog.SelectedStep);
                        }
                    }
                }
            });
        }

        private void TryLoadConfigurationFromStep(PluginStepInfo step)
        {
            TryLoadConfigurationFromStep(step, showAlerts: true);
        }

        private bool TryLoadConfigurationFromStep(PluginStepInfo step, bool showAlerts)
        {
            if (step == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(step.UnsecureConfiguration))
            {
                if (showAlerts)
                {
                    MessageBox.Show("The selected step does not contain an unsecure configuration.", "No Configuration",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return false;
            }

            PluginConfiguration config;
            try
            {
                config = JsonConvert.DeserializeObject<PluginConfiguration>(step.UnsecureConfiguration);
            }
            catch (Exception ex)
            {
                DiagnosticLog.LogError("Load Configuration from Step", ex);
                if (showAlerts)
                {
                    MessageBox.Show(
                        $"Unable to parse the step configuration.\n\n{ex.Message}\n\n" +
                        "This may indicate a corrupt or incompatible configuration file. Check the diagnostics log for details.",
                        "Invalid Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            if (config == null)
            {
                DiagnosticLog.LogWarning("Load Configuration from Step", "Configuration deserialized to null");
                if (showAlerts)
                {
                    MessageBox.Show("The selected step did not return a valid configuration payload.", "Invalid Configuration",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.Entity))
            {
                if (!string.IsNullOrWhiteSpace(step.PrimaryEntity))
                {
                    config.Entity = step.PrimaryEntity;
                }
                else if (!string.IsNullOrWhiteSpace(step.SecondaryEntity))
                {
                    config.Entity = step.SecondaryEntity;
                }
            }

            BeginApplyingConfiguration(config, step);
            return true;
        }

        private void BeginApplyingConfiguration(PluginConfiguration config, PluginStepInfo sourceStep)
        {
            var resolvedEntity = config.Entity;

            if (string.IsNullOrWhiteSpace(resolvedEntity) && sourceStep != null)
            {
                resolvedEntity = sourceStep.PrimaryEntity ?? sourceStep.SecondaryEntity;
            }

            if (!string.IsNullOrWhiteSpace(resolvedEntity))
            {
                config.Entity = resolvedEntity;
            }

            if (string.IsNullOrWhiteSpace(config.Entity))
            {
                MessageBox.Show("The configuration does not specify an entity logical name.", "Missing Entity",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetActiveRegistryStep(null);
            pendingConfigFromPlugin = config;
            pendingConfigSourceStep = sourceStep;
            pendingConfigTargetEntity = config.Entity;

            EnsureEntityAvailableForPendingConfig(config.Entity);
        }

        private void EnsureEntityAvailableForPendingConfig(string entityLogicalName)
        {
            var targetItem = entityDropdown.Items.Cast<EntityItem>()
                .FirstOrDefault(i => i.LogicalName.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase));

            if (targetItem != null)
            {
                if (Equals(entityDropdown.SelectedItem, targetItem))
                {
                    HandleEntitySelectionChanged();
                }
                else
                {
                    entityDropdown.SelectedItem = targetItem;
                }

                return;
            }

            var defaultSolution = solutionDropdown?.Items.Cast<SolutionItem>()
                .FirstOrDefault(IsDefaultSolution);
            if (defaultSolution != null && !Equals(solutionDropdown.SelectedItem, defaultSolution))
            {
                suppressSolutionSelectionChanged = true;
                solutionDropdown.SelectedItem = defaultSolution;
                suppressSolutionSelectionChanged = false;
                HandleSolutionSelectionChanged();

                targetItem = entityDropdown.Items.Cast<EntityItem>()
                    .FirstOrDefault(i => i.LogicalName.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase));
                if (targetItem != null)
                {
                    entityDropdown.SelectedItem = targetItem;
                    return;
                }
            }

            LoadSingleEntityAndSelect(entityLogicalName);
        }

        private void LoadSingleEntityAndSelect(string entityLogicalName)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Loading metadata for {entityLogicalName}...",
                Work = (worker, args) =>
                {
                    var request = new RetrieveEntityRequest
                    {
                        LogicalName = entityLogicalName,
                        EntityFilters = EntityFilters.Entity
                    };
                    var response = (RetrieveEntityResponse)Service.Execute(request);
                    args.Result = response.EntityMetadata;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Unable to load metadata for {entityLogicalName}: {args.Error.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ClearPendingConfigurationState();
                        return;
                    }

                    var metadata = args.Result as EntityMetadata;
                    if (metadata == null)
                    {
                        MessageBox.Show($"Metadata for entity {entityLogicalName} was not found.", "Not Found",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ClearPendingConfigurationState();
                        return;
                    }

                    entities.Add(metadata);

                    var entityItem = new EntityItem
                    {
                        DisplayName = metadata.DisplayName?.UserLocalizedLabel?.Label ?? metadata.LogicalName,
                        LogicalName = metadata.LogicalName,
                        Metadata = metadata
                    };

                    entityDropdown.Items.Add(entityItem);
                    entityDropdown.Enabled = true;
                    entityDropdown.SelectedItem = entityItem;
                }
            });
        }

        private void TryApplyPendingConfiguration()
        {
            if (pendingConfigFromPlugin == null || string.IsNullOrWhiteSpace(pendingConfigTargetEntity))
                return;

            if (!string.Equals(currentEntityLogicalName, pendingConfigTargetEntity, StringComparison.OrdinalIgnoreCase))
                return;

            ApplyConfigurationToUi(pendingConfigFromPlugin, pendingConfigSourceStep);
            ClearPendingConfigurationState();
        }

        private void TryAutoLoadPublishedConfiguration()
        {
            var targetEntity = pendingAutoLoadEntity;
            if (string.IsNullOrWhiteSpace(targetEntity) || autoLoadInProgress)
            {
                return;
            }

            if (Service == null || !pluginPresenceVerified)
            {
                return;
            }

            if (!string.Equals(currentEntityLogicalName, targetEntity, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (autoLoadAttemptedEntities.Contains(targetEntity))
            {
                return;
            }

            if (!EnsureActivePluginTypeLoaded() || activePluginType == null)
            {
                return;
            }

            var pluginTypeId = activePluginType.PluginTypeId;
            if (pluginTypeId == Guid.Empty)
            {
                autoLoadInProgress = false;
                return;
            }

            autoLoadAttemptedEntities.Add(targetEntity);
            autoLoadInProgress = true;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading existing NameBuilder configuration...",
                AsyncArgument = new { Entity = targetEntity, PluginTypeId = pluginTypeId },
                Work = (worker, args) =>
                {
                    dynamic arg = args.Argument;
                    var entityLogicalName = (string)arg.Entity;
                    var typeId = (Guid)arg.PluginTypeId;

                    PluginStepInfo resolvedStep = null;
                    Exception capturedError = null;

                    try
                    {
                        var updateMessageId = GetSdkMessageId("Update");
                        resolvedStep = FindExistingStep(typeId, updateMessageId, entityLogicalName, "Update");

                        if (resolvedStep == null)
                        {
                            var createMessageId = GetSdkMessageId("Create");
                            resolvedStep = FindExistingStep(typeId, createMessageId, entityLogicalName, "Create");
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedError = ex;
                    }

                    args.Result = new AutoLoadResult
                    {
                        EntityLogicalName = entityLogicalName,
                        Step = resolvedStep,
                        Error = capturedError
                    };
                },
                PostWorkCallBack = (args) =>
                {
                    autoLoadInProgress = false;

                    if (args.Error != null)
                    {
                        DiagnosticLog.LogWarning("Auto Load Configuration", args.Error.Message);
                        return;
                    }

                    if (args.Result is AutoLoadResult result)
                    {
                        if (!string.Equals(result.EntityLogicalName, currentEntityLogicalName, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        if (result.Error != null)
                        {
                            DiagnosticLog.LogWarning("Auto Load Configuration", $"Unable to auto-load configuration for {result.EntityLogicalName}: {result.Error.Message}");
                            return;
                        }

                        if (result.Step != null)
                        {
                            TryLoadConfigurationFromStep(result.Step, showAlerts: false);
                        }
                        else
                        {
                            statusLabel.Text = $"No published NameBuilder configuration found for {result.EntityLogicalName}.";
                            statusLabel.ForeColor = Color.DimGray;
                        }
                    }
                }
            });
        }

        private bool EnsureEntityChangeAllowed(EntityItem target)
        {
            if (target == null || string.IsNullOrWhiteSpace(currentEntityLogicalName))
            {
                return true;
            }

            if (string.Equals(target.LogicalName, currentEntityLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!HasUnsavedChanges())
            {
                return true;
            }

            var entityName = currentEntityDisplayName ?? currentEntityLogicalName;
            var response = MessageBox.Show(this,
                $"You have unpublished NameBuilder changes for {entityName}.\n\nSwitch entities without publishing?",
                "Unpublished Changes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            return response == DialogResult.Yes;
        }

        private bool HasUnsavedChanges()
        {
            if (string.IsNullOrWhiteSpace(currentEntityLogicalName))
            {
                return false;
            }

            var snapshot = GetCurrentJsonSnapshot();

            if (string.IsNullOrWhiteSpace(committedEntityLogicalName))
            {
                return !string.IsNullOrWhiteSpace(snapshot);
            }

            if (!string.Equals(committedEntityLogicalName, currentEntityLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(snapshot);
            }

            return !string.Equals(snapshot ?? string.Empty, committedConfigJson ?? string.Empty, StringComparison.Ordinal);
        }

        private string GetCurrentJsonSnapshot()
        {
            GenerateJson();
            return jsonOutputTextBox.Text ?? string.Empty;
        }

        private void CaptureCommittedSnapshot()
        {
            if (string.IsNullOrWhiteSpace(currentEntityLogicalName))
            {
                return;
            }

            committedConfigJson = GetCurrentJsonSnapshot();
            committedEntityLogicalName = currentEntityLogicalName;
        }

        private void ApplyConfigurationToUi(PluginConfiguration config, PluginStepInfo sourceStep)
        {
            suppressBlockSelection = true;
            currentConfig = config;

            if (string.IsNullOrWhiteSpace(currentConfig.Entity) && !string.IsNullOrWhiteSpace(currentEntityLogicalName))
            {
                currentConfig.Entity = currentEntityLogicalName;
            }

            targetFieldTextBox.Text = string.IsNullOrWhiteSpace(config.TargetField) ? "name" : config.TargetField;

            if (config.MaxLength.HasValue)
            {
                var bounded = Math.Max((int)maxLengthNumeric.Minimum, Math.Min((int)maxLengthNumeric.Maximum, config.MaxLength.Value));
                maxLengthNumeric.Value = bounded;
            }
            else
            {
                maxLengthNumeric.Value = 0;
            }

            enableTracingCheckBox.Checked = config.EnableTracing ?? false;

            fieldBlocks.Clear();
            RebuildFieldsPanel();

            if (config.Fields != null)
            {
                foreach (var field in config.Fields)
                {
                    var attrMeta = allAttributes?.FirstOrDefault(a =>
                        a.LogicalName.Equals(field.Field, StringComparison.OrdinalIgnoreCase));
                    AddFieldBlock(field, attrMeta, applyDefaults: false);
                }
            }

            suppressBlockSelection = false;

            if (entityHeaderBlock != null)
            {
                SelectBlock(entityHeaderBlock);
            }

            GenerateJsonAndPreview();

            var sourceName = sourceStep?.Name;
            statusLabel.Text = string.IsNullOrEmpty(sourceName)
                ? "Configuration loaded"
                : $"Configuration loaded from step \"{sourceName}\"";
            statusLabel.ForeColor = Color.MediumBlue;

            SetActiveRegistryStep(sourceStep);
            CaptureCommittedSnapshot();
        }

        private void ClearPendingConfigurationState()
        {
            pendingConfigFromPlugin = null;
            pendingConfigSourceStep = null;
            pendingConfigTargetEntity = null;
        }

        private void SetActiveRegistryStep(PluginStepInfo step)
        {
            activeRegistryStep = step;

            if (publishToolButton == null)
                return;

            publishToolButton.Enabled = pluginPresenceVerified && Service != null;
            var tooltipTarget = activeRegistryStep != null
                ? activeRegistryStep.Name ?? activeRegistryStep.StepId.ToString()
                : (currentEntityDisplayName ?? currentEntityLogicalName ?? "this entity");
            publishToolButton.ToolTipText = $"Publish configuration changes back to Dataverse steps for {tooltipTarget}";
        }

        private (PluginStepInfo insertStep, PluginStepInfo updateStep) GetCachedEntitySteps(string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName) || cachedPluginSteps == null)
            {
                return (null, null);
            }

            var insertStep = cachedPluginSteps.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(s.PrimaryEntity) &&
                s.MessageName != null &&
                s.MessageName.Equals("Create", StringComparison.OrdinalIgnoreCase) &&
                s.PrimaryEntity.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase));

            var updateStep = cachedPluginSteps.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(s.PrimaryEntity) &&
                s.MessageName != null &&
                s.MessageName.Equals("Update", StringComparison.OrdinalIgnoreCase) &&
                s.PrimaryEntity.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase));

            return (insertStep, updateStep);
        }

        private PluginStepInfo ResolveStepFromDataverse(string entityLogicalName, string messageName)
        {
            if (activePluginType == null || string.IsNullOrWhiteSpace(entityLogicalName) || string.IsNullOrWhiteSpace(messageName))
            {
                return null;
            }

            try
            {
                var messageId = GetSdkMessageId(messageName);
                var step = FindExistingStep(activePluginType.PluginTypeId, messageId, entityLogicalName, messageName);
                if (step != null)
                {
                    cachedPluginSteps.RemoveAll(s => s.StepId == step.StepId);
                    cachedPluginSteps.Add(step);
                }
                return step;
            }
            catch (Exception ex)
            {
                var warningKey = $"{entityLogicalName}:{messageName}";
                if (publishStepResolutionWarnings.Add(warningKey))
                {
                    var message = $"Unable to locate the existing {messageName} step for {entityLogicalName}: {ex.Message}";
                    statusLabel.Text = message;
                    statusLabel.ForeColor = Color.Firebrick;
                    MessageBox.Show(this, message, "Publish Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return null;
            }
        }

        private bool EnsureActivePluginTypeLoaded()
        {
            if (activePluginType != null)
            {
                return true;
            }

            try
            {
                var assemblyQuery = new QueryExpression("pluginassembly")
                {
                    ColumnSet = new ColumnSet("pluginassemblyid", "name"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("name", ConditionOperator.Equal, "NameBuilder")
                        }
                    }
                };

                var assembly = Service.RetrieveMultiple(assemblyQuery).Entities.FirstOrDefault();
                if (assembly == null)
                {
                    var message = "The NameBuilder (Name Builder) assembly is not installed in this environment. Install the plug-in before publishing.";
                    MessageBox.Show(message, "Plugin Not Installed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = message;
                    statusLabel.ForeColor = Color.Firebrick;
                    return false;
                }

                var pluginTypeQuery = new QueryExpression("plugintype")
                {
                    ColumnSet = new ColumnSet("plugintypeid", "name", "typename"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("pluginassemblyid", ConditionOperator.Equal, assembly.Id)
                        }
                    }
                };

                var pluginTypeEntities = Service.RetrieveMultiple(pluginTypeQuery).Entities;
                var pluginTypes = pluginTypeEntities.Select(e => new PluginTypeInfo
                {
                    PluginTypeId = e.Id,
                    Name = e.GetAttributeValue<string>("name"),
                    TypeName = e.GetAttributeValue<string>("typename")
                }).ToList();

                if (pluginTypes.Count == 0)
                {
                    var message = "No plug-in types were found under the NameBuilder assembly. Install the Name Builder plug-in before publishing.";
                    MessageBox.Show(message, "Plugin Not Installed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = message;
                    statusLabel.ForeColor = Color.Firebrick;
                    return false;
                }

                var selected = ResolvePluginType(pluginTypes) ?? pluginTypes.First();
                activePluginType = selected;
                return true;
            }
            catch (Exception ex)
            {
                var message = $"Unable to locate the NameBuilder type: {ex.Message}";
                MessageBox.Show(message, "Plugin Lookup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = message;
                statusLabel.ForeColor = Color.Firebrick;
                return false;
            }
        }

        private void LoadEntities()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading entities...",
                Work = (worker, args) =>
                {
                    var request = new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = false
                    };

                    var response = (RetrieveAllEntitiesResponse)Service.Execute(request);
                    args.Result = response.EntityMetadata
                        .Where(e => (e.IsCustomizable?.Value ?? false)
                            && e.IsIntersect != true)
                        .OrderBy(e => e.DisplayName?.UserLocalizedLabel?.Label ?? e.LogicalName)
                        .ToList();
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error loading entities: {args.Error.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    entities = (List<EntityMetadata>)args.Result;
                    statusLabel.Text = $"Loaded {entities.Count} entities";
                    statusLabel.ForeColor = Color.Green;

                    ApplySolutionFilterIfReady();

                        CaptureCommittedSnapshot();
                }
            });
        }

        private void EntityDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressEntitySelectionChanged)
            {
                return;
            }

            var target = entityDropdown?.SelectedItem as EntityItem;
            var previous = lastSelectedEntityItem;

            if (!EnsureEntityChangeAllowed(target))
            {
                suppressEntitySelectionChanged = true;
                if (previous != null && entityDropdown.Items.Contains(previous))
                {
                    entityDropdown.SelectedItem = previous;
                }
                else
                {
                    entityDropdown.SelectedIndex = -1;
                }
                suppressEntitySelectionChanged = false;
                return;
            }

            HandleEntitySelectionChanged();
            lastSelectedEntityItem = target;
        }

        private void HandleEntitySelectionChanged()
        {
            if (entityDropdown?.SelectedItem == null)
            {
                return;
            }

            var selectedEntity = (EntityItem)entityDropdown.SelectedItem;
            currentEntityLogicalName = selectedEntity.LogicalName;
            currentEntityDisplayName = selectedEntity.DisplayName;
            pendingAutoLoadEntity = currentEntityLogicalName;
            committedConfigJson = null;
            committedEntityLogicalName = null;

            ClearSampleRecordCache();

            PersistConnectionPreference(pref => pref.LastEntityLogicalName = currentEntityLogicalName);

            viewDropdown.Items.Clear();
            viewDropdown.Enabled = false;
            sampleRecordDropdown.Items.Clear();
            sampleRecordDropdown.Enabled = false;
            sampleRecord = null;

            fieldsPanel.Controls.Clear();
            fieldBlocks.Clear();

            CreateEntityHeaderBlock(selectedEntity.DisplayName, selectedEntity.LogicalName);

            ExecuteMethod(() => LoadViewsAndAttributes(selectedEntity.LogicalName));
            TryAutoLoadPublishedConfiguration();
            lastSelectedEntityItem = selectedEntity;
        }
        
        private void CreateEntityHeaderBlock(string displayName, string logicalName)
        {
            // Create a dummy field configuration for the header
            var dummyConfig = new FieldConfiguration { Field = "_entity_header_" };
            
            entityHeaderBlock = new FieldBlockControl(dummyConfig, null)
            {
                BackColor = GetFieldBlockBackground(isEntityHeader: true),
                ShowDragHandle = false  // Entity header is not movable
            };
            entityHeaderBlock.Height = 85;
            // Set width after creation to ensure proper sizing
            var panelWidth = fieldsPanel.ClientSize.Width > 0 ? fieldsPanel.ClientSize.Width : fieldsPanel.Width;
            entityHeaderBlock.Width = panelWidth - 25; // Account for scrollbar
            
            // Manually set the labels for the entity header
            var fieldLabel = entityHeaderBlock.Controls.OfType<System.Windows.Forms.Label>().FirstOrDefault(l => l.Font.Bold);
            if (fieldLabel != null)
            {
                fieldLabel.Text = $"{displayName} ({logicalName})";
                fieldLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
                fieldLabel.TextAlign = ContentAlignment.MiddleCenter;
                fieldLabel.AutoSize = false;
                fieldLabel.Height = 36;
            }
            
            var typeLabel = entityHeaderBlock.Controls.OfType<System.Windows.Forms.Label>().FirstOrDefault(l => !l.Font.Bold);
            if (typeLabel != null)
            {
                typeLabel.Text = "Entity Configuration";
                typeLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                typeLabel.TextAlign = ContentAlignment.MiddleCenter;
                typeLabel.AutoSize = false;
                typeLabel.Height = 22;
                typeLabel.ForeColor = Color.DimGray;
            }

            if (fieldLabel != null && typeLabel != null)
            {
                LayoutEntityHeaderLabels(entityHeaderBlock, fieldLabel, typeLabel);
                entityHeaderBlock.Resize += (s, e) => LayoutEntityHeaderLabels(entityHeaderBlock, fieldLabel, typeLabel);
            }
            
            // Hide the move/delete buttons and drag handle for header
            foreach (var button in entityHeaderBlock.Controls.OfType<Button>())
            {
                button.Visible = false;
            }
            
            // Explicitly hide drag handle panel if it exists
            var dragHandlePanel = entityHeaderBlock.Controls.Cast<Control>().FirstOrDefault(c => c.Name == "DragHandle");
            if (dragHandlePanel != null)
            {
                dragHandlePanel.Visible = false;
            }
            
            entityHeaderBlock.EditClicked += (s, e) =>
            {
                SelectBlock(entityHeaderBlock);
                ShowGlobalProperties();
            };
            
            fieldsPanel.Controls.Add(entityHeaderBlock);
            fieldsPanel.PerformLayout();
        }

        private void LoadViewsAndAttributes(string entityLogicalName)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading attributes and views...",
                Work = (worker, args) =>
                {
                    // Load attributes
                    var attrRequest = new RetrieveEntityRequest
                    {
                        LogicalName = entityLogicalName,
                        EntityFilters = EntityFilters.Attributes | EntityFilters.Entity
                    };
                    var attrResponse = (RetrieveEntityResponse)Service.Execute(attrRequest);
                    var attributes = attrResponse.EntityMetadata.Attributes
                        .Where(a => a.IsValidForRead == true && a.AttributeOf == null)
                        .OrderBy(a => a.DisplayName?.UserLocalizedLabel?.Label ?? a.LogicalName)
                        .ToList();
                    
                    // Get primary name attribute and its max length
                    var primaryNameAttribute = attrResponse.EntityMetadata.PrimaryNameAttribute;
                    int? primaryNameMaxLength = null;
                    if (!string.IsNullOrEmpty(primaryNameAttribute))
                    {
                        var primaryAttr = attributes.FirstOrDefault(a => a.LogicalName == primaryNameAttribute);
                        if (primaryAttr is StringAttributeMetadata stringAttr && stringAttr.MaxLength.HasValue)
                        {
                            primaryNameMaxLength = stringAttr.MaxLength.Value;
                        }
                        else if (primaryAttr is MemoAttributeMetadata memoAttr && memoAttr.MaxLength.HasValue)
                        {
                            primaryNameMaxLength = memoAttr.MaxLength.Value;
                        }
                    }
                    
                    // Load system views
                    var systemViewQuery = new QueryExpression("savedquery")
                    {
                        ColumnSet = new ColumnSet("name", "savedqueryid", "layoutxml", "fetchxml"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("returnedtypecode", ConditionOperator.Equal, entityLogicalName),
                                new ConditionExpression("querytype", ConditionOperator.Equal, 0), // Public/system views
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };
                    var systemViews = Service.RetrieveMultiple(systemViewQuery).Entities;

                    // Load personal views
                    var personalViewQuery = new QueryExpression("userquery")
                    {
                        ColumnSet = new ColumnSet("name", "userqueryid", "layoutxml", "fetchxml"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("returnedtypecode", ConditionOperator.Equal, entityLogicalName),
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };
                    var personalViews = Service.RetrieveMultiple(personalViewQuery).Entities;
                    
                    args.Result = new
                    {
                        Attributes = attributes,
                        PersonalViews = personalViews ?? new EntityCollection().Entities,
                        SystemViews = systemViews ?? new EntityCollection().Entities,
                        PrimaryNameAttribute = primaryNameAttribute,
                        PrimaryNameMaxLength = primaryNameMaxLength
                    };
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error loading data: {args.Error.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    dynamic result = args.Result;
                    currentPrimaryNameAttribute = result.PrimaryNameAttribute;
                    allAttributes = result.Attributes;
                    currentAttributes = allAttributes;
                    
                    // Update target field name with entity's primary name attribute
                    if (!string.IsNullOrEmpty(result.PrimaryNameAttribute))
                    {
                        targetFieldTextBox.Text = result.PrimaryNameAttribute;
                        
                        // Update max length to match the primary name field's max length
                        if (result.PrimaryNameMaxLength != null && result.PrimaryNameMaxLength > 0)
                        {
                            maxLengthNumeric.Value = result.PrimaryNameMaxLength;
                        }
                        else
                        {
                            maxLengthNumeric.Value = 0; // No limit if not available
                        }
                        
                        GenerateJsonAndPreview();
                    }
                    
                    // Select the entity header block to show properties with the correct target field
                    if (entityHeaderBlock != null)
                    {
                        SelectBlock(entityHeaderBlock);
                        
                        // After properties panel is created, update the max length control by name
                        if (result.PrimaryNameMaxLength != null && result.PrimaryNameMaxLength > 0)
                        {
                            var maxLengthControl = propertiesPanel.Controls.Find("GlobalMaxLengthNumeric", false).FirstOrDefault() as NumericUpDown;
                            if (maxLengthControl != null)
                            {
                                // Ensure Maximum is high enough before setting Value
                                if (maxLengthControl.Maximum < result.PrimaryNameMaxLength)
                                {
                                    maxLengthControl.Maximum = result.PrimaryNameMaxLength;
                                }
                                maxLengthControl.Value = result.PrimaryNameMaxLength;
                            }
                        }
                    }
                    
                    // Populate attribute listbox
                    attributeListBox.Items.Clear();
                    foreach (var attribute in currentAttributes)
                    {
                        var displayName = attribute.DisplayName?.UserLocalizedLabel?.Label ?? attribute.LogicalName;
                        attributeListBox.Items.Add(new AttributeItem
                        {
                            DisplayName = $"{displayName} ({attribute.LogicalName})",
                            LogicalName = attribute.LogicalName,
                            Metadata = attribute
                        });
                    }
                    
                    // Populate view dropdown: personal views first, separator, then system views
                    viewDropdown.Items.Clear();
                    viewDropdown.Items.Add(new ViewItem { Name = "(All Attributes)", ViewId = Guid.Empty });

                    foreach (Entity view in result.PersonalViews)
                    {
                        viewDropdown.Items.Add(new ViewItem
                        {
                            Name = view.GetAttributeValue<string>("name"),
                            ViewId = view.Id,
                            View = view,
                            IsPersonal = true
                        });
                    }

                    if (result.PersonalViews != null && result.PersonalViews.Count > 0 && result.SystemViews != null && result.SystemViews.Count > 0)
                    {
                        viewDropdown.Items.Add(new ViewItem { Name = "— System Views —", ViewId = Guid.Empty, IsSeparator = true });
                    }

                    foreach (Entity view in result.SystemViews)
                    {
                        viewDropdown.Items.Add(new ViewItem
                        {
                            Name = view.GetAttributeValue<string>("name"),
                            ViewId = view.Id,
                            View = view,
                            IsPersonal = false
                        });
                    }

                    viewDropdown.Enabled = viewDropdown.Items.Count > 0;
                    attributeListBox.Enabled = true;
                    RestoreViewSelectionFromPreferences();
                    var personalCount = (result.PersonalViews as EntityCollection)?.Entities?.Count ?? result.PersonalViews?.Count ?? 0;
                    var systemCount = (result.SystemViews as EntityCollection)?.Entities?.Count ?? result.SystemViews?.Count ?? 0;
                    statusLabel.Text = $"Loaded {currentAttributes.Count} attributes, {personalCount + systemCount} views";

                    TryApplyPendingConfiguration();

                    if (pendingConfigFromPlugin == null)
                    {
                        CaptureCommittedSnapshot();
                    }
                }
            });
        }

        private void ViewDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressViewSelectionChanged)
            {
                return;
            }

            HandleViewSelectionChanged();
        }

        private void HandleViewSelectionChanged()
        {
            if (viewDropdown?.SelectedItem == null)
            {
                return;
            }

            var selectedView = (ViewItem)viewDropdown.SelectedItem;

            if (selectedView.IsSeparator)
            {
                suppressViewSelectionChanged = true;
                var newIndex = Math.Min(viewDropdown.Items.Count - 1, viewDropdown.SelectedIndex + 1);
                if (newIndex == viewDropdown.SelectedIndex && viewDropdown.SelectedIndex > 0)
                {
                    newIndex = viewDropdown.SelectedIndex - 1;
                }
                viewDropdown.SelectedIndex = newIndex;
                suppressViewSelectionChanged = false;
                return;
            }

            if (!selectedView.IsSeparator)
            {
                PersistConnectionPreference(pref => pref.LastViewId = selectedView.ViewId);
            }

            sampleRecordDropdown.Items.Clear();
            sampleRecordDropdown.Enabled = false;
            sampleRecord = null;
            previewTextBox.Clear();

            if (selectedView.ViewId == Guid.Empty)
            {
                currentAttributes = allAttributes;
                ExecuteMethod(() => LoadSampleRecordsForEntity());
            }
            else
            {
                ExecuteMethod(() => FilterAttributesByView(selectedView.View));
                return;
            }

            RefreshAttributeList();
        }

        private void FilterAttributesByView(Entity view)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Filtering attributes...",
                Work = (worker, args) =>
                {
                    var layoutXml = view.GetAttributeValue<string>("layoutxml");
                    if (string.IsNullOrEmpty(layoutXml))
                    {
                        args.Result = allAttributes;
                        return;
                    }
                    
                    // Parse layoutxml to get column names
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(layoutXml);
                    var cellNodes = doc.SelectNodes("//cell[@name]");
                    
                    var viewAttributeNames = new HashSet<string>();
                    foreach (System.Xml.XmlNode node in cellNodes)
                    {
                        var attrName = node.Attributes["name"]?.Value;
                        if (!string.IsNullOrEmpty(attrName))
                            viewAttributeNames.Add(attrName);
                    }
                    
                    args.Result = new
                    {
                        FilteredAttributes = allAttributes.Where(a => viewAttributeNames.Contains(a.LogicalName)).ToList(),
                        View = view,
                        ViewAttributes = viewAttributeNames
                    };
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error filtering attributes: {args.Error.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        currentAttributes = allAttributes;
                    }
                    else
                    {
                        dynamic result = args.Result;
                        currentAttributes = result.FilteredAttributes;
                        
                        // Load sample records from this view
                        LoadSampleRecords(result.View);
                    }
                    
                    RefreshAttributeList();
                }
            });
        }

        private void RefreshAttributeList()
        {
            attributeListBox.Items.Clear();
            foreach (var attribute in currentAttributes)
            {
                var displayName = attribute.DisplayName?.UserLocalizedLabel?.Label ?? attribute.LogicalName;
                attributeListBox.Items.Add(new AttributeItem
                {
                    DisplayName = $"{displayName} ({attribute.LogicalName})",
                    LogicalName = attribute.LogicalName,
                    Metadata = attribute
                });
            }
        }

        private void LoadSampleRecords(Entity view)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading sample records...",
                Work = (worker, args) =>
                {
                    var fetchXml = view.GetAttributeValue<string>("fetchxml");
                    if (string.IsNullOrEmpty(fetchXml))
                    {
                        args.Result = new { Records = new EntityCollection(), PrimaryNameAttribute = currentPrimaryNameAttribute, HasAllAttributes = false };
                        return;
                    }

                    // Parse fetch XML
                    var doc = new System.Xml.XmlDocument();
                    doc.LoadXml(fetchXml);
                    var fetchNode = doc.SelectSingleNode("//fetch");
                    if (fetchNode.Attributes["count"] == null)
                        fetchNode.Attributes.Append(doc.CreateAttribute("count"));
                    fetchNode.Attributes["count"].Value = "10";

                    // Get the entity node
                    var entityNode = doc.SelectSingleNode("//entity");
                    var entityName = entityNode.Attributes["name"].Value;

                    // Get entity metadata to find the primary name attribute
                    var metadataRequest = new RetrieveEntityRequest
                    {
                        LogicalName = entityName,
                        EntityFilters = EntityFilters.Entity | EntityFilters.Attributes
                    };
                    var metadataResponse = (RetrieveEntityResponse)Service.Execute(metadataRequest);
                    var primaryNameAttr = metadataResponse.EntityMetadata.PrimaryNameAttribute;

                    // Only add the primary name attribute if it's not already in the fetch
                    if (!string.IsNullOrEmpty(primaryNameAttr))
                    {
                        var existingAttr = doc.SelectSingleNode($"//attribute[@name='{primaryNameAttr}']");
                        if (existingAttr == null)
                        {
                            var attrNode = doc.CreateElement("attribute");
                            attrNode.SetAttribute("name", primaryNameAttr);
                            entityNode.AppendChild(attrNode);
                        }
                    }

                    var records = Service.RetrieveMultiple(new FetchExpression(doc.OuterXml));
                    args.Result = new { Records = records, PrimaryNameAttribute = primaryNameAttr, HasAllAttributes = false };
                },
                PostWorkCallBack = HandleSampleRecordsLoaded
            });
        }

        private void LoadSampleRecordsForEntity()
        {
            if (string.IsNullOrEmpty(currentEntityLogicalName))
                return;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading sample records...",
                Work = (worker, args) =>
                {
                    EntityCollection records = null;

                    if (EntitySupportsStateCode())
                    {
                        try
                        {
                            records = RetrieveEntitySampleRecords(useStateFilter: true);
                        }
                        catch
                        {
                            records = null;
                        }
                    }

                    if (records == null || records.Entities.Count == 0)
                    {
                        records = RetrieveEntitySampleRecords(useStateFilter: false);
                    }

                    args.Result = new { Records = records, PrimaryNameAttribute = currentPrimaryNameAttribute, HasAllAttributes = false };
                },
                PostWorkCallBack = HandleSampleRecordsLoaded
            });
        }

        private EntityCollection RetrieveEntitySampleRecords(bool useStateFilter)
        {
            var query = new QueryExpression(currentEntityLogicalName)
            {
                ColumnSet = new ColumnSet(GetMinimalSampleColumns().ToArray()),
                TopCount = 10
            };

            if (useStateFilter && EntitySupportsStateCode())
            {
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }

            return Service.RetrieveMultiple(query);
        }

        private IEnumerable<string> GetMinimalSampleColumns()
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var field in GetDisplayNameAttributes())
            {
                columns.Add(field);
            }

            foreach (var field in GetRequiredAttributesForPreview())
            {
                if (AttributeExists(field))
                {
                    columns.Add(field);
                }
            }

            if (columns.Count == 0)
            {
                columns.Add(GetFallbackAttributeForSampling());
            }

            return columns;
        }

        private IEnumerable<string> GetDisplayNameAttributes()
        {
            var candidates = new List<string>
            {
                currentPrimaryNameAttribute,
                "name",
                "fullname",
                "subject",
                "title",
                "description",
                currentEntityLogicalName + "name"
            };

            return candidates
                .Where(c => !string.IsNullOrWhiteSpace(c) && AttributeExists(c))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private string GetFallbackAttributeForSampling()
        {
            var preferred = new[]
            {
                currentPrimaryNameAttribute,
                currentEntityLogicalName + "id",
                "createdon",
                "modifiedon"
            };

            foreach (var candidate in preferred)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && AttributeExists(candidate))
                {
                    return candidate;
                }
            }

            var firstExisting = allAttributes?.FirstOrDefault()?.LogicalName;
            if (!string.IsNullOrWhiteSpace(firstExisting))
            {
                return firstExisting;
            }

            // As a last resort fall back to "name" (may still fail if entity truly has no attributes exposed)
            return "name";
        }

        private bool AttributeExists(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName) || allAttributes == null)
            {
                return false;
            }

            return allAttributes.Any(a => logicalName.Equals(a.LogicalName, StringComparison.OrdinalIgnoreCase));
        }

        private bool EntitySupportsStateCode()
        {
            return allAttributes?.Any(a => a.LogicalName.Equals("statecode", StringComparison.OrdinalIgnoreCase)) == true;
        }

        private void HandleSampleRecordsLoaded(RunWorkerCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show($"Error loading records: {args.Error.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            dynamic result = args.Result;
            var records = (EntityCollection)result.Records;
            bool hasAllAttributes = false;
            try
            {
                hasAllAttributes = result.HasAllAttributes;
            }
            catch
            {
                hasAllAttributes = false;
            }

            PopulateSampleRecordDropdown(records, hasAllAttributes);
        }

        private void PopulateSampleRecordDropdown(EntityCollection records, bool recordsHaveAllAttributes)
        {
            sampleRecordDropdown.Items.Clear();

            if (records == null || records.Entities.Count == 0)
            {
                sampleRecordDropdown.Items.Add("(No records found)");
                sampleRecordDropdown.Enabled = false;
                sampleRecord = null;
                previewTextBox.Text = "Select a sample record to see preview";
                previewTextBox.BackColor = Color.LightGray;
                return;
            }

            foreach (var record in records.Entities)
            {
                var cachedRecord = CacheSampleRecord(record, recordsHaveAllAttributes);
                var displayValue = GetRecordDisplayName(cachedRecord);
                sampleRecordDropdown.Items.Add(new RecordItem
                {
                    DisplayName = displayValue,
                    Record = cachedRecord,
                    HasAllAttributes = recordsHaveAllAttributes
                });
            }

            sampleRecordDropdown.Enabled = true;
            sampleRecordDropdown.SelectedIndex = 0;
        }

        private string GetRecordDisplayName(Entity record)
        {
            // Try to get the primary name attribute value
            var nameFields = new List<string>();
            if (!string.IsNullOrEmpty(currentPrimaryNameAttribute))
            {
                nameFields.Add(currentPrimaryNameAttribute);
            }
            nameFields.AddRange(new[] { "name", "fullname", "subject", "title", "description", currentEntityLogicalName + "name" });

            foreach (var field in nameFields)
            {
                if (record.Contains(field) && record[field] != null)
                {
                    var value = record.GetAttributeValue<string>(field);
                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }
            }
            
            // Fallback to ID only
            return record.Id.ToString().Substring(0, 8) + "...";
        }

        private Entity CacheSampleRecord(Entity record, bool hasAllAttributes)
        {
            if (record == null || record.Id == Guid.Empty)
            {
                return record;
            }

            var logicalName = string.IsNullOrWhiteSpace(record.LogicalName) ? currentEntityLogicalName : record.LogicalName;
            var cache = GetRecordCacheForEntity(logicalName);

            if (cache.TryGetValue(record.Id, out var existing))
            {
                MergeEntityAttributes(existing, record);
                return existing;
            }

            cache[record.Id] = record;
            return record;
        }

        private Dictionary<Guid, Entity> GetRecordCacheForEntity(string logicalName)
        {
            logicalName = string.IsNullOrWhiteSpace(logicalName) ? currentEntityLogicalName : logicalName;
            if (!sampleRecordCache.TryGetValue(logicalName, out var cache))
            {
                cache = new Dictionary<Guid, Entity>();
                sampleRecordCache[logicalName] = cache;
            }

            return cache;
        }

        private Entity EnsureRecordHasAttributes(Entity record, IEnumerable<string> requiredAttributes)
        {
            if (record == null)
            {
                return record;
            }

            var logicalName = string.IsNullOrWhiteSpace(record.LogicalName) ? currentEntityLogicalName : record.LogicalName;
            var cache = GetRecordCacheForEntity(logicalName);

            if (cache.TryGetValue(record.Id, out var cached))
            {
                record = cached;
            }
            else if (record.Id != Guid.Empty)
            {
                cache[record.Id] = record;
            }

            if (record.Id == Guid.Empty || Service == null)
            {
                return record;
            }

            var required = (requiredAttributes ?? Enumerable.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (required.Count == 0)
            {
                return record;
            }

            var missing = required.Where(r => !record.Contains(r)).ToList();
            if (missing.Count == 0)
            {
                return record;
            }

            var refreshed = Service.Retrieve(logicalName, record.Id, new ColumnSet(missing.ToArray()));
            MergeEntityAttributes(record, refreshed);
            return record;
        }

        private void MergeEntityAttributes(Entity target, Entity source)
        {
            if (target == null || source == null)
            {
                return;
            }

            foreach (var kvp in source.Attributes)
            {
                target[kvp.Key] = kvp.Value;
            }

            if (source.FormattedValues != null)
            {
                foreach (var kvp in source.FormattedValues)
                {
                    target.FormattedValues[kvp.Key] = kvp.Value;
                }
            }
        }

        private IEnumerable<string> GetRequiredAttributesForPreview()
        {
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var block in fieldBlocks)
            {
                AddFieldAttributes(block.Configuration, required);
            }

            return required;
        }

        private void AddFieldAttributes(FieldConfiguration config, HashSet<string> required)
        {
            if (config == null || required == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.Field))
            {
                required.Add(config.Field);
            }

            if (config.AlternateField != null)
            {
                AddFieldAttributes(config.AlternateField, required);
            }

            AddConditionAttributes(config.IncludeIf, required);
        }

        private void AddConditionAttributes(FieldCondition condition, HashSet<string> required)
        {
            if (condition == null || required == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(condition.Field))
            {
                required.Add(condition.Field);
            }

            if (condition.AnyOf != null)
            {
                foreach (var child in condition.AnyOf)
                {
                    AddConditionAttributes(child, required);
                }
            }

            if (condition.AllOf != null)
            {
                foreach (var child in condition.AllOf)
                {
                    AddConditionAttributes(child, required);
                }
            }
        }

        private void SampleRecordDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sampleRecordDropdown.SelectedItem == null || !(sampleRecordDropdown.SelectedItem is RecordItem))
            {
                sampleRecord = null;
                previewTextBox.Clear();
                return;
            }
            
            var selectedRecord = (RecordItem)sampleRecordDropdown.SelectedItem;
            var requiredAttributes = GetRequiredAttributesForPreview();
            sampleRecord = EnsureRecordHasAttributes(selectedRecord.Record, requiredAttributes);
            selectedRecord.Record = sampleRecord;
            selectedRecord.HasAllAttributes = requiredAttributes.All(attr => sampleRecord != null && sampleRecord.Contains(attr));
            GeneratePreview();
        }

        private void AttributeListBox_DoubleClick(object sender, EventArgs e)
        {
            AddFieldFromSelection();
        }

        private void AddFieldFromSelection()
        {
            if (attributeListBox.SelectedItem == null) return;

            var selectedAttribute = (AttributeItem)attributeListBox.SelectedItem;
            var config = new FieldConfiguration
            {
                Field = selectedAttribute.LogicalName
            };
            
            AddFieldBlock(config, selectedAttribute.Metadata, applyDefaults: true);
        }

        private void AddFieldBlock(FieldConfiguration config, AttributeMetadata attrMetadata = null, bool applyDefaults = false)
        {
            var block = new FieldBlockControl(config, attrMetadata)
            {
                BackColor = GetFieldBlockBackground()
            };

            // Auto-detect type from metadata if available
            if (attrMetadata != null && string.IsNullOrEmpty(config.Type))
            {
                config.Type = InferTypeFromMetadata(attrMetadata);
            }

            if (applyDefaults)
            {
                ApplyDefaultsToFieldConfiguration(config);
            }

            block.UpdateDisplay();
            
            // Make blocks clickable to select and edit in properties panel
            block.Click += (s, e) => SelectBlock(block);
            block.EditClicked += (s, e) => SelectBlock(block);
            
            block.DeleteClicked += (s, e) =>
            {
                if (MessageBox.Show("Remove this field block?", "Confirm", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    fieldsPanel.Controls.Remove(block);
                    fieldBlocks.Remove(block);
                    if (selectedBlock == block)
                    {
                        selectedBlock = null;
                        ShowGlobalProperties();
                    }
                    GenerateJsonAndPreview();
                }
            };
            
            // Drag handle events - highlight when clicked and select block
            block.DragHandleClicked += (s, e) =>
            {
                SelectBlock(block);
                block.HighlightDragHandle(true);
            };
            
            // Move up button
            block.MoveUpClicked += (s, e) =>
            {
                var currentIndex = fieldBlocks.IndexOf(block);
                if (currentIndex > 0) // Allow moving to first position
                {
                    fieldBlocks.RemoveAt(currentIndex);
                    fieldBlocks.Insert(currentIndex - 1, block);
                    RebuildFieldsPanel();
                    GenerateJsonAndPreview();
                }
            };
            
            // Move down button
            block.MoveDownClicked += (s, e) =>
            {
                var currentIndex = fieldBlocks.IndexOf(block);
                if (currentIndex >= 0 && currentIndex < fieldBlocks.Count - 1)
                {
                    fieldBlocks.RemoveAt(currentIndex);
                    fieldBlocks.Insert(currentIndex + 1, block);
                    RebuildFieldsPanel();
                    GenerateJsonAndPreview();
                }
            };
            
            fieldBlocks.Add(block);
            // Set width after adding to panel for proper measurement
            var panelWidth = fieldsPanel.ClientSize.Width > 0 ? fieldsPanel.ClientSize.Width : fieldsPanel.Width;
            block.Width = panelWidth - 25; // Account for scrollbar
            
            // Rebuild panel to update arrow visibility for all blocks
            RebuildFieldsPanel();
            GenerateJsonAndPreview();
            
            // Auto-select the new block unless suppressed during bulk operations
            if (!suppressBlockSelection)
            {
                SelectBlock(block);
            }
        }

        private Color GetFieldBlockBackground(bool isEntityHeader = false)
        {
            var baseColor = propertiesPanel?.BackColor ?? SystemColors.Control;
            return isEntityHeader ? AdjustColorBrightness(baseColor, -0.12f) : baseColor;
        }

        private Color AdjustColorBrightness(Color color, float correctionFactor)
        {
            correctionFactor = Math.Max(-1f, Math.Min(1f, correctionFactor));

            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A,
                ClampToByte(red),
                ClampToByte(green),
                ClampToByte(blue));
        }

        private int ClampToByte(float value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (int)value;
        }

        private void LayoutEntityHeaderLabels(FieldBlockControl headerBlock, System.Windows.Forms.Label nameLabel, System.Windows.Forms.Label subtitleLabel)
        {
            if (headerBlock == null || nameLabel == null || subtitleLabel == null)
                return;

            var horizontalPadding = 10;
            var availableWidth = Math.Max(50, headerBlock.ClientSize.Width - horizontalPadding * 2);

            nameLabel.Width = availableWidth;
            subtitleLabel.Width = availableWidth;
            nameLabel.Left = horizontalPadding;
            subtitleLabel.Left = horizontalPadding;

            var totalContentHeight = nameLabel.Height + subtitleLabel.Height;
            var startTop = Math.Max(8, (headerBlock.ClientSize.Height - totalContentHeight) / 2);

            nameLabel.Top = startTop;
            subtitleLabel.Top = nameLabel.Bottom;

            nameLabel.TextAlign = ContentAlignment.MiddleCenter;
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
        }

        private void ApplyDefaultsToFieldConfiguration(FieldConfiguration config)
        {
            var settings = PluginUserSettings.Load();

            if (string.IsNullOrEmpty(config.Prefix) && !string.IsNullOrEmpty(settings.DefaultPrefix))
            {
                config.Prefix = settings.DefaultPrefix;
            }

            if (string.IsNullOrEmpty(config.Suffix) && !string.IsNullOrEmpty(settings.DefaultSuffix))
            {
                config.Suffix = settings.DefaultSuffix;
            }

            if (!config.TimezoneOffsetHours.HasValue && settings.DefaultTimezoneOffset.HasValue)
            {
                if (config.Type == "date" || config.Type == "datetime")
                {
                    config.TimezoneOffsetHours = settings.DefaultTimezoneOffset;
                }
            }

            if (string.IsNullOrEmpty(config.Format))
            {
                if ((config.Type == "number" || config.Type == "currency") && !string.IsNullOrEmpty(settings.DefaultNumberFormat))
                {
                    config.Format = settings.DefaultNumberFormat;
                }
                else if ((config.Type == "date" || config.Type == "datetime") && !string.IsNullOrEmpty(settings.DefaultDateFormat))
                {
                    config.Format = settings.DefaultDateFormat;
                }
            }
        }

        private void SelectBlock(FieldBlockControl block)
        {
            // Deselect previous block
            if (selectedBlock != null)
            {
                selectedBlock.BackColor = GetFieldBlockBackground(selectedBlock == entityHeaderBlock);
            }
            
            selectedBlock = block;
            
            if (block != entityHeaderBlock)
            {
                block.BackColor = Color.LightSkyBlue;
            }
            
            // Show appropriate properties
            if (block == entityHeaderBlock)
            {
                ShowGlobalProperties();
            }
            else
            {
                ShowBlockProperties(block);
            }
        }

        private void ShowGlobalProperties()
        {
            propertiesPanel.SuspendLayout();
            propertiesPanel.Controls.Clear();
            
            propertiesTitleLabel.Text = "Global Configuration";
            propertiesPanel.Controls.Add(propertiesTitleLabel);
            
            int y = 45;

            // Row 1: Plugin Solution (full width)
            var solutionLabel = new System.Windows.Forms.Label
            {
                Text = "Plugin Solution:",
                Location = new Point(10, y + 3),
                Size = new Size(120, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var solutionCombo = new ComboBox
            {
                Location = new Point(140, y),
                Size = new Size(220, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var availableSolutions = solutions ?? new List<SolutionItem>();
            foreach (var sol in availableSolutions)
            {
                solutionCombo.Items.Add(sol);
            }
            var preference = GetConnectionPreference();
            var preferredSolutionId = preference?.PluginSolutionId;
            if (preferredSolutionId.HasValue)
            {
                var selected = availableSolutions.FirstOrDefault(s => s.SolutionId == preferredSolutionId.Value);
                if (selected != null)
                {
                    solutionCombo.SelectedItem = selected;
                }
            }
            if (solutionCombo.SelectedIndex == -1 && solutionCombo.Items.Count > 0)
            {
                solutionCombo.SelectedIndex = 0;
            }
            helpToolTip?.SetToolTip(solutionCombo, "Select the unmanaged solution where NameBuilder plugin assemblies and steps should live.");
            solutionCombo.SelectedIndexChanged += (s, e) =>
            {
                var selected = solutionCombo.SelectedItem as SolutionItem;
                PersistConnectionPreference(pref =>
                {
                    pref.PluginSolutionId = selected?.SolutionId;
                    pref.PluginSolutionUniqueName = selected?.UniqueName;
                });
            };
            propertiesPanel.Controls.AddRange(new Control[] { solutionLabel, solutionCombo });
            y += 40;

            // Row 2: Target Field Name (full width)
            var targetLabel = new System.Windows.Forms.Label
            {
                Text = "Target Field Name:",
                Location = new Point(10, y + 3),
                Size = new Size(120, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var targetTextBox = new TextBox
            {
                Text = targetFieldTextBox.Text,
                Location = new Point(140, y),
                Size = new Size(220, 23)
            };
            helpToolTip?.SetToolTip(targetTextBox, "Logical name of the destination column (defaults to name). This becomes targetField in JSON.");
            targetTextBox.TextChanged += (s, e) => {
                targetFieldTextBox.Text = targetTextBox.Text;
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { targetLabel, targetTextBox });
            y += 40;

            // Row 3: Global Max Length + Enable Tracing
            var maxLabel = new System.Windows.Forms.Label
            {
                Text = "Global Max Length:",
                Location = new Point(10, y + 3),
                Size = new Size(120, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var maxNumeric = new NumericUpDown
            {
                Name = "GlobalMaxLengthNumeric",
                Location = new Point(140, y),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 100000,
                Value = maxLengthNumeric.Value
            };
            var maxHintLabel = new System.Windows.Forms.Label
            {
                Text = "(0 = no limit)",
                Location = new Point(140, y + 26),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            helpToolTip?.SetToolTip(maxNumeric, "Optional length cap applied to the final string. Set to 0 for unlimited.");
            maxNumeric.ValueChanged += (s, e) => {
                maxLengthNumeric.Value = maxNumeric.Value;
                GenerateJsonAndPreview();
            };

            // Vertical separator
            var separatorBar = new System.Windows.Forms.Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Width = 2,
                Height = 23,
                    Location = new Point(225, y),
                BackColor = Color.LightGray
            };

            var traceCheckBox = new CheckBox
            {
                Text = "Enable Tracing",
                Checked = enableTracingCheckBox.Checked,
                    Location = new Point(235, y + 3),
                AutoSize = true
            };
            helpToolTip?.SetToolTip(traceCheckBox, "When enabled, the NameBuilder plug-in writes verbose traces to the Dataverse Plug-in Trace Log.");
            traceCheckBox.CheckedChanged += (s, e) => {
                enableTracingCheckBox.Checked = traceCheckBox.Checked;
                GenerateJsonAndPreview();
            };

            propertiesPanel.Controls.AddRange(new Control[] { maxLabel, maxNumeric, maxHintLabel, separatorBar, traceCheckBox });
            y += 55;
            
            // Horizontal separator line
            var separatorLine = new System.Windows.Forms.Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Height = 2,
                Location = new Point(10, y),
                Width = 350
            };
            propertiesPanel.Controls.Add(separatorLine);
            y += 10;
            
            // Separator line
            var separatorLabel = new System.Windows.Forms.Label
            {
                Text = "Default Field Properties:",
                Location = new Point(10, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            propertiesPanel.Controls.Add(separatorLabel);
            y += 30;
            
            var infoLabel2 = new System.Windows.Forms.Label
            {
                Text = "These defaults will be applied to new fields and can update existing fields using defaults.",
                Location = new Point(10, y),
                Size = new Size(350, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            propertiesPanel.Controls.Add(infoLabel2);
            y += 35;
            
            var settings = PluginUserSettings.Load();
            
            // Default Prefix
            var defaultPrefixLabel = new System.Windows.Forms.Label
            {
                Text = "Default\nPrefix:",
                Location = new Point(10, y),
                Size = new Size(70, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var defaultPrefixTextBox = new TextBox
            {
                Text = settings.DefaultPrefix ?? "",
                Location = new Point(85, y + 5),
                Size = new Size(120, 23)
            };
            helpToolTip?.SetToolTip(defaultPrefixTextBox, "Default prefix applied to new string fields and any fields still using the previous default.");
            var prefixPreview = MakeSpacesVisible(defaultPrefixTextBox, 210, y + 8);
            propertiesPanel.Controls.Add(prefixPreview);
            defaultPrefixTextBox.TextChanged += (s, e) => {
                var oldValue = settings.DefaultPrefix;
                settings.DefaultPrefix = string.IsNullOrEmpty(defaultPrefixTextBox.Text) ? null : defaultPrefixTextBox.Text;
                settings.Save();
                UpdateFieldsWithDefaultChange("prefix", oldValue, settings.DefaultPrefix);
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { defaultPrefixLabel, defaultPrefixTextBox });
            y += 40;
            
            // Default Suffix
            var defaultSuffixLabel = new System.Windows.Forms.Label
            {
                Text = "Default\nSuffix:",
                Location = new Point(10, y),
                Size = new Size(70, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var defaultSuffixTextBox = new TextBox
            {
                Text = settings.DefaultSuffix ?? "",
                Location = new Point(85, y + 5),
                Size = new Size(120, 23)
            };
            helpToolTip?.SetToolTip(defaultSuffixTextBox, "Default suffix applied to new string fields and propagated when unchanged.");
            var suffixPreview = MakeSpacesVisible(defaultSuffixTextBox, 210, y + 8);
            propertiesPanel.Controls.Add(suffixPreview);
            defaultSuffixTextBox.TextChanged += (s, e) => {
                var oldValue = settings.DefaultSuffix;
                settings.DefaultSuffix = string.IsNullOrEmpty(defaultSuffixTextBox.Text) ? null : defaultSuffixTextBox.Text;
                settings.Save();
                UpdateFieldsWithDefaultChange("suffix", oldValue, settings.DefaultSuffix);
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { defaultSuffixLabel, defaultSuffixTextBox });
            y += 45;
            
            // Default Timezone
            var defaultTzLabel = new System.Windows.Forms.Label
            {
                Text = "Default\nTimezone:",
                Location = new Point(10, y),
                Size = new Size(70, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var defaultTzCombo = new ComboBox
            {
                Location = new Point(85, y + 5),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            helpToolTip?.SetToolTip(defaultTzCombo, "Timezone offset (in hours) applied to new date/datetime fields when no override exists.");
            var tzOptions = GetTimezoneOptions();
            defaultTzCombo.Items.Add("(None)");
            defaultTzCombo.Items.AddRange(tzOptions.Select(o => o.Label).ToArray());
            if (settings.DefaultTimezoneOffset.HasValue)
            {
                var match = tzOptions.FirstOrDefault(o => o.Offset == settings.DefaultTimezoneOffset.Value);
                if (match != null) defaultTzCombo.SelectedItem = match.Label;
                else defaultTzCombo.SelectedIndex = 0;
            }
            else
            {
                defaultTzCombo.SelectedIndex = 0;
            }
            defaultTzCombo.SelectedIndexChanged += (s, e) => {
                var oldValue = settings.DefaultTimezoneOffset;
                if (defaultTzCombo.SelectedItem.ToString() == "(None)")
                {
                    settings.DefaultTimezoneOffset = null;
                }
                else
                {
                    var sel = tzOptions.FirstOrDefault(o => o.Label == (string)defaultTzCombo.SelectedItem);
                    if (sel != null)
                    {
                        settings.DefaultTimezoneOffset = sel.Offset;
                    }
                }
                settings.Save();
                UpdateFieldsWithDefaultChange("timezone", oldValue, settings.DefaultTimezoneOffset);
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { defaultTzLabel, defaultTzCombo });
            y += 45;
            
            // Default Number Format
            var defaultNumberFormatLabel = new System.Windows.Forms.Label
            {
                Text = "Default Number\nFormat:",
                Location = new Point(10, y),
                Size = new Size(70, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var defaultNumberFormatTextBox = new TextBox
            {
                Text = settings.DefaultNumberFormat ?? "",
                Location = new Point(85, y + 5),
                Size = new Size(200, 23)
            };
            helpToolTip?.SetToolTip(defaultNumberFormatTextBox, "Standard .NET numeric format string used for new number/currency blocks.");
            defaultNumberFormatTextBox.TextChanged += (s, e) => {
                var oldValue = settings.DefaultNumberFormat;
                settings.DefaultNumberFormat = string.IsNullOrWhiteSpace(defaultNumberFormatTextBox.Text) ? null : defaultNumberFormatTextBox.Text;
                settings.Save();
                UpdateFieldsWithDefaultChange("numberformat", oldValue, settings.DefaultNumberFormat);
                GenerateJsonAndPreview();
            };
            var numberFormatHint = new System.Windows.Forms.Label
            {
                Text = "e.g., #,##0.00 or 0.0K",
                Location = new Point(85, y + 30),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F)
            };
            propertiesPanel.Controls.AddRange(new Control[] { defaultNumberFormatLabel, defaultNumberFormatTextBox, numberFormatHint });
            y += 55;
            
            // Default Date Format
            var defaultDateFormatLabel = new System.Windows.Forms.Label
            {
                Text = "Default Date\nFormat:",
                Location = new Point(10, y),
                Size = new Size(70, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var defaultDateFormatTextBox = new TextBox
            {
                Text = settings.DefaultDateFormat ?? "",
                Location = new Point(85, y + 5),
                Size = new Size(200, 23)
            };
            helpToolTip?.SetToolTip(defaultDateFormatTextBox, "Date or datetime format string applied to new temporal blocks.");
            defaultDateFormatTextBox.TextChanged += (s, e) => {
                var oldValue = settings.DefaultDateFormat;
                settings.DefaultDateFormat = string.IsNullOrWhiteSpace(defaultDateFormatTextBox.Text) ? null : defaultDateFormatTextBox.Text;
                settings.Save();
                UpdateFieldsWithDefaultChange("dateformat", oldValue, settings.DefaultDateFormat);
                GenerateJsonAndPreview();
            };
            var dateFormatHint = new System.Windows.Forms.Label
            {
                Text = "e.g., yyyy-MM-dd or dd/MM/yyyy",
                Location = new Point(85, y + 30),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 7.5F)
            };
            propertiesPanel.Controls.AddRange(new Control[] { defaultDateFormatLabel, defaultDateFormatTextBox, dateFormatHint });
            
            propertiesPanel.ResumeLayout();
        }

        private void ShowBlockProperties(FieldBlockControl block)
        {
            propertiesPanel.SuspendLayout();
            propertiesPanel.Controls.Clear();
            
            propertiesTitleLabel.Text = $"Field: {block.Configuration.Field}";
            propertiesPanel.Controls.Add(propertiesTitleLabel);
            
            var friendlyName = ResolveFriendlyAttributeName(block);
            System.Windows.Forms.Label friendlyNameLabel = null;
            if (!string.IsNullOrWhiteSpace(friendlyName) &&
                !friendlyName.Equals(block.Configuration.Field, StringComparison.OrdinalIgnoreCase))
            {
                friendlyNameLabel = new System.Windows.Forms.Label
                {
                    Text = $"({friendlyName})",
                    Location = new Point(propertiesTitleLabel.Left, propertiesTitleLabel.Bottom + 4),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.DimGray
                };
                propertiesPanel.Controls.Add(friendlyNameLabel);
            }
            
            int y = friendlyNameLabel != null ? friendlyNameLabel.Bottom + 20 : 50;
            int labelWidth = 110;
            int controlX = labelWidth + 8;
            int contentWidth = Math.Max(220, propertiesPanel.ClientSize.Width - controlX - 20);
            
            // Field Type
            AddPropertyLabel("Field Type:", 10, y);
            var typeCombo = new ComboBox
            {
                Location = new Point(controlX, y),
                Size = new Size(contentWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            typeCombo.Items.AddRange(new[] { "auto-detect", "string", "lookup", "date", "datetime", "optionset", "number", "currency", "boolean" });
            typeCombo.SelectedItem = block.Configuration.Type ?? "auto-detect";
            typeCombo.SelectedIndexChanged += (s, e) => {
                block.Configuration.Type = typeCombo.SelectedItem.ToString() == "auto-detect" ? null : typeCombo.SelectedItem.ToString();
                block.UpdateDisplay();
                ShowBlockProperties(block); // Refresh properties based on new type
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.Add(typeCombo);
            y += 35;
            
            // Determine the effective field type for filtering properties
            var effectiveType = block.Configuration.Type;
            var isAutoDetect = string.IsNullOrEmpty(effectiveType);
            
            // Show properties based on field type
            // Timezone picklist (only for date/datetime, or auto-detect)
            if (isAutoDetect || effectiveType == "date" || effectiveType == "datetime")
            {
                AddPropertyLabel("Timezone:", 10, y);
                var tzCombo = new ComboBox
                {
                    Location = new Point(controlX, y),
                    Size = new Size(contentWidth, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                var tzOptions = GetTimezoneOptions();
                tzCombo.Items.AddRange(tzOptions.Select(o => o.Label).ToArray());
                var persisted = PluginUserSettings.Load().DefaultTimezoneOffset;
                if (block.Configuration.TimezoneOffsetHours.HasValue) persisted = block.Configuration.TimezoneOffsetHours;
                if (persisted.HasValue)
                {
                    var match = tzOptions.FirstOrDefault(o => o.Offset == persisted.Value);
                    if (match != null) tzCombo.SelectedItem = match.Label;
                }
                tzCombo.SelectedIndexChanged += (s, e) =>
                {
                    var sel = tzOptions.FirstOrDefault(o => o.Label == (string)tzCombo.SelectedItem);
                    if (sel != null)
                    {
                        block.Configuration.TimezoneOffsetHours = sel.Offset;
                        var st = PluginUserSettings.Load(); st.DefaultTimezoneOffset = sel.Offset; st.Save();
                        GenerateJsonAndPreview();
                    }
                };
                propertiesPanel.Controls.Add(tzCombo);
                y += 35;
            }
            
            // Format (for date/datetime/number/currency, or auto-detect)
            if (isAutoDetect || effectiveType == "date" || effectiveType == "datetime" || 
                effectiveType == "number" || effectiveType == "currency")
            {
                AddPropertyLabel("Format:", 10, y);
                var formatTextBox = new TextBox
                {
                    Text = block.Configuration.Format ?? "",
                    Location = new Point(controlX, y),
                    Size = new Size(contentWidth, 23),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                formatTextBox.TextChanged += (s, e) => {
                    block.Configuration.Format = string.IsNullOrWhiteSpace(formatTextBox.Text) ? null : formatTextBox.Text;
                    block.UpdateDisplay();
                    GenerateJsonAndPreview();
                };
                propertiesPanel.Controls.Add(formatTextBox);
                y += 30;
                
                var formatHintLabel = new System.Windows.Forms.Label
                {
                    Text = "Date: yyyy-MM-dd | Number: #,##0.00 | Scale: 0.0K, 0.00M",
                    Location = new Point(controlX, y),
                    Size = new Size(contentWidth, 30),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 7.5F),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    AutoSize = false
                };
                propertiesPanel.Controls.Add(formatHintLabel);
                y += 40;
            }
            
            // Prefix/Suffix (for all types)
            AddPropertyLabel("Prefix:", 10, y);
            var prefixTextBox = new TextBox
            {
                Text = block.Configuration.Prefix ?? "",
                Location = new Point(controlX, y),
                Size = new Size(90, 23)
            };
            var fieldPrefixPreview = MakeSpacesVisible(prefixTextBox, controlX + 95, y + 3);
            propertiesPanel.Controls.Add(fieldPrefixPreview);
            prefixTextBox.TextChanged += (s, e) => {
                block.Configuration.Prefix = string.IsNullOrEmpty(prefixTextBox.Text) ? null : prefixTextBox.Text;
                block.UpdateDisplay();
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.Add(prefixTextBox);
            y += 30;
            
            AddPropertyLabel("Suffix:", 10, y);
            var suffixTextBox = new TextBox
            {
                Text = block.Configuration.Suffix ?? "",
                Location = new Point(controlX, y),
                Size = new Size(90, 23)
            };
            var fieldSuffixPreview = MakeSpacesVisible(suffixTextBox, controlX + 95, y + 3);
            propertiesPanel.Controls.Add(fieldSuffixPreview);
            suffixTextBox.TextChanged += (s, e) => {
                block.Configuration.Suffix = string.IsNullOrEmpty(suffixTextBox.Text) ? null : suffixTextBox.Text;
                block.UpdateDisplay();
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.Add(suffixTextBox);
            y += 35;
            
            // Max Length (for string/lookup types, or auto-detect)
            if (isAutoDetect || effectiveType == "string" || effectiveType == "lookup")
            {
                AddPropertyLabel("Max Length:", 10, y);
                var maxLengthNumeric = new NumericUpDown
                {
                    Value = block.Configuration.MaxLength ?? 0,
                    Location = new Point(controlX, y),
                    Size = new Size(80, 23),
                    Minimum = 0,
                    Maximum = 10000
                };
                var maxHint = new System.Windows.Forms.Label
                {
                    Text = block.Configuration.MaxLength.HasValue ? "" : "Max",
                    Location = new Point(controlX + 90, y + 3),
                    AutoSize = true,
                    ForeColor = Color.Gray
                };
                maxLengthNumeric.Enter += (s, e) =>
                {
                    if (maxLengthNumeric.Value == 0)
                        maxLengthNumeric.Value = 20;
                };
                maxLengthNumeric.ValueChanged += (s, e) => {
                    block.Configuration.MaxLength = maxLengthNumeric.Value == 0 ? null : (int?)maxLengthNumeric.Value;
                    maxHint.Text = maxLengthNumeric.Value == 0 ? "Max" : "";
                    block.UpdateDisplay();
                    GenerateJsonAndPreview();
                };
                propertiesPanel.Controls.Add(maxLengthNumeric);
                propertiesPanel.Controls.Add(maxHint);
                y += 35;
                
                // Truncation Indicator (only with Max Length)
                AddPropertyLabel("Truncation:", 10, y);
                var truncTextBox = new TextBox
                {
                    Text = block.Configuration.TruncationIndicator ?? "...",
                    Location = new Point(controlX, y),
                    Size = new Size(100, 23)
                };
                truncTextBox.TextChanged += (s, e) => {
                    block.Configuration.TruncationIndicator = string.IsNullOrWhiteSpace(truncTextBox.Text) ? "..." : truncTextBox.Text;
                    GenerateJsonAndPreview();
                };
                propertiesPanel.Controls.Add(truncTextBox);
                y += 35;
            }
            
            var attributeSource = allAttributes ?? currentAttributes ?? new List<AttributeMetadata>();

            // Default / Alternate Button
            var fallbackButton = new Button
            {
                Text = (block.Configuration.AlternateField != null || !string.IsNullOrWhiteSpace(block.Configuration.Default))
                    ? "Edit default if blank"
                    : "Default if blank",
                Location = new Point(10, y),
                Size = new Size(200, 30)
            };
            helpToolTip.SetToolTip(fallbackButton, "Define alternate attributes or literal text to use whenever the primary field is empty.");
            fallbackButton.Click += (s, e) => {
                var existingAlternate = block.Configuration.AlternateField ?? new FieldConfiguration();
                using (var dialog = new AlternateFieldDialog(existingAlternate, block.Configuration.Default, attributeSource))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        block.Configuration.AlternateField = dialog.Result;
                        block.Configuration.Default = dialog.DefaultValue;
                        block.UpdateDisplay();
                        GenerateJsonAndPreview();
                        ShowBlockProperties(block);
                    }
                }
            };
            propertiesPanel.Controls.Add(fallbackButton);
            y += 40;
            
            // Condition Button
            var conditionButton = new Button
            {
                Text = block.Configuration.IncludeIf != null ? "Edit Condition (includeIf)" : "Add Condition (includeIf)",
                Location = new Point(10, y),
                Size = new Size(200, 30)
            };
            helpToolTip.SetToolTip(conditionButton, "Add includeIf logic so this block only renders when the comparison rules are met.");
            conditionButton.Click += (s, e) => {
                using (var dialog = new ConditionDialog(block.Configuration.IncludeIf, attributeSource, block.Configuration.Field))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        block.Configuration.IncludeIf = dialog.Result;
                        conditionButton.Text = block.Configuration.IncludeIf != null ? "Edit Condition (includeIf)" : "Add Condition (includeIf)";
                        block.UpdateDisplay();
                        GenerateJsonAndPreview();
                        ShowBlockProperties(block);
                    }
                }
            };
            propertiesPanel.Controls.Add(conditionButton);
            y += 45;

            var summaryContainer = new Panel
            {
                Location = new Point(10, y),
                Size = new Size(propertiesPanel.ClientSize.Width - 20, Math.Max(120, propertiesPanel.ClientSize.Height - y - 20)),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = propertiesPanel.BackColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            var summaryBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                BackColor = summaryContainer.BackColor,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F),
                ScrollBars = ScrollBars.Vertical,
                Text = BuildFieldBehaviorSummary(block.Configuration)
            };
            summaryContainer.Controls.Add(summaryBox);
            propertiesPanel.Controls.Add(summaryContainer);
            y += summaryContainer.Height + 10;
            
            propertiesPanel.ResumeLayout();
        }

        private void AddPropertyLabel(string text, int x, int y)
        {
            var label = new System.Windows.Forms.Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F)
            };
            propertiesPanel.Controls.Add(label);
        }

        private string BuildFieldBehaviorSummary(FieldConfiguration config)
        {
            if (config == null)
            {
                return "No field selected.";
            }

            var lines = new List<string>();
            var primaryLabel = string.IsNullOrWhiteSpace(config.Field)
                ? "primary field"
                : FormatFieldLabel(config.Field);
            lines.Add($"Use '{primaryLabel}' when not blank.");

            AppendFallbackSummary(config, lines);

            var conditionText = DescribeCondition(config.IncludeIf);
            if (!string.IsNullOrWhiteSpace(conditionText))
            {
                lines.Add("Condition: " + conditionText);
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void AppendFallbackSummary(FieldConfiguration config, List<string> lines)
        {
            if (config == null || lines == null)
            {
                return;
            }

            var visited = new HashSet<FieldConfiguration>();
            var cursor = config;

            while (cursor != null && visited.Add(cursor))
            {
                var alternate = cursor.AlternateField;

                if (alternate != null && !string.IsNullOrWhiteSpace(alternate.Field))
                {
                    lines.Add($"If blank -> use '{FormatFieldLabel(alternate.Field)}'.");
                    cursor = alternate;
                    continue;
                }

                if (alternate != null && string.IsNullOrWhiteSpace(alternate.Field) &&
                    !string.IsNullOrWhiteSpace(alternate.Default))
                {
                    lines.Add($"If blank -> default to \"{alternate.Default}\".");
                    break;
                }

                if (!string.IsNullOrWhiteSpace(cursor.Default))
                {
                    lines.Add($"If blank -> default to \"{cursor.Default}\".");
                }
                else
                {
                    lines.Add("If blank -> leave empty.");
                }
                break;
            }

            if (config.AlternateField != null && !string.IsNullOrWhiteSpace(config.Default))
            {
                lines.Add($"Additional default detected -> \"{config.Default}\".");
            }
        }

        private string DescribeCondition(FieldCondition condition)
        {
            if (condition == null)
            {
                return null;
            }

            var segments = new List<string>();

            if (!string.IsNullOrWhiteSpace(condition.Field) && !string.IsNullOrWhiteSpace(condition.Operator))
            {
                var comparison = condition.Operator;
                if (!string.IsNullOrWhiteSpace(condition.Value))
                {
                    var formattedValue = FormatOptionValue(condition.Field, condition.Value);
                    comparison += " \"" + formattedValue + "\"";
                }
                segments.Add($"{FormatFieldLabel(condition.Field)} {comparison}".Trim());
            }

            if (condition.AllOf != null && condition.AllOf.Count > 0)
            {
                var allSegments = condition.AllOf
                    .Select(DescribeCondition)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                if (allSegments.Count > 0)
                {
                    segments.Add("all of (" + string.Join("; ", allSegments) + ")");
                }
            }

            if (condition.AnyOf != null && condition.AnyOf.Count > 0)
            {
                var anySegments = condition.AnyOf
                    .Select(DescribeCondition)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                if (anySegments.Count > 0)
                {
                    segments.Add("any of (" + string.Join("; ", anySegments) + ")");
                }
            }

            return segments.Count == 0 ? null : string.Join("; ", segments);
        }

        private string ResolveFriendlyAttributeName(FieldBlockControl block)
        {
            var logicalName = block?.Configuration?.Field;
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return null;
            }

            var friendly = block.AttributeMetadata?.DisplayName?.UserLocalizedLabel?.Label;
            if (!string.IsNullOrWhiteSpace(friendly))
            {
                return friendly;
            }

            var metadata = allAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase)) ??
                           currentAttributes?.FirstOrDefault(a => a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

            if (metadata != null && block.AttributeMetadata == null)
            {
                block.AttributeMetadata = metadata;
            }

            var fallbackLabel = metadata?.DisplayName?.UserLocalizedLabel?.Label;
            return string.IsNullOrWhiteSpace(fallbackLabel) ? null : fallbackLabel;
        }

        private Image LoadToolbarIcon(string fileName, Icon fallbackIcon)
        {
            foreach (var iconPath in EnumerateIconSearchPaths(fileName))
            {
                try
                {
                    if (!File.Exists(iconPath))
                    {
                        continue;
                    }

                    using (var fs = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var original = Image.FromStream(fs))
                    {
                        return new Bitmap(original, new Size(18, 18));
                    }
                }
                catch
                {
                    // Skip invalid paths and continue searching.
                }
            }

            var embedded = LoadEmbeddedToolbarIcon(fileName);
            if (embedded != null)
            {
                return embedded;
            }

            return CreateToolbarIcon(fallbackIcon);
        }

        private IEnumerable<string> EnumerateIconSearchPaths(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                yield break;
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var baseDirectory = string.IsNullOrWhiteSpace(assemblyPath) ? null : Path.GetDirectoryName(assemblyPath);

            var searchRoots = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseDirectory))
            {
                searchRoots.Add(baseDirectory);

                var parent = Directory.GetParent(baseDirectory);
                if (parent != null)
                {
                    searchRoots.Add(parent.FullName);
                }
            }

            // Also probe a plugin-specific subfolder under each root (matches NuGet layout).
            var additionalRoots = new List<string>();
            foreach (var root in searchRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                additionalRoots.Add(Path.Combine(root, "NameBuilderConfigurator"));
            }
            searchRoots.AddRange(additionalRoots);

            foreach (var root in searchRoots.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var candidate = Path.Combine(root, "Assets", "Icon", fileName);
                if (visited.Add(candidate))
                {
                    yield return candidate;
                }

                candidate = Path.Combine(root, fileName);
                if (visited.Add(candidate))
                {
                    yield return candidate;
                }
            }
        }

        private Image LoadEmbeddedToolbarIcon(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (fileName.Equals("NameBuilder_Monoline_32x32.png", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var bytes = Convert.FromBase64String(EmbeddedNameBuilderMonoline32Png);
                    using (var ms = new MemoryStream(bytes))
                    using (var original = Image.FromStream(ms))
                    {
                        return new Bitmap(original, new Size(18, 18));
                    }
                }
                catch
                {
                    // Ignore corrupt resources.
                }
            }

            return null;
        }

        private static Bitmap CreateToolbarIcon(Icon baseIcon)
        {
            if (baseIcon == null)
            {
                return null;
            }

            var bitmap = baseIcon.ToBitmap();
            if (bitmap.Width == 18 && bitmap.Height == 18)
            {
                return bitmap;
            }

            var resized = new Bitmap(bitmap, new Size(18, 18));
            bitmap.Dispose();
            return resized;
        }
        
        /// <summary>
        /// Adds a small preview label next to the textbox so users can quickly see whitespace.
        /// </summary>
        private System.Windows.Forms.Label MakeSpacesVisible(TextBox textBox, int labelX, int labelY)
        {
            textBox.Font = SpacePreviewInputFont;
            
            // Create a small label to show spaces as tab arrows
            var previewLabel = new System.Windows.Forms.Label
            {
                Location = new Point(labelX, labelY),
                AutoSize = true,
                Font = SpacePreviewLabelFont,
                ForeColor = Color.Gray,
                Text = ""
            };
            
            EventHandler updatePreview = (s, e) =>
            {
                var text = textBox.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    previewLabel.Text = "\"" + text + "\"";
                    previewLabel.ForeColor = text.Contains(" ") ? Color.OrangeRed : Color.Gray;
                }
                else
                {
                    previewLabel.Text = "";
                }
            };
            
            textBox.TextChanged += updatePreview;
            updatePreview(textBox, EventArgs.Empty);
            
            return previewLabel;
        }
        
        private void UpdateFieldsWithDefaultChange(string propertyType, object oldValue, object newValue)
        {
            // Update all fields that are still using the old default value
            foreach (var block in fieldBlocks)
            {
                var config = block.Configuration;
                bool updated = false;
                
                switch (propertyType.ToLower())
                {
                    case "prefix":
                        if (config.Prefix == (string)oldValue)
                        {
                            config.Prefix = (string)newValue;
                            updated = true;
                        }
                        break;
                    case "suffix":
                        if (config.Suffix == (string)oldValue)
                        {
                            config.Suffix = (string)newValue;
                            updated = true;
                        }
                        break;
                    case "timezone":
                        if (config.TimezoneOffsetHours == (int?)oldValue)
                        {
                            config.TimezoneOffsetHours = (int?)newValue;
                            updated = true;
                        }
                        break;
                    case "numberformat":
                        // Only update number/currency fields
                        if ((config.Type == "number" || config.Type == "currency") && config.Format == (string)oldValue)
                        {
                            config.Format = (string)newValue;
                            updated = true;
                        }
                        break;
                    case "dateformat":
                        // Only update date/datetime fields
                        if ((config.Type == "date" || config.Type == "datetime") && config.Format == (string)oldValue)
                        {
                            config.Format = (string)newValue;
                            updated = true;
                        }
                        break;
                }
                
                if (updated)
                {
                    block.UpdateDisplay();
                }
            }
        }

        private HashSet<string> GetReferencedAttributesFromConfiguration()
        {
            var attributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (currentConfig?.Fields == null)
            {
                return attributes;
            }

            foreach (var field in currentConfig.Fields)
            {
                CollectAttributesFromField(field, attributes);
            }

            return attributes;
        }

        private void CollectAttributesFromField(FieldConfiguration config, HashSet<string> collector)
        {
            if (config == null || collector == null)
                return;

            if (!string.IsNullOrWhiteSpace(config.Field))
            {
                collector.Add(config.Field);
            }

            if (config.AlternateField != null)
            {
                CollectAttributesFromField(config.AlternateField, collector);
            }

            if (config.IncludeIf != null)
            {
                CollectAttributesFromCondition(config.IncludeIf, collector);
            }
        }

        private void CollectAttributesFromCondition(FieldCondition condition, HashSet<string> collector)
        {
            if (condition == null)
                return;

            if (!string.IsNullOrWhiteSpace(condition.Field))
            {
                collector.Add(condition.Field);
            }

            if (condition.AnyOf != null)
            {
                foreach (var inner in condition.AnyOf)
                {
                    CollectAttributesFromCondition(inner, collector);
                }
            }

            if (condition.AllOf != null)
            {
                foreach (var inner in condition.AllOf)
                {
                    CollectAttributesFromCondition(inner, collector);
                }
            }
        }

        private PublishResult ExecutePublish(PublishContext context)
        {
            var normalizedAttributes = NormalizeAttributes(context.AttributeNames);
            var attributeCsv = BuildAttributeCsv(normalizedAttributes);

            var result = new PublishResult();

            if (context.PublishInsert)
            {
                var insertStep = EnsureStepForMessage(context, "Create", normalizedAttributes, attributeCsv, ensurePreImage: false);
                if (insertStep != null)
                {
                    result.UpdatedSteps.Add(insertStep.Name ?? $"{context.EntityDisplayName} Create");
                    result.StepMetadata.Add(insertStep);
                    
                    if (context.SolutionId.HasValue)
                    {
                        AddStepToSolution(insertStep.StepId, context.SolutionId);
                    }
                }
            }

            if (context.PublishUpdate)
            {
                var updateStep = EnsureStepForMessage(context, "Update", normalizedAttributes, attributeCsv, ensurePreImage: true);
                if (updateStep != null)
                {
                    result.UpdatedSteps.Add(updateStep.Name ?? $"{context.EntityDisplayName} Update");
                    result.StepMetadata.Add(updateStep);
                    
                    if (context.SolutionId.HasValue)
                    {
                        AddStepToSolution(updateStep.StepId, context.SolutionId);
                    }
                }
            }

            return result;
        }

        private PluginStepInfo EnsureStepForMessage(PublishContext context, string messageName, IReadOnlyCollection<string> normalizedAttributes, string attributeCsv, bool ensurePreImage)
        {
            var messageId = GetSdkMessageId(messageName);
            var stepInfo = FindExistingStep(context.PluginTypeId, messageId, context.EntityLogicalName, messageName);

            if (stepInfo == null)
            {
                stepInfo = CreateStep(context.PluginTypeId, context.EntityLogicalName, context.EntityDisplayName, messageId,
                    messageName, context.JsonPayload, attributeCsv);
            }
            else
            {
                UpdateStepConfiguration(stepInfo.StepId, context.JsonPayload, attributeCsv);
                stepInfo.UnsecureConfiguration = context.JsonPayload;
                stepInfo.FilteringAttributes = attributeCsv;
            }

            if (ensurePreImage && stepInfo != null && normalizedAttributes.Count > 0)
            {
                EnsurePreImageAttributes(stepInfo.StepId, normalizedAttributes);
            }

            return stepInfo;
        }

        private List<string> NormalizeAttributes(IEnumerable<string> attributes)
        {
            if (attributes == null)
            {
                return new List<string>();
            }

            return attributes
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a)
                .ToList();
        }

        private string BuildAttributeCsv(IReadOnlyCollection<string> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return null;
            }

            return string.Join(",", attributes);
        }

        private PluginStepInfo FindExistingStep(Guid pluginTypeId, Guid messageId, string entityLogicalName, string messageName)
        {
            var filterId = GetSdkMessageFilterId(messageId, entityLogicalName);

            var query = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("sdkmessageprocessingstepid", "name", "description", "configuration", "sdkmessagefilterid", "sdkmessageid", "filteringattributes", "stage", "mode"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("eventhandler", ConditionOperator.Equal, pluginTypeId),
                        new ConditionExpression("sdkmessageid", ConditionOperator.Equal, messageId),
                        new ConditionExpression("sdkmessagefilterid", ConditionOperator.Equal, filterId)
                    }
                }
            };

            var entities = Service.RetrieveMultiple(query).Entities;
            
            // If multiple steps exist for the same entity/message combination, use the first one and log a warning
            if (entities.Count > 1)
            {
                DiagnosticLog.LogWarning("Find Existing Step", 
                    $"Found {entities.Count} NameBuilder steps for {entityLogicalName}/{messageName}. Using the first one. Consider cleaning up duplicate steps.");
            }
            
            var entity = entities.FirstOrDefault();
            return entity == null ? null : BuildPluginStepInfoFromEntity(entity, messageName, filterId);
        }

        private PluginStepInfo CreateStep(Guid pluginTypeId, string entityLogicalName, string entityDisplayName, Guid messageId,
            string messageName, string json, string attributeCsv)
        {
            var filterId = GetSdkMessageFilterId(messageId, entityLogicalName);
            var friendlyName = string.IsNullOrWhiteSpace(entityDisplayName) ? entityLogicalName : entityDisplayName;
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                friendlyName = "Entity";
            }

            var schemaName = string.IsNullOrWhiteSpace(entityLogicalName) ? friendlyName : entityLogicalName;

            var stepName = string.IsNullOrWhiteSpace(schemaName)
                ? $"NameBuilder - {friendlyName} - {messageName}"
                : $"NameBuilder - {friendlyName} ({schemaName}) - {messageName}";

            var step = new Entity("sdkmessageprocessingstep")
            {
                ["name"] = stepName,
                ["sdkmessageid"] = new EntityReference("sdkmessage", messageId),
                ["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filterId),
                ["eventhandler"] = new EntityReference("plugintype", pluginTypeId),
                ["configuration"] = json,
                ["filteringattributes"] = attributeCsv,
                ["mode"] = new OptionSetValue(0),
                ["stage"] = new OptionSetValue(20),
                ["supporteddeployment"] = new OptionSetValue(0),
                ["rank"] = 1
            };

            var stepId = Service.Create(step);

            return new PluginStepInfo
            {
                StepId = stepId,
                Name = stepName,
                UnsecureConfiguration = json,
                PrimaryEntity = entityLogicalName,
                MessageId = messageId,
                MessageName = messageName,
                MessageFilterId = filterId,
                FilteringAttributes = attributeCsv,
                Stage = 20,
                Mode = 0
            };
        }

        private void UpdateStepConfiguration(Guid stepId, string json, string attributeCsv)
        {
            var entity = new Entity("sdkmessageprocessingstep") { Id = stepId };
            entity["configuration"] = json;
            entity["filteringattributes"] = attributeCsv;
            Service.Update(entity);
        }

        private void EnsurePreImageAttributes(Guid stepId, IReadOnlyCollection<string> requiredAttributes)
        {
            var requiredList = (requiredAttributes ?? Array.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requiredList.Count == 0)
            {
                return;
            }

            var query = new QueryExpression("sdkmessageprocessingstepimage")
            {
                ColumnSet = new ColumnSet("sdkmessageprocessingstepimageid", "attributes", "entityalias", "messagepropertyname"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("sdkmessageprocessingstepid", ConditionOperator.Equal, stepId),
                        new ConditionExpression("imagetype", ConditionOperator.Equal, 0)
                    }
                }
            };

            var existing = Service.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (existing == null)
            {
                CreatePreImage(stepId, requiredList, null, null);
                return;
            }

            var union = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingAttributes = existing.GetAttributeValue<string>("attributes");
            if (!string.IsNullOrWhiteSpace(existingAttributes))
            {
                foreach (var token in existingAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    union.Add(token.Trim());
                }
            }

            foreach (var attr in requiredList)
            {
                union.Add(attr);
            }

            var mergedList = union.OrderBy(a => a).ToList();
            var merged = string.Join(",", mergedList);
            var update = new Entity("sdkmessageprocessingstepimage") { Id = existing.Id };
            update["attributes"] = merged;

            var alias = existing.GetAttributeValue<string>("entityalias");
            var messageProperty = existing.GetAttributeValue<string>("messagepropertyname");

            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = "PreImage";
                update["entityalias"] = alias;
            }

            if (string.IsNullOrWhiteSpace(messageProperty))
            {
                messageProperty = "Target";
                update["messagepropertyname"] = messageProperty;
            }

            try
            {
                Service.Update(update);
            }
            catch (FaultException<OrganizationServiceFault> fault) when (IsStepImageNullReferenceFault(fault))
            {
                Service.Delete("sdkmessageprocessingstepimage", existing.Id);
                CreatePreImage(stepId, mergedList, alias, messageProperty);
            }
        }

        private void CreatePreImage(Guid stepId, IList<string> attributes, string alias, string messageProperty)
        {
            var safeAlias = string.IsNullOrWhiteSpace(alias) ? "PreImage" : alias.Trim();
            var safeProperty = string.IsNullOrWhiteSpace(messageProperty) ? "Target" : messageProperty.Trim();
            var attributeString = attributes != null && attributes.Count > 0
                ? string.Join(",", attributes)
                : null;

            var newImage = new Entity("sdkmessageprocessingstepimage")
            {
                ["name"] = safeAlias,
                ["entityalias"] = safeAlias,
                ["messagepropertyname"] = safeProperty,
                ["imagetype"] = new OptionSetValue(0),
                ["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", stepId),
                ["attributes"] = attributeString
            };

            Service.Create(newImage);
        }

        private bool IsStepImageNullReferenceFault(FaultException<OrganizationServiceFault> fault)
        {
            if (fault == null)
            {
                return false;
            }

            var candidates = new[]
            {
                fault.Detail?.InnerFault?.Message,
                fault.Detail?.Message,
                fault.Message
            };

            foreach (var text in candidates)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (text.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    text.IndexOf("SdkMessageProcessingStepImage", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private PluginStepInfo BuildPluginStepInfoFromEntity(Entity entity, string messageName = null, Guid? filterId = null)
        {
            var messageRef = entity.GetAttributeValue<EntityReference>("sdkmessageid");
            var resolvedFilterId = filterId ?? entity.GetAttributeValue<EntityReference>("sdkmessagefilterid")?.Id;
            var filterDetails = ResolveFilterEntityNames(resolvedFilterId);
            return new PluginStepInfo
            {
                StepId = entity.Id,
                Name = entity.GetAttributeValue<string>("name") ?? "(Unnamed Step)",
                Description = entity.GetAttributeValue<string>("description") ?? string.Empty,
                UnsecureConfiguration = entity.GetAttributeValue<string>("configuration") ?? string.Empty,
                PrimaryEntity = filterDetails.Primary,
                SecondaryEntity = filterDetails.Secondary,
                MessageId = messageRef?.Id,
                MessageName = messageName ?? messageRef?.Name,
                MessageFilterId = resolvedFilterId,
                FilteringAttributes = entity.GetAttributeValue<string>("filteringattributes"),
                Stage = entity.GetAttributeValue<OptionSetValue>("stage")?.Value,
                Mode = entity.GetAttributeValue<OptionSetValue>("mode")?.Value
            };
        }

        private Guid GetSdkMessageId(string messageName)
        {
            if (string.IsNullOrWhiteSpace(messageName))
                throw new ArgumentNullException(nameof(messageName));

            if (sdkMessageCache.TryGetValue(messageName, out var cachedId))
            {
                return cachedId;
            }

            var query = new QueryExpression("sdkmessage")
            {
                ColumnSet = new ColumnSet("sdkmessageid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, messageName)
                    }
                }
            };

            var entity = Service.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (entity == null)
            {
                throw new InvalidOperationException($"Dataverse message '{messageName}' was not found.");
            }

            sdkMessageCache[messageName] = entity.Id;
            return entity.Id;
        }

        private Guid GetSdkMessageFilterId(Guid messageId, string entityLogicalName)
        {
            if (messageId == Guid.Empty)
                throw new ArgumentException("messageId must be provided", nameof(messageId));
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                throw new ArgumentNullException(nameof(entityLogicalName));

            var cacheKey = $"{messageId:D}:{entityLogicalName.ToLowerInvariant()}";
            if (messageFilterCache.TryGetValue(cacheKey, out var cachedId))
            {
                return cachedId;
            }

            var filterQuery = new QueryExpression("sdkmessagefilter")
            {
                ColumnSet = new ColumnSet("sdkmessagefilterid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("sdkmessageid", ConditionOperator.Equal, messageId),
                        new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal, entityLogicalName)
                    }
                }
            };

            var filterEntity = Service.RetrieveMultiple(filterQuery).Entities.FirstOrDefault();
            if (filterEntity == null)
            {
                filterQuery.Criteria.Conditions.RemoveAt(1);
                filterQuery.Criteria.Conditions.Add(new ConditionExpression("secondaryobjecttypecode", ConditionOperator.Equal, entityLogicalName));
                filterEntity = Service.RetrieveMultiple(filterQuery).Entities.FirstOrDefault();
            }

            if (filterEntity == null)
            {
                throw new InvalidOperationException($"Unable to locate a message filter for entity '{entityLogicalName}' and message ID {messageId}.");
            }

            var filterId = filterEntity.Id;
            messageFilterCache[cacheKey] = filterId;
            messageFilterDetailsCache[filterId] = (
                filterEntity.GetAttributeValue<string>("primaryobjecttypecode"),
                filterEntity.GetAttributeValue<string>("secondaryobjecttypecode"));
            return filterId;
        }

        private void ShowPublishError(Exception ex)
        {
            var fault = ExtractFaultException(ex);
            var details = fault != null
                ? BuildDetailedFaultMessage(fault)
                : BuildGenericExceptionMessage(ex);

            if (string.IsNullOrWhiteSpace(details))
            {
                details = "An unexpected error occurred while publishing.";
            }

            statusLabel.Text = "Publish failed.";
            statusLabel.ForeColor = Color.Firebrick;

            var sb = new StringBuilder();
            sb.AppendLine("Publishing configuration failed.");
            sb.AppendLine();
            sb.AppendLine(details);

            MessageBox.Show(this, sb.ToString(), "Publish Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private FaultException<OrganizationServiceFault> ExtractFaultException(Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            if (ex is FaultException<OrganizationServiceFault> directFault)
            {
                return directFault;
            }

            if (ex is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    var nested = ExtractFaultException(inner);
                    if (nested != null)
                    {
                        return nested;
                    }
                }
            }

            return ExtractFaultException(ex.InnerException);
        }

        private string BuildGenericExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new Stack<Exception>();

            if (ex != null)
            {
                stack.Push(ex);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                {
                    continue;
                }

                var text = string.IsNullOrWhiteSpace(current.Message)
                    ? current.GetType().FullName
                    : current.Message.Trim();

                if (!string.IsNullOrWhiteSpace(text) && seen.Add(text))
                {
                    messages.Add(text);
                }

                if (current is AggregateException currentAggregate)
                {
                    foreach (var inner in currentAggregate.InnerExceptions)
                    {
                        stack.Push(inner);
                    }
                }
                else if (current.InnerException != null)
                {
                    stack.Push(current.InnerException);
                }
            }

            if (messages.Count == 0)
            {
                messages.Add("An unexpected error occurred while publishing.");
            }

            return string.Join(" | ", messages);
        }

        private string BuildDetailedFaultMessage(FaultException<OrganizationServiceFault> fault)
        {
            if (fault == null)
                return "An unexpected Dataverse error occurred.";

            var detail = fault.Detail;
            var parts = new List<string>();

            var message = detail?.Message ?? fault.Message;
            if (!string.IsNullOrWhiteSpace(message))
            {
                parts.Add(message.Trim());
            }

            if (detail != null)
            {
                if (detail.ErrorCode != 0)
                {
                    parts.Add($"ErrorCode: {detail.ErrorCode}");
                }

                if (detail.Timestamp != DateTime.MinValue)
                {
                    parts.Add($"Timestamp: {detail.Timestamp:O}");
                }

                if (!string.IsNullOrWhiteSpace(detail.TraceText))
                {
                    var traceLines = detail.TraceText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Take(3);
                    if (traceLines.Any())
                    {
                        parts.Add("Trace: " + string.Join(" || ", traceLines));
                    }
                }

                if (detail.InnerFault != null && !string.IsNullOrWhiteSpace(detail.InnerFault.Message))
                {
                    parts.Add($"Inner: {detail.InnerFault.Message.Trim()}");
                }

                var convertDump = ExtractConvertAttributeDump(detail.ErrorDetails);
                if (!string.IsNullOrWhiteSpace(convertDump))
                {
                    parts.Add($"Convert attributes: {convertDump}");
                }

                if (detail.ErrorDetails != null && detail.ErrorDetails.Contains("PluginTrace"))
                {
                    var pluginTrace = detail.ErrorDetails["PluginTrace"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(pluginTrace))
                    {
                        parts.Add("PluginTrace: " + pluginTrace.Trim());
                    }
                }
            }

            if (parts.Count == 0)
            {
                parts.Add("An unexpected Dataverse error occurred.");
            }

            return string.Join(" | ", parts);
        }

        private string ExtractConvertAttributeDump(ErrorDetailCollection errorDetails)
        {
            if (errorDetails == null || errorDetails.Count == 0)
            {
                return null;
            }

            var convertEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in errorDetails)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    continue;
                }

                if (kvp.Key.IndexOf("convert", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                convertEntries[kvp.Key] = kvp.Value?.ToString();
            }

            if (convertEntries.Count == 0)
            {
                return null;
            }

            return JsonConvert.SerializeObject(convertEntries, Formatting.None);
        }

        private void UpdateCachedStepsAfterPublish(PublishResult result)
        {
            if (result?.StepMetadata == null || result.StepMetadata.Count == 0)
                return;

            foreach (var step in result.StepMetadata)
            {
                cachedPluginSteps.RemoveAll(s =>
                    s.StepId == step.StepId ||
                    (!string.IsNullOrWhiteSpace(s.PrimaryEntity) && !string.IsNullOrWhiteSpace(step.PrimaryEntity) &&
                        s.PrimaryEntity.Equals(step.PrimaryEntity, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(s.MessageName) && !string.IsNullOrWhiteSpace(step.MessageName) &&
                        s.MessageName.Equals(step.MessageName, StringComparison.OrdinalIgnoreCase)));

                cachedPluginSteps.Add(step);
            }
        }

        private (string Primary, string Secondary) ResolveFilterEntityNames(Guid? filterId)
        {
            if (!filterId.HasValue || filterId.Value == Guid.Empty)
            {
                return (null, null);
            }

            if (messageFilterDetailsCache.TryGetValue(filterId.Value, out var cached))
            {
                return cached;
            }

            var filter = Service.Retrieve("sdkmessagefilter", filterId.Value,
                new ColumnSet("primaryobjecttypecode", "secondaryobjecttypecode"));

            var details = (
                filter?.GetAttributeValue<string>("primaryobjecttypecode"),
                filter?.GetAttributeValue<string>("secondaryobjecttypecode"));

            messageFilterDetailsCache[filterId.Value] = details;
            return details;
        }

        private string InferTypeFromMetadata(AttributeMetadata meta)
        {
            var type = meta.AttributeType.ToString().ToLowerInvariant();
            switch (type)
            {
                case "string": return "string";
                case "memo": return "string";
                case "datetime": return "datetime";
                case "date": return "date";
                case "boolean": return "boolean";
                case "integer": return "number";
                case "decimal": return "number";
                case "double": return "number";
                case "money": return "currency";
                case "picklist": return "optionset";
                case "state": return "optionset";
                case "status": return "optionset";
                case "lookup": return "lookup";
                default: return "string";
            }
        }

        private class TzOption { public string Label; public int Offset; }
        private List<TzOption> GetTimezoneOptions()
        {
            // Common TZs with offsets relative to UTC
            return new List<TzOption>
            {
                new TzOption{ Label = "UTC (±0)", Offset = 0 },
                new TzOption{ Label = "Pacific (UTC-8)", Offset = -8 },
                new TzOption{ Label = "Mountain (UTC-7)", Offset = -7 },
                new TzOption{ Label = "Central (UTC-6)", Offset = -6 },
                new TzOption{ Label = "Eastern (UTC-5)", Offset = -5 },
                new TzOption{ Label = "London (UTC+0)", Offset = 0 },
                new TzOption{ Label = "CET (UTC+1)", Offset = 1 },
                new TzOption{ Label = "EET (UTC+2)", Offset = 2 },
                new TzOption{ Label = "IST (UTC+5.5)", Offset = 6 },
                new TzOption{ Label = "CST China (UTC+8)", Offset = 8 },
                new TzOption{ Label = "JST (UTC+9)", Offset = 9 },
                new TzOption{ Label = "AEST (UTC+10)", Offset = 10 }
            };
        }

        private void RebuildFieldsPanel()
        {
            fieldsPanel.SuspendLayout();
            fieldsPanel.Controls.Clear();
            
            // Add entity header first if it exists
            if (entityHeaderBlock != null)
            {
                fieldsPanel.Controls.Add(entityHeaderBlock);
            }
            
            // Add field blocks and configure move buttons
            for (int i = 0; i < fieldBlocks.Count; i++)
            {
                var block = fieldBlocks[i];
                fieldsPanel.Controls.Add(block);
                
                // Show/hide move buttons based on position
                bool isFirst = (i == 0);
                bool isLast = (i == fieldBlocks.Count - 1);
                
                // Hide up arrow on first block, down arrow on last block
                block.SetMoveButtonsVisible(showUp: !isFirst, showDown: !isLast);
            }
            
            fieldsPanel.ResumeLayout(true);
            fieldsPanel.PerformLayout();
        }

        private string ComputeLocalPluginAssemblyHash(bool refresh = false)
        {
            if (!refresh && !string.IsNullOrWhiteSpace(cachedLocalPluginHash))
            {
                return cachedLocalPluginHash;
            }

            var path = ResolveLocalPluginAssemblyPath(refresh);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                cachedLocalPluginHash = null;
                return null;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                cachedLocalPluginHash = ComputeSha256Hex(bytes);
            }
            catch
            {
                cachedLocalPluginHash = null;
            }

            return cachedLocalPluginHash;
        }

        private static string ComputeSha256Hex(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static string ComputeSha256HexFromBase64(string base64Content)
        {
            if (string.IsNullOrWhiteSpace(base64Content))
            {
                return null;
            }

            try
            {
                var bytes = Convert.FromBase64String(base64Content);
                return ComputeSha256Hex(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static string FormatHashPreview(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return "unknown";
            }

            var normalized = hash.Trim();
            return normalized.Length <= 12 ? normalized : normalized.Substring(0, 12) + "…";
        }

        private void GenerateJsonAndPreview()
        {
            GenerateJson();
            GeneratePreview();
        }

        private void GenerateJson()
        {
            currentConfig.Entity = currentEntityLogicalName;
            currentConfig.TargetField = string.IsNullOrWhiteSpace(targetFieldTextBox.Text) ? "name" : targetFieldTextBox.Text;
            currentConfig.MaxLength = maxLengthNumeric.Value == 0 ? null : (int?)maxLengthNumeric.Value;
            currentConfig.EnableTracing = enableTracingCheckBox.Checked ? (bool?)true : null;
            
            // Propagate default timezone to blocks using date/datetime if not set
            var defaultTz = PluginUserSettings.Load().DefaultTimezoneOffset;
            currentConfig.Fields = fieldBlocks.Select(b => {
                var cfg = b.Configuration;
                if ((cfg.Type == "date" || cfg.Type == "datetime") && !cfg.TimezoneOffsetHours.HasValue && defaultTz.HasValue)
                    cfg.TimezoneOffsetHours = defaultTz;
                return cfg;
            }).ToList();
            
            if (currentConfig.Fields.Count == 0)
            {
                jsonOutputTextBox.Clear();
                copyJsonToolButton.Enabled = false;
                exportJsonToolButton.Enabled = false;
                return;
            }

            var json = JsonConvert.SerializeObject(currentConfig, Formatting.Indented);
            jsonOutputTextBox.Text = json;
            copyJsonToolButton.Enabled = true;
            exportJsonToolButton.Enabled = true;
        }

        private void GeneratePreview()
        {
            if (sampleRecord == null || fieldBlocks.Count == 0)
            {
                previewTextBox.Text = sampleRecord == null ? 
                    "Select a sample record to see preview" : 
                    "Add field blocks to see preview";
                previewTextBox.BackColor = Color.LightGray;
                return;
            }

            var requiredAttributes = GetRequiredAttributesForPreview();
            sampleRecord = EnsureRecordHasAttributes(sampleRecord, requiredAttributes);

            try
            {
                var parts = new List<string>();
                
                foreach (var block in fieldBlocks)
                {
                    var config = block.Configuration;
                    
                    // Check includeIf condition
                    if (config.IncludeIf != null && !EvaluateCondition(config.IncludeIf, sampleRecord))
                        continue;
                    
                    var value = GetFieldValue(config, sampleRecord);
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Apply prefix/suffix
                        if (!string.IsNullOrEmpty(config.Prefix))
                            value = config.Prefix + value;
                        if (!string.IsNullOrEmpty(config.Suffix))
                            value = value + config.Suffix;
                        
                        parts.Add(value);
                    }
                }
                
                var result = string.Concat(parts);
                
                // Apply max length
                if (currentConfig.MaxLength.HasValue && result.Length > currentConfig.MaxLength.Value)
                {
                    var truncIndicator = fieldBlocks.FirstOrDefault(b => !string.IsNullOrEmpty(b.Configuration.TruncationIndicator))
                        ?.Configuration.TruncationIndicator ?? "...";
                    result = TruncateWithIndicator(result, currentConfig.MaxLength.Value, truncIndicator);
                }
                
                previewTextBox.Text = string.IsNullOrEmpty(result) ? "(empty)" : result;
                previewTextBox.BackColor = Color.LightYellow;
            }
            catch (Exception ex)
            {
                previewTextBox.Text = $"Error generating preview: {ex.Message}";
                previewTextBox.BackColor = Color.LightCoral;
            }
        }

        private string TruncateWithIndicator(string value, int maxLength, string indicator)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (maxLength <= 0)
            {
                return string.Empty;
            }

            var safeIndicator = string.IsNullOrEmpty(indicator) ? "..." : indicator;

            if (maxLength <= safeIndicator.Length)
            {
                return safeIndicator.Length > maxLength ? safeIndicator.Substring(0, maxLength) : safeIndicator;
            }

            var prefixLength = Math.Max(0, Math.Min(value.Length, maxLength - safeIndicator.Length));
            return value.Substring(0, prefixLength) + safeIndicator;
        }

        private bool EvaluateCondition(FieldCondition condition, Entity record)
        {
            if (condition == null)
            {
                return true;
            }

            if (condition.AnyOf != null && condition.AnyOf.Count > 0)
            {
                return condition.AnyOf.Any(c => EvaluateCondition(c, record));
            }

            if (condition.AllOf != null && condition.AllOf.Count > 0)
            {
                return condition.AllOf.All(c => EvaluateCondition(c, record));
            }

            if (string.IsNullOrWhiteSpace(condition.Field))
            {
                return true;
            }

            var context = GetConditionValueContext(record, condition.Field);
            var op = (condition.Operator ?? "equals").ToLowerInvariant();
            var comparisonValue = condition.Value ?? string.Empty;

            switch (op)
            {
                case "equals":
                    return MatchesAnyString(context, comparisonValue, (candidate, target) =>
                        string.Equals(candidate, target, StringComparison.OrdinalIgnoreCase));
                case "notequals":
                    return !MatchesAnyString(context, comparisonValue, (candidate, target) =>
                        string.Equals(candidate, target, StringComparison.OrdinalIgnoreCase));
                case "contains":
                    return MatchesAnyString(context, comparisonValue, (candidate, target) =>
                        candidate.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0);
                case "notcontains":
                    return !MatchesAnyString(context, comparisonValue, (candidate, target) =>
                        candidate.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0);
                case "in":
                    return MatchesInList(context, comparisonValue, negate: false);
                case "notin":
                    return MatchesInList(context, comparisonValue, negate: true);
                case "isempty":
                    return !context.HasValue || context.Candidates.All(string.IsNullOrWhiteSpace);
                case "isnotempty":
                    return context.HasValue && context.Candidates.Any(value => !string.IsNullOrWhiteSpace(value));
                case "greaterthan":
                    return CompareOrdered(context, comparisonValue, OrderedComparison.GreaterThan);
                case "lessthan":
                    return CompareOrdered(context, comparisonValue, OrderedComparison.LessThan);
                case "greaterthanorequal":
                    return CompareOrdered(context, comparisonValue, OrderedComparison.GreaterThanOrEqual);
                case "lessthanorequal":
                    return CompareOrdered(context, comparisonValue, OrderedComparison.LessThanOrEqual);
                default:
                    return true;
            }
        }

        private sealed class ConditionValueContext
        {
            public bool HasValue { get; set; }
            public decimal? NumericValue { get; set; }
            public DateTime? DateValue { get; set; }
            public List<string> Candidates { get; } = new List<string>();
        }

        private enum OrderedComparison
        {
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual
        }

        private ConditionValueContext GetConditionValueContext(Entity record, string fieldName)
        {
            var context = new ConditionValueContext();

            if (record == null || string.IsNullOrWhiteSpace(fieldName) || !record.Contains(fieldName) || record[fieldName] == null)
            {
                return context;
            }

            context.HasValue = true;
            var value = record[fieldName];

            switch (value)
            {
                case string str:
                    AddCandidate(context, str);
                    break;
                case EntityReference entityReference:
                    if (!string.IsNullOrWhiteSpace(entityReference.Name))
                    {
                        AddCandidate(context, entityReference.Name);
                    }
                    AddCandidate(context, entityReference.Id.ToString());
                    break;
                case OptionSetValue optionSet:
                    context.NumericValue = optionSet.Value;
                    if (record.FormattedValues != null && record.FormattedValues.TryGetValue(fieldName, out var optionLabel) && !string.IsNullOrWhiteSpace(optionLabel))
                    {
                        AddCandidate(context, optionLabel);
                    }
                    AddCandidate(context, optionSet.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case Money money:
                    context.NumericValue = money.Value;
                    if (record.FormattedValues != null && record.FormattedValues.TryGetValue(fieldName, out var moneyLabel) && !string.IsNullOrWhiteSpace(moneyLabel))
                    {
                        AddCandidate(context, moneyLabel);
                    }
                    AddCandidate(context, money.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case DateTime dateTime:
                    context.DateValue = dateTime;
                    AddCandidate(context, dateTime.ToString("o"));
                    break;
                case bool flag:
                    context.NumericValue = flag ? 1 : 0;
                    AddCandidate(context, flag ? "true" : "false");
                    break;
                default:
                    if (TryConvertToDecimal(value, out var numericValue))
                    {
                        context.NumericValue = numericValue;
                    }

                    if (value is IFormattable formattable)
                    {
                        AddCandidate(context, formattable.ToString(null, CultureInfo.InvariantCulture));
                    }
                    else if (value != null)
                    {
                        AddCandidate(context, value.ToString());
                    }
                    break;
            }

            if (context.Candidates.Count == 0)
            {
                AddCandidate(context, value?.ToString() ?? string.Empty);
            }

            return context;
        }

        private void AddCandidate(ConditionValueContext context, string candidate)
        {
            if (context == null)
            {
                return;
            }

            context.Candidates.Add(candidate ?? string.Empty);
        }

        private bool MatchesAnyString(ConditionValueContext context, string comparisonValue, Func<string, string, bool> predicate)
        {
            if (context == null || predicate == null)
            {
                return false;
            }

            var target = comparisonValue ?? string.Empty;
            var candidates = context.Candidates.Count > 0 ? context.Candidates : new List<string> { string.Empty };

            foreach (var candidate in candidates)
            {
                var value = candidate ?? string.Empty;
                if (predicate(value, target))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CompareOrdered(ConditionValueContext context, string comparisonValue, OrderedComparison comparison)
        {
            if (context == null)
            {
                return false;
            }

            if (context.NumericValue.HasValue && TryParseDecimal(comparisonValue, out var numericTarget))
            {
                return CompareNumbers(context.NumericValue.Value, numericTarget, comparison);
            }

            if (context.DateValue.HasValue && TryParseDateTime(comparisonValue, out var dateTarget))
            {
                return CompareDates(context.DateValue.Value, dateTarget, comparison);
            }

            var candidate = context.Candidates.FirstOrDefault() ?? string.Empty;
            var target = comparisonValue ?? string.Empty;
            var comparisonResult = string.Compare(candidate, target, StringComparison.OrdinalIgnoreCase);
            return CompareIntegers(comparisonResult, comparisonType: comparison);
        }

        private bool MatchesInList(ConditionValueContext context, string comparisonValue, bool negate)
        {
            if (context == null)
            {
                return false;
            }

            var values = (comparisonValue ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            if (values.Count == 0)
            {
                return false;
            }

            var match = MatchesAnyString(context, null, (candidate, _) =>
                values.Any(v => string.Equals(candidate, v, StringComparison.OrdinalIgnoreCase)));

            return negate ? !match : match;
        }

        private bool CompareNumbers(decimal candidate, decimal target, OrderedComparison comparison)
        {
            switch (comparison)
            {
                case OrderedComparison.GreaterThan:
                    return candidate > target;
                case OrderedComparison.GreaterThanOrEqual:
                    return candidate >= target;
                case OrderedComparison.LessThan:
                    return candidate < target;
                case OrderedComparison.LessThanOrEqual:
                    return candidate <= target;
                default:
                    return false;
            }
        }

        private bool CompareDates(DateTime candidate, DateTime target, OrderedComparison comparison)
        {
            switch (comparison)
            {
                case OrderedComparison.GreaterThan:
                    return candidate > target;
                case OrderedComparison.GreaterThanOrEqual:
                    return candidate >= target;
                case OrderedComparison.LessThan:
                    return candidate < target;
                case OrderedComparison.LessThanOrEqual:
                    return candidate <= target;
                default:
                    return false;
            }
        }

        private bool CompareIntegers(int compareResult, OrderedComparison comparisonType)
        {
            switch (comparisonType)
            {
                case OrderedComparison.GreaterThan:
                    return compareResult > 0;
                case OrderedComparison.GreaterThanOrEqual:
                    return compareResult >= 0;
                case OrderedComparison.LessThan:
                    return compareResult < 0;
                case OrderedComparison.LessThanOrEqual:
                    return compareResult <= 0;
                default:
                    return false;
            }
        }

        private bool TryConvertToDecimal(object value, out decimal numericValue)
        {
            switch (value)
            {
                case byte b:
                    numericValue = b;
                    return true;
                case short s:
                    numericValue = s;
                    return true;
                case int i:
                    numericValue = i;
                    return true;
                case long l:
                    numericValue = l;
                    return true;
                case float f:
                    numericValue = (decimal)f;
                    return true;
                case double d:
                    numericValue = (decimal)d;
                    return true;
                case decimal dec:
                    numericValue = dec;
                    return true;
                default:
                    if (value is IConvertible convertible)
                    {
                        try
                        {
                            numericValue = convertible.ToDecimal(CultureInfo.InvariantCulture);
                            return true;
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    numericValue = 0;
                    return false;
            }
        }

        private bool TryParseDecimal(string value, out decimal numericValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                numericValue = 0;
                return false;
            }

            return decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out numericValue);
        }

        private bool TryParseDateTime(string value, out DateTime dateValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                dateValue = default;
                return false;
            }

            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dateValue);
        }

        private string GetFieldValue(FieldConfiguration config, Entity record)
        {
            var fieldName = config.Field;
            
            if (!record.Contains(fieldName) || record[fieldName] == null)
            {
                // Try alternate field
                if (config.AlternateField != null)
                    return GetFieldValue(config.AlternateField, record);
                
                // Use default value
                return config.Default ?? "";
            }
            
            var value = record[fieldName];
            var valueStr = "";
            
            // Format based on type (infer if needed)
            if (string.IsNullOrEmpty(config.Type))
            {
                var meta = allAttributes.FirstOrDefault(a => a.LogicalName == fieldName);
                if (meta != null) config.Type = InferTypeFromMetadata(meta);
            }
            
            // Format based on type
            if (value is EntityReference entityRef)
            {
                valueStr = entityRef.Name ?? entityRef.Id.ToString();
            }
            else if (value is OptionSetValue optionSet)
            {
                // Try to get the formatted value (label) instead of the integer
                if (record.FormattedValues.Contains(fieldName))
                {
                    valueStr = record.FormattedValues[fieldName];
                }
                else
                {
                    valueStr = optionSet.Value.ToString();
                }
            }
            else if (value is Money money)
            {
                valueStr = FormatMoneyValue(fieldName, money, record, config);
            }
            else if (value is DateTime dateTime)
            {
                if (!string.IsNullOrEmpty(config.Format))
                {
                    try
                    {
                        if (config.TimezoneOffsetHours.HasValue)
                            dateTime = dateTime.AddHours(config.TimezoneOffsetHours.Value);
                        valueStr = dateTime.ToString(config.Format);
                    }
                    catch { valueStr = dateTime.ToString(); }
                }
                else
                {
                    valueStr = dateTime.ToString();
                }
            }
            else if (value is decimal || value is double || value is float || value is int || value is long || value is short)
            {
                var numValue = Convert.ToDecimal(value);
                valueStr = FormatNumericValue(numValue, config.Format);
            }
            else
            {
                valueStr = value.ToString();
            }
            
            // Apply max length and truncation
            if (config.MaxLength.HasValue && valueStr.Length > config.MaxLength.Value)
            {
                var truncIndicator = config.TruncationIndicator ?? "...";
                valueStr = TruncateWithIndicator(valueStr, config.MaxLength.Value, truncIndicator);
            }
            
            return valueStr;
        }

        private string FormatMoneyValue(string fieldName, Money money, Entity record, FieldConfiguration config)
        {
            if (money == null)
            {
                return string.Empty;
            }

            string formattedValue = null;
            string formattedFromRecord = null;
            var hasFormattedValue = false;
            if (record?.FormattedValues != null && !string.IsNullOrWhiteSpace(fieldName))
            {
                hasFormattedValue = record.FormattedValues.TryGetValue(fieldName, out formattedFromRecord);
            }

            if (!string.IsNullOrWhiteSpace(config?.Format))
            {
                formattedValue = FormatNumericValue(money.Value, config.Format);
            }
            else if (hasFormattedValue)
            {
                formattedValue = formattedFromRecord;
            }

            if (string.IsNullOrWhiteSpace(formattedValue))
            {
                formattedValue = money.Value.ToString("N2");
            }

            var symbol = GetCurrencySymbol(record);
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                formattedValue = ApplyCurrencySymbol(formattedValue, symbol);
            }

            return formattedValue;
        }

        private string ApplyCurrencySymbol(string formattedValue, string symbol)
        {
            if (string.IsNullOrWhiteSpace(formattedValue) || string.IsNullOrWhiteSpace(symbol))
            {
                return formattedValue;
            }

            var trimmed = formattedValue.Trim();

            if (trimmed.StartsWith(symbol, StringComparison.Ordinal))
            {
                return trimmed;
            }

            if (trimmed.IndexOf(symbol, StringComparison.Ordinal) >= 0)
            {
                return trimmed;
            }

            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                return "-" + symbol + trimmed.Substring(1);
            }

            if (trimmed.StartsWith("(", StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                var inner = trimmed.Substring(1, trimmed.Length - 2);
                return "(" + ApplyCurrencySymbol(inner, symbol) + ")";
            }

            return symbol + trimmed;
        }

        private string FormatNumericValue(decimal numericValue, string format)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    return numericValue.ToString();
                }

                var scaleInfo = DetectScaleFormat(format);
                if (scaleInfo != null)
                {
                    var scaledValue = numericValue / scaleInfo.Divisor;
                    var numericFormat = string.IsNullOrWhiteSpace(scaleInfo.TrimmedFormat) ? "0.##" : scaleInfo.TrimmedFormat;
                    return scaledValue.ToString(numericFormat) + scaleInfo.Suffix;
                }

                return numericValue.ToString(format);
            }
            catch
            {
                return numericValue.ToString();
            }
        }

        private ScaleFormatInfo DetectScaleFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                return null;
            }

            foreach (var token in new[] { 'B', 'M', 'K' })
            {
                var tokenString = token.ToString();
                var index = format.IndexOf(tokenString, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    var divisor = token == 'B' ? 1000000000m : token == 'M' ? 1000000m : 1000m;
                    var suffixChar = format[index].ToString();
                    var trimmedFormat = format.Remove(index, 1);
                    return new ScaleFormatInfo
                    {
                        Divisor = divisor,
                        Suffix = suffixChar,
                        TrimmedFormat = trimmedFormat
                    };
                }
            }

            return null;
        }

        private string GetCurrencySymbol(Entity record)
        {
            if (record == null)
            {
                return null;
            }

            var currencyRef = record.GetAttributeValue<EntityReference>("transactioncurrencyid");
            if (currencyRef == null || currencyRef.Id == Guid.Empty)
            {
                return null;
            }

            if (currencySymbolCache.TryGetValue(currencyRef.Id, out var cached))
            {
                return cached;
            }

            try
            {
                var currency = Service?.Retrieve("transactioncurrency", currencyRef.Id, new ColumnSet("currencysymbol", "isocurrencycode"));
                var symbol = currency?.GetAttributeValue<string>("currencysymbol");
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    symbol = currency?.GetAttributeValue<string>("isocurrencycode");
                }

                currencySymbolCache[currencyRef.Id] = symbol;
                return symbol;
            }
            catch
            {
                currencySymbolCache[currencyRef.Id] = null;
                return null;
            }
        }

        private void CopyJsonButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(jsonOutputTextBox.Text))
            {
                Clipboard.SetText(jsonOutputTextBox.Text);
                MessageBox.Show("JSON copied to clipboard!", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportJsonButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(jsonOutputTextBox.Text)) return;

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                saveDialog.DefaultExt = "json";
                
                var selectedEntity = (EntityItem)entityDropdown.SelectedItem;
                if (selectedEntity != null)
                    saveDialog.FileName = $"{selectedEntity.LogicalName}_nameconfig.json";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.WriteAllText(saveDialog.FileName, jsonOutputTextBox.Text, Encoding.UTF8);
                        MessageBox.Show($"Configuration exported to:\n{saveDialog.FileName}", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting file: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ImportJsonToolButton_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openDialog.Multiselect = false;

                if (openDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var config = JsonConvert.DeserializeObject<PluginConfiguration>(json);
                    if (config == null)
                    {
                        throw new InvalidOperationException("The selected file did not contain a valid configuration.");
                    }

                    BeginApplyingConfiguration(config, null);
                    statusLabel.Text = $"Configuration imported from {Path.GetFileName(openDialog.FileName)}";
                    statusLabel.ForeColor = Color.MediumBlue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to import configuration: {ex.Message}", "Import Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class SolutionComponentLoadResult
        {
            public Guid SolutionId { get; set; }
            public HashSet<Guid> EntityMetadataIds { get; set; } = new HashSet<Guid>();
        }

        private class ScaleFormatInfo
        {
            public decimal Divisor { get; set; }
            public string Suffix { get; set; }
            public string TrimmedFormat { get; set; }
        }

        private class PublishContext
        {
            public Guid PluginTypeId { get; set; }
            public string PluginTypeName { get; set; }
            public string EntityLogicalName { get; set; }
            public string EntityDisplayName { get; set; }
            public string JsonPayload { get; set; }
            public List<string> AttributeNames { get; set; }
            public bool PublishInsert { get; set; }
            public bool PublishUpdate { get; set; }
            public Guid? SolutionId { get; set; }
        }

        private class PublishResult
        {
            public List<string> UpdatedSteps { get; } = new List<string>();
            public List<PluginStepInfo> StepMetadata { get; } = new List<PluginStepInfo>();
        }

        private class AutoLoadResult
        {
            public string EntityLogicalName { get; set; }
            public PluginStepInfo Step { get; set; }
            public Exception Error { get; set; }
        }

        private class EntityItem
        {
            public string DisplayName { get; set; }
            public string LogicalName { get; set; }
            public EntityMetadata Metadata { get; set; }

            public override string ToString() => DisplayName;
        }

        private class AttributeItem
        {
            public string DisplayName { get; set; }
            public string LogicalName { get; set; }
            public AttributeMetadata Metadata { get; set; }

            public override string ToString() => DisplayName;
        }

        private class ViewItem
        {
            public string Name { get; set; }
            public Guid ViewId { get; set; }
            public Entity View { get; set; }
            public bool IsPersonal { get; set; }
            public bool IsSeparator { get; set; }

            public override string ToString() => Name;
        }

        private class RecordItem
        {
            public string DisplayName { get; set; }
            public Entity Record { get; set; }
            public bool HasAllAttributes { get; set; }

            public override string ToString() => DisplayName;
        }

        private class PluginPresenceCheckResult
        {
            public bool IsInstalled { get; set; }
            public string Message { get; set; }
            public PluginTypeInfo ResolvedPluginType { get; set; }
            public string InstalledVersion { get; set; }
            public Guid? PluginAssemblyId { get; set; }
            public string AssemblyName { get; set; }
            public DateTime? LastUpdatedOn { get; set; }
            public List<PluginTypeInfo> RegisteredPluginTypes { get; set; } = new List<PluginTypeInfo>();
            public string InstalledHash { get; set; }
        }
    }
}

// Helper settings persistence
class PluginUserSettings
{
    public double LeftPanelProportion { get; set; } = 0.30;
    public double RightPanelProportion { get; set; } = 0.35;
    public int PreviewHeight { get; set; } = 60;
    public int? DefaultTimezoneOffset { get; set; } = null;
    public string DefaultPrefix { get; set; } = null;
    public string DefaultSuffix { get; set; } = null;
    public string DefaultNumberFormat { get; set; } = null;
    public string DefaultDateFormat { get; set; } = null;
    public Dictionary<string, ConnectionPreference> ConnectionPreferences { get; set; } = new Dictionary<string, ConnectionPreference>(StringComparer.OrdinalIgnoreCase);

    private static string SettingsPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NameBuilderConfigurator", "settings.json");
    private static PluginUserSettings cachedInstance;
    private static readonly object settingsLock = new object();

    public static PluginUserSettings Load(bool forceReload = false)
    {
        if (!forceReload && cachedInstance != null)
        {
            return cachedInstance;
        }

        lock (settingsLock)
        {
            if (!forceReload && cachedInstance != null)
            {
                return cachedInstance;
            }

            var path = SettingsPath;
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

            try
            {
                if (!System.IO.File.Exists(path))
                {
                    cachedInstance = CreateFirstRunDefaults();
                    cachedInstance.Save();
                }
                else
                {
                    var json = System.IO.File.ReadAllText(path);
                    cachedInstance = JsonConvert.DeserializeObject<PluginUserSettings>(json) ?? new PluginUserSettings();
                }
            }
            catch
            {
                cachedInstance = CreateFirstRunDefaults();
            }

            cachedInstance.ConnectionPreferences = NormalizePreferences(cachedInstance.ConnectionPreferences);

            return cachedInstance;
        }
    }

    public void Save()
    {
        lock (settingsLock)
        {
            try
            {
                var path = SettingsPath;
                var dir = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                System.IO.File.WriteAllText(path, json, Encoding.UTF8);
                cachedInstance = this;
            }
            catch
            {
                // Ignore persistence errors but keep existing cached instance
            }
        }
    }

    private static PluginUserSettings CreateFirstRunDefaults()
    {
        return new PluginUserSettings
        {
            DefaultSuffix = " | ",
            DefaultTimezoneOffset = 0,
            DefaultNumberFormat = "#,###.0",
            DefaultDateFormat = "yyyy.MM.dd",
            ConnectionPreferences = new Dictionary<string, ConnectionPreference>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static Dictionary<string, ConnectionPreference> NormalizePreferences(Dictionary<string, ConnectionPreference> source)
    {
        return source == null
            ? new Dictionary<string, ConnectionPreference>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, ConnectionPreference>(source, StringComparer.OrdinalIgnoreCase);
    }

    public ConnectionPreference GetConnectionPreference(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || ConnectionPreferences == null)
        {
            return null;
        }

        ConnectionPreferences.TryGetValue(key, out var preference);
        return preference;
    }

    public ConnectionPreference GetOrCreateConnectionPreference(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (ConnectionPreferences == null)
        {
            ConnectionPreferences = new Dictionary<string, ConnectionPreference>(StringComparer.OrdinalIgnoreCase);
        }

        if (!ConnectionPreferences.TryGetValue(key, out var preference) || preference == null)
        {
            preference = new ConnectionPreference();
            ConnectionPreferences[key] = preference;
        }

        return preference;
    }
}

class ConnectionPreference
{
    public Guid? LastSolutionId { get; set; }
    public string LastSolutionUniqueName { get; set; }
    public string LastEntityLogicalName { get; set; }
    public Guid? LastViewId { get; set; }
    public Guid? PluginSolutionId { get; set; }
    public string PluginSolutionUniqueName { get; set; }
}


