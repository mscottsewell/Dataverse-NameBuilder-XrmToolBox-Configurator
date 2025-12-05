using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk.Metadata;

namespace NameBuilderConfigurator
{
    /// <summary>
    /// Represents a single field configuration block in the visual builder
    /// </summary>
    public class FieldBlockControl : UserControl
    {
        private Label fieldLabel;
        private Label typeLabel;
        private Button deleteButton;
        private Panel dragIndicator;
        private Panel dragHandle;
        private Button upButton;
        private Button downButton;
        
        public FieldConfiguration Configuration { get; set; }
        public AttributeMetadata AttributeMetadata { get; set; }
        public bool ShowDragHandle { get; set; } = true;
        
        public event EventHandler EditClicked;
        public event EventHandler DeleteClicked;
        public event EventHandler DragHandleClicked;
        public event EventHandler MoveUpClicked;
        public event EventHandler MoveDownClicked;
        
        public FieldBlockControl(FieldConfiguration config, AttributeMetadata attrMetadata = null)
        {
            Configuration = config;
            AttributeMetadata = attrMetadata;
            
            InitializeComponent();
            UpdateDisplay();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.Height = 60;
            this.Width = 500; // Default width, will be resized by parent
            this.MinimumSize = new Size(200, 60);
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.LightYellow;
            this.Margin = new Padding(3);
            this.Padding = new Padding(5);
            this.Cursor = Cursors.Hand;
            this.Visible = true;
            // Note: Anchoring doesn't work in FlowLayoutPanel, width is set manually by parent
            
            // Make the entire control clickable
            this.Click += (s, e) => EditClicked?.Invoke(this, e);
            
            // Drag handle on the left with up/down buttons (conditionally added)
            if (ShowDragHandle)
            {
                dragHandle = new Panel
                {
                    Location = new Point(0, 0),
                    Width = 30,
                    Height = 60,
                    BackColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand,
                    Name = "DragHandle"
                };
                
                // Up button
                upButton = new Button
                {
                    Text = "▲",
                    Width = 28,
                    Height = 25,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = Color.DarkBlue,
                    Location = new Point(1, 2)
                };
                upButton.Click += (s, e) => 
                {
                    EditClicked?.Invoke(this, e);
                    MoveUpClicked?.Invoke(this, e);
                };
                dragHandle.Controls.Add(upButton);
                
                // Down button
                downButton = new Button
                {
                    Text = "▼",
                    Width = 28,
                    Height = 25,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    ForeColor = Color.DarkBlue,
                    Location = new Point(1, 33)
                };
                downButton.Click += (s, e) => 
                {
                    EditClicked?.Invoke(this, e);
                    MoveDownClicked?.Invoke(this, e);
                };
                dragHandle.Controls.Add(downButton);
                
                dragHandle.Click += (s, e) => DragHandleClicked?.Invoke(this, e);
            }
            
            // Field label
            fieldLabel = new Label
            {
                Location = new Point(ShowDragHandle ? 35 : 10, 10),
                AutoSize = false,
                Width = Math.Max(100, this.ClientSize.Width - (ShowDragHandle ? 80 : 50)),
                Height = 20,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            fieldLabel.Click += (s, e) => EditClicked?.Invoke(this, e);
            
            // Type and details label
            typeLabel = new Label
            {
                Location = new Point(ShowDragHandle ? 35 : 10, 30),
                AutoSize = false,
                Width = Math.Max(100, this.ClientSize.Width - (ShowDragHandle ? 80 : 50)),
                Height = 20,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                AutoEllipsis = true,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            typeLabel.Click += (s, e) => EditClicked?.Invoke(this, e);
            
            // Delete button pinned to right
            deleteButton = new Button
            {
                Text = "✕",
                Width = 30,
                Height = 50,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 12F),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            deleteButton.Left = this.ClientSize.Width - deleteButton.Width - 5;
            deleteButton.Top = 5;
            deleteButton.Click += (s, e) => DeleteClicked?.Invoke(this, e);
            
            // Drag indicator (hidden by default)
            dragIndicator = new Panel
            {
                Height = 4,
                BackColor = Color.DodgerBlue,
                Visible = false,
                Dock = DockStyle.Top
            };
            
            this.Resize += (s, e) => {
                fieldLabel.Width = this.ClientSize.Width - (ShowDragHandle ? 80 : 50);
                typeLabel.Width = this.ClientSize.Width - (ShowDragHandle ? 80 : 50);
                deleteButton.Left = this.ClientSize.Width - deleteButton.Width - 5;
            };
            
            var controls = new System.Collections.Generic.List<Control> { dragIndicator };
            if (dragHandle != null) controls.Add(dragHandle);
            controls.AddRange(new Control[] { fieldLabel, typeLabel, deleteButton });
            this.Controls.AddRange(controls.ToArray());
            
            this.ResumeLayout();
        }
        
        public void ShowDragIndicator(bool show)
        {
            dragIndicator.Visible = show;
        }
        
        public void HighlightDragHandle(bool highlight)
        {
            if (dragHandle != null)
            {
                dragHandle.BackColor = highlight ? Color.RoyalBlue : Color.LightGray;
            }
        }
        
        public void SetMoveButtonsVisible(bool showUp, bool showDown)
        {
            if (upButton != null) upButton.Visible = showUp;
            if (downButton != null) downButton.Visible = showDown;
        }
        
        public void UpdateDisplay()
        {
            if (Configuration == null) return;
            
            // Update field label - show display name (schema name)
            var displayName = AttributeMetadata?.DisplayName?.UserLocalizedLabel?.Label ?? Configuration.Field;
            var displayText = $"{displayName} ({Configuration.Field})";
            if (!string.IsNullOrEmpty(Configuration.Prefix))
                displayText = $"\"{Configuration.Prefix}\" {displayText}";
            if (!string.IsNullOrEmpty(Configuration.Suffix))
                displayText = $"{displayText} \"{Configuration.Suffix}\"";
            fieldLabel.Text = displayText;
            
            // Update type and details
            var details = string.IsNullOrEmpty(Configuration.Type) ? "auto" : Configuration.Type;
            
            if (!string.IsNullOrEmpty(Configuration.Format))
                details += $" | {Configuration.Format}";
            if (Configuration.MaxLength.HasValue)
                details += $" | max: {Configuration.MaxLength}";
            if (!string.IsNullOrEmpty(Configuration.Default))
                details += $" | default: \"{Configuration.Default}\"";
            if (Configuration.AlternateField != null)
                details += $" | alt: {Configuration.AlternateField.Field}";
            if (Configuration.IncludeIf != null)
                details += " | conditional";
            
            typeLabel.Text = details;
        }
    }
}
