using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeedScanning {
    public partial class FormFeedScanner : Form {
        const int SCAN_INTERVAL = 3600000;

        private FeedScanner feedScanner;
        private System.Windows.Forms.Timer refreshTimer;
        private DateTime lastScan;

        public FormFeedScanner() {
            // TODO: Complete member initialization
            InitializeComponent();
            textBoxFeedUrl.Text = FeedScanner.FEED_URL;
            numericUpDownPages.Value = FeedScanner.MAX_PAGES;
            
            feedScanner = new FeedScanner();
            feedScanner.RunWorkerCompleted += feedSifter_RunWorkerCompleted;
            feedScanner.ProgressChanged += feedSifter_ProgressChanged;

            feedItemBindingSource.DataSource = feedScanner.FeedEntries;
            favoriteShowBindingSource.DataSource = feedScanner.FavoriteShows;
            QualitySetting.ValueType = typeof(Quality);
            QualitySetting.DataSource = Enum.GetValues(typeof(Quality));

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = SCAN_INTERVAL;
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;            
        }

        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e) {
            if (e.IsAvailable) {
                RefreshFeed();
            }
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e) {
            switch (e.Mode) {
                case PowerModes.Resume:
                    refreshTimer.Start();
                    break;
                case PowerModes.Suspend:
                    refreshTimer.Stop();
                    break;
            }
        }

        void refreshTimer_Tick(object sender, EventArgs e) {
            RefreshFeed();
        }

        private void RefreshFeed(bool force = false) {
            if (!feedScanner.IsBusy && NetworkInterface.GetIsNetworkAvailable() && (force || (DateTime.Now - lastScan).TotalMilliseconds >= SCAN_INTERVAL)) {
                feedScanner.RunWorkerAsync((int)numericUpDownPages.Value);
            }
        }

        void feedSifter_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            Invoke((MethodInvoker)delegate() {
                textBoxFeedUrl.BackColor = Color.LightGreen;
                if (e.ProgressPercentage < 100) {
                    buttonRefreshCancel.Text = "Cancel";
                    toolStripProgressBar1.Value = e.ProgressPercentage;
                    toolStripStatusLabel1.ToolTipText = string.Empty;
                    toolStripStatusLabel1.Text = "Scanning...";
                    notifyIcon1.ShowBalloonTip(1000, "FeedScanner", "Scanning... " + e.ProgressPercentage.ToString() + "%", ToolTipIcon.Info);
                } else if (e.ProgressPercentage >= 100) {
                    buttonRefreshCancel.Text = "Refresh";
                    toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                    toolStripProgressBar1.Value = 100;
                    toolStripStatusLabel1.Text = "Done!";
                    notifyIcon1.ShowBalloonTip(10000, "FeedScanner", "Done.", ToolTipIcon.Info);
                }
            });
        }

        void feedSifter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Invoke((MethodInvoker)delegate() {
                if (e.Error != null) {
                    textBoxFeedUrl.BackColor = Color.Tomato;
                    toolStripStatusLabel1.Text = e.Error.Message;
                    toolStripStatusLabel1.ToolTipText = e.Error.ToString();
                    buttonRefreshCancel.Text = "Refresh";
                } else {
                    feedItemBindingSource.ResetBindings(false);
                    textBoxFeedUrl.BackColor = Color.LightGreen;
                }
                lastScan = DateTime.Now;
                toolStripStatusLabelLastScan.Text = lastScan.ToString("dd.MM. HH:mm:ss");
            });

        }

        private void buttonRefresh_Click(object sender, EventArgs e) {
            if (feedScanner.IsBusy) {
                feedScanner.CancelAsync();
            } else {
                RefreshFeed(true);
            }
        }

        protected override void OnLoad(EventArgs e) {
            if (Program.IsTask) {
                this.Close();
            }
            RefreshFeed(true);
            base.OnLoad(e);
        }

        private bool allowVisible;     // ContextMenu's Show command used
        private bool allowClose;       // ContextMenu's Exit command used

        protected override void SetVisibleCore(bool value) {
            if (!allowVisible) {
                value = false;
                if (!this.IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            fileSystemWatcher1.Dispose();
            if (!allowClose) {
                this.Hide();
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void toolStripMenuItemExit_Click(object sender, EventArgs e) {
            allowClose = true;
            Application.Exit();
        }

        private void toolStripMenuItemShow_Click(object sender, EventArgs e) {
            allowVisible = true;
            Show();
            Activate();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Right:
                    break;
                default:
                    toolStripMenuItemShow_Click(sender, e);
                    break;
            }
        }

        private void dataGridViewFeed_CellDoubleClick(object sender, DataGridViewCellEventArgs e) {
            Process.Start(((FeedItem)dataGridViewFeed.Rows[e.RowIndex].DataBoundItem).Id);
        }

        private void dataGridViewFavoriteShow_Leave(object sender, EventArgs e) {
            dataGridViewFeed.EndEdit();
        }

        private void dataGridViewFeed_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e) {
            FeedItem current = (FeedItem)dataGridViewFeed.Rows[e.RowIndex].DataBoundItem;
            Color color = current.Favorite ? Color.Gold : Color.White;
            color = current.Done ? Color.LightGreen : color;
            dataGridViewFeed.Rows[e.RowIndex].DefaultCellStyle.BackColor = color;
        }

        private void buttonDeleteDone_Click(object sender, EventArgs e) {
            feedScanner.RemoveDoneEntries();
            feedItemBindingSource.ResetBindings(false);
        }

        private void buttonPurgeNonFavorites_Click(object sender, EventArgs e) {
            if (MessageBox.Show("Are you sure you want to purge all non-favorite shows?", "Question", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                feedScanner.RemoveNonFavoriteEntries();
                feedItemBindingSource.ResetBindings(false);
            }
        }

        private void fileSystemWatcher1_Changed(object sender, System.IO.FileSystemEventArgs e) {
            Invoke((MethodInvoker)delegate() {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = new FileInfo(e.FullPath).Length;
                int order = 0;
                while (len >= 1024 && order + 1 < sizes.Length) {
                    order++;
                    len = len / 1024;
                }
                // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
                // show a single decimal place, and no space.
                toolStripStatusFileSize.Text = String.Format("{0:0.##} {1}", len, sizes[order]);
                double len2 = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
                int order2 = 0;
                while (len2 >= 1024 && order2 + 1 < sizes.Length) {
                    order2++;
                    len2 = len2 / 1024;
                }
                toolStripStatusMemoryUsage.Text = String.Format("{0:0.##} {1}", len2, sizes[order]);
            });
        }

        private void checkBoxFavoritesOnly_CheckedChanged(object sender, EventArgs e) {
            this.SuspendLayout();
            CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[dataGridViewFeed.DataSource];
            currencyManager1.SuspendBinding();
            for (int i = 0; i < dataGridViewFeed.Rows.Count; i++) {
                FeedItem current = (FeedItem)dataGridViewFeed.Rows[i].DataBoundItem;
                dataGridViewFeed.Rows[i].Visible = current.Favorite || checkBoxShowAll.Checked;
            }
            currencyManager1.ResumeBinding();
            this.ResumeLayout();
        }
    }
}
