using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FeedScanning {
    public class FeedScanner : BackgroundWorker, IDisposable {
        public const string FEED_HISTORY = "FeedHistory.xml";
        public const string FAVORITE_SHOWS = "FavoriteShows.xml";
        public const int MAX_PAGES = 40;
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
                for (int page = 0; page < MAX_PAGES; page++) {                    
                    using (StreamXmlSanitizer streamXmlSanitizer = new StreamXmlSanitizer(webClient.OpenRead(FEED_URL + page))) {
                        foreach (var entry in SyndicationFeed.Load(XmlReader.Create((TextReader)streamXmlSanitizer, new XmlReaderSettings() { ProhibitDtd = false, CheckCharacters = true })).Items.OrderByDescending(x => x.PublishDate)) {
                            if (CancellationPending || FeedEntries.Any(oldEntry => oldEntry.Id == entry.Id)) {
                                goto done;
                            }
                            newItems.Add(new FeedItem(entry));
                            ReportProgress((page * 100) / MAX_PAGES);
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
            base.OnRunWorkerCompleted(e);
        }

        private void SetFavorites() {
            FeedEntries.Where(x => !x.Done).ToList().ForEach(x => x.Favorite = FavoriteShows.Any(fav => x.Title.Contains(fav.SearchPattern)));
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
            SaveFeedHistory();
            SaveFavoriteShows();
            base.Dispose(disposing);
        }
    }
}
