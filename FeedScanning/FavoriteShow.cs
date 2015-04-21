using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedScanning {
    public class FavoriteShow {
        public string SearchPattern { get; set; }

        public Quality QualitySetting { get; set; }
        public enum Quality {
            SD, HD720p, HD1080p
        }
    }
}
