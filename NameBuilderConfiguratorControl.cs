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
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using McTools.Xrm.Connection;

namespace NameBuilderConfigurator
{
    public partial class NameBuilderConfiguratorControl : PluginControlBase
    {
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
        private ToolStripButton publishToolButton;
        private System.Windows.Forms.Label statusLabel;
        private NumericUpDown maxLengthNumeric;
        private CheckBox enableTracingCheckBox;
        private TextBox targetFieldTextBox;
        private Panel propertiesPanel;
        private System.Windows.Forms.Label propertiesTitleLabel;
        private FieldBlockControl selectedBlock = null;
        private FieldBlockControl entityHeaderBlock = null;
        
        private List<EntityMetadata> entities = new List<EntityMetadata>();
        private List<AttributeMetadata> currentAttributes = new List<AttributeMetadata>();
        private List<AttributeMetadata> allAttributes = new List<AttributeMetadata>();
        private List<FieldBlockControl> fieldBlocks = new List<FieldBlockControl>();
        private PluginConfiguration currentConfig = new PluginConfiguration();
        private Entity sampleRecord = null;
        private string currentEntityLogicalName = null;
        private string currentEntityDisplayName = null;
        private string currentPrimaryNameAttribute = null;
        private PluginConfiguration pendingConfigFromPlugin;
        private string pendingConfigTargetEntity;
        private PluginStepInfo pendingConfigSourceStep;
        private PluginStepInfo activeRegistryStep;
        private PluginTypeInfo activePluginType;
        private List<PluginStepInfo> cachedPluginSteps = new List<PluginStepInfo>();
        private bool suppressBlockSelection;
        private readonly Dictionary<string, Guid> sdkMessageCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Guid> messageFilterCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Guid, (string Primary, string Secondary)> messageFilterDetailsCache = new Dictionary<Guid, (string, string)>();
        private readonly Dictionary<Guid, string> currencySymbolCache = new Dictionary<Guid, string>();
        private readonly HashSet<string> publishStepResolutionWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ConnectionDetail lastConnectionDetail;
        private bool pluginPresenceCheckRunning;
        private bool pluginPresenceVerified;
        private static readonly Font SpacePreviewInputFont = new Font("Consolas", 9F);
        private static readonly Font SpacePreviewLabelFont = new Font("Consolas", 8F);

        public NameBuilderConfiguratorControl()
        {
            InitializeComponent();
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (!ReferenceEquals(detail, lastConnectionDetail))
            {
                lastConnectionDetail = detail;
                pluginPresenceVerified = false;
                cachedPluginSteps.Clear();
                activePluginType = null;
                activeRegistryStep = null;
            }

            if (newService != null)
            {
                EnsureNameBuilderPluginPresence();
            }
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
                Text = "Load Entities",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("LoadEntities.png", SystemIcons.Application)
            };
            loadEntitiesToolButton.Click += LoadEntitiesButton_Click;
            ribbon.Items.Add(loadEntitiesToolButton);
            
            retrieveConfigToolButton = new ToolStripButton
            {
                Text = "Retrieve Configuration",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("RetrieveConfiguration.png", SystemIcons.Information)
            };
            retrieveConfigToolButton.Click += RetrieveConfigurationToolButton_Click;
            ribbon.Items.Add(retrieveConfigToolButton);

            ribbon.Items.Add(new ToolStripSeparator());
            
            importJsonToolButton = new ToolStripButton
            {
                Text = "Import JSON",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Image = LoadToolbarIcon("ImportJSON.png", SystemIcons.Shield)
            };
            importJsonToolButton.Click += ImportJsonToolButton_Click;
            ribbon.Items.Add(importJsonToolButton);

            exportJsonToolButton = new ToolStripButton
            {
                Text = "Export JSON",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("ExportJSON.png", SystemIcons.Asterisk)
            };
            exportJsonToolButton.Click += ExportJsonButton_Click;
            ribbon.Items.Add(exportJsonToolButton);

            copyJsonToolButton = new ToolStripButton
            {
                Text = "Copy JSON",
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                Enabled = false,
                Image = LoadToolbarIcon("CopyJSON.png", SystemIcons.Question)
            };
            copyJsonToolButton.Click += CopyJsonButton_Click;
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
            var leftRightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 320
            };

            // Right side is split vertically: top preview and bottom content
            var rightTopBottomSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 22
            };

            // Bottom of right side: split between middle and right panels
            var middleRightSplitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 360
            };
            
            // LEFT PANEL - Entity/View/Sample/Attributes
            var leftPanel = new Panel { 
                Dock = DockStyle.Fill, 
                Padding = new Padding(6)
            };
            
            // Entity
            var entityLabel = new System.Windows.Forms.Label {
                Text = "Entity:",
                Location = new Point(5, 5),
                AutoSize = true
            };
            leftPanel.Controls.Add(entityLabel);
            
            entityDropdown = new ComboBox
            {
                Location = new Point(5, 25),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            entityDropdown.SelectedIndexChanged += EntityDropdown_SelectedIndexChanged;
            leftPanel.Controls.Add(entityDropdown);
            
            // View
            var viewLabel = new System.Windows.Forms.Label {
                Text = "View (optional):",
                Location = new Point(5, 60),
                AutoSize = true
            };
            leftPanel.Controls.Add(viewLabel);
            
            viewDropdown = new ComboBox
            {
                Location = new Point(5, 80),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            viewDropdown.SelectedIndexChanged += ViewDropdown_SelectedIndexChanged;
            leftPanel.Controls.Add(viewDropdown);
            
            // Sample Record
            var sampleLabel = new System.Windows.Forms.Label {
                Text = "Sample Record:",
                Location = new Point(5, 115),
                AutoSize = true
            };
            leftPanel.Controls.Add(sampleLabel);
            
            sampleRecordDropdown = new ComboBox
            {
                Location = new Point(5, 135),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 23,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            sampleRecordDropdown.SelectedIndexChanged += SampleRecordDropdown_SelectedIndexChanged;
            leftPanel.Controls.Add(sampleRecordDropdown);
            
            // Attributes
            var attributeLabel = new System.Windows.Forms.Label {
                Text = "Available Attributes:(double-click to add)",
                Location = new Point(5, 170),
                AutoSize = true
            };
            leftPanel.Controls.Add(attributeLabel);
            
            // Attributes listbox - scales between label above and status below
            attributeListBox = new ListBox
            {
                Location = new Point(5, 190),
                Width = leftPanel.ClientSize.Width - 16,
                Height = 200,
                Enabled = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            attributeListBox.DoubleClick += AttributeListBox_DoubleClick;
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
            leftPanel.Controls.Add(statusLabel);
            
            leftPanel.Resize += (s, e) =>
            {
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

            // Ensure initial splitter positions match the default layout
            this.Load += (s, e) =>
            {
                var settings = PluginUserSettings.Load();
                var totalWidth = mainContainer.ClientSize.Width;
                
                // Default proportions: Left 30%, Middle 35%, Right 35%
                var leftProportion = settings.LeftPanelProportion;
                var rightProportion = settings.RightPanelProportion;
                
                // Calculate left panel width (ensure minimum 280px, maximum 50%)
                var leftWidth = (int)(totalWidth * leftProportion);
                leftRightSplitter.SplitterDistance = Math.Max(280, Math.Min(leftWidth, (int)(totalWidth * 0.5)));
                
                // Calculate right panel splits based on proportions
                var rightWidth = totalWidth - leftRightSplitter.SplitterDistance;
                var rightPanelWidth = (int)(totalWidth * rightProportion);
                var middleWidth = rightWidth - Math.Max(350, rightPanelWidth);
                middleRightSplitter.SplitterDistance = Math.Max(100, middleWidth);
                
                // Preview height (persisted)
                rightTopBottomSplitter.SplitterDistance = Math.Max(20, Math.Min(settings.PreviewHeight, 50));

                // Save proportions on splitter moved
                leftRightSplitter.SplitterMoved += (s2, e2) => {
                    var st = PluginUserSettings.Load();
                    var total = mainContainer.ClientSize.Width;
                    if (total > 0) st.LeftPanelProportion = (double)leftRightSplitter.SplitterDistance / total;
                    st.Save();
                };
                middleRightSplitter.SplitterMoved += (s2, e2) => {
                    var st = PluginUserSettings.Load();
                    var total = mainContainer.ClientSize.Width;
                    if (total > 0)
                    {
                        var rightPanelActualWidth = Math.Max(0, (total - leftRightSplitter.SplitterDistance) - middleRightSplitter.SplitterDistance);
                        st.RightPanelProportion = total > 0 ? (double)rightPanelActualWidth / total : st.RightPanelProportion;
                    }
                    st.Save();
                };
                rightTopBottomSplitter.SplitterMoved += (s2, e2) =>
                {
                    var st = PluginUserSettings.Load();
                    st.PreviewHeight = Math.Max(20, Math.Min(rightTopBottomSplitter.SplitterDistance, 50));
                    st.Save();
                };
                
                // Auto-load entities if already connected
                if (Service != null)
                {
                    ExecuteMethod(LoadEntities);
                    EnsureNameBuilderPluginPresence();
                }
            };
            this.ResumeLayout();
        }

        private void EnsureNameBuilderPluginPresence()
        {
            if (Service == null || pluginPresenceCheckRunning || pluginPresenceVerified)
            {
                return;
            }

            pluginPresenceCheckRunning = true;

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

                    if (args.Error != null)
                    {
                        MessageBox.Show($"Unable to verify the NameBuilder plug-in: {args.Error.Message}",
                            "Plugin Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (args.Result is PluginPresenceCheckResult result)
                    {
                        if (!result.IsInstalled)
                        {
                            pluginPresenceVerified = false;
                            statusLabel.Text = result.Message;
                            statusLabel.ForeColor = Color.Firebrick;
                            MessageBox.Show(this, result.Message, "NameBuilder Plug-in Required",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else
                        {
                            pluginPresenceVerified = true;
                            statusLabel.Text = "NameBuilder plug-in registration verified.";
                            statusLabel.ForeColor = Color.ForestGreen;

                            if (result.ResolvedPluginType != null)
                            {
                                activePluginType = result.ResolvedPluginType;
                            }
                        }
                    }
                }
            });
        }

        private PluginPresenceCheckResult PerformNameBuilderPluginPresenceCheck()
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
                return new PluginPresenceCheckResult
                {
                    IsInstalled = false,
                    Message = "The NameBuilder plug-in assembly is missing in this Dataverse environment. The NameBuilder Plugin must be installed first."
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
                    Message = "No plug-in types were found under the NameBuilder assembly. The NameBuilder Plugin must be installed first."
                };
            }

            return new PluginPresenceCheckResult
            {
                IsInstalled = true,
                ResolvedPluginType = ResolvePluginType(pluginTypes) ?? pluginTypes.FirstOrDefault()
            };
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
            ExecuteMethod(LoadEntities);
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

                var context = new PublishContext
                {
                    PluginTypeId = activePluginType.PluginTypeId,
                    PluginTypeName = activePluginType.Name ?? activePluginType.TypeName,
                    EntityLogicalName = currentEntityLogicalName,
                    EntityDisplayName = entityDisplayName,
                    JsonPayload = json,
                    AttributeNames = attributeSet.ToList(),
                    PublishInsert = dialog.PublishInsert,
                    PublishUpdate = dialog.PublishUpdate
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
            if (string.IsNullOrWhiteSpace(step.UnsecureConfiguration))
            {
                MessageBox.Show("The selected step does not contain an unsecure configuration.", "No Configuration",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PluginConfiguration config;
            try
            {
                config = JsonConvert.DeserializeObject<PluginConfiguration>(step.UnsecureConfiguration);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to parse the step configuration: {ex.Message}", "Invalid Configuration",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (config == null)
            {
                MessageBox.Show("The selected step did not return a valid configuration payload.", "Invalid Configuration",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
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
                entityDropdown.Enabled = true;

                if (entityDropdown.SelectedItem == targetItem)
                {
                    EntityDropdown_SelectedIndexChanged(entityDropdown, EventArgs.Empty);
                }
                else
                {
                    entityDropdown.SelectedItem = targetItem;
                }

                return;
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
                    AddFieldBlock(field, attrMeta, applyDefaults: true);
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

            publishToolButton.Enabled = true;
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
                    entityDropdown.Items.Clear();
                    
                    foreach (var entity in entities)
                    {
                        var displayName = entity.DisplayName?.UserLocalizedLabel?.Label ?? entity.LogicalName;
                        entityDropdown.Items.Add(new EntityItem 
                        { 
                            DisplayName = displayName,
                            LogicalName = entity.LogicalName,
                            Metadata = entity
                        });
                    }
                    
                    entityDropdown.Enabled = true;
                    statusLabel.Text = $"Loaded {entities.Count} entities";
                    statusLabel.ForeColor = Color.Green;
                }
            });
        }

        private void EntityDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (entityDropdown.SelectedItem == null) return;

            var selectedEntity = (EntityItem)entityDropdown.SelectedItem;
            currentEntityLogicalName = selectedEntity.LogicalName;
            currentEntityDisplayName = selectedEntity.DisplayName;
            
            // Clear dependent dropdowns
            viewDropdown.Items.Clear();
            viewDropdown.Enabled = false;
            sampleRecordDropdown.Items.Clear();
            sampleRecordDropdown.Enabled = false;
            sampleRecord = null;
            
            // Clear existing blocks
            fieldsPanel.Controls.Clear();
            fieldBlocks.Clear();
            
            // Add entity header block
            CreateEntityHeaderBlock(selectedEntity.DisplayName, selectedEntity.LogicalName);

            // Don't select header yet - wait until data is loaded
            
            ExecuteMethod(() => LoadViewsAndAttributes(selectedEntity.LogicalName));
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
                    
                    // Load views
                    var viewQuery = new QueryExpression("savedquery")
                    {
                        ColumnSet = new ColumnSet("name", "savedqueryid", "layoutxml", "fetchxml"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("returnedtypecode", ConditionOperator.Equal, entityLogicalName),
                                new ConditionExpression("querytype", ConditionOperator.Equal, 0), // Public views
                                new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                            }
                        },
                        Orders = { new OrderExpression("name", OrderType.Ascending) }
                    };
                    var views = Service.RetrieveMultiple(viewQuery).Entities;
                    
                    args.Result = new { Attributes = attributes, Views = views, PrimaryNameAttribute = primaryNameAttribute, PrimaryNameMaxLength = primaryNameMaxLength };
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
                    
                    // Populate view dropdown
                    viewDropdown.Items.Clear();
                    viewDropdown.Items.Add(new ViewItem { Name = "(All Attributes)", ViewId = Guid.Empty });
                    
                    foreach (Entity view in result.Views)
                    {
                        viewDropdown.Items.Add(new ViewItem
                        {
                            Name = view.GetAttributeValue<string>("name"),
                            ViewId = view.Id,
                            View = view
                        });
                    }
                    
                    viewDropdown.SelectedIndex = 0;
                    viewDropdown.Enabled = true;
                    attributeListBox.Enabled = true;
                    statusLabel.Text = $"Loaded {currentAttributes.Count} attributes, {result.Views.Count} views";

                    TryApplyPendingConfiguration();
                }
            });
        }

        private void ViewDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (viewDropdown.SelectedItem == null) return;
            
            var selectedView = (ViewItem)viewDropdown.SelectedItem;
            
            // Clear sample record
            sampleRecordDropdown.Items.Clear();
            sampleRecordDropdown.Enabled = false;
            sampleRecord = null;
            previewTextBox.Clear();
            
            if (selectedView.ViewId == Guid.Empty)
            {
                // Show all attributes
                currentAttributes = allAttributes;
                ExecuteMethod(() => LoadSampleRecordsForEntity());
            }
            else
            {
                // Filter attributes based on view columns
                ExecuteMethod(() => FilterAttributesByView(selectedView.View));
                return;
            }
            
            // Refresh attribute list
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

                    args.Result = new { Records = records, PrimaryNameAttribute = currentPrimaryNameAttribute, HasAllAttributes = true };
                },
                PostWorkCallBack = HandleSampleRecordsLoaded
            });
        }

        private EntityCollection RetrieveEntitySampleRecords(bool useStateFilter)
        {
            var query = new QueryExpression(currentEntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                TopCount = 10
            };

            if (useStateFilter && EntitySupportsStateCode())
            {
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            }

            return Service.RetrieveMultiple(query);
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
                var displayValue = GetRecordDisplayName(record);
                sampleRecordDropdown.Items.Add(new RecordItem
                {
                    DisplayName = displayValue,
                    Record = record,
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

        private void SampleRecordDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sampleRecordDropdown.SelectedItem == null || !(sampleRecordDropdown.SelectedItem is RecordItem))
            {
                sampleRecord = null;
                previewTextBox.Clear();
                return;
            }
            
            var selectedRecord = (RecordItem)sampleRecordDropdown.SelectedItem;
            if (selectedRecord.HasAllAttributes)
            {
                sampleRecord = selectedRecord.Record;
                GeneratePreview();
                return;
            }

            var recordToRetrieve = selectedRecord.Record;
            if (recordToRetrieve == null || recordToRetrieve.Id == Guid.Empty)
            {
                sampleRecord = recordToRetrieve;
                GeneratePreview();
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Retrieving record details...",
                AsyncArgument = selectedRecord,
                Work = (worker, args) =>
                {
                    var item = (RecordItem)args.Argument;
                    var logicalName = item.Record?.LogicalName ?? currentEntityLogicalName;
                    if (string.IsNullOrWhiteSpace(logicalName))
                    {
                        args.Result = item.Record;
                        return;
                    }

                    var fullRecord = Service.Retrieve(logicalName, item.Record.Id, new ColumnSet(true));
                    args.Result = fullRecord;
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show($"Error retrieving record details: {args.Error.Message}", "Sample Record",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        sampleRecord = selectedRecord.Record;
                        GeneratePreview();
                        return;
                    }

                    var fullRecord = args.Result as Entity;
                    if (fullRecord != null)
                    {
                        selectedRecord.Record = fullRecord;
                        selectedRecord.HasAllAttributes = true;
                        sampleRecord = fullRecord;
                    }
                    else
                    {
                        sampleRecord = selectedRecord.Record;
                    }

                    GeneratePreview();
                }
            });
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
            
            // Target Field
            var targetLabel = new System.Windows.Forms.Label
            {
                Text = "Target Field Name:",
                Location = new Point(10, y),
                AutoSize = true
            };
            var targetTextBox = new TextBox
            {
                Text = targetFieldTextBox.Text,
                Location = new Point(10, y + 20),
                Size = new Size(250, 23)
            };
            targetTextBox.TextChanged += (s, e) => {
                targetFieldTextBox.Text = targetTextBox.Text;
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { targetLabel, targetTextBox });
            y += 55;
            
            // Max Length
            var maxLabel = new System.Windows.Forms.Label
            {
                Text = "Global Max Length (0 = no limit):",
                Location = new Point(10, y),
                AutoSize = true
            };
            var maxNumeric = new NumericUpDown
            {
                Name = "GlobalMaxLengthNumeric",
                Location = new Point(10, y + 20),
                Size = new Size(100, 23),
                Minimum = 0,
                Maximum = 100000,
                Value = maxLengthNumeric.Value
            };
            maxNumeric.ValueChanged += (s, e) => {
                maxLengthNumeric.Value = maxNumeric.Value;
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.AddRange(new Control[] { maxLabel, maxNumeric });
            y += 55;
            
            // Enable Tracing
            var traceCheckBox = new CheckBox
            {
                Text = "Enable Tracing",
                Checked = enableTracingCheckBox.Checked,
                Location = new Point(10, y),
                AutoSize = true
            };
            traceCheckBox.CheckedChanged += (s, e) => {
                enableTracingCheckBox.Checked = traceCheckBox.Checked;
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.Add(traceCheckBox);
            y += 40;
            
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
            int labelWidth = 120;
            int controlX = labelWidth + 10;
            
            // Field Type
            AddPropertyLabel("Field Type:", 10, y);
            var typeCombo = new ComboBox
            {
                Location = new Point(controlX, y),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
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
                    Size = new Size(220, 23),
                    DropDownStyle = ComboBoxStyle.DropDownList
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
                    Size = new Size(200, 23)
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
                    Size = new Size(240, 30),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 7.5F)
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
            
            // Default Value (for all types)
            AddPropertyLabel("Default Value:", 10, y);
            var defaultTextBox = new TextBox
            {
                Text = block.Configuration.Default ?? "",
                Location = new Point(controlX, y),
                Size = new Size(200, 23)
            };
            defaultTextBox.TextChanged += (s, e) => {
                block.Configuration.Default = string.IsNullOrWhiteSpace(defaultTextBox.Text) ? null : defaultTextBox.Text;
                GenerateJsonAndPreview();
            };
            propertiesPanel.Controls.Add(defaultTextBox);
            y += 35;
            
            // Alternate Field Button
            var alternateButton = new Button
            {
                Text = block.Configuration.AlternateField != null ? "Edit Alternate Field" : "Add Alternate Field",
                Location = new Point(10, y),
                Size = new Size(200, 30)
            };
            alternateButton.Click += (s, e) => {
                using (var dialog = new AlternateFieldDialog(block.Configuration.AlternateField))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        block.Configuration.AlternateField = dialog.Result;
                        alternateButton.Text = block.Configuration.AlternateField != null ? "Edit Alternate Field" : "Add Alternate Field";
                        block.UpdateDisplay();
                        GenerateJsonAndPreview();
                    }
                }
            };
            propertiesPanel.Controls.Add(alternateButton);
            y += 40;
            
            // Condition Button
            var conditionButton = new Button
            {
                Text = block.Configuration.IncludeIf != null ? "Edit Condition (includeIf)" : "Add Condition (includeIf)",
                Location = new Point(10, y),
                Size = new Size(200, 30)
            };
            conditionButton.Click += (s, e) => {
                var attributeSource = allAttributes ?? currentAttributes ?? new List<AttributeMetadata>();
                using (var dialog = new ConditionDialog(block.Configuration.IncludeIf, attributeSource, block.Configuration.Field))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        block.Configuration.IncludeIf = dialog.Result;
                        conditionButton.Text = block.Configuration.IncludeIf != null ? "Edit Condition (includeIf)" : "Add Condition (includeIf)";
                        block.UpdateDisplay();
                        GenerateJsonAndPreview();
                    }
                }
            };
            propertiesPanel.Controls.Add(conditionButton);
            
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
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var baseDirectory = Path.GetDirectoryName(assemblyPath);
                if (!string.IsNullOrWhiteSpace(baseDirectory))
                {
                    var iconPath = Path.Combine(baseDirectory, "Assets", "Icon", fileName);
                    if (File.Exists(iconPath))
                    {
                        using (var fs = new FileStream(iconPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        using (var original = Image.FromStream(fs))
                        {
                            return new Bitmap(original, new Size(18, 18));
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors and fall back to default icon.
            }

            return CreateToolbarIcon(fallbackIcon);
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
                }
            }

            if (context.PublishUpdate)
            {
                var updateStep = EnsureStepForMessage(context, "Update", normalizedAttributes, attributeCsv, ensurePreImage: true);
                if (updateStep != null)
                {
                    result.UpdatedSteps.Add(updateStep.Name ?? $"{context.EntityDisplayName} Update");
                    result.StepMetadata.Add(updateStep);
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

            var entity = Service.RetrieveMultiple(query).Entities.FirstOrDefault();
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
                case "isempty":
                    return !context.HasValue || context.Candidates.All(string.IsNullOrWhiteSpace);
                case "isnotempty":
                    return context.HasValue && context.Candidates.Any(value => !string.IsNullOrWhiteSpace(value));
                case "greaterthan":
                    return CompareOrdered(context, comparisonValue, greaterThan: true);
                case "lessthan":
                    return CompareOrdered(context, comparisonValue, greaterThan: false);
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

        private bool CompareOrdered(ConditionValueContext context, string comparisonValue, bool greaterThan)
        {
            if (context == null)
            {
                return false;
            }

            if (context.NumericValue.HasValue && TryParseDecimal(comparisonValue, out var numericTarget))
            {
                return greaterThan ? context.NumericValue.Value > numericTarget : context.NumericValue.Value < numericTarget;
            }

            if (context.DateValue.HasValue && TryParseDateTime(comparisonValue, out var dateTarget))
            {
                return greaterThan ? context.DateValue.Value > dateTarget : context.DateValue.Value < dateTarget;
            }

            var candidate = context.Candidates.FirstOrDefault() ?? string.Empty;
            var target = comparisonValue ?? string.Empty;
            var comparison = string.Compare(candidate, target, StringComparison.OrdinalIgnoreCase);
            return greaterThan ? comparison > 0 : comparison < 0;
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
        }

        private class PublishResult
        {
            public List<string> UpdatedSteps { get; } = new List<string>();
            public List<PluginStepInfo> StepMetadata { get; } = new List<PluginStepInfo>();
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
            DefaultDateFormat = "yyyy.MM.dd"
        };
    }
}


