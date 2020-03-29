using PegaTrade.Layer.Models.Account;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Coins
{
    public class PortfolioRequest
    {
        public int PortfolioID { get; set; }
        public ViewUser ViewUser { get; set; }
        public Types.CoinCurrency Currency { get; set; } = Types.CoinCurrency.USD;
        public bool UseCombinedDisplay { get; set; } = true;
        public bool ViewOtherUser { get; set; }
    }
}
