using PegaTrade.Layer.Models.Community;
using System;
using System.Collections.Generic;
using System.Text;
using PegaTrade.Layer.Models.Coins;

// Session/Cache Fetch Models used in BaseControllers
namespace PegaTrade.Layer.Models.Helpers
{
    public class MarketFetchData
    {
        public List<MarketCoin> MarketCoins { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool CurrentlyUpdating { get; set; }
    }

    public class OfficialCoinFetchData
    {
        public List<OfficialCoin> OfficialCoins { get; set; }
        public DateTime LastMarketUpdated { get; set; }
    }

    public class ConversationFetchData
    {
        public int Take { get; set; }
        public Types.ConvThreadCategory Category { get; set; }
        public int OfficialCoinId { get; set; }
        public List<BBThread> Results { get; set; }
    }
}
