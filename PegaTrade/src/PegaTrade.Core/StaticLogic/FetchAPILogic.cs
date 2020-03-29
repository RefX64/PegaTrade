using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PegaTrade.Core.Data.ServiceModels;
using PegaTrade.Core.Data;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Core.StaticLogic
{
    public static class FetchAPILogic
    {
        public static async Task<List<MarketCoin>> GetAllCoinsFromApiAsync()
        {
            return await FetchAPIRepository.GetAllCoinsFromApiAsync();
        }

        public static async Task<Dictionary<int, HistoricCoinPrice>> GetAllHistoricPrice_BTC_ETH(string serverMapPath = null, bool useHomeUrl = false)
        {
            return await FetchAPIRepository.GetAllHistoricPrice_BTC_ETH(serverMapPath, useHomeUrl);
        }

        #region Import Trades/Data from Exchanges
        // .csv files -> Need proper encoding. Open .csv with notepad, and click "Save As". On the dialog, it should show current encoding.

        public static List<ImportedCoin> ImportCSV_Coins(Types.Exchanges exchange, Stream fileStream)
        {
            switch (exchange)
            {
                case Types.Exchanges.Custom: return CSVImport_Custom(fileStream);
                case Types.Exchanges.BitTrex: return CSVImport_BitTrex(fileStream);
                case Types.Exchanges.Kraken: return CSVImport_Kraken(fileStream);
                case Types.Exchanges.GDax: return CSVImport_GDax(fileStream);
                case Types.Exchanges.Binance: return CSVImport_Binance(fileStream);
            }

            return new List<ImportedCoin>();
        }

        /// <summary>
        /// Converts ImportedCoin coins into the proper CryptoCoin. 
        /// Then generates the total price paid of the converted coins. Call this after every import. 
        /// </summary>
        public static List<CryptoCoin> FormatCoinsAndGenerateTotalPricePaid(List<ImportedCoin> importedCoins, Dictionary<int, HistoricCoinPrice> historicCoinPrice)
        {
            // Filter: Always need Shares & Total price to be over 0 for each coins
            List<CryptoCoin> localExtractedCoins = importedCoins.Where(x => x.Shares > 0 && x.TotalPricePaidInCurrency > 0).Select(x => x.ToPOCO()).ToList();
            localExtractedCoins.ForEach(x => { x.TotalPricePaidUSD = CryptoLogic.GenerateTotalPricePaidUSD(x, historicCoinPrice); });

            return localExtractedCoins;
        }

        // CSV Import Logic for different Exchanges
        private static List<ImportedCoin> CSVImport_Custom(Stream fileStream)
        {
            List<ImportedCoin> importedCoins = new List<ImportedCoin>();

            // 0Symbol(Currency-Coin), 1.OrderType, 2Quantity, 3TotalPricePaid, 4OrderDate
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.Default))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!line.Contains(',') || line.ToLower().StartsWith("symbol")) { continue; }

                    try
                    {
                        string[] values = line.Split(',').Select(x => x.Trim()).ToArray();
                        ImportedCoin coin = new ImportedCoin
                        {
                            Symbol = values[0],
                            OrderType = values[1],
                            Shares = decimal.Parse(values[2]),
                            TotalPricePaidInCurrency = decimal.Parse(values[3]),
                            OrderDate = DateTime.Parse(values[4]),
                            Exchange = Types.Exchanges.Custom
                        };

                        importedCoins.Add(coin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return importedCoins;
        }

        private static List<ImportedCoin> CSVImport_BitTrex(Stream fileStream)
        {
            List<ImportedCoin> importedCoins = new List<ImportedCoin>();

            // 0.OrderUuid,1.Exchange,2.Type,3.Quantity,4.Limit,5.CommissionPaid,6.Price,7.Opened,8.Closed
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.Unicode))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!line.Contains(',') || line.ToLower().StartsWith("orderuuid")) { continue; }

                    try
                    {
                        string[] values = line.Split(',').Select(x => x.Trim()).ToArray();
                        ImportedCoin coin = new ImportedCoin
                        {
                            Symbol = values[1],
                            OrderType = values[2],
                            Shares = decimal.Parse(values[3]),
                            TotalPricePaidInCurrency = decimal.Parse(values[6]),
                            OrderDate = DateTime.Parse(values[8]),
                            Exchange = Types.Exchanges.BitTrex
                        };

                        importedCoins.Add(coin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return importedCoins;
        }

        private static List<ImportedCoin> CSVImport_Kraken(Stream fileStream)
        {
            List<ImportedCoin> importedCoins = new List<ImportedCoin>();

            // 0txid	1ordertxid	2pair	3time	4type	5ordertype	6price	7cost	8fee 9vol	10margin	11misc	12ledgers
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.Default)) // ANSI
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!line.Contains(',') || line.ToLower().StartsWith("\"txid\"")) { continue; }

                    try
                    {
                        string[] values = line.Split(',').Select(x => x.Replace("\"", string.Empty).Trim()).ToArray();

                        string unformattedSymbol = values[2];
                        string appendAtTheFront = string.Empty;

                        unformattedSymbol = unformattedSymbol.Replace("XX", "X");
                        if (unformattedSymbol.EndsWith("XBT"))
                        {
                            unformattedSymbol = unformattedSymbol.Replace("XBT", string.Empty);
                            appendAtTheFront = "BTC-";
                        }
                        else if (unformattedSymbol.EndsWith("USD") || unformattedSymbol.EndsWith("USDT"))
                        {
                            unformattedSymbol = unformattedSymbol.Replace("USD", string.Empty).Replace("USDT", string.Empty);
                            appendAtTheFront = "USD-";
                        }
                        
                        if (unformattedSymbol.Length == 4 && unformattedSymbol.EndsWith("X") || unformattedSymbol.EndsWith("Z"))
                        {
                            unformattedSymbol = unformattedSymbol.Substring(0, (unformattedSymbol.Length - 1)); // Remove last character. X:BTC, Z:Cash
                        }
                        if (unformattedSymbol.Length == 4 && unformattedSymbol.StartsWith("X") || unformattedSymbol.StartsWith("Z"))
                        {
                            unformattedSymbol = unformattedSymbol.Substring(1); // Remove first character.
                        }
                        unformattedSymbol = unformattedSymbol.Replace("XBT", "BTC");

                        string formattedSymbol = appendAtTheFront + unformattedSymbol;
                        if (formattedSymbol.Length != 7) { continue; }

                        ImportedCoin coin = new ImportedCoin
                        {
                            Symbol = formattedSymbol,
                            OrderType = values[4],
                            Shares = decimal.Parse(values[9]),
                            TotalPricePaidInCurrency = decimal.Parse(values[7]),
                            OrderDate = DateTime.Parse(values[3]),
                            Exchange = Types.Exchanges.Kraken
                        };

                        importedCoins.Add(coin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return importedCoins;
        }

        private static List<ImportedCoin> CSVImport_GDax(Stream fileStream)
        {
            List<ImportedCoin> importedCoins = new List<ImportedCoin>();

            // 0-trade id	1-product	2-side	3-created at	4-size	5-size unit	6-price	7-fee	8-total	price/fee/total unit
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.ASCII))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (!line.Contains(',') || line.ToLower().StartsWith("trade")) { continue; }

                    try
                    {
                        string[] values = line.Split(',').Select(x => x.Trim()).ToArray();
                        ImportedCoin coin = new ImportedCoin
                        {
                            Symbol = "USD-" + values[1].Replace("-USD", string.Empty),
                            OrderType = values[2],
                            Shares = decimal.Parse(values[4]),
                            TotalPricePaidInCurrency = decimal.Parse(values[6]) * decimal.Parse(values[4]),
                            OrderDate = DateTime.Parse(values[3]),
                            Exchange = Types.Exchanges.GDax
                        };

                        importedCoins.Add(coin);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return importedCoins;
        }

        private static List<ImportedCoin> CSVImport_Binance(Stream fileStream)
        {
            string extractSymbolFromMarket(string market, string currency)
            {
                market = market.Replace(currency, string.Empty);
                return $"{currency}-{market}";
            }

            List<ImportedCoin> importedCoins = new List<ImportedCoin>();

            //0Date	1Market	2Type	3Price	4Amount	5Total	6Fee	7Fee Coin
            using (var stream = new StreamReader(fileStream))
            {
                using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(fileStream))
                {
                    do
                    {
                        while (reader.Read())
                        {
                            string date = reader.GetString(0);
                            if (!string.IsNullOrEmpty(date) && date.ToUpperInvariant() != "DATE")
                            {
                                string market = reader.GetString(1).ToUpperInvariant(); // market: ETHBTC -> Symbol|Currency, bought ETH with BTC
                                string symbol = string.Empty;
                                if (market.EndsWith("BTC")) { symbol = extractSymbolFromMarket(market, "BTC"); }
                                else if (market.EndsWith("USDT")) { symbol = extractSymbolFromMarket(market, "USDT"); }
                                else if (market.EndsWith("ETH")) { symbol = extractSymbolFromMarket(market, "ETH"); }

                                if (!string.IsNullOrEmpty(symbol))
                                {
                                    ImportedCoin coin = new ImportedCoin
                                    {
                                        Symbol = symbol,
                                        OrderType = reader.GetString(2),
                                        Shares = decimal.Parse(reader.GetString(4)),
                                        TotalPricePaidInCurrency = decimal.Parse(reader.GetString(5)),
                                        OrderDate = DateTime.Parse(reader.GetString(0)),
                                        Exchange = Types.Exchanges.Binance
                                    };

                                    importedCoins.Add(coin);
                                }
                            }
                        }
                    } while (reader.NextResult());
                }
            }

            return importedCoins;
        }

        // API Imports
        public static async Task<ResultsPair<List<CryptoCoin>>> ApiImport_EtherAddress(List<MarketCoin> marketCoins, string etherAddress, int portfolioId, PegaUser user)
        {
            return await FetchAPIRepository.ApiImport_EtherAddress(marketCoins, etherAddress, portfolioId, user);
        }

        public static async Task<List<ImportedCoin>> ImportAPI_Coins(ExchangeApiInfo exchangeApiInfo, PegaUser user)
        {
            switch (exchangeApiInfo.Exchange)
            {
                case Types.Exchanges.BitTrex: return await FetchAPIRepository.ApiImport_BitTrex(exchangeApiInfo.ApiPublic, exchangeApiInfo.ApiPrivate, user);
                case Types.Exchanges.GDax: return await FetchAPIRepository.ApiImport_GDax(exchangeApiInfo.ApiPublic, exchangeApiInfo.ApiPrivate, exchangeApiInfo.ApiThirdKey, user);
            }

            return new List<ImportedCoin>();
        }

        #endregion
    }
}
