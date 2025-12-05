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
        private TextBox defaultTextBox;
        private TextBox prefixTextBox;
        private TextBox suffixTextBox;
        private NumericUpDown timezoneOffsetNumeric;
        private Button alternateFieldButton;
        private Button conditionButton;
        private Button okButton;
        private Button cancelButton;
        
        private Label formatExampleLabel;
        
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
            this.Controls.Add(typeComboBox);
            y += 35;
            
            // Format
            AddLabel("Format:", 15, y);
            formatTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23)
            };
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
            this.Controls.Add(prefixTextBox);
            y += 35;
            
            // Suffix
            AddLabel("Suffix:", 15, y);
            suffixTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23)
            };
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
            var clearMaxButton = new Button
            {
                Text = "Clear",
                Location = new Point(controlX + 110, y),
                Size = new Size(60, 23)
            };
            clearMaxButton.Click += (s, e) => maxLengthNumeric.Value = 0;
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
            this.Controls.Add(truncationTextBox);
            y += 35;
            
            // Default Value
            AddLabel("Default Value:", 15, y);
            defaultTextBox = new TextBox
            {
                Location = new Point(controlX, y),
                Size = new Size(controlWidth, 23)
            };
            this.Controls.Add(defaultTextBox);
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
            var clearTzButton = new Button
            {
                Text = "Clear",
                Location = new Point(controlX + 110, y),
                Size = new Size(60, 23)
            };
            clearTzButton.Click += (s, e) => timezoneOffsetNumeric.Value = 0;
            this.Controls.Add(timezoneOffsetNumeric);
            this.Controls.Add(clearTzButton);
            y += 35;
            
            // Alternate Field Button
            alternateFieldButton = new Button
            {
                Text = config.AlternateField != null ? "Edit Alternate Field..." : "Add Alternate Field...",
                Location = new Point(15, y),
                Size = new Size(200, 30)
            };
            alternateFieldButton.Click += AlternateFieldButton_Click;
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
            defaultTextBox.Text = config.Default ?? "";
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
            
            using (var dialog = new AlternateFieldDialog(altConfig))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    config.AlternateField = dialog.Result;
                    alternateFieldButton.Text = config.AlternateField != null ? 
                        "Edit Alternate Field..." : "Add Alternate Field...";
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
            config.Default = string.IsNullOrWhiteSpace(defaultTextBox.Text) ? null : defaultTextBox.Text;
            config.Prefix = string.IsNullOrWhiteSpace(prefixTextBox.Text) ? null : prefixTextBox.Text;
            config.Suffix = string.IsNullOrWhiteSpace(suffixTextBox.Text) ? null : suffixTextBox.Text;
            config.TimezoneOffsetHours = timezoneOffsetNumeric.Value == 0 ? null : (int?)timezoneOffsetNumeric.Value;
            
            Result = config;
        }
    }
}
