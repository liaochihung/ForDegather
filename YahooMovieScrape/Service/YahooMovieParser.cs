using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YahooMovieScrape.Model;

namespace YahooMovieScrape.Service
{
    /*
    // 每部電影的資訊都放在div item中
 <div class="item">
    <div class="img">
        <a href="https://tw.rd.yahoo.com/referurl/movie/thisweek/info/*https://tw.movies.yahoo.com/movieinfo_main.html/id=6344">
            <img src="https://s.yimg.com/vu/movies/fp/mpost4/63/44/6344.jpg" title="死亡筆記本：決戰新世界">
		</a>
    </div>
    <div class="text">
        <h4>
            <a href="https://tw.rd.yahoo.com/referurl/movie/thisweek/info/*https://tw.movies.yahoo.com/movieinfo_main.html/id=6344">死亡筆記本：決戰新世界</a>
        </h4>
        <h5>
            <a href="https://tw.rd.yahoo.com/referurl/movie/thisweek/info/*https://tw.movies.yahoo.com/movieinfo_main.html/id=6344">Death
                Note Light up the NEW world</a></h5>
        <span class="date">上映日期：<span>2016-11-25</span></span>
        <p>
            ★ 史上最經典鬥智推理代表作《死亡筆記本》，電影版十年後全新篇章再起！ ★ 《寄生獸》東出昌大 X 《紙之月》池松壯亮 X 《暗殺教室》
            <ins>...<a href="movieinfo_main.html/id=6344" hpp="thisweek-guide">詳全文</a></ins>
        </p>
        <div class="clearfix">
            <ul class="links clearfix">
                <li class="intro"><a
                        href="https://tw.rd.yahoo.com/referurl/movie/thisweek/info/*https://tw.movies.yahoo.com/movieinfo_main.html/id=6344">電影介紹</a>
                </li>
                <li class="trailer"><a
                        href="https://tw.rd.yahoo.com/referurl/movie/thisweek/trailer/*https://tw.movies.yahoo.com/video/死亡筆記本-決戰新世界-中文版預告-015209257.html">預告片</a>
                </li>
                <li class="photo"><a
                        href="https://tw.rd.yahoo.com/referurl/movie/thisweek/photo/*https://tw.movies.yahoo.com/movieinfo_photos.html/id=6344">劇照</a>
                </li>
                <li class="time"><a
                        href="https://tw.rd.yahoo.com/referurl/movie/thisweek/time/*https://tw.movies.yahoo.com/movietime_result.html/id=6344">時刻表</a>
                </li>
            </ul>
        </div>
    </div>
</div>
     */
    public static class YahooMovieParser
    {
        private static readonly object Sync = new object();
        public static List<MovieInfo> Parse(string webContent)
        {
            var html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(webContent);

            var root = html.DocumentNode;

            // this nodes include all the movie shows in current page, see 
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