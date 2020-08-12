using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Renamer.DesktopClient
{
    public partial class MainWindow : Form
    {
        private readonly Dictionary<MessageType, RichTextBox> reportingBoxes;

        public MainWindow()
        {
            InitializeComponent();

            Renamer.CancelRenaming = CancelRenaming;
            Renamer.ClearReports = ClearReports;
            Renamer.Report = Report;

            reportingBoxes = new Dictionary<MessageType, RichTextBox>
            {
                { MessageType.Information, richTxtInformation },
                { MessageType.ChangeReport, richTxtChanges },
                { MessageType.SkippingItem, richTxtSkippedItems },
                { MessageType.Error, richTxtPreventedActions }
            };
        }

        private void Report(Message message)
            => reportingBoxes[message.Type].AppendText(message.Content + Environment.NewLine);

        private void ClearReports()
            => reportingBoxes.Values.ToList().ForEach(
                reportingBox => reportingBox.Clear()
            );

        private bool CancelRenaming(Message message)
            => MessageBox.Show
            (
                text: message.Content,
                caption: "Proceed with renaming?",
                buttons: MessageBoxButtons.YesNo,
                icon: MessageBoxIcon.Question
            ) == DialogResult.No;

        private void btnRename_Click(object sender, EventArgs e)
        {
            if (txtFrom.Text == txtTo.Text)
            {
                MessageBox.Show
                (
                    text: "Please don't use same values for 'from' and 'to' for renaming.",
                    caption: "Invalid 'from' and 'to' values!",
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error
                );

                txtFrom.Focus();

                return;
            }

            if (Directory.Exists(txtLocation.Text) == false)
            {
                MessageBox.Show
                (
                    text: "Please enter or select a valid location from which the renaming will occur.",
                    caption: "Location doesn't exist!",
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error
                );

                txtLocation.Focus();

                return;
            }

            Renamer.Run(txtFrom.Text, txtTo.Text, txtLocation.Text);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog
            {
                Description = "Pick the root directory from which the renaming will occur."
            })
            {
                if (folderBrowser.ShowDialog() != DialogResult.OK)
                    return;

                txtLocation.Text = folderBrowser.SelectedPath;
            }
        }
    }
}
