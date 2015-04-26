using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace FeedScanning {
    public class FeedScanner : BackgroundWorker, IDisposable {
        public const string FEED_HISTORY = "FeedHistory.xml";
        public const string FAVORITE_SHOWS = "FavoriteShows.xml";
        public const int MAX_PAGES = 15;
        public const string FEED_URL = "http://www.rlsbb.com/category/tv-shows/feed/?paged=";

        public List<FeedItem> FeedEntries { get; set; }
        public List<FavoriteShow> FavoriteShows { get; set; }

        public FeedScanner() {
            FeedEntries = LoadFeedHistory();
            FavoriteShows = LoadFavoriteShows();
            WorkerSupportsCancellation = true;
            WorkerReportsProgress = true;
        }

        private List<FeedItem> LoadFeedHistory(string saveFile = FEED_HISTORY) {
            try {
                return SerializationHelper.DeSerialize<List<FeedItem>>(saveFile);
            } catch {
                return new List<FeedItem>();
            }
        }

        private void SaveFeedHistory(string saveFile = FEED_HISTORY) {
            FeedEntries.TrimExcess();
            SerializationHelper.Serialize(saveFile, FeedEntries);
        }

        private List<FavoriteShow> LoadFavoriteShows(string saveFile = FAVORITE_SHOWS) {
            try {
                return SerializationHelper.DeSerialize<List<FavoriteShow>>(saveFile);
            } catch {
                return new List<FavoriteShow>();
            }
        }

        private void SaveFavoriteShows(string saveFile = FAVORITE_SHOWS) {
            FavoriteShows.TrimExcess();
            SerializationHelper.Serialize(saveFile, FavoriteShows);
        }

        protected override void OnDoWork(DoWorkEventArgs e) {
            ReportProgress(0);
            List<FeedItem> newItems = new List<FeedItem>();
            using (WebClient webClient = new WebClient()) {
                for (int page = 0; page < (int)e.Argument; page++) {
                    using (StreamXmlSanitizer streamXmlSanitizer = new StreamXmlSanitizer(webClient.OpenRead(FEED_URL + page))) {
                        foreach (var entry in SyndicationFeed.Load(XmlReader.Create((TextReader)streamXmlSanitizer, new XmlReaderSettings() { ProhibitDtd = false, CheckCharacters = true })).Items.OrderByDescending(x => x.PublishDate)) {
                            if (CancellationPending) {
                                goto done;
                            }
                            if (!FeedEntries.Any(x=>x.Id == entry.Id) || FeedEntries.RemoveAll(x => x.Id == entry.Id && !x.Done && x.Favorite) > 0){
                                newItems.Add(new FeedItem(entry));
                            }
                            ReportProgress((page * 100) / (int)e.Argument);
                        }
                    }
                }
            }
        done:
            FeedEntries.InsertRange(0, newItems);
            ReportProgress(100);
        }

        protected override void OnProgressChanged(ProgressChangedEventArgs e) {
            base.OnProgressChanged(e);
        }

        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e) {
            SetFavorites();
            SaveFeedHistory();
            SaveFavoriteShows();
            CopyLinksToClipboard();
            base.OnRunWorkerCompleted(e);
        }

        private void SetFavorites() {
            FeedEntries.ToList().ForEach(x => x.Favorite = FavoriteShows.Any(fav => x.Title.ToLower().Contains(fav.SearchPattern.ToLower())));
        }

        public void CopyLinksToClipboard() {
            StringBuilder links = new StringBuilder();
            foreach (var item in FeedEntries.Where(x => !x.Done && x.Favorite)) {
                switch (FavoriteShows.First(x => item.Title.ToLower().Contains(x.SearchPattern.ToLower())).QualitySetting) {
                    case Quality.SD:
                        string linkSD = item.LinksSD.FirstOrDefault();
                        if (!string.IsNullOrEmpty(linkSD)) {
                            links.AppendLine(linkSD);
                            item.Done = true;
                        }
                        break;
                    case Quality.HD720p:
                        string link720p = item.Links720p.FirstOrDefault();
                        if (!string.IsNullOrEmpty(link720p)) {
                            links.AppendLine(link720p);
                            item.Done = true;
                        }
                        break;
                    case Quality.HD1080p:
                        string link1080p = item.Links1080p.FirstOrDefault();
                        if (!string.IsNullOrEmpty(link1080p)) {
                            links.AppendLine(link1080p);
                            item.Done = true;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (!string.IsNullOrEmpty(links.ToString())) {
                Clipboard.SetText(links.ToString());
                Debug.Write(links.ToString());
            }
            SaveFeedHistory();
        }


        public void RemoveDoneEntries() {
            FeedEntries.RemoveAll(x => x.Done);
            SaveFeedHistory();
        }

        public void RemoveNonFavoriteEntries() {
            FeedEntries.RemoveAll(x => !x.Favorite);
            SaveFeedHistory();
        }

        protected override void Dispose(bool disposing) {
            CancelAsync();
            SaveFeedHistory();
            SaveFavoriteShows();
            base.Dispose(disposing);
        }
    }
}
