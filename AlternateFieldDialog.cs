using System;
using System.Drawing;
using System.Windows.Forms;

namespace  NameBuilderConfigurator
{
    /// <summary>
    /// Simple dialog for configuring an alternate field
    /// </summary>
    public class AlternateFieldDialog : Form
    {
        private TextBox fieldTextBox;
        private ComboBox typeComboBox;
        private TextBox defaultTextBox;
        private Button okButton;
        private Button cancelButton;
        private Button removeButton;
        
        public FieldConfiguration Result { get; private set; }
        
        public AlternateFieldDialog(FieldConfiguration config)
        {
            InitializeComponent();
            
            if (config != null && !string.IsNullOrEmpty(config.Field))
            {
                fieldTextBox.Text = config.Field;
                typeComboBox.SelectedItem = config.Type ?? "(auto-detect)";
                defaultTextBox.Text = config.Default ?? "";
            }
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
            fieldTextBox = new TextBox
            {
                Location = new Point(170, y),
                Size = new Size(300, 23)
            };
            this.Controls.Add(fieldLabel);
            this.Controls.Add(fieldTextBox);
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
                Text = "The alternate field is used when the primary field is null or empty.",
                Location = new Point(20, y),
                Size = new Size(400, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8F)
            };
            this.Controls.Add(infoLabel);
            y += 40;
            
            // Buttons
            removeButton = new Button
            {
                Text = "Remove Alternate",
                Location = new Point(20, y),
                Size = new Size(130, 30)
            };
            removeButton.Click += (s, e) =>
            {
                Result = null;
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
                if (string.IsNullOrWhiteSpace(fieldTextBox.Text))
                {
                    MessageBox.Show("Please enter a field name.", "Required", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                Result = new FieldConfiguration
                {
                    Field = fieldTextBox.Text.Trim(),
                    Type = typeComboBox.SelectedIndex == 0 ? null : typeComboBox.SelectedItem?.ToString(),
                    Default = string.IsNullOrWhiteSpace(defaultTextBox.Text) ? null : defaultTextBox.Text.Trim()
                };
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
    }
}
