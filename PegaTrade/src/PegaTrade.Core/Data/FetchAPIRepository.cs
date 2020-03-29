using Mapster;
using PegaTrade.Core.Data.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PegaTrade.Core.EntityFramework;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Helpers;
using static NetJSON.NetJSON;
using LocalCoins = PegaTrade.Layer.Models.Coins;
using LocalAccount = PegaTrade.Layer.Models.Account;
using System.IO;
using PegaTrade.Core.StaticLogic.Helper;

namespace PegaTrade.Core.Data
{
    // Some source codes: https://github.com/kroitor/ccxt

    public static class FetchAPIRepository
    {
        public static async Task<List<LocalCoins.MarketCoin>> GetAllCoinsFromApiAsync(bool useLocal = false)
        {
            return await ExecuteFetchCoinFromCMC(1500, false);
        }

        // todo: for homeurl, change to map local path "/file/etc" -> may need to do something like Server.MapPath + "/File" 
        public static async Task<Dictionary<int, LocalCoins.HistoricCoinPrice>> GetAllHistoricPrice_BTC_ETH(string serverMapPath = null, bool useHomeUrl = false)
        {
            // Others: https://poloniex.com/chartData/USDT_BTC-86400.json, https://poloniex.com/chartData/USDT_ETH-86400.json

            string BTCHistoricUrl = !useHomeUrl ? "https://poloniex.com/public?command=returnChartData&currencyPair=USDT_BTC&start=1424304000&end=9999999999&period=14400" : "http://pegatrade.com/files/Prices/USDT_BTC-86400.json";
            string ETH_HistoricUrl = !useHomeUrl ? "https://poloniex.com/public?command=returnChartData&currencyPair=USDT_ETH&start=1424304000&end=9999999999&period=14400" : "http://pegatrade.com/files/Prices/USDT_ETH-86400.json";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Todo: Save the file - Probably need to do on BaseController
                    // string physicalWebRootPath = Server.MapPath("~/");
                    // using (var stream = new FileStream(filePath, FileMode.Create)) { await formFile.CopyToAsync(stream); }

                    var btcGetTask = client.GetAsync(BTCHistoricUrl);
                    var ethGetTask = client.GetAsync(ETH_HistoricUrl);

                    string btcJsonString = await btcGetTask.Result.Content.ReadAsStringAsync();
                    string ethJsonString = await ethGetTask.Result.Content.ReadAsStringAsync();

                    // If error, GetAllHistoricPrice_BTC_ETH(serverMapPath, useHomeUrl: true);

                    if (!string.IsNullOrEmpty(serverMapPath) && !useHomeUrl)
                    {
                        if (btcJsonString.Length > 1000) { var _ = SaveStringToFile(btcJsonString, Path.Combine(serverMapPath, "USDT_BTC-86400.json")).ConfigureAwait(false); }
                        if (ethJsonString.Length > 1000) { var _ = SaveStringToFile(ethJsonString, Path.Combine(serverMapPath, "USDT_ETH-86400.json")).ConfigureAwait(false); }
                    }

                    Dictionary<int, LocalCoins.HistoricCoinPrice> priceResults = new Dictionary<int, LocalCoins.HistoricCoinPrice>();

                    // Get/Add BTC results
                    List<API_PolonixHistoricCoinPriceResult> btcResults = Deserialize<List<API_PolonixHistoricCoinPriceResult>>(btcJsonString);
                    btcResults.ForEach(x =>
                    {
                        priceResults.Add(x.date, new LocalCoins.HistoricCoinPrice { USD_BTH_Price = x.weightedAverage });
                    });

                    // Get/Add ETH results
                    List<API_PolonixHistoricCoinPriceResult> ethResults = Deserialize<List<API_PolonixHistoricCoinPriceResult>>(ethJsonString);
                    ethResults.ForEach(x =>
                    {
                        if (priceResults.ContainsKey(x.date)) { priceResults[x.date].USD_ETH_Price = x.weightedAverage; }
                        else { priceResults.Add(x.date, new LocalCoins.HistoricCoinPrice { USD_ETH_Price = x.weightedAverage }); }
                    });

                    return priceResults;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogException(new[] { "GetAllHistoricPrice_BTC_ETH", $"serverMapPath:{serverMapPath}, useHome:{useHomeUrl}" }, ex);
                if (!useHomeUrl) { return await GetAllHistoricPrice_BTC_ETH(serverMapPath, true); }
                return null; // This will throw an exception... we don't want people to add coins until we know the historic price
            }
        }

        private static async Task SaveStringToFile(string data, string filePath)
        {
            await Task.Delay(20000);
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(data);
                }
            }
        }

        private static async Task<List<LocalCoins.MarketCoin>> ExecuteFetchCoinFromCMC(int total, bool useLocalFile)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync($"https://api.coinmarketcap.com/v1/ticker/?start=0&limit={total}");
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        List<CoinMarketCoins> fetchedCoins = Deserialize<List<CoinMarketCoins>>(jsonString);

                        var config = TypeAdapterConfig<CoinMarketCoins, LocalCoins.MarketCoin>.NewConfig().Map(dst => dst.CoinMarketCapID, src => src.Id)
                            .Map(dst => dst.CurrentSymbolPriceBTC, src => src.PriceBtc)
                            .Map(dst => dst.CurrentSymbolPriceUSD, src => src.PriceUsd);

                        var results = fetchedCoins.Adapt<List<LocalCoins.MarketCoin>>(config.Config);
                        return results;
                    }
                }

                return new List<LocalCoins.MarketCoin>();
            }
            catch
            {
                return new List<LocalCoins.MarketCoin>();
            }
        }

        #region Trades Imports from API Keys
        
        public static async Task<ResultsPair<List<LocalCoins.CryptoCoin>>> ApiImport_EtherAddress(List<LocalCoins.MarketCoin> marketCoins, string etherAddress, int portfolioId, LocalAccount.PegaUser user)
        {
            ResultsPair<List<LocalCoins.CryptoCoin>> generateError(string errorMessage) => ResultsPair.CreateError<List<LocalCoins.CryptoCoin>>(errorMessage);

            LocalCoins.CryptoCoin generateCoin(string symbol, decimal shares, decimal pricePerUnit)
            {
                return new LocalCoins.CryptoCoin
                {
                    CoinCurrency = Types.CoinCurrency.USD,
                    Exchange = Types.Exchanges.EtherAddressLookup,
                    OrderDate = DateTime.Now,
                    OrderType = Types.OrderType.Buy,
                    PortfolioId = portfolioId,
                    Shares = shares,
                    TotalPricePaidUSD = shares * pricePerUnit,
                    PricePerUnit = pricePerUnit,
                    Symbol = symbol
                };
            }

            if (etherAddress.Length != 42) { return generateError("This ethereum address is not valid."); }

            string url = $"https://api.ethplorer.io/getAddressInfo/{etherAddress}?apiKey=freekey";

            using (HttpClient client = new HttpClient())
            {
                string jsonResult = await client.GetStringAsync(url);
                if (string.IsNullOrEmpty(jsonResult)) { return generateError("An error has occured while calling the API endpoint"); }

                API_EthplorerAddressLookup result = Newtonsoft.Json.JsonConvert.DeserializeObject<API_EthplorerAddressLookup>(jsonResult);
                if (result != null)
                {
                    List<LocalCoins.CryptoCoin> coins = new List<LocalCoins.CryptoCoin>();
                    if (result.Eth.Balance > 0)
                    {
                        coins.Add(generateCoin("USD-ETH", (decimal)result.Eth.Balance, marketCoins.First(x => x.Symbol == "ETH").CurrentSymbolPriceUSD));

                        if (!result.Tokens.IsNullOrEmpty())
                        {
                            foreach (var x in result.Tokens.Where(x => x.TokenInfo != null && x.Balance > 0))
                            {
                                try
                                {
                                    LocalCoins.MarketCoin marketCoin = marketCoins.FirstOrDefault(m => m.Name.ToUpper() == x.TokenInfo.Name.ToUpper());
                                    if (marketCoin == null) { marketCoin = marketCoins.FirstOrDefault(m => m.Symbol.ToUpper() == x.TokenInfo.Symbol.ToUpper()); }
                                    if (marketCoin == null) { continue; }

                                    double tokenDecimals = x.TokenInfo.Decimals;
                                    decimal shares = tokenDecimals <= 0 ? (decimal)x.Balance : (decimal)(x.Balance / Math.Pow(10, x.TokenInfo.Decimals));

                                    coins.Add(generateCoin($"USD-{x.TokenInfo.Symbol}", shares, marketCoin.CurrentSymbolPriceUSD));
                                }
                                catch { }
                            };
                        }
                    }

                    return ResultsPair.CreateSuccess(coins);
                }

                return generateError("Something went wrong, or you may not have any coins to import");
            }
        }

        public static async Task<List<ImportedCoin>> ApiImport_BitTrex(string encryptedApiPublic, string encryptedApiSecret, LocalAccount.PegaUser user)
        {
            string apiKey = Cryptography.Decrypt(encryptedApiPublic, Types.EncryptionType.ApiKey_Public, user); // 3df1e84424d2416a9284804f67ce21ca 
            string apiSecret = Cryptography.Decrypt(encryptedApiSecret, Types.EncryptionType.ApiKey_Private, user); // 3fca8d88270f4bfeb56742bf5fd70841

            string url = "https://bittrex.com/api/v1.1/account/getorderhistory";
            
            int nonce = Utilities.GetCurrentEpochTime();
            string uri = $"{url}?apikey={apiKey}&nonce={nonce}&count=100";
            string signKey = Utilities.GenerateHmacSHA512Hash(uri, apiSecret);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("apisign", signKey);
                string jsonResult = await client.GetStringAsync(uri);
                API_BitTrexImportResult result = Newtonsoft.Json.JsonConvert.DeserializeObject<API_BitTrexImportResult>(jsonResult);

                if (result != null && result.success)
                {
                    return result.result.Select(x => new ImportedCoin
                    {
                        Symbol = x.Exchange,
                        Shares = Convert.ToDecimal(x.Quantity),
                        OrderDate = DateTime.Parse(x.TimeStamp),
                        OrderType = x.OrderType,
                        TotalPricePaidInCurrency = Convert.ToDecimal(x.Price),
                        Exchange = Types.Exchanges.BitTrex
                    }).ToList();
                }
                return new List<ImportedCoin>();
            }
        }

        public static async Task<List<ImportedCoin>> ApiImport_GDax(string encryptedApiPublic, string encryptedApiSecret, string passphrase, LocalAccount.PegaUser user)
        {
            string apiKey = Cryptography.Decrypt(encryptedApiPublic, Types.EncryptionType.ApiKey_Public, user); //"c6b8915e430a662b1ee2563e710d8a51";
            string apiSecret = Cryptography.Decrypt(encryptedApiSecret, Types.EncryptionType.ApiKey_Private, user); // "4cIDR3rTUJtQvCnrrr5wGQvcvbeBef0Cy/JTmTmfr4C38rdL1+wEDuTG8jztU1ZXlu6Z2Pti7nWCwUouG463tQ==";

            string url = "https://api.gdax.com/fills";
            int timeStamp = Utilities.GetCurrentEpochTime();
            string toSignMessage = timeStamp + "GET" + "/fills";

            string signKey;
            using (var hmac = new System.Security.Cryptography.HMACSHA256(Convert.FromBase64String(apiSecret)))
            {
                signKey = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toSignMessage)));
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "PegaTrade_APICaller");
                client.DefaultRequestHeaders.Add("CB-ACCESS-KEY", apiKey);
                client.DefaultRequestHeaders.Add("CB-ACCESS-SIGN", signKey);
                client.DefaultRequestHeaders.Add("CB-ACCESS-TIMESTAMP", timeStamp.ToString());
                client.DefaultRequestHeaders.Add("CB-ACCESS-PASSPHRASE", passphrase);

                string jsonResult = await client.GetStringAsync(url);
                List<API_GDaxImportResult> result = Deserialize<List<API_GDaxImportResult>>(jsonResult);

                if (!result.IsNullOrEmpty())
                {
                    return result.Where(x => x.settled).Select(x => new ImportedCoin
                    {
                        Symbol = "USD-" + x.product_id.Replace("-USD", string.Empty),
                        Shares = Convert.ToDecimal(x.size),
                        OrderDate = x.created_at,
                        OrderType = x.side,
                        TotalPricePaidInCurrency = (Convert.ToDecimal(x.price) * Convert.ToDecimal(x.size)),
                        Exchange = Types.Exchanges.GDax
                    }).ToList();
                }
                return new List<ImportedCoin>();
            }
        }

        #endregion
    }
}
