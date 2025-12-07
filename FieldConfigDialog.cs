using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk.Metadata;

namespace NameBuilderConfigurator
{
    /// <summary>
    /// Dialog for editing field configuration details
    /// </summary>
    public class FieldConfigDialog : Form
    {
        private FieldConfiguration config;
        private AttributeMetadata attrMetadata;
        
        private TextBox fieldTextBox;
        private ComboBox typeComboBox;
        private TextBox formatTextBox;
        private NumericUpDown maxLengthNumeric;
        private TextBox truncationTextBox;
        private TextBox prefixTextBox;
        private TextBox suffixTextBox;
        private NumericUpDown timezoneOffsetNumeric;
        private Button alternateFieldButton;
        private Button conditionButton;
        private Button okButton;
        private Button cancelButton;
        
        private Label formatExampleLabel;
        private readonly ToolTip helpToolTip;
        
        public FieldConfiguration Result { get; private set; }
        
        public FieldConfigDialog(FieldConfiguration configuration, AttributeMetadata metadata = null)
        {
            config = new FieldConfiguration
            {
                Field = configuration.Field,
                Type = configuration.Type,
                Format = configuration.Format,
                MaxLength = configuration.MaxLength,
                TruncationIndicator = configuration.TruncationIndicator ?? "...",
                Default = configuration.Default,
                Prefix = configuration.Prefix,
                Suffix = configuration.Suffix,
                TimezoneOffsetHours = configuration.TimezoneOffsetHours,
                AlternateField = configuration.AlternateField,
                IncludeIf = configuration.IncludeIf
            };
            
            attrMetadata = metadata;
            
            helpToolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };
            
            InitializeComponent();
            LoadConfiguration();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Field Configuration";
            this.Size = new Size(500, 650);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            
            int y = 15;
            int labelWidth = 130;
            int controlX = labelWidth + 20;
            int controlWidth = 310;
            int halfWidth = 150;
            
            // Field Name
            AddLabel("Field Name:", 15, y);
            fieldTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23),
                ReadOnly = true,
                BackColor = SystemColors.Control
            };
            this.Controls.Add(fieldTextBox);
            y += 35;
            
            // Type
            AddLabel("Field Type:", 15, y);
            typeComboBox = new ComboBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            typeComboBox.Items.AddRange(new object[] {
                "(auto-detect)", "string", "lookup", "date", "datetime", 
                "optionset", "picklist", "number", "currency", "boolean"
            });
            typeComboBox.SelectedIndexChanged += TypeComboBox_SelectedIndexChanged;
            helpToolTip.SetToolTip(typeComboBox, "Data type of the field. Auto-detect infers from metadata; override if needed. Affects format examples and timezone visibility.");
            this.Controls.Add(typeComboBox);
            y += 35;
            
            // Format
            AddLabel("Format:", 15, y);
            formatTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23)
            };
            helpToolTip.SetToolTip(formatTextBox, "Format string for dates (.NET format) or numbers (e.g., #,##0.00). Leave blank to use field value as-is.");
            this.Controls.Add(formatTextBox);
            y += 25;
            
            formatExampleLabel = new Label
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 35),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F),
                Text = "Date: yyyy-MM-dd | Number: #,##0.00 | Scaling: 0.0K, 0.00M, 0B"
            };
            this.Controls.Add(formatExampleLabel);
            y += 45;
            
            // Prefix
            AddLabel("Prefix:", 15, y);
            prefixTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(halfWidth, 23)
            };
            helpToolTip.SetToolTip(prefixTextBox, "Static text prepended before the field value. Example: 'INV-' becomes 'INV-12345'.");
            this.Controls.Add(prefixTextBox);
            y += 35;
            
            // Suffix
            AddLabel("Suffix:", 15, y);
            suffixTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23)
            };
            helpToolTip.SetToolTip(suffixTextBox, "Static text appended after the field value. Example: ' Inc' becomes 'Company Inc'.");
            this.Controls.Add(suffixTextBox);
            y += 35;
            
            // Max Length
            AddLabel("Max Length:", 15, y);
            maxLengthNumeric = new NumericUpDown
            {
                Location = new Point(controlX, y),
                Size = new Size(100, 23),
                Minimum = 0,
                Maximum = 10000,
                Value = 0
            };
            helpToolTip.SetToolTip(maxLengthNumeric, "Maximum length for this field. Set to 0 for unlimited. Truncation is applied after prefix/suffix.");
            var clearMaxButton = new Button
            {
                Text = "Clear",
                Location = new Point(controlX + 110, y),
                Size = new Size(60, 23)
            };
            clearMaxButton.Click += (s, e) => maxLengthNumeric.Value = 0;
            helpToolTip.SetToolTip(clearMaxButton, "Reset max length to unlimited.");
            this.Controls.Add(maxLengthNumeric);
            this.Controls.Add(clearMaxButton);
            y += 35;
            
            // Truncation Indicator
            AddLabel("Truncation Indicator:", 15, y);
            truncationTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23),
                Text = "..."
            };
            helpToolTip.SetToolTip(truncationTextBox, "Indicator appended when truncation occurs. Default is '...'. Example: 'Very long text...' if max length is exceeded.");
            this.Controls.Add(truncationTextBox);
            y += 35;
            
            // Timezone Offset
            AddLabel("Timezone Offset (hrs):", 15, y);
            timezoneOffsetNumeric = new NumericUpDown
            {
                Location = new Point(controlX, y),
                Size = new Size(100, 23),
                Minimum = -12,
                Maximum = 14,
                Value = 0
            };
            helpToolTip.SetToolTip(timezoneOffsetNumeric, "Hours to add/subtract for date/time conversion (UTC offset). Only applies to date/datetime fields. Example: -5 for EST.");
            var clearTzButton = new Button
            {
                Text = "Clear",
                Location = new Point(controlX + 110, y),
                Size = new Size(60, 23)
            };
            clearTzButton.Click += (s, e) => timezoneOffsetNumeric.Value = 0;
            helpToolTip.SetToolTip(clearTzButton, "Reset timezone offset to 0 (UTC).");
            this.Controls.Add(timezoneOffsetNumeric);
            this.Controls.Add(clearTzButton);
            y += 35;
            
            // Default/Alternate Button
            alternateFieldButton = new Button
            {
                Text = (config.AlternateField != null || !string.IsNullOrWhiteSpace(config.Default))
                    ? "Edit Default if blank..."
                    : "Default if blank...",
                Location = new Point(15, y),
                Size = new Size(200, 30)
            };
            alternateFieldButton.Click += AlternateFieldButton_Click;
            helpToolTip.SetToolTip(alternateFieldButton, "Configure fallback behavior: pick an alternate field and/or provide a literal default when this field is empty.");
            this.Controls.Add(alternateFieldButton);
            y += 40;
            
            // Condition Button
            conditionButton = new Button
            {
                Text = config.IncludeIf != null ? "Edit Condition..." : "Add Condition (includeIf)...",
                Location = new Point(15, y),
                Size = new Size(200, 30)
            };
            conditionButton.Click += ConditionButton_Click;
            helpToolTip.SetToolTip(conditionButton, "Add conditional logic to include this field only when specified criteria are met (e.g., status equals 'active').");
            this.Controls.Add(conditionButton);
            y += 50;
            
            // OK and Cancel buttons
            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(300, y),
                Size = new Size(80, 30)
            };
            okButton.Click += OkButton_Click;
            
            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(390, y),
                Size = new Size(80, 30)
            };
            
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void AddLabel(string text, int x, int y)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(x, y + 3),
                Size = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            this.Controls.Add(label);
        }
        
        private void LoadConfiguration()
        {
            fieldTextBox.Text = config.Field;
            
            if (string.IsNullOrEmpty(config.Type))
                typeComboBox.SelectedIndex = 0;
            else
                typeComboBox.SelectedItem = config.Type;
            
            formatTextBox.Text = config.Format ?? "";
            maxLengthNumeric.Value = config.MaxLength ?? 0;
            truncationTextBox.Text = config.TruncationIndicator ?? "...";
            prefixTextBox.Text = config.Prefix ?? "";
            suffixTextBox.Text = config.Suffix ?? "";
            timezoneOffsetNumeric.Value = config.TimezoneOffsetHours ?? 0;
        }
        
        private void TypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var type = typeComboBox.SelectedItem?.ToString();
            
            if (type == "date" || type == "datetime")
            {
                formatExampleLabel.Text = "Examples: yyyy-MM-dd, MM/dd/yyyy, yyyy-MM-dd HH:mm";
            }
            else if (type == "number" || type == "currency")
            {
                formatExampleLabel.Text = "Examples: #,##0.00 (1,234.56), 0.0K (1.2K), 0.00M (1.23M), 0B (2B)";
            }
            else
            {
                formatExampleLabel.Text = "Date: yyyy-MM-dd | Number: #,##0.00 | Scaling: 0.0K, 0.00M, 0B";
            }
        }
        
        private void AlternateFieldButton_Click(object sender, EventArgs e)
        {
            var altConfig = config.AlternateField ?? new FieldConfiguration();
            
            using (var dialog = new AlternateFieldDialog(altConfig, config.Default))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    config.AlternateField = dialog.Result;
                    config.Default = dialog.DefaultValue;
                    alternateFieldButton.Text = (config.AlternateField != null || !string.IsNullOrWhiteSpace(config.Default))
                        ? "Edit Default if blank..."
                        : "Default if blank...";
                }
            }
        }
        
        private void ConditionButton_Click(object sender, EventArgs e)
        {
            var condition = config.IncludeIf ?? new FieldCondition();
            
            var attributeList = attrMetadata != null ? new[] { attrMetadata } : null;
            using (var dialog = new ConditionDialog(condition, attributeList, config.Field))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    config.IncludeIf = dialog.Result;
                    conditionButton.Text = config.IncludeIf != null ?
                        "Edit Condition..." : "Add Condition (includeIf)...";
                }
            }
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            config.Type = typeComboBox.SelectedIndex == 0 ? null : typeComboBox.SelectedItem?.ToString();
            config.Format = string.IsNullOrWhiteSpace(formatTextBox.Text) ? null : formatTextBox.Text;
            config.MaxLength = maxLengthNumeric.Value == 0 ? null : (int?)maxLengthNumeric.Value;
            config.TruncationIndicator = string.IsNullOrWhiteSpace(truncationTextBox.Text) ? null : truncationTextBox.Text;
            config.Prefix = string.IsNullOrWhiteSpace(prefixTextBox.Text) ? null : prefixTextBox.Text;
            config.Suffix = string.IsNullOrWhiteSpace(suffixTextBox.Text) ? null : suffixTextBox.Text;
            config.TimezoneOffsetHours = timezoneOffsetNumeric.Value == 0 ? null : (int?)timezoneOffsetNumeric.Value;
            
            Result = config;
        }
    }
}
