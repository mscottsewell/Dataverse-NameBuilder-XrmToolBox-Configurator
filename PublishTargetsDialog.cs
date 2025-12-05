using System;
using System.Drawing;
using System.Windows.Forms;

namespace NameBuilderConfigurator
{
    internal class PublishTargetsDialog : Form
    {
        private readonly CheckBox insertCheckBox;
        private readonly CheckBox updateCheckBox;
        private readonly Button okButton;

        public bool PublishInsert => insertCheckBox.Checked;
        public bool PublishUpdate => updateCheckBox.Checked;

        public PublishTargetsDialog(string entityDisplayName, bool insertExists, bool updateExists)
        {
            Text = "Publish Configuration";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 260);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var instructions = new Label
            {
                Text = $"Choose which steps for \"{entityDisplayName}\" should receive the updated configuration.",
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(10, 12, 10, 0),
                AutoSize = false
            };
            Controls.Add(instructions);

            insertCheckBox = new CheckBox
            {
                Text = "Insert (Create) step",
                Checked = true,
                Location = new Point(15, 60),
                AutoSize = true
            };
            Controls.Add(insertCheckBox);
            Controls.Add(new Label
            {
                Text = insertExists ? "Existing step will be updated." : "Step will be created (PreOperation, synchronous).",
                Location = new Point(35, 82),
                AutoSize = true,
                ForeColor = insertExists ? Color.ForestGreen : Color.DarkOrange
            });

            updateCheckBox = new CheckBox
            {
                Text = "Update step",
                Checked = true,
                Location = new Point(15, 120),
                AutoSize = true
            };
            Controls.Add(updateCheckBox);
            Controls.Add(new Label
            {
                Text = updateExists ? "Existing step will be updated." : "Step will be created (PreOperation, synchronous).",
                Location = new Point(35, 142),
                AutoSize = true,
                ForeColor = updateExists ? Color.ForestGreen : Color.DarkOrange
            });

            okButton = new Button
            {
                Text = "Publish",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(210, 180)
            };
            okButton.Click += (s, e) =>
            {
                if (!insertCheckBox.Checked && !updateCheckBox.Checked)
                {
                    MessageBox.Show("Select at least one step to publish.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                }
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(300, 180)
            };

            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
