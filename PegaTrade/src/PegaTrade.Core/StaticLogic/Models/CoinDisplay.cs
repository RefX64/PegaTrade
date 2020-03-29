using System.Globalization;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;

namespace PegaTrade.Core.StaticLogic.Models
{
    public sealed class CoinDisplay : CryptoCoin
    {
        /// <summary>
        /// The currency we want to display the coin as. Do not confuse with CoinCurrency. CoinCurrency is what currency we used to Buy/Sell the coin.
        /// DisplayCurrency is the currency we want to display the Profit/Loss at. 
        /// </summary>
        public Types.CoinCurrency DisplayCurrency { private get; set; }

        // Current Price (in BTC/USD) of the Symbol. E.g. (b.0004/$1) - Just for main display
        public string DisplayCurrentSymbolPriceCurrency()
        {
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return $"{DisplayCurrency.Symbol()}{CurrentSymbolPriceETH.ToDecimalPrecision(6)} / ${MarketCoin.CurrentSymbolPriceUSD.ToTwoDigit()}"; }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return $"{MarketCoin.CurrentSymbolPriceBTC.RemoveTrailingZero()} / {DisplayCurrency.Symbol()}{MarketCoin.CurrentSymbolPriceUSD.UsdToEuro().ToTwoDigit()}"; }

            return $"฿{MarketCoin.CurrentSymbolPriceBTC.RemoveTrailingZero()} / ${MarketCoin.CurrentSymbolPriceUSD.ToTwoDigit()}"; // BTC & USD
        }

        public decimal GetPrice(Types.PriceValue priceType)
        {
            switch (priceType)
            {
                case Types.PriceValue.InitialBoughtValue:
                case Types.PriceValue.InitialSoldValue:
                    return GetGeneratedInitialPricePaid();

                case Types.PriceValue.CurrentValue: return GetGeneratedCurrentHoldingValue();
                case Types.PriceValue.CurrentProfitLoss: return GetGeneratedProfitInCurrency();
                case Types.PriceValue.SoldEndValue: return GetGeneratedSoldEndValue();
                case Types.PriceValue.SoldProfitLoss: return (GetGeneratedSoldEndValue() - GetGeneratedInitialPricePaid()).ToDecimalPrecision(8);
            }
            return 0;
        }

        public string ViewPrice(Types.PriceValue priceType)
        {
            return ToCurrencyDisplayFormat(GetPrice(priceType));
        }

        // --- Initial price paid
        private decimal GetGeneratedInitialPricePaid()
        {
            if (DisplayCurrency == Types.CoinCurrency.USD) { return TotalPricePaidUSD.GetValueOrDefault(); }
            if (DisplayCurrency == Types.CoinCurrency.BTC) { return (GetGeneratedCorrectInitialPricePerUnit()).ToDecimalPrecision(8); }
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return (GetGeneratedCorrectInitialPricePerUnit()).ToDecimalPrecision(6); }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return TotalPricePaidUSD.GetValueOrDefault().UsdToEuro(); }

            return 0;
        }

        // --- Current Investment value. For example, bought for $100, coin is currently at $105. Returns $105.
        private decimal GetGeneratedCurrentHoldingValue()
        {
            if (DisplayCurrency == Types.CoinCurrency.USD) { return CalculateCurrentTotalPrice_USD(); }
            if (DisplayCurrency == Types.CoinCurrency.BTC) { return (MarketCoin.CurrentSymbolPriceBTC * Shares).ToDecimalPrecision(8); }
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return (CurrentSymbolPriceETH * Shares).ToDecimalPrecision(6); }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return CalculateCurrentTotalPrice_USD().UsdToEuro(); }

            return 0;
        }

        // --- Current Profit/Loss for this coin
        private decimal GetGeneratedProfitInCurrency()
        {
            decimal profit = CalculateCurrentProfit();
            if (profit == 0) { return 0; }

            if (DisplayCurrency == Types.CoinCurrency.USD) { return profit; }
            if (DisplayCurrency == Types.CoinCurrency.BTC) { return (profit / CurrentUSDPriceOfBTC).ToDecimalPrecision(8); }
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return (profit / CurrentUSDPriceOfETH).ToDecimalPrecision(6); }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return profit.UsdToEuro(); }

            return 0;
        }

        // Sold end value. Full value of the coin you've sold. For example, Bought for $100, sold for $120. Returns $120.
        private decimal GetGeneratedSoldEndValue()
        {
            decimal profit = TotalSoldPricePaidUSD.GetValueOrDefault();
            if (profit == 0 || TotalPricePaidUSD.GetValueOrDefault() == 0) { return 0; } // If TotalBoughtPrice is 0, it's a transfer coin. Not sure of profit.

            if (DisplayCurrency == Types.CoinCurrency.USD) { return profit; }
            if (DisplayCurrency == Types.CoinCurrency.BTC) { return (profit / OrderedDateUSDPriceOfBTC).ToDecimalPrecision(8); }
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return (profit / OrderedDateUSDPriceOfETH).ToDecimalPrecision(6); }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return profit.UsdToEuro(); }

            return 0;
        }

        // ---------- Logic
        private decimal GetGeneratedCorrectInitialPricePerUnit()
        {
            if (CoinCurrency == Types.CoinCurrency.USD)
            {
                if (TotalPricePaidUSD == 0) { return 0; }
                if (DisplayCurrency == Types.CoinCurrency.BTC) { return TotalPricePaidUSD.GetValueOrDefault() / OrderedDateUSDPriceOfBTC; }
                if (DisplayCurrency == Types.CoinCurrency.ETH) { return TotalPricePaidUSD.GetValueOrDefault() / OrderedDateUSDPriceOfETH; }
            }
            if (DisplayCurrency == Types.CoinCurrency.ETH)
            {
                return GeneratedPricePerUnitETH * Shares;
            }
            return PricePerUnit * Shares;
        }

        private string ToCurrencyDisplayFormat(decimal price)
        {
            if (DisplayCurrency == Types.CoinCurrency.USD) { return $"{DisplayCurrency.Symbol()}{price.ToTwoDigit()}"; }
            if (DisplayCurrency == Types.CoinCurrency.BTC) { return $"{DisplayCurrency.Symbol()}{price}"; }
            if (DisplayCurrency == Types.CoinCurrency.ETH) { return $"{DisplayCurrency.Symbol()}{price}"; }
            if (DisplayCurrency == Types.CoinCurrency.EUR) { return $"{DisplayCurrency.Symbol()}{price.ToTwoDigit()}"; }

            return "0";
        }
    }
}
