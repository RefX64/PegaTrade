using System;
using System.Collections.Generic;
using System.Text;
using PegaTrade.Layer.Models;

namespace PegaTrade.Core.StaticLogic
{
    public static class SubscriptionLogic
    {
        public static int GetMaxAllowedPortfolioPerUser(Types.SubscriptionLevel subscriptionLevel)
        {
            switch (subscriptionLevel)
            {
                case Types.SubscriptionLevel.Free: return 3;
                case Types.SubscriptionLevel.Premium: return 5;
                case Types.SubscriptionLevel.Ultimate: return 10;
                case Types.SubscriptionLevel.GodLevel:
                case Types.SubscriptionLevel.Admin: return 99;
            }
            return 0;
        }

        public static int GetMaxAllowedTradesImportPerUser(Types.SubscriptionLevel subscriptionLevel)
        {
            switch (subscriptionLevel)
            {
                case Types.SubscriptionLevel.Free: return 300;
                case Types.SubscriptionLevel.Premium: return 2000;
                case Types.SubscriptionLevel.Ultimate: return 10000;
                case Types.SubscriptionLevel.GodLevel:
                case Types.SubscriptionLevel.Admin: return 100000000;
            }
            return 0;
        }
    }
}
