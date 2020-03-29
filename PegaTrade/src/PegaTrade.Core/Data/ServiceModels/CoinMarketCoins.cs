using NetJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Core.Data.ServiceModels
{
    internal class CoinMarketCoins
    {
        [NetJSONProperty("id")]
        public string Id { get; set; }

        [NetJSONProperty("name")]
        public string Name { get; set; }

        [NetJSONProperty("symbol")]
        public string Symbol { get; set; }

        [NetJSONProperty("rank")]
        public string Rank { get; set; }

        [NetJSONProperty("price_usd")]
        public string PriceUsd { get; set; }

        [NetJSONProperty("price_btc")]
        public string PriceBtc { get; set; }

        [NetJSONProperty("24h_volume_usd")]
        public string VolumeUsd24hr { get; set; }

        [NetJSONProperty("market_cap_usd")]
        public string MarketCapUsd { get; set; }

        [NetJSONProperty("available_supply")]
        public string AvailableSupply { get; set; }

        [NetJSONProperty("total_supply")]
        public string TotalSupply { get; set; }

        [NetJSONProperty("percent_change_1h")]
        public string PercentChange1h { get; set; }

        [NetJSONProperty("percent_change_24h")]
        public string PercentChange24h { get; set; }

        [NetJSONProperty("percent_change_7d")]
        public string PercentChange7d { get; set; }

        [NetJSONProperty("last_updated")]
        public string LastUpdated { get; set; }
    }
}
