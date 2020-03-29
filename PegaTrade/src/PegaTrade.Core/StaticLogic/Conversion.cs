using System.Collections.Generic;
using System.Linq;
using PegaTrade.Core.Data.ServiceModels;
using PegaTrade.Core.StaticLogic.Models;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;
using Mapster;
using PegaTrade.Core.StaticLogic.Helper;

namespace PegaTrade.Core.StaticLogic
{
    public static class Conversion
    {
        #region Coins

        public static CryptoCoin ToPOCO(this ImportedCoin item)
        {
            return new CryptoCoin
            {
                Symbol = item.Symbol.Replace("USDT-", "USD-"),
                CoinCurrency = CryptoLogic.GenerateCoinCurrencyFromSymbol(item.Symbol),
                PricePerUnit = (item.TotalPricePaidInCurrency / item.Shares),
                Shares = item.Shares,
                Exchange = item.Exchange,
                OrderDate = item.OrderDate, // Closed/Order completed date
                OrderType = CryptoLogic.GetProperOrderTypeFromString(item.OrderType)
            };
        }

        public static List<CoinDisplay> ToCoinDisplay(this List<CryptoCoin> items, Types.CoinCurrency currency)
        {
            if (items.IsNullOrEmpty()) { return new List<CoinDisplay>(); }
            return items.Select(x => x.ToCoinDisplay(currency)).ToList();
        }

        private static CoinDisplay ToCoinDisplay(this CryptoCoin item, Types.CoinCurrency currency)
        {
            // Why manually and not auto-mapper? Tons of data, and most used method. So Mapster can get slow. 
            // Also, You cannot cast a child object into a parent object. Need to create a new instance of it or use reflection.

            return new CoinDisplay
            {
                CoinId = item.CoinId,
                Symbol = item.Symbol,
                Shares = item.Shares,
                OrderDate = item.OrderDate,
                PricePerUnit = item.PricePerUnit,
                CoinCurrency = item.CoinCurrency,
                TotalPricePaidUSD = item.TotalPricePaidUSD,
                OrderType = item.OrderType,
                Notes = item.Notes,
                Exchange = item.Exchange,
                SoldCoinCurrency = item.SoldCoinCurrency,
                SoldPricePerUnit = item.SoldPricePerUnit,
                TotalSoldPricePaidUSD = item.TotalSoldPricePaidUSD,
                PortfolioId = item.PortfolioId,
                MarketCoin = item.MarketCoin,
                GeneratedPricePerUnitETH = item.GeneratedPricePerUnitETH,
                CurrentUSDPriceOfBTC = item.CurrentUSDPriceOfBTC,
                CurrentUSDPriceOfETH = item.CurrentUSDPriceOfETH,
                OrderedDateUSDPriceOfBTC = item.OrderedDateUSDPriceOfBTC,
                OrderedDateUSDPriceOfETH = item.OrderedDateUSDPriceOfETH,
                DisplayCurrency = currency,
                IsCurrentlyCombined = item.IsCurrentlyCombined
            };
        }

        #endregion
    }
}
