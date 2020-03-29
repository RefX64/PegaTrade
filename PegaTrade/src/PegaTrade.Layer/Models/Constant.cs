using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Layer.Models
{
    public static class Constant
    {
        #region Sessions
        public static class Session
        {
            public const string GlobalAllCoinsAPIData = "GlobalAllCoinsAPIData";
            public const string GlobalHistoricCoinsPrice = "GlobalHistoricCoinsPrice";
            public const string GlobalAllOfficialCoins = "GlobalAllOfficialCoins";
            public const string GlobalThreadsByCategory = "GlobalThreadsByCategory";
            public const string GlobalThreadsWithComments = "GlobalThreadsWithComments";

            public const string SessionCurrentUser = "SessionCurrentUser";
        }
        #endregion
    }
}
