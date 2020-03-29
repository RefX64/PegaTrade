using PegaTrade.Layer.Models.Coins;
using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Account
{
    // For the users we would like to view. For example, when we want to view holdings of JohnDoe12's portfolio "MyTopPicks".
    public class ViewUser
    {
        public string Username { get; set; }
        public string PortfolioName { get; set; }
        public int SelectedPortfolioID { get; set; }
        public List<Portfolio> Portfolios { get; set; }
    }
}
