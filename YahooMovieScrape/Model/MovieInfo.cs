using System;

namespace YahooMovieScrape.Model
{
    public class MovieInfo
    {
        public Uri ImageUri { get; set; }
        public string ChineseName { get; set; }
        public string EnglishName { get; set; }
        public DateTime ReleaseDateTime { get; set; }
        public string BriefDescription { get; set; }
        public string FullyDescription { get; set; }

        public override string ToString()
        {
            return string.Format("Movie [{0}], [{1}], [{2}], [{3}], [{4}]", ChineseName, EnglishName, ReleaseDateTime,
                BriefDescription, ImageUri);
        }
    }
}