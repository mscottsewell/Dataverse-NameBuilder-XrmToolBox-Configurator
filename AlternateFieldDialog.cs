using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk.Metadata;

namespace NameBuilderConfigurator
{
    /// <summary>
    /// Simple dialog for configuring an alternate field
    /// </summary>
    public class AlternateFieldDialog : Form
    {
        private ComboBox fieldComboBox;
        private TextBox fieldTextBox;
        private ComboBox typeComboBox;
        private TextBox defaultTextBox;
        private Button okButton;
        private Button cancelButton;
        private Button removeButton;
        private readonly List<AttributeListItem> attributeItems;
        private readonly bool useAttributePicker;
        private readonly string noneDisplayLabel = "(None)";
        
        private sealed class AttributeListItem
        {
            public string LogicalName { get; set; }
            public string DisplayLabel { get; set; }
            public AttributeMetadata Metadata { get; set; }
            public override string ToString() => DisplayLabel ?? LogicalName ?? "(None)";
        }
        
        public FieldConfiguration Result { get; private set; }
        public string DefaultValue { get; private set; }
        
        public AlternateFieldDialog(FieldConfiguration config, string currentDefault, IEnumerable<AttributeMetadata> attributes = null)
        {
            attributeItems = attributes?
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.LogicalName))
                .Select(a => new AttributeListItem
                {
                    LogicalName = a.LogicalName,
                    DisplayLabel = BuildAttributeLabel(a),
                    Metadata = a
                })
                .OrderBy(a => a.DisplayLabel, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            useAttributePicker = attributeItems != null && attributeItems.Count > 0;
            
            InitializeComponent();
            
            ApplyExistingConfiguration(config, currentDefault);
        }
        
        private void InitializeComponent()
        {
            this.Text = "Alternate Field";
            this.Size = new Size(450, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            
            int y = 20;
            
            // Field Name
            var fieldLabel = new Label
            {
                Text = "Alternate Field Name:",
                Location = new Point(20, y + 3),
                Size = new Size(140, 20)
            };
            Control fieldInput;
            if (useAttributePicker)
            {
                fieldComboBox = new ComboBox
                {
                    Location = new Point(170, y - 1),
                    Size = new Size(240, 24),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                PopulateAttributeCombo();
                fieldComboBox.SelectedIndexChanged += FieldComboBox_SelectedIndexChanged;
                fieldInput = fieldComboBox;
            }
            else
            {
                fieldTextBox = new TextBox
                {
                    Location = new Point(170, y),
                    Size = new Size(240, 23)
                };
                fieldInput = fieldTextBox;
            }
            this.Controls.Add(fieldLabel);
            this.Controls.Add(fieldInput);
            y += 35;
            
            // Type
            var typeLabel = new Label
            {
                Text = "Type:",
                Location = new Point(20, y + 3),
                Size = new Size(140, 20)
            };
            typeComboBox = new ComboBox
            {
                Location = new Point(170, y),
                Size = new Size(240, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            typeComboBox.Items.AddRange(new object[] {
                "(auto-detect)", "string", "lookup", "date", "datetime", 
                "optionset", "number", "currency"
            });
            typeComboBox.SelectedIndex = 0;
            this.Controls.Add(typeLabel);
            this.Controls.Add(typeComboBox);
            y += 35;
            
            // Default Value
            var defaultLabel = new Label
            {
                Text = "Default if empty:",
                Location = new Point(20, y + 3),
                Size = new Size(140, 20)
            };
            defaultTextBox = new TextBox
            {
                Location = new Point(170, y),
                Size = new Size(240, 23)
            };
            this.Controls.Add(defaultLabel);
            this.Controls.Add(defaultTextBox);
            y += 50;
            
            // Info label
            var infoLabel = new Label
            {
                Text = useAttributePicker
                    ? "Pick another attribute to use before falling back to the default text. Choose (None) to always use the default."
                    : "Provide a field name to use before falling back to the default text. Leave it blank to always use the default.",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            this.Controls.Add(infoLabel);
            y += 30;
            
            // Buttons
            removeButton = new Button
            {
                Text = "Clear fallback",
                Location = new Point(20, y),
                Size = new Size(130, 30)
            };
            removeButton.Click += (s, e) =>
            {
                Result = null;
                DefaultValue = null;
                DialogResult = DialogResult.OK;
            };
            
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(240, y),
                Size = new Size(80, 30)
            };
            okButton.Click += (s, e) =>
            {
                var fieldName = DetermineSelectedFieldName();
                var defaultValue = string.IsNullOrWhiteSpace(defaultTextBox.Text) ? null : defaultTextBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(fieldName) && string.IsNullOrWhiteSpace(defaultValue))
                {
                    MessageBox.Show("Enter a default value when specifying an alternate field.", "Default Required",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(fieldName) && string.IsNullOrWhiteSpace(defaultValue))
                {
                    Result = null;
                    DefaultValue = null;
                }
                else if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    Result = new FieldConfiguration
                    {
                        Field = fieldName,
                        Type = typeComboBox.SelectedIndex == 0 ? null : typeComboBox.SelectedItem?.ToString(),
                        Default = defaultValue
                    };
                    DefaultValue = null;
                }
                else
                {
                    Result = null;
                    DefaultValue = defaultValue;
                }
                DialogResult = DialogResult.OK;
            };
            
            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(330, y),
                Size = new Size(80, 30)
            };
            
            this.Controls.Add(removeButton);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void PopulateAttributeCombo()
        {
            if (!useAttributePicker || fieldComboBox == null)
            {
                return;
            }

            fieldComboBox.Items.Clear();
            fieldComboBox.Items.Add(new AttributeListItem
            {
                DisplayLabel = noneDisplayLabel,
                LogicalName = null
            });

            foreach (var item in attributeItems)
            {
                fieldComboBox.Items.Add(item);
            }

            fieldComboBox.SelectedIndex = 0;
        }

        private void ApplyExistingConfiguration(FieldConfiguration config, string currentDefault)
        {
            var defaultValue = config?.Default ?? currentDefault ?? string.Empty;
            defaultTextBox.Text = defaultValue;

            var typeValue = config?.Type ?? "(auto-detect)";
            var typeIndex = typeComboBox.Items.IndexOf(typeValue);
            typeComboBox.SelectedIndex = typeIndex >= 0 ? typeIndex : 0;

            var existingField = config?.Field;

            if (useAttributePicker)
            {
                EnsureFieldSelection(existingField);
            }
            else if (fieldTextBox != null)
            {
                fieldTextBox.Text = existingField ?? string.Empty;
            }
        }

        private void EnsureFieldSelection(string logicalName)
        {
            if (!useAttributePicker || fieldComboBox == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(logicalName))
            {
                fieldComboBox.SelectedIndex = 0;
                return;
            }

            for (int i = 0; i < fieldComboBox.Items.Count; i++)
            {
                if (fieldComboBox.Items[i] is AttributeListItem item &&
                    string.Equals(item.LogicalName, logicalName, StringComparison.OrdinalIgnoreCase))
                {
                    fieldComboBox.SelectedIndex = i;
                    return;
                }
            }

            var placeholder = new AttributeListItem
            {
                LogicalName = logicalName,
                DisplayLabel = $"{logicalName} (not available)"
            };
            fieldComboBox.Items.Add(placeholder);
            fieldComboBox.SelectedItem = placeholder;
        }

        private void FieldComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Keep type selection unless user previously changed it.
            if (fieldComboBox?.SelectedItem is AttributeListItem item && typeComboBox != null)
            {
                if (typeComboBox.SelectedIndex <= 0)
                {
                    typeComboBox.SelectedIndex = 0;
                }
            }
        }

        private string DetermineSelectedFieldName()
        {
            if (useAttributePicker)
            {
                if (fieldComboBox?.SelectedItem is AttributeListItem item)
                {
                    return string.IsNullOrWhiteSpace(item.LogicalName) ? null : item.LogicalName;
                }
                return null;
            }

            return string.IsNullOrWhiteSpace(fieldTextBox?.Text) ? null : fieldTextBox.Text.Trim();
        }

        private static string BuildAttributeLabel(AttributeMetadata metadata)
        {
            if (metadata == null)
            {
                return null;
            }

            var logical = metadata.LogicalName ?? string.Empty;
            var friendly = metadata.DisplayName?.UserLocalizedLabel?.Label;
            return string.IsNullOrWhiteSpace(friendly) ? logical : $"{friendly} ({logical})";
        }
    }
}
