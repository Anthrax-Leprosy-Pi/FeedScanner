using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FeedScanning {
    public class FeedItem {
        const string REGEX_PATTERN = @"(http|ftp|https):\/\/([uploaded|ul]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?";
        public string Id { get; set; }
        public string Title { get; set; }
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
            Links = new List<string>();
            if (originalItem.Content != null && originalItem.Content is TextSyndicationContent) {
                MatchCollection matchList = Regex.Matches(((TextSyndicationContent)originalItem.Content).Text, REGEX_PATTERN);
                Links = matchList.Cast<Match>().Select(match => match.Value).ToList();
            } else {
                Links = originalItem.Links.Select(sl => sl.Uri.ToString()).Where(sl => sl.Contains("uploaded") || sl.Contains("ul.to")).ToList();
            }
        }
    }
}
