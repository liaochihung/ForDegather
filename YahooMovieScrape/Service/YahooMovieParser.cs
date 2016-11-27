using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YahooMovieScrape.Model;

namespace YahooMovieScrape.Service
{
    public static class YahooMovieParser
    {
        public static List<MovieInfo> Parse(string webContent)
        {
            var html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webContent);

            var root = html.DocumentNode;
            var nodes = root.Descendants()
                .Where(n => n.GetAttributeValue("class", "").Equals("item"));

            var movieInfos = new List<MovieInfo>();

            foreach (var node in nodes)
            {
                var mi = new MovieInfo();

                var divImg = node
                    .Descendants()
                    .Single(n => n.GetAttributeValue("class", "").Equals("img"))
                    .Descendants("a")
                    .Single()
                    .Descendants("img")
                    .Single();

                var divText = node.Descendants()
                    .Single(n => n.GetAttributeValue("class", "").Equals("text"));

                var cname = divText
                    .Descendants("h4")
                    .Single()
                    .Descendants("a")
                    .Single()
                    .InnerText;

                var ename = divText
                    .Descendants("h5")
                    .Single()
                    .Descendants("a")
                    .Single()
                    .InnerText;

                var rDate = divText
                    .Descendants("span")
                    .FirstOrDefault()
                    .ChildNodes[1].InnerText;

                var briefDescription = divText
                    .Descendants("p")
                    .Single()
                    .FirstChild
                    .InnerText;

                mi.ImageUri = new Uri(divImg.Attributes[0].Value);
                mi.ChineseName = cname;
                mi.EnglishName = ename;
                mi.ReleaseDateTime = DateTime.ParseExact(rDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                mi.BriefDescription = briefDescription;

                movieInfos.Add(mi);
            }
            return movieInfos;
        }
    }
}