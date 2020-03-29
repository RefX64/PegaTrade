using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Coins
{
    public class OfficialCoin
    {
        public int OfficialCoinId { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }

        public MarketCoin MarketCoin { get; set; }
    }
}
