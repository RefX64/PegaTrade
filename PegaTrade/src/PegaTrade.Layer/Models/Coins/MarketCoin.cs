using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Coins
{
    [Serializable]
    public class MarketCoin
    {
        public string CoinMarketCapID { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public decimal CurrentSymbolPriceUSD { get; set; } // Current price of the SYMBOL in USD
        public decimal CurrentSymbolPriceBTC { get; set; } // Current price of the SYMBOL in BTC

        // Currently not used. Uncomment as needed.
        public string Rank { get; set; }
        public decimal VolumeUsd24hr { get; set; }
        public decimal MarketCapUsd { get; set; }
        public decimal AvailableSupply { get; set; }
        //public decimal TotalSupply { get; set; }
        //public decimal PercentChange1h { get; set; }
        public decimal PercentChange24h { get; set; }
        public decimal PercentChange7d { get; set; }
        //public string LastUpdated { get; set; }

        public string GetCoinImageName()
        {
            if (string.IsNullOrEmpty(CoinMarketCapID)) { return string.Empty; }
            return $"{CoinMarketCapID}.png";
        }
    }
}
