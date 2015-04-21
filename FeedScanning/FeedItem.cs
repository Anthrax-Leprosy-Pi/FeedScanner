using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;

namespace FeedScanning {
    public class FeedItem {
        public string Id { get; set; }
        public string Title { get; set; }
        public string RefreshUri { get; set; }
        public List<string> Links { get; set; }
        public IEnumerable<string> LinksSD { get { return Links.Except(Links720p).Except(Links1080p); } }
        public IEnumerable<string> Links720p { get { return Links.Where(link => link.Contains("720")); } }
        public IEnumerable<string> Links1080p { get { return Links.Where(link => link.Contains("1080")); } }
        public bool Done { get; set; }
        public bool Favorite { get; set; }
        
        public FeedItem() { }
        public FeedItem(SyndicationItem originalItem) {
            Id = originalItem.Id;
            Title = originalItem.Title.Text;
            RefreshUri = originalItem.Links.First(link => link.RelationshipType == "alternate").Uri.ToString() + "/feed";
            Links = originalItem.Links.Select(sl => sl.Uri.ToString()).Where(sl => sl.Contains("uploaded") || sl.Contains("ul.to")).ToList();
        }
    }
}
