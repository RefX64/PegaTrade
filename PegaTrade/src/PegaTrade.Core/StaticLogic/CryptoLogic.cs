using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PegaTrade.Core.Data;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Language;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Layer.Models.Helpers;

namespace PegaTrade.Core.StaticLogic
{
    public static class CryptoLogic
    {
        #region Portfolio
        
        public static async Task<List<Portfolio>> GetAllUserPortfolioAsync(PegaUser user)
        {
            return await CryptoRepository.GetAllUserPortfolioAsync(user);
        }

        public static async Task<ResultsPair<Portfolio>> InsertNewPortfolio(PegaUser user, string portfolioName, Types.PortfolioDisplayType displayType, bool isDefault)
        {
            var validateRequest = IsValidDBRequest(user, 0);
            if (!validateRequest.IsSuccess) { return ResultsPair.CreateError<Portfolio>(validateRequest.Message); }

            return await CryptoRepository.InsertNewPortfolio(user.UserId, portfolioName, displayType, isDefault);
        }

        public static ResultsPair<Portfolio> UpdatePortfolio(int portfolioId, string portfolioName, Types.PortfolioDisplayType displayType, bool isDefault, PegaUser user)
        {
            if (!IsValidDBRequest(user, portfolioId, validatePortfolio: true).IsSuccess) { return ResultsPair.CreateError<Portfolio>(Lang.PortfolioNotFound); }
            return CryptoRepository.UpdatePortfolio(portfolioId, portfolioName, displayType, isDefault);
        }

        public static async Task<ResultsItem> DeletePortfolio(int portfolioId, PegaUser user)
        {
            var validateRequest = IsValidDBRequest(user, portfolioId, validatePortfolio: true);
            if (!validateRequest.IsSuccess) { return validateRequest; }

            ResultsItem deleteResult = await CryptoRepository.DeletePortfolio(portfolioId);

            // Since we're deleting the only portfolio we have, create a new default one.
            if (user.Portfolios.Count == 1) { await InsertNewPortfolio(user, "Default", Types.PortfolioDisplayType.Private, true); }

            return deleteResult;
        }

        #endregion

        #region Coins
        
        public static async Task<CryptoCoin> GetSingleCoinByUser(long coinId, PegaUser user)
        {
            CryptoCoin coin = await CryptoRepository.GetSingleCoinByUser(coinId);
            return !IsValidDBRequest(user, coin.PortfolioId, validatePortfolio: true, validateDemoUser: false).IsSuccess ? null : coin;
        }

        public static async Task<List<CryptoCoin>> GetAllUserCoinsByPortfolioIdAsync(int portfolioId)
        {
            // Make sure controller has !user.HasPortfolio check
            return await CryptoRepository.GetAllUserCoinsByPortfolioIdAsync(portfolioId);
        }

        public static async Task<ResultsItem> InsertCoinsToUserPortfolioAsync(List<CryptoCoin> coins, PegaUser user, int? portfolioId = null)
        {
            // Validation: Do not allow symbol to end in -USD. 
            if (coins.Any(x => x.Symbol.ToUpperInvariant().Contains("-USD")))
            {
                if (coins.Count == 1) { return ResultsItem.Error("The coin symbol must not end in -USD. It should be USD-. For example: USD-ETH, USD-XRP."); }
                coins = coins.Where(x => !x.Symbol.ToUpperInvariant().Contains("-USD")).ToList();
            }

            if (portfolioId != null) { coins.ForEach(x => x.PortfolioId = portfolioId.Value); }
            if (coins.Any(x => !IsValidDBRequest(user, x.PortfolioId, validatePortfolio: true).IsSuccess)) { return ResultsItem.Error(Lang.PortfolioNotFound); }

            return await CryptoRepository.InsertCoinsToUserPortfolioAsync(coins);
        }

        public static async Task<ResultsItem> UpdateUserCoinAsync(CryptoCoin coin, PegaUser user)
        {
            var validateRequest = IsValidDBRequest(user, coin.PortfolioId, validatePortfolio: true);
            if (!validateRequest.IsSuccess) { return validateRequest; }

            return await CryptoRepository.UpdateUserCoinAsync(coin);
        }

        public static async Task<ResultsItem> DeleteUserCoinsAsync(List<CryptoCoin> coins, PegaUser user)
        {
            if (coins.Any(x => !IsValidDBRequest(user, x.PortfolioId, validatePortfolio: true).IsSuccess)) { return ResultsItem.Error(Lang.PortfolioNotFound); }
            return await CryptoRepository.DeleteUserCoinAsync(coins);
        }

        public static async Task<ResultsItem> DeleteAllUserCoinByExchangeAsync(int portfolioId, Types.Exchanges exchange, PegaUser user)
        {
            var validateRequest = IsValidDBRequest(user, portfolioId, validatePortfolio: true);
            if (!validateRequest.IsSuccess) { return validateRequest; }

            return await CryptoRepository.DeleteAllUserCoinByExchangeAsync(portfolioId, exchange);
        }

        public static async Task<ResultsPair<Portfolio>> ResetAllUserTrades(PegaUser user)
        {
            return await CryptoRepository.ResetAllUserTrades(user);
        }

        #endregion

        #region Official Coins

        public static List<OfficialCoin> GetAllOfficialCoins() => CryptoRepository.GetAllOfficialCoins();

        public static OfficialCoin FindOfficialCoinFromIdentifier(string identifier, List<OfficialCoin> officialCoins)
        {
            OfficialCoin coin = officialCoins.FirstOrDefault(x => x.Name.EqualsTo(identifier)) ?? officialCoins.FirstOrDefault(x => x.Symbol.EqualsTo(identifier));
            return coin;
        }

        #endregion

        #region API

        public static async Task<List<ExchangeApiInfo>> GetAllUserExchangeAPIAsync(int userId)
        {
            return await CryptoRepository.GetAllUserExchangeAPIAsync(userId);
        }

        public static ResultsItem InsertNewAPI(ExchangeApiInfo exchangeApiInfo, PegaUser user)
        {
            var validateRequest = IsValidDBRequest(user, 0);
            if (!validateRequest.IsSuccess) { return validateRequest; }

            return CryptoRepository.InsertNewAPI(exchangeApiInfo, user);
        }

        public static ResultsItem DeleteAPI(int apiId, PegaUser user)
        {
            var validateRequest = IsValidDBRequest(user, apiId, validateAPI: true);
            if (!validateRequest.IsSuccess) { return validateRequest; }
            
            return CryptoRepository.DeleteAPI(apiId);
        }

        #endregion

        #region Sorting, Grouping, etc. (Trades Organize)

        /// <summary>
        /// Combines trades/transactions into one from the same type. For example 2 BTC-XRP trades, will turn into 1 with weighted average.
        /// </summary>
        public static List<CryptoCoin> ToCoinsCombinedMode(List<CryptoCoin> coins)
        {
            List<CryptoCoin> formattedCoins = new List<CryptoCoin>();
            coins = coins.OrderBy(x => x.OrderDate).ToList();

            foreach (CryptoCoin coin in coins)
            {
                // Fix this
                // Todo: Debug try/catch logger -> If debug, throw exception, else, log exception with details
                List<CryptoCoin> similarExistingCoins = formattedCoins.Where(x => x.OrderType == coin.OrderType && x.Symbol.EqualsTo(coin.Symbol) && x.Exchange == coin.Exchange).ToList();
                if (similarExistingCoins.IsNullOrEmpty()) { formattedCoins.Add(coin); continue; }

                if (coin.TotalPricePaidUSD.GetValueOrDefault() == 0 && !similarExistingCoins.Any(x => x.TotalPricePaidUSD.GetValueOrDefault() == 0))
                {
                    // This is a tranfer coin (coins that was transfered from other exchanges) but no other transfer coins exist. 
                    // Transfer coins cannot be merged with transaction coins. Create a seperate entry for them.
                    formattedCoins.Add(coin); continue;
                }

                if (coin.TotalPricePaidUSD.GetValueOrDefault() > 0 && !similarExistingCoins.Any(x => x.TotalPricePaidUSD.GetValueOrDefault() > 0))
                {
                    // This is a transaction coin (buy/sell from same exchange) but no other transaction coins exist. Since cannot be merged with transfer coin, create & continue.
                    formattedCoins.Add(coin); continue;
                }

                CryptoCoin existingFormattedCoin = coin.TotalPricePaidUSD.GetValueOrDefault() > 0 ? similarExistingCoins.First(x => x.TotalPricePaidUSD.GetValueOrDefault() > 0)
                                                                                                  : similarExistingCoins.First(x => x.TotalPricePaidUSD.GetValueOrDefault() == 0);
                if (existingFormattedCoin == null) { formattedCoins.Add(coin); continue; } // Nothing exists yet. Just add it to list as the first one.

                existingFormattedCoin.IsCurrentlyCombined = true;

                // These are transfer coins. They will always remain $0. Just merge shares, and keep them at $0.
                if (existingFormattedCoin.TotalPricePaidUSD.GetValueOrDefault() == 0 || coin.TotalPricePaidUSD.GetValueOrDefault() == 0)
                {
                    existingFormattedCoin.Shares += coin.Shares;
                    existingFormattedCoin.OrderDate = existingFormattedCoin.OrderDate > coin.OrderDate ? existingFormattedCoin.OrderDate : coin.OrderDate;
                    continue;
                }

                // Cannot be divisible by 0
                bool buyOrderValidated = existingFormattedCoin.PricePerUnit > 0 && coin.PricePerUnit > 0;
                bool sellOrderValidated = coin.OrderType == Types.OrderType.Sell && coin.TotalSoldPricePaidUSD.GetValueOrDefault() > 0 && coin.SoldPricePerUnit.GetValueOrDefault() > 0;

                // OrderType:Sell needs both buyOrder & sellOrder validated.
                if (existingFormattedCoin.Shares <= 0 && !buyOrderValidated || 
                   (coin.OrderType == Types.OrderType.Sell && !buyOrderValidated && !sellOrderValidated)) { continue; }

                // Weighted Avg: ((1st_Price * Shares) + (2nd_Price * Shares)) / TotalShares
                decimal totalShares = existingFormattedCoin.Shares + coin.Shares;
                decimal weightedAveragePricePerUnit = ((existingFormattedCoin.PricePerUnit * existingFormattedCoin.Shares) + (coin.PricePerUnit * coin.Shares)) / totalShares;
                
                existingFormattedCoin.OrderDate = existingFormattedCoin.OrderDate > coin.OrderDate ? existingFormattedCoin.OrderDate : coin.OrderDate;
                existingFormattedCoin.PricePerUnit = weightedAveragePricePerUnit;
                existingFormattedCoin.TotalPricePaidUSD += coin.TotalPricePaidUSD;
                if (coin.OrderType == Types.OrderType.Sell)
                {
                    existingFormattedCoin.SoldPricePerUnit = ((existingFormattedCoin.SoldPricePerUnit.GetValueOrDefault() * existingFormattedCoin.Shares) + (coin.SoldPricePerUnit.GetValueOrDefault() * coin.Shares)) / totalShares;
                    existingFormattedCoin.TotalSoldPricePaidUSD += coin.TotalSoldPricePaidUSD;
                }
                existingFormattedCoin.Shares = totalShares;
            }

            return formattedCoins;
        }

        #endregion

        #region Helpers

        public static List<CryptoCoin> UpdateCoinsCurrentPrice(List<CryptoCoin> coins, List<MarketCoin> apiFetchedCoins, Dictionary<int, HistoricCoinPrice> historicPrices, bool useCombinedDisplay = true, Types.CoinCurrency currency = Types.CoinCurrency.USD)
        {
            MarketCoin btcCoin = apiFetchedCoins.FirstOrDefault(x => x.Symbol == "BTC");
            MarketCoin ethCoin = apiFetchedCoins.FirstOrDefault(x => x.Symbol == "ETH");
            if (btcCoin == null || ethCoin == null) { return coins; }

            coins = CorrectCryptoCoinSymbols(coins);

            if (useCombinedDisplay) { coins = ToCoinsCombinedMode(coins); }

            foreach (CryptoCoin coin in coins)
            {
                string[] supportedSymbolPrefix = { "BTC-", "ETH-", "USDT-", "USD-" };
                if (!supportedSymbolPrefix.Any(x => coin.Symbol.StartsWith(x))) { continue; } // We do not support anything other than BTC, ETH, USD

                string[] symbol = coin.Symbol.Contains("-") ? coin.Symbol.Split('-') : new[] { "", coin.Symbol };

                coin.CurrentUSDPriceOfBTC = btcCoin.CurrentSymbolPriceUSD;
                coin.CurrentUSDPriceOfETH = ethCoin.CurrentSymbolPriceUSD;

                if (coin.OrderDate != DateTime.MinValue)
                {
                    coin.OrderedDateUSDPriceOfBTC = GetPriceOfETHorBTCOnSpecificDate(historicPrices, coin.OrderDate.ToEpochDayAt12am(), Types.CoinCurrency.BTC);
                    if (currency == Types.CoinCurrency.ETH)
                    {
                        coin.OrderedDateUSDPriceOfETH = GetPriceOfETHorBTCOnSpecificDate(historicPrices, coin.OrderDate.ToEpochDayAt12am(), Types.CoinCurrency.ETH);
                        if (coin.OrderedDateUSDPriceOfETH > 0)
                        {
                            coin.GeneratedPricePerUnitETH = (coin.TotalPricePaidUSD.GetValueOrDefault() / coin.Shares) / coin.OrderedDateUSDPriceOfETH;
                        }
                    }
                }

                MarketCoin fetchedCoin = apiFetchedCoins.FirstOrDefault(x => x.Symbol.Contains(symbol[1]));
                if (fetchedCoin == null) { continue; }

                coin.MarketCoin.CurrentSymbolPriceBTC = fetchedCoin.CurrentSymbolPriceBTC;
                coin.MarketCoin.CurrentSymbolPriceUSD = fetchedCoin.CurrentSymbolPriceUSD;
                coin.MarketCoin.Name = fetchedCoin.Name;
                coin.MarketCoin.CoinMarketCapID = fetchedCoin.CoinMarketCapID;
            }

            return coins;
        }

        /// <summary>
        /// CSV Imports ALL trades as seperate. You could have 10 buys, and 5 sold. If those 5 sold were from the 10 coins you bought,
        /// It will return 15 trades, instead of of 10 trades (bought 10 coins but 5 is sold).
        /// This will combine all Buys/Sells into a single trade. For example, If you buy BTC-ETH, and then sell BTC-ETH, it will count as 1 trade.
        /// </summary>
        /// <param name="unformattedCoins"></param>
        /// <returns></returns>
        public static List<CryptoCoin> FormatCoinsAndBoughtSoldLogicUpdate(List<CryptoCoin> unformattedCoins)
        {
            // Sort by order date. Oldest date to newest.
            List<CryptoCoin> coins = unformattedCoins.OrderBy(x => x.OrderDate).ToList();
            List<CryptoCoin> formattedCoins = new List<CryptoCoin>();

            foreach (CryptoCoin coin in coins)
            {
                if (coin.OrderType == Types.OrderType.Buy) { formattedCoins.Add(coin); }
                else if (coin.OrderType == Types.OrderType.Sell)
                {
                    // Order type is sell. Find previous bought coin, remove that from formattedCoins, and re-add it as sold.
                    formattedCoins = RemoveSoldCoinsAndReturnUpdatedList(formattedCoins, coin);
                }
            }

            return formattedCoins;
        }

        /// <summary>
        /// Will return coins with both Bought & Sold Coins
        /// </summary>
        /// <param name="coins">Formatted coins, has proper bought/sold coins.</param>
        /// <param name="soldCoin">The new trade (type->sold) coin that needs to be formatted and added in to the list.</param>
        /// <returns></returns>
        private static List<CryptoCoin> RemoveSoldCoinsAndReturnUpdatedList(List<CryptoCoin> coins, CryptoCoin soldCoin)
        {
            if (soldCoin.TotalPricePaidUSD == 0) { return coins; } // Yikes... what kind of coin is this?

            // LastOrDefault: First-In, First-Out system.
            CryptoCoin holdingCoin = coins.LastOrDefault(x => x.Symbol == soldCoin.Symbol && x.OrderType == Types.OrderType.Buy);

            // There are no previous buy orders. This could mean the coins were transferred from other exchanges.
            // We don't know when this sold coin was bought. Just add it with the sold information.
            if (holdingCoin == null || holdingCoin.TotalPricePaidUSD.GetValueOrDefault() == 0)
            {
                UpdateSoldCoinsFieldFromOriginal(null, soldCoin, updateOnlySoldPrice: true);
                soldCoin.IsTransfer = true;
                coins.Add(soldCoin);
                return coins;
            }

            if (holdingCoin.Shares == soldCoin.Shares) // Even match, holding coin's share is now 0. So remove it.
            {
                coins.Remove(holdingCoin); // remove the bought coin.

                // This is  a sold coin type, so assign all CoinCurrency, PricePerUnit, and TotalPricePaid to SOLD value. 
                // Then update bought value from previous bought coin.
                soldCoin = UpdateSoldCoinsFieldFromOriginal(holdingCoin, soldCoin);
                coins.Add(soldCoin);
            }
            else if (holdingCoin.Shares > soldCoin.Shares) // We didn't sell ALL. We're still holding shares, but we've also sold some.
            {
                CryptoCoin clonedHoldingCoin = Utilities.DeepClone(holdingCoin);

                // Scenario Example:
                // Holding: 10-Shares, Bought them Total:$1000... sold 6-Shares, Sold them total: $700
                // Update remaining holding $ value: (1000 / 10) * 4 = $400. (We have $400 worth left)
                // Get total price paid by the sold shares amount: (1000 / 10) * 6 = $600 (Initially, we paid $600 total for these)
                // Update the bought/sold price: 6-bought for: $600, 6-sold for: $700. Profit: $100. (Bought all for $600, sold for $700)

                // Update the holding coin
                decimal remainingShares = holdingCoin.Shares - soldCoin.Shares;
                decimal remainingHoldingTotalPricePaid = (holdingCoin.TotalPricePaidUSD.Value / holdingCoin.Shares) * remainingShares; // $400
                holdingCoin.Shares = remainingShares;
                holdingCoin.TotalPricePaidUSD = remainingHoldingTotalPricePaid;

                // Get/Update the actual total price paid based on the shares sold
                clonedHoldingCoin.TotalPricePaidUSD = (clonedHoldingCoin.TotalPricePaidUSD.Value / clonedHoldingCoin.Shares) * soldCoin.Shares; // $600

                // Total Shares: 6, Bought price: $600, Sold Price: $700. Total Profit: $100
                soldCoin = UpdateSoldCoinsFieldFromOriginal(clonedHoldingCoin, soldCoin);
                coins.Add(soldCoin);
            }
            else if (soldCoin.Shares > holdingCoin.Shares) 
            {
                coins.Remove(holdingCoin);
                CryptoCoin clonedSoldCoin = Utilities.DeepClone(soldCoin);

                // Scenario Example:
                // Holding: 10-Shares, bought them Total at:$1000... sold 12-Shares, sold them total at: $900
                // Update total sold $ based on actual/holding's bought $: (900 / 12) * 10 = $750. (Paid $1000 for 10, Sold 10 for $750)
                // Get total remaining shares PriceUSD: (900 / 12) * 2 = $150. (We still have 2 shares $150 worth to sell)
                // Try again and sell: 2-Shares, Total: $150

                // Update the actual total price paid based on the shares sold (Scenario: selling 10 shares)
                decimal soldPricePerCoinUSD = (clonedSoldCoin.TotalPricePaidUSD.Value / clonedSoldCoin.Shares);
                soldCoin.TotalPricePaidUSD = soldPricePerCoinUSD * holdingCoin.Shares; // $750 (Sold 10 for $750. that we bought for $1000)

                // Update sold coins and add it to the list
                decimal remainingShares = soldCoin.Shares - holdingCoin.Shares; // Remaining shares (2)
                soldCoin = UpdateSoldCoinsFieldFromOriginal(holdingCoin, soldCoin);
                soldCoin.Shares = holdingCoin.Shares; // holdingCoin.Shares is the amount we were able to sell in this transaction. (10)
                coins.Add(soldCoin);

                // (Scenario: We still have a sold coin of -2 shares. Try again, and find the next 2 shares to sell.)
                clonedSoldCoin.Shares = remainingShares; // How much we have remaining/Need to sell.
                clonedSoldCoin.TotalPricePaidUSD = soldPricePerCoinUSD * clonedSoldCoin.Shares; // Total price worth of remaining shares.

                return RemoveSoldCoinsAndReturnUpdatedList(coins, clonedSoldCoin);
            }

            return coins;
        }

        /// <summary>
        /// Updates the coin as sold. Populates the sold coin's SOLD & Original properties, such as SoldCurrency, SoldPrice, and TotalSoldPriceUSD,
        /// based on the original coin. Basically, update soldCoin to have both bought & sold values.
        /// </summary>
        /// <param name="originalCoin">The original coin that was bought</param>
        /// <param name="soldCoin">The sold version of that original coin</param>
        /// <param name="updateOnlySoldPrice">
        ///     If we don't know the bought price, set bought details as 0, and only update the sold price. The coin could have been transferred over and sold here;
        ///     which could be the reason we don't know bought price.
        /// </param> 
        /// <returns>Sold version of the coin with original bought/paid price</returns>
        private static CryptoCoin UpdateSoldCoinsFieldFromOriginal(CryptoCoin originalCoin, CryptoCoin soldCoin, bool updateOnlySoldPrice = false)
        {
            // If soldCoin's sold prices are not already populated, that means their sold values resides in the bought properties.
            // So, move bought properties over to the sold property.
            if (soldCoin.SoldCoinCurrency == Types.CoinCurrency.Unknown) { soldCoin.SoldCoinCurrency = soldCoin.CoinCurrency; }
            if (soldCoin.SoldPricePerUnit.GetValueOrDefault() == 0) { soldCoin.SoldPricePerUnit = soldCoin.PricePerUnit; }
            if (soldCoin.TotalSoldPricePaidUSD.GetValueOrDefault() == 0) { soldCoin.TotalSoldPricePaidUSD = soldCoin.TotalPricePaidUSD; }

            if (!updateOnlySoldPrice)
            {
                // originalCoin still holds the original BOUGHT price for this sold coin. 
                soldCoin.CoinCurrency = originalCoin.CoinCurrency;
                soldCoin.PricePerUnit = originalCoin.PricePerUnit;
                soldCoin.TotalPricePaidUSD = originalCoin.TotalPricePaidUSD;
            }
            else
            {
                // Unknown bought price, but it's a sold coin so we know the sold price. (Could be tranferred over).
                soldCoin.PricePerUnit = 0;
                soldCoin.TotalPricePaidUSD = 0;
            }
            
            return soldCoin;
        }

        /// <summary>
        /// Generates the currency of the transaction from the symbol. BTC-XRP means it was BTC.
        /// </summary>
        public static Types.CoinCurrency GenerateCoinCurrencyFromSymbol(string symbol)
        {
            // BitTrex -> BTC-ETH, BTC-ANS, USDT-BTC, USDT-ETH... The first symbols are Currency type.

            symbol = symbol.ToUpperInvariant();
            if (symbol.StartsWith("USDT-") || symbol.StartsWith("USD-")) { return Types.CoinCurrency.USD; }
            if (symbol.StartsWith("BTC-")) { return Types.CoinCurrency.BTC; }
            if (symbol.StartsWith("ETH-")) { return Types.CoinCurrency.ETH; }

            return Types.CoinCurrency.Unknown;
        }

        /// <summary>
        /// Determines whether it a buy or a sell order?
        /// </summary>
        public static Types.OrderType GetProperOrderTypeFromString(string orderTypeAsString)
        {
            string orderTypeLower = orderTypeAsString.ToLowerInvariant();
            if (orderTypeLower.Contains("buy"))
            {
                return Types.OrderType.Buy;
            }
            if (orderTypeLower.Contains("sell"))
            {
                return Types.OrderType.Sell;
            }

            return Types.OrderType.None;
        }

        /// <summary>
        /// Calculates the total price paid of a trade. When you place a buy/sell order, you do not know the actual USD price paid/sold. 
        /// This method will calculate that based on the coin's order date & historic data on BTC/ETH.
        /// </summary>
        /// <param name="coin">The coin to generate price for</param>
        /// <param name="historicData">Historic prices of BTC/ETH</param>
        /// <param name="forceUseCurrency">Ignore coin's default currency, and use the forced currency.</param>
        /// <returns>Total Price paid USD</returns>
        public static decimal GenerateTotalPricePaidUSD(CryptoCoin coin, Dictionary<int, HistoricCoinPrice> historicData, Types.CoinCurrency forceUseCurrency = Types.CoinCurrency.Unknown)
        {
            // E.g. USDT-BTC -> PricePerUnit (one whole unit): $5000, TotalShares: .5. TotalPricePaidUSD: $2500
            if (coin.CoinCurrency == Types.CoinCurrency.USD) { return coin.PricePerUnit * coin.Shares; }
            
            int epochTimeInt = coin.OrderDate.ToEpochDayAt12am();
            if (historicData.ContainsKey(epochTimeInt))
            {
                // E.g. ETH-GNT -> Currency:ETH. June 1st, 2017, ETH Price: $300. PricePerUnit: .0008, Shares: 5000. CurrentETH: $320, CurrentPPU: .0009
                // Total Price Paid: ((.0008*5000)*300)=$1200... CurrentPriceWorth: ((.0009*5000)*320)=$1440. Profit: $240
                decimal coinCurrencyPriceUSD = (coin.CoinCurrency == Types.CoinCurrency.ETH || forceUseCurrency == Types.CoinCurrency.ETH) 
                                                                     ? (decimal)historicData[epochTimeInt].USD_ETH_Price : (decimal)historicData[epochTimeInt].USD_BTH_Price;

                return (coin.PricePerUnit * coin.Shares) * coinCurrencyPriceUSD;
            }

            return 0;
        }

        /// <summary>
        /// Get's the price of BTC or ETH at the specified date. For example, how much was BTC on 6/1/2017? 
        /// Epoch time must be at 12am. Use: DateTime.ToEpochDayAt12am()
        /// </summary>
        public static decimal GetPriceOfETHorBTCOnSpecificDate(Dictionary<int, HistoricCoinPrice> historicData, int epochTimeInt, Types.CoinCurrency currency)
        {
            if (historicData.ContainsKey(epochTimeInt))
            {
                return currency == Types.CoinCurrency.ETH ? (decimal)historicData[epochTimeInt].USD_ETH_Price : (decimal)historicData[epochTimeInt].USD_BTH_Price;
            }

            return 0;
        }

        /// <summary>
        /// Gets the latest price of Coin Currency.  For example, if we want the current price of BTC, we pass in BTC as coin currency.
        /// </summary>
        public static decimal GetLatestPriceOfCurrency(Types.CoinCurrency coinCurrency, List<MarketCoin> marketCoins)
        {
            if (coinCurrency == Types.CoinCurrency.BTC) { return marketCoins.FirstOrDefault(x => x.Symbol == "BTC").CurrentSymbolPriceUSD; }
            if (coinCurrency == Types.CoinCurrency.ETH) { return marketCoins.FirstOrDefault(x => x.Symbol == "ETH").CurrentSymbolPriceUSD; }
            return 0;
        }

        /// <summary>
        /// Gets the latest price of Coin Currency.  For example, if we want the current price of BTC, we pass in BTC as coin currency.
        /// </summary>
        public static decimal GetLatestPriceOfSymbol(string symbol, List<MarketCoin> marketCoins, Types.CoinCurrency currency = Types.CoinCurrency.USD)
        {
            MarketCoin marketCoin = marketCoins.FirstOrDefault(x => x.Symbol == symbol.Split('-')[1].ToUpperInvariant());
            if (marketCoin == null) { return 0; }

            if (currency == Types.CoinCurrency.USD) { return marketCoin.CurrentSymbolPriceUSD; }
            if (currency == Types.CoinCurrency.BTC) { return marketCoin.CurrentSymbolPriceBTC; }
            if (currency == Types.CoinCurrency.ETH)
            {
                MarketCoin ethCoin = marketCoins.FirstOrDefault(x => x.Symbol == "ETH");
                return marketCoin.CurrentSymbolPriceUSD / ethCoin.CurrentSymbolPriceUSD;
            }
            return 0;
        }

        /// <summary>
        /// Gets the price per unit of the coin based on currency. For example, Coin-USD: $500, Coin-BTC: .1
        /// </summary>
        public static decimal GetPricePerUnitOfCoin(CryptoCoin coin, Types.CoinCurrency currency)
        {
            if (currency == Types.CoinCurrency.BTC) { return coin.MarketCoin.CurrentSymbolPriceBTC; }
            if (currency == Types.CoinCurrency.USD) { return coin.MarketCoin.CurrentSymbolPriceUSD; }
            if (currency == Types.CoinCurrency.ETH) { return coin.CurrentSymbolPriceETH; }
            return 0;
        }

        /// <summary>
        /// Makes sure the symbols are in correct format. For example, recently, ANS changed to NEO. This makes sure the changes are applied.
        /// </summary>
        private static List<CryptoCoin> CorrectCryptoCoinSymbols(List<CryptoCoin> coins)
        {
            foreach (CryptoCoin coin in coins)
            {
                if (coin.Exchange == Types.Exchanges.BitTrex && coin.Symbol.ContainsTheWord("BCC")) { coin.Symbol = coin.Symbol.Replace("BCC", "BCH"); }
                else if (coin.Exchange == Types.Exchanges.Kraken && coin.Symbol.ContainsTheWord("XBT")) { coin.Symbol = coin.Symbol.Replace("XBT", "BTC"); }
                else if (coin.Symbol.ContainsTheWord("ANS")) { coin.Symbol = coin.Symbol.Replace("ANS", "NEO"); }
            }

            return coins;
        }

        #endregion

        private static ResultsItem IsValidDBRequest(PegaUser user, int id, bool validateAPI = false, bool validatePortfolio = false, bool validateDemoUser = true)
        {
            if (validateDemoUser && user.Username.ToUpperInvariant() == "DEMOUSER") { return ResultsItem.Error("DemoUser is not allowed to perform this action. Please login or create an account for free."); }
            if (validatePortfolio && !user.HasPortfolio(id)) { return ResultsItem.Error(Lang.PortfolioNotFound); }
            if (validateAPI && !user.ExchangeApiList.Any(x => x.Id == id)) { return ResultsItem.Error(Lang.ApiDeleteNotFound); }

            return ResultsItem.Success(string.Empty);
        }
    }
}
