using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace PegaTrade.Layer.Models.Coins
{
    // TotalPricePaidUSD: We will calculate USD/EUR Profit gain/loss based on this. This will ALWAYS need to be populated. 
    //                    If not, get the CoinCurrency's price on the OrderDate, and use that * Shares as the TotalPricePaidUSD.


    // READ ME: Whatever local properties gets changed here, make sure to assign it correct on the ToCoinDisplay method.
    // Located here: Conversion.ToCoinDisplay()
    [Serializable]
    [DataContract]
    public class CryptoCoin
    {
        #region Database Properties

        public long CoinId { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 6)]
        public string Symbol { get; set; }

        [Required]
        [Range(.000001, int.MaxValue)]
        public decimal Shares { get; set; }

        // Price per 1 unit
        [Required]
        [Range(.00000001, int.MaxValue)]
        public decimal PricePerUnit { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        public Types.CoinCurrency CoinCurrency { get; set; }
        public decimal? TotalPricePaidUSD { get; set; }
        public Types.OrderType OrderType { get; set; }
        public string Notes { get; set; }
        public Types.Exchanges Exchange { get; set; }
        public Types.CoinCurrency SoldCoinCurrency { get; set; }
        public decimal? SoldPricePerUnit { get; set; }
        public decimal? TotalSoldPricePaidUSD { get; set; }
        public int PortfolioId { get; set; }

        #endregion

        #region Local Properties (Not saved to DB)
        public bool IsTransfer { get; set; } // Has been Transferred from other exchange (Not bought/sold here)

        // CoinMarketCap Api
        public MarketCoin MarketCoin { get; set; } = new MarketCoin();

        public decimal CurrentSymbolPriceETH => (MarketCoin.CurrentSymbolPriceUSD == 0 || CurrentUSDPriceOfETH == 0)
                                                 ? 0 : (MarketCoin.CurrentSymbolPriceUSD / CurrentUSDPriceOfETH); // Close enough, don't need to label as generated.

        // The amount you've paid per coins in ETH value. .1 BTC-PriPerUnit, would mean something like .03 ETH-PPU
        public decimal GeneratedPricePerUnitETH { get; set; } // Only available during Currency ETH. Set it manually on UpdateCoinsCurrentPrice
        public decimal CurrentUSDPriceOfBTC { get; set; } // How much BTC right now in Cash? $6000?
        public decimal CurrentUSDPriceOfETH { get; set; }

        // What was the price of BTC/ETH on the day you've placed the order? E.g. Order placed on 7/1/2017, BTC was $3000
        public decimal OrderedDateUSDPriceOfBTC { get; set; }
        public decimal OrderedDateUSDPriceOfETH { get; set; }

        // Gets the current TOTAL value of your holdings. For example (ETH), if bought 5 shares, and ETH is currently $300. returns $1500.
        // How much did you profit/loss (in USD) from ths coin? 
        public decimal CurrentCalculatedProfitUSD { get; set; }


        public bool IsCurrentlyCombined { get; set; } // Currently this coin is combined with similar transaction. It can't be edited.
        #endregion

        #region Logics

        #region Current Holding Calculation

        /// <summary>
        /// Calculate the current profit of this coin/transaction in USD. CurrentMarketValue - InitialPricePaid.
        /// </summary>
        /// <returns></returns>
        protected decimal CalculateCurrentProfit()
        {
            if (TotalPricePaidUSD.GetValueOrDefault() == 0 || Shares == 0 || CoinCurrency == Types.CoinCurrency.Unknown) { return 0; }

            decimal profitLoss = CalculateCurrentTotalPrice_USD() - TotalPricePaidUSD.GetValueOrDefault();
            return profitLoss;
        }

        /// <summary>
        /// Gets the CURRENT/LATEST price of this coin's value in USD. Basically this coin's latest value.
        /// </summary>
        /// <returns></returns>
        protected decimal CalculateCurrentTotalPrice_USD()
        {
            if (CoinCurrency == Types.CoinCurrency.USD) { return MarketCoin.CurrentSymbolPriceUSD * Shares; }
            if (CoinCurrency == Types.CoinCurrency.BTC) { return (MarketCoin.CurrentSymbolPriceBTC * Shares) * CurrentUSDPriceOfBTC; }
            if (CoinCurrency == Types.CoinCurrency.ETH) { return (CurrentSymbolPriceETH * Shares) * CurrentUSDPriceOfETH; }

            return 0;
        }

        public decimal CalculatePercentageChange()
        {
            if (TotalPricePaidUSD.GetValueOrDefault() == 0 || Shares == 0 || CoinCurrency == Types.CoinCurrency.Unknown) { return 0; }

            var change = CalculateCurrentTotalPrice_USD() - TotalPricePaidUSD.GetValueOrDefault();
            return (change / TotalPricePaidUSD.GetValueOrDefault()) * 100;
        }

        public string GenerateStockCSSBasedOnPL(bool textOnly = false)
        {
            return GetCSSBasedOnPercentagePL(CalculatePercentageChange(), textOnly);
        }

        private string GetCSSBasedOnPercentagePL(decimal percentage, bool textOnly = false)
        {
            string css = string.Empty;
            if (percentage > 20) { css = "coinrose-best"; } // > 20.1
            else if (percentage > 10) { css = "coinrose-better"; } // > 10.1
            else if (percentage > 0) { css = "coinrose-good"; } // > 1
            else if (percentage < -20) { css = "coinfell-worst"; } // > -20.1
            else if (percentage < -10) { css = "coinfell-worse"; } // > 8.1
            else if (percentage < 0) { css = "coinfell-bad"; } // > -0.1

            return string.IsNullOrEmpty(css) ? string.Empty : (textOnly ? css + "-text" : css);
        }

        #endregion

        #region Sold calculation

        public decimal GenerateCurrentTotalPriceInUSD()
        {
            if (TotalPricePaidUSD.GetValueOrDefault() == 0 || TotalSoldPricePaidUSD.GetValueOrDefault() == 0)
            {
                return 0;
            }

            return TotalSoldPricePaidUSD.Value - TotalPricePaidUSD.Value;
        }

        public decimal CalculateSoldPercentageChange()
        {
            if (TotalPricePaidUSD.GetValueOrDefault() == 0 || TotalSoldPricePaidUSD.GetValueOrDefault() == 0)
            {
                return 0;
            }

            var change = TotalSoldPricePaidUSD - TotalPricePaidUSD;
            var percentageChange = (change / TotalPricePaidUSD) * 100;
            return percentageChange.GetValueOrDefault();
        }

        public string GetSoldProfitCSS(bool textOnly = true)
        {
            decimal percentageChange = CalculateSoldPercentageChange();
            if (percentageChange == 0) { return string.Empty; }

            return GetCSSBasedOnPercentagePL(percentageChange, textOnly);
        }

        #endregion

        public string GetExchangeIconImageSM()
        {
            if (Exchange == Types.Exchanges.Custom) { return "pegatrade_18.png"; }
            return $"{Exchange.ToString().ToLowerInvariant()}_18.png";
        }

        #endregion
    }
}
