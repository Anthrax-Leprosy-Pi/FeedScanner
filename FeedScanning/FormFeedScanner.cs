using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FeedScanning {
    public partial class FormFeedScanner : Form {
        private FeedScanner feedScanner;
        private Timer refreshTimer;

        public FormFeedScanner() {
            // TODO: Complete member initialization
            InitializeComponent();
            textBoxFeedUrl.Text = FeedScanner.FEED_URL;
            feedScanner = new FeedScanner();
            feedScanner.RunWorkerCompleted += feedSifter_RunWorkerCompleted;
            feedScanner.ProgressChanged += feedSifter_ProgressChanged;
            feedItemBindingSource.DataSource = feedScanner.FeedEntries;
            favoriteShowBindingSource.DataSource = feedScanner.FavoriteShows;
            QualitySetting.ValueType = typeof(FavoriteShow.Quality);
            QualitySetting.DataSource = Enum.GetValues(typeof(FavoriteShow.Quality));
            feedScanner.RunWorkerAsync();
            refreshTimer = new Timer();
            refreshTimer.Interval = 3600000;
            refreshTimer.Tick += refreshTimer_Tick;
            refreshTimer.Start();
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            allowVisible = !Program.IsTask;
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e) {
            switch (e.Mode) {
                case PowerModes.Resume:
                    RefreshFeed();
                    refreshTimer.Start();
                    break;
                case PowerModes.StatusChange:
                    break;
                case PowerModes.Suspend:
                    refreshTimer.Stop();
                    break;
                default:
                    break;
            }
        }

        void refreshTimer_Tick(object sender, EventArgs e) {
            RefreshFeed();
        }

        private void RefreshFeed() {
            if (!feedScanner.IsBusy) {
                feedScanner.RunWorkerAsync();
            }
        }

        void feedSifter_ProgressChanged(object sender, ProgressChangedEventArgs e) {            
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
        }

        void feedSifter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Invoke((MethodInvoker)delegate() {
                if (e.Error != null) {
                    textBoxFeedUrl.BackColor = Color.Tomato;
                    toolStripStatusLabel1.Text = "Error!";
                    toolStripStatusLabel1.ToolTipText = e.Error.ToString();
                } else {
                    feedItemBindingSource.ResetBindings(false);
                    textBoxFeedUrl.BackColor = Color.LightGreen;
                }
            });
        }

        private void buttonRefresh_Click(object sender, EventArgs e) {
            if (feedScanner.IsBusy) {
                feedScanner.CancelAsync();
            } else {
                RefreshFeed();
            }
        }

        protected override void OnLoad(EventArgs e) {
            if (Program.IsTask) {
                this.Close();
            }
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
    }
}
