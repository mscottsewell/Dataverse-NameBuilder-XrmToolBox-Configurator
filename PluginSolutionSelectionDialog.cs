using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NameBuilderConfigurator
{
    public class PluginSolutionSelectionDialog : Form
    {
        private ComboBox solutionComboBox;
        private Button okButton;
        private Button cancelButton;
        private Label instructionLabel;
        private List<SolutionItem> solutions;

        public Guid? SelectedSolutionId { get; private set; }
        public string SelectedSolutionUniqueName { get; private set; }

        public PluginSolutionSelectionDialog(List<SolutionItem> availableSolutions, Guid? currentSolutionId)
        {
            solutions = availableSolutions ?? new List<SolutionItem>();
            InitializeComponent();
            PopulateSolutions(currentSolutionId);
        }

        private void InitializeComponent()
        {
            this.Text = "Select Plugin Solution";
            this.Width = 500;
            this.Height = 200;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            instructionLabel = new Label
            {
                Text = "Select the solution where the NameBuilder plugin and its step registrations should be stored:",
                Location = new Point(15, 15),
                Size = new Size(450, 40),
                AutoSize = false
            };
            this.Controls.Add(instructionLabel);

            var solutionLabel = new Label
            {
                Text = "Plugin Solution:",
                Location = new Point(15, 65),
                AutoSize = true
            };
            this.Controls.Add(solutionLabel);

            solutionComboBox = new ComboBox
            {
                Location = new Point(15, 88),
                Width = 450,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(solutionComboBox);

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 125),
                Width = 75
            };
            this.Controls.Add(okButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(390, 125),
                Width = 75
            };
            this.Controls.Add(cancelButton);

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            okButton.Click += OkButton_Click;
        }

        private void PopulateSolutions(Guid? currentSolutionId)
        {
            solutionComboBox.Items.Clear();

            foreach (var solution in solutions)
            {
                solutionComboBox.Items.Add(solution);
                if (currentSolutionId.HasValue && solution.SolutionId == currentSolutionId.Value)
                {
                    solutionComboBox.SelectedItem = solution;
                }
            }

            if (solutionComboBox.SelectedIndex == -1 && solutionComboBox.Items.Count > 0)
            {
                solutionComboBox.SelectedIndex = 0;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            var selected = solutionComboBox.SelectedItem as SolutionItem;
            if (selected != null)
            {
                SelectedSolutionId = selected.SolutionId;
                SelectedSolutionUniqueName = selected.UniqueName;
            }
        }
    }
}
