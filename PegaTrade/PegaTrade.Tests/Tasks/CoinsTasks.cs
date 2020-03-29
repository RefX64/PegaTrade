using PegaTrade.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegaTrade.Core.EntityFramework;
using PegaTrade.Core.StaticLogic;
using LocalCoins = PegaTrade.Layer.Models.Coins;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace PegaTrade.Tests.Tasks
{
    [TestClass]
    public class CoinsTasks
    {
        public CoinsTasks()
        {
            AppSettingsTestInit.Initialize();
        }

        //[TestMethod]
        public void RefreshOfficialCoinsInDB()
        {
            List<LocalCoins.MarketCoin> fetchedCoins = FetchAPILogic.GetAllCoinsFromApiAsync().Result;

            using (PegasunDBContext db = new PegasunDBContext())
            {
                List<OfficialCoins> existingOfficialCoins = db.OfficialCoins.ToList();
                fetchedCoins.ForEach(x =>
                {
                    if (!existingOfficialCoins.Any(o => o.Name == x.CoinMarketCapID))
                    {
                        db.OfficialCoins.Add(new OfficialCoins
                        {
                            Name = x.CoinMarketCapID,
                            Symbol = x.Symbol
                        });
                    }
                });
                db.SaveChanges();
            }
        }

        [TestMethod]
        public void SaveCoinImagesFromCoinMarketCap()
        {
            bool reFetchAllImages = false;
            string directoryPath = @"E:\Developed\Website\PegaTrade\src\PegaTrade\wwwroot\lib\img\coins\size32";
            string baseImageUrl = "https://files.coinmarketcap.com/static/img/coins/32x32/";

            string url = "https://coinmarketcap.com/all/views/all/";
            HtmlDocument doc = new HtmlWeb().Load(url);

            HtmlNode tableNode = doc.DocumentNode.SelectSingleNode("//table[@id='currencies-all']");
            HtmlNodeCollection tableRows = tableNode.SelectNodes("tbody/tr");

            Parallel.ForEach(tableRows, new ParallelOptions { MaxDegreeOfParallelism = 20 }, x =>
            {
                try
                {
                    string coinMarketCapId = x.Id.Replace("id-", string.Empty);
                    string imgOuterHtml = x.ChildNodes[3].OuterHtml;
                    string imgNumber = Regex.Match(imgOuterHtml, @"s-s-\d+").Value.Replace("s-s-", string.Empty);

                    string filePath = $"{directoryPath}\\{coinMarketCapId}.png";
                    if (!File.Exists(filePath) || reFetchAllImages)
                    {
                        string imageUrl = $"{baseImageUrl}{imgNumber}.png";
                        var request = System.Net.WebRequest.CreateHttp(imageUrl);
                        var responseTask = request.GetResponseAsync();
                        Task.WaitAll(responseTask);

                        var response = responseTask.Result;
                        var responseStream = response.GetResponseStream();

                        if (responseStream != null && responseStream != Stream.Null)
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                            {
                                responseStream.CopyTo(stream);
                                Console.WriteLine($"Created/Updated: {coinMarketCapId}");
                            }
                        }

                        System.Threading.Thread.Sleep(500);
                    }
                }
                catch { }
            });
        }

        //[TestMethod] // Old Way - Does not work anymore
        //public void SaveCoinImagesFromCoinMarketCap()
        //{
        //    bool reFetchAllImages = false;
        //    string directoryPath = @"E:\Developed\Website\PegaTrade\src\PegaTrade\wwwroot\lib\img\coins\size32";
        //    string baseImageUrl = "https://files.coinmarketcap.com/static/img/coins/32x32/"; // symbolName.png (ripple.png)

        //    List<LocalCoins.MarketCoin> allCoins = FetchAPILogic.GetAllCoinsFromApiAsync().Result;
        //    var first300Coins = allCoins.Take(1500);

        //    Parallel.ForEach(first300Coins, new ParallelOptions { MaxDegreeOfParallelism = 20 }, x =>
        //    {
        //        try
        //        {
        //            string filePath = $"{directoryPath}\\{x.CoinMarketCapID}.png";
        //            if (!File.Exists(filePath) || reFetchAllImages)
        //            {
        //                string url = $"{baseImageUrl}{x.CoinMarketCapID}.png";
        //                var request = System.Net.WebRequest.CreateHttp(url);
        //                var responseTask = request.GetResponseAsync();
        //                Task.WaitAll(responseTask);

        //                var response = responseTask.Result;
        //                var responseStream = response.GetResponseStream();

        //                if (responseStream != null && responseStream != Stream.Null)
        //                {
        //                    using (FileStream stream = new FileStream(filePath, FileMode.Create)) { responseStream.CopyTo(stream); }
        //                }

        //                System.Threading.Thread.Sleep(500);
        //            }
        //        }
        //        catch { }
        //    });
        //}
    }
}
