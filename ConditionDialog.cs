using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.Metadata;

namespace  NameBuilderConfigurator
{
    /// <summary>
    /// Dialog for configuring field conditions (includeIf)
    /// </summary>
    public class ConditionDialog : Form
    {
        private RadioButton simpleRadio;
        private RadioButton compoundRadio;
        private Panel simplePanel;
        private Panel compoundPanel;
        
        // Simple condition controls
        private ComboBox conditionFieldComboBox;
        private ComboBox operatorComboBox;
        private TextBox valueTextBox;
        private ComboBox valueComboBox;
        
        // Compound condition controls
        private RadioButton anyOfRadio;
        private RadioButton allOfRadio;
        private TextBox compoundTextBox;
        
        private Button okButton;
        private Button cancelButton;
        private Button removeButton;
        private readonly ToolTip helpToolTip;
        
        public FieldCondition Result { get; private set; }
        
        private readonly List<AttributeMetadata> availableAttributes;
        private readonly string defaultFieldLogicalName;
        private bool suppressFieldChange;

        private static readonly string[] Operators = new[]
        {
            "equals", "notEquals", "contains", "notContains", 
            "in", "notIn", "greaterThan", "lessThan",
            "greaterThanOrEqual", "lessThanOrEqual",
            "isEmpty", "isNotEmpty"
        };
        
        public ConditionDialog(FieldCondition condition, IEnumerable<AttributeMetadata> attributes = null, string defaultField = null)
        {
            availableAttributes = attributes?.ToList() ?? new List<AttributeMetadata>();
            defaultFieldLogicalName = defaultField;
            
            helpToolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 300,
                ReshowDelay = 100,
                ShowAlways = true
            };
            
            InitializeComponent();
            PopulateFieldDropdown(null);
            LoadCondition(condition);
        }
        
        private void InitializeComponent()
        {
            this.Text = "Field Condition (includeIf)";
            this.Size = new Size(550, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            
            int y = 20;
            int panelHeight = 165;
            
            // Simple vs Compound
            simpleRadio = new RadioButton
            {
                Text = "Simple Condition",
                Location = new Point(20, y),
                Size = new Size(150, 25),
                Checked = true
            };
            simpleRadio.CheckedChanged += (s, e) => UpdatePanelVisibility();
            helpToolTip.SetToolTip(simpleRadio, "Single comparison: field [operator] value. Example: statuscode equals 1.");
            
            compoundRadio = new RadioButton
            {
                Text = "Compound Condition (anyOf/allOf)",
                Location = new Point(200, y),
                Size = new Size(300, 25)
            };
            compoundRadio.CheckedChanged += (s, e) => UpdatePanelVisibility();
            helpToolTip.SetToolTip(compoundRadio, "Multiple conditions combined with AND/OR logic. Example: (status equals 'active' OR type equals 'primary').");
            
            this.Controls.Add(simpleRadio);
            this.Controls.Add(compoundRadio);
            y += 35;
            
            // Simple panel
            simplePanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(500, panelHeight),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            int py = 15;
            var fieldLabel = new Label
            {
                Text = "Field:",
                Location = new Point(15, py + 3),
                Size = new Size(100, 20)
            };
            conditionFieldComboBox = new ComboBox
            {
                Location = new Point(125, py),
                Size = new Size(350, 23),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            conditionFieldComboBox.SelectedIndexChanged += ConditionFieldComboBox_SelectedIndexChanged;
            conditionFieldComboBox.Leave += ConditionFieldComboBox_Leave;
            helpToolTip.SetToolTip(conditionFieldComboBox, "Field to compare. Choose from available attributes or type a logical name manually.");
            simplePanel.Controls.Add(fieldLabel);
            simplePanel.Controls.Add(conditionFieldComboBox);
            py += 35;
            
            var operatorLabel = new Label
            {
                Text = "Operator:",
                Location = new Point(15, py + 3),
                Size = new Size(100, 20)
            };
            operatorComboBox = new ComboBox
            {
                Location = new Point(125, py),
                Size = new Size(350, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            operatorComboBox.Items.AddRange(Operators.Cast<object>().ToArray());
            operatorComboBox.SelectedIndexChanged += OperatorComboBox_SelectedIndexChanged;
            helpToolTip.SetToolTip(operatorComboBox, "Comparison operator: equals, contains, startsWith, isNull (empty), isEmpty, greaterThan, etc.");
            simplePanel.Controls.Add(operatorLabel);
            simplePanel.Controls.Add(operatorComboBox);
            py += 35;
            
            var valueLabel = new Label
            {
                Text = "Value:",
                Location = new Point(15, py + 3),
                Size = new Size(100, 20)
            };
            valueTextBox = new TextBox
            {
                Location = new Point(125, py),
                Size = new Size(350, 23)
            };
            valueComboBox = new ComboBox
            {
                Location = new Point(125, py),
                Size = new Size(350, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };
            valueComboBox.SelectedIndexChanged += ValueComboBox_SelectedIndexChanged;
            simplePanel.Controls.Add(valueLabel);
            simplePanel.Controls.Add(valueComboBox);
            simplePanel.Controls.Add(valueTextBox);
            py += 35;
            
            var exampleLabel = new Label
            {
                Text = "Example: field=\"statecode\", operator=\"equals\", value=\"0\"",
                Location = new Point(15, py),
                Size = new Size(460, 40),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            simplePanel.Controls.Add(exampleLabel);
            
            this.Controls.Add(simplePanel);
            
            // Compound panel
            compoundPanel = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(500, panelHeight),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            
            py = 15;
            anyOfRadio = new RadioButton
            {
                Text = "anyOf (OR - any condition must be true)",
                Location = new Point(15, py),
                Size = new Size(300, 25),
                Checked = true
            };
            compoundPanel.Controls.Add(anyOfRadio);
            py += 30;
            
            allOfRadio = new RadioButton
            {
                Text = "allOf (AND - all conditions must be true)",
                Location = new Point(15, py),
                Size = new Size(300, 25)
            };
            compoundPanel.Controls.Add(allOfRadio);
            py += 35;
            
            var compoundLabel = new Label
            {
                Text = "Enter JSON array of conditions:",
                Location = new Point(15, py),
                Size = new Size(300, 20)
            };
            compoundPanel.Controls.Add(compoundLabel);
            py += 25;
            
            compoundTextBox = new TextBox
            {
                Location = new Point(15, py),
                Size = new Size(465, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F)
            };
            compoundPanel.Controls.Add(compoundTextBox);
            
            this.Controls.Add(compoundPanel);
            y += panelHeight + 20;
            
            // Info
            var infoLabel = new Label
            {
                Text = "Conditions control when a field is included in the name. Leave empty to always include.",
                Location = new Point(20, y),
                Size = new Size(500, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            this.Controls.Add(infoLabel);
            y += 40;
            
            // Buttons
            removeButton = new Button
            {
                Text = "Remove Condition",
                Location = new Point(20, y),
                Size = new Size(140, 30)
            };
            removeButton.Click += (s, e) =>
            {
                Result = null;
                DialogResult = DialogResult.OK;
            };
            
            okButton = new Button
            {
                Text = "OK",
                Location = new Point(360, y),
                Size = new Size(80, 30)
            };
            okButton.Click += OkButton_Click;
            
            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(450, y),
                Size = new Size(80, 30)
            };
            
            this.Controls.Add(removeButton);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void UpdatePanelVisibility()
        {
            simplePanel.Visible = simpleRadio.Checked;
            compoundPanel.Visible = compoundRadio.Checked;
        }
        
        private void OperatorComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var requiresValue = ValueIsRequired();
            valueTextBox.Enabled = requiresValue;
            valueComboBox.Enabled = requiresValue;

            if (requiresValue)
            {
                UpdateValueInputForField(ResolveSelectedAttribute(), GetCurrentValueInput());
            }
            else
            {
                valueComboBox.Visible = false;
                valueTextBox.Visible = true;
                valueTextBox.Text = string.Empty;
            }
        }

        private void ConditionFieldComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressFieldChange)
                return;

            UpdateValueInputForField(ResolveSelectedAttribute(), null);
        }

        private void ConditionFieldComboBox_Leave(object sender, EventArgs e)
        {
            if (suppressFieldChange)
                return;

            UpdateValueInputForField(ResolveSelectedAttribute(), null);
        }

        private void ValueComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (valueComboBox.SelectedItem is OptionValueItem option)
            {
                valueTextBox.Text = option.Value;
            }
        }

        private void PopulateFieldDropdown(string selectedFieldLogicalName)
        {
            if (conditionFieldComboBox == null)
                return;

            suppressFieldChange = true;
            conditionFieldComboBox.Items.Clear();

            if (availableAttributes != null && availableAttributes.Count > 0)
            {
                var ordered = availableAttributes
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.LogicalName))
                    .Select(a => new AttributeListItem
                    {
                        LogicalName = a.LogicalName,
                        DisplayText = GetAttributeDisplayName(a),
                        Metadata = a
                    })
                    .OrderBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in ordered)
                {
                    conditionFieldComboBox.Items.Add(item);
                }
            }

            suppressFieldChange = false;

            if (!string.IsNullOrWhiteSpace(selectedFieldLogicalName))
            {
                SetFieldSelection(selectedFieldLogicalName);
            }
            else if (conditionFieldComboBox.Items.Count > 0)
            {
                conditionFieldComboBox.SelectedIndex = 0;
            }
        }

        private void SetFieldSelection(string logicalName)
        {
            suppressFieldChange = true;
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                if (conditionFieldComboBox.Items.Count > 0)
                {
                    conditionFieldComboBox.SelectedIndex = 0;
                }
                else
                {
                    conditionFieldComboBox.Text = string.Empty;
                }
            }
            else
            {
                var match = conditionFieldComboBox.Items.Cast<object>()
                    .OfType<AttributeListItem>()
                    .FirstOrDefault(i => i.LogicalName != null && i.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    conditionFieldComboBox.SelectedItem = match;
                }
                else
                {
                    conditionFieldComboBox.Text = logicalName;
                }
            }
            suppressFieldChange = false;
        }

        private AttributeMetadata ResolveSelectedAttribute()
        {
            var logicalName = GetSelectedFieldLogicalName();
            if (string.IsNullOrWhiteSpace(logicalName) || availableAttributes == null)
            {
                return null;
            }

            return availableAttributes.FirstOrDefault(a => a != null &&
                a.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));
        }

        private string GetSelectedFieldLogicalName()
        {
            if (conditionFieldComboBox.SelectedItem is AttributeListItem item &&
                !string.IsNullOrWhiteSpace(item.LogicalName))
            {
                return item.LogicalName;
            }

            return string.IsNullOrWhiteSpace(conditionFieldComboBox.Text)
                ? null
                : conditionFieldComboBox.Text.Trim();
        }

        private bool ValueIsRequired()
        {
            var op = operatorComboBox.SelectedItem?.ToString();
            return !(op == "isEmpty" || op == "isNotEmpty");
        }

        private string GetCurrentValueInput()
        {
            if (valueComboBox.Visible && valueComboBox.SelectedItem is OptionValueItem option)
            {
                return option.Value;
            }

            return string.IsNullOrWhiteSpace(valueTextBox.Text) ? null : valueTextBox.Text.Trim();
        }

        private void UpdateValueInputForField(AttributeMetadata attribute, string desiredValue)
        {
            if (!ValueIsRequired())
            {
                valueComboBox.Visible = false;
                valueTextBox.Visible = true;
                return;
            }

            var useDropdown = ShouldUseOptionDropdown(attribute);

            if (!useDropdown)
            {
                valueComboBox.Visible = false;
                valueTextBox.Visible = true;
                if (desiredValue != null)
                {
                    valueTextBox.Text = desiredValue;
                }
                return;
            }

            var options = BuildOptionItems(attribute);
            if (options == null || options.Count == 0)
            {
                valueComboBox.Visible = false;
                valueTextBox.Visible = true;
                if (desiredValue != null)
                {
                    valueTextBox.Text = desiredValue;
                }
                return;
            }

            valueComboBox.BeginUpdate();
            valueComboBox.Items.Clear();
            foreach (var option in options)
            {
                valueComboBox.Items.Add(option);
            }
            valueComboBox.EndUpdate();

            valueComboBox.Visible = true;
            valueTextBox.Visible = false;

            if (!string.IsNullOrWhiteSpace(desiredValue))
            {
                var match = options.FirstOrDefault(o => o.Value != null &&
                    o.Value.Equals(desiredValue, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    valueComboBox.SelectedItem = match;
                }
                else
                {
                    valueComboBox.Visible = false;
                    valueTextBox.Visible = true;
                    valueTextBox.Text = desiredValue;
                    return;
                }
            }
            else
            {
                valueComboBox.SelectedIndex = options.Count > 0 ? 0 : -1;
            }

            if (valueComboBox.SelectedItem is OptionValueItem selected)
            {
                valueTextBox.Text = selected.Value;
            }
            else
            {
                valueTextBox.Text = desiredValue ?? string.Empty;
            }
        }

        private bool ShouldUseOptionDropdown(AttributeMetadata attribute)
        {
            if (attribute == null)
            {
                return false;
            }

            var op = operatorComboBox.SelectedItem?.ToString();
            if (op == "in" || op == "notIn")
            {
                return false;
            }

            return attribute is EnumAttributeMetadata || attribute is BooleanAttributeMetadata;
        }

        private List<OptionValueItem> BuildOptionItems(AttributeMetadata attribute)
        {
            if (attribute is BooleanAttributeMetadata boolMeta)
            {
                var items = new List<OptionValueItem>();
                if (boolMeta.OptionSet?.TrueOption != null)
                {
                    items.Add(CreateOptionItem(boolMeta.OptionSet.TrueOption));
                }
                if (boolMeta.OptionSet?.FalseOption != null)
                {
                    items.Add(CreateOptionItem(boolMeta.OptionSet.FalseOption));
                }
                return items.Where(i => i != null).ToList();
            }

            if (attribute is EnumAttributeMetadata enumMeta && enumMeta.OptionSet != null)
            {
                return enumMeta.OptionSet.Options
                    .Where(o => o != null)
                    .Select(CreateOptionItem)
                    .Where(o => o != null)
                    .OrderBy(o => o.DisplayText, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return null;
        }

        private OptionValueItem CreateOptionItem(OptionMetadata option)
        {
            if (option == null)
            {
                return null;
            }

            var label = option.Label?.UserLocalizedLabel?.Label ?? option.Value?.ToString() ?? "";
            var value = option.Value?.ToString();
            return new OptionValueItem
            {
                DisplayText = string.IsNullOrWhiteSpace(label) ? value : $"{label} ({value})",
                Value = value
            };
        }

        private string GetAttributeDisplayName(AttributeMetadata attribute)
        {
            if (attribute == null)
            {
                return string.Empty;
            }

            var friendly = attribute.DisplayName?.UserLocalizedLabel?.Label;
            if (string.IsNullOrWhiteSpace(friendly))
            {
                return attribute.LogicalName ?? string.Empty;
            }

            return $"{friendly} ({attribute.LogicalName})";
        }

        private class AttributeListItem
        {
            public string LogicalName { get; set; }
            public string DisplayText { get; set; }
            public AttributeMetadata Metadata { get; set; }
            public override string ToString() => DisplayText;
        }

        private class OptionValueItem
        {
            public string DisplayText { get; set; }
            public string Value { get; set; }
            public override string ToString() => DisplayText;
        }
        
        private void LoadCondition(FieldCondition condition)
        {
            if (condition == null)
            {
                simpleRadio.Checked = true;
                SetFieldSelection(defaultFieldLogicalName);
                UpdateValueInputForField(ResolveSelectedAttribute(), null);
                return;
            }
            
            if (condition.AnyOf != null || condition.AllOf != null)
            {
                compoundRadio.Checked = true;
                anyOfRadio.Checked = condition.AnyOf != null;
                allOfRadio.Checked = condition.AllOf != null;
                
                var conditions = condition.AnyOf ?? condition.AllOf;
                compoundTextBox.Text = Newtonsoft.Json.JsonConvert.SerializeObject(conditions, Newtonsoft.Json.Formatting.Indented);
                SetFieldSelection(defaultFieldLogicalName);
                UpdateValueInputForField(ResolveSelectedAttribute(), null);
            }
            else
            {
                simpleRadio.Checked = true;
                SetFieldSelection(condition.Field ?? defaultFieldLogicalName);
                if (!string.IsNullOrEmpty(condition.Operator))
                    operatorComboBox.SelectedItem = condition.Operator;
                valueTextBox.Text = condition.Value ?? "";
                UpdateValueInputForField(ResolveSelectedAttribute(), condition.Value);
            }
        }
        
        private void OkButton_Click(object sender, EventArgs e)
        {
            if (simpleRadio.Checked)
            {
                var selectedField = GetSelectedFieldLogicalName();
                if (string.IsNullOrWhiteSpace(selectedField))
                {
                    MessageBox.Show("Please enter a field name.", "Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (operatorComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select an operator.", "Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var op = operatorComboBox.SelectedItem.ToString();
                var valueRequired = ValueIsRequired();
                var enteredValue = valueRequired ? GetCurrentValueInput() : null;
                if (valueRequired && string.IsNullOrWhiteSpace(enteredValue))
                {
                    MessageBox.Show("Please enter a value.", "Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                Result = new FieldCondition
                {
                    Field = selectedField,
                    Operator = op,
                    Value = string.IsNullOrWhiteSpace(enteredValue) ? null : enteredValue.Trim()
                };
            }
            else
            {
                if (string.IsNullOrWhiteSpace(compoundTextBox.Text))
                {
                    MessageBox.Show("Please enter the conditions JSON array.", "Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                try
                {
                    var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FieldCondition>>(compoundTextBox.Text);
                    
                    Result = new FieldCondition();
                    if (anyOfRadio.Checked)
                        Result.AnyOf = conditions;
                    else
                        Result.AllOf = conditions;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid JSON: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            
            DialogResult = DialogResult.OK;
        }
    }
}
