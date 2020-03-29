using System;

namespace PegaTrade.Layer.Models.Account
{
    public class PTUserInfo
    {
        public int UserId { get; set; }
        public string Location { get; set; }
        public string Website { get; set; }
        public string Bio { get; set; }
        public string FavoriteCoins { get; set; }
        public Types.SubscriptionLevel SubscriptionLevel { get; set; }
        public DateTime? SubscriptionExpireDate { get; set; }

        public string GetSubscriptionColor()
        {
            switch (SubscriptionLevel)
            {
                case Types.SubscriptionLevel.Premium: return "#0d78a9";
                case Types.SubscriptionLevel.Ultimate: return "#009688";
                case Types.SubscriptionLevel.Admin:
                case Types.SubscriptionLevel.GodLevel: return "#d00444";
                default: return "#333";
            }
        }
    }
}