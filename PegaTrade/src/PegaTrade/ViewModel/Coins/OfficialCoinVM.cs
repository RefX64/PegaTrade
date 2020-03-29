using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Coins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PegaTrade.ViewModel.Community;

namespace PegaTrade.ViewModel.Coins
{
    public class OfficialCoinVM
    {
        public ConversationsVM ConversationsVM { get; set; }

        public OfficialCoin OfficialCoin { get; set; }
        public PegaUser User { get; set; }

        public string Identifier { get; set; } // The coin identifier. Used to fetch the correct coin.
    }
}
