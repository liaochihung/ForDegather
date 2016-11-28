using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using YahooMovieScrape.Model;

namespace YahooMovieScrape.Service
{
    public static class YahooMovieParser
    {
        private static readonly object Sync = new object();
        public static List<MovieInfo> Parse(string webContent)
        {
            var html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webContent);

            var root = html.DocumentNode;
            var nodes = root.Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("item"));

            var movieInfos = new List<MovieInfo>();

            // if not consider the order
            Parallel.ForEach(nodes, node =>
            {
                var mi = new MovieInfo();

                var divImg = node
                    .Descendants().Single(n => n.GetAttributeValue("class", "").Equals("img"))
                    .Descendants("a").Single()
                    .Descendants("img").Single()
                    .Attributes[0].Value;

                var divText = node.Descendants()
                    .Single(n => n.GetAttributeValue("class", "").Equals("text"));

                var cname = divText
                    .Descendants("h4").Single()
                    .Descendants("a").Single()
                    .InnerText;

                var ename = divText
                    .Descendants("h5").Single()
                    .Descendants("a").Single()
                    .InnerText;

                var rDate = divText
                    .Descendants("span").FirstOrDefault()
                    .ChildNodes[1].InnerText;

                var briefDescription = divText
                    .Descendants("p").Single()
                    .FirstChild
                    .InnerText;

                mi.ImageUri = new Uri(divImg);
                mi.ChineseName = cname;
                mi.EnglishName = ename;
                mi.ReleaseDateTime = DateTime.ParseExact(rDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                mi.BriefDescription = briefDescription;

                lock (Sync)
                {
                    movieInfos.Add(mi);
                }
            });
            return movieInfos;
        }
    }
}