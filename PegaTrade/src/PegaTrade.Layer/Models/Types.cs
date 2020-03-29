using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Layer.Models
{
    public static class Types
    {
        public enum ResultsType
        {
            Successful = 0,
            Information = 1,
            Warning = 2,
            Error = 4,
            Other = 8,
            Updated = 16
        }

        public enum Platform
        {
            Unknown = 0,
            SystemUtilities = 1,
            PegaTrade = 2
        }
        
        public enum Exchanges
        {
            None = 0,
            Custom = 1,
            BitTrex = 2,
            Kraken = 3,
            CoinBase = 4,
            GDax = 5,
            Poloniex = 6,
            [Description("EthAdress")]
            EtherAddressLookup = 7,
            Binance = 8
        }

        public enum OrderType
        {
            None = 0,
            Custom = 1,
            Buy = 2,
            Sell = 3
        }

        public enum CoinCurrency
        {
            Unknown = 0,
            USD = 1,
            EUR = 2,
            BTC = 3,
            ETH = 4
        }

        public enum PortfolioDisplayType
        {
            Private = 0,
            //FriendsOrFollowOnly = 1,
            MembersOnly = 2,
            Public = 3
        }

        public enum EncryptionType
        {
            Unknown = 0,
            ApiKey_Public = 1,
            ApiKey_Private = 2
        }

        public enum SystemCode
        {
            Unknown = 0,
            Pegasun = 1,
            PegaTrade = 2
        }

        public enum ApiAction
        {
            Unkown = 0,
            TradesImport = 1
        }

        public enum CleanInputType
        {
            Letters, 
            Digits,
            Space,
            Dash,
            AZ09CommonCharsSM
        }

        public enum ConfirmationAuthType
        {
            Unknown = 0,
            EmailConfirm = 1,
            PasswordChange = 2
        }

        public enum PriceValue
        {
            Unknown = 0,
            InitialBoughtValue = 1, // Bought it at $100
            CurrentValue = 2, // Current value of it is now $120
            CurrentProfitLoss = 3, // Profit of $20
            InitialSoldValue = 4, // Only used for CoinsVM/Coins Block Summary. InitialBoughtValue can be used instead in other places.
            SoldEndValue = 5, // Sold it at $130
            SoldProfitLoss = 6 // Sold profit of $30
        }

        public enum SubscriptionLevel
        {
            Unknown = 0,
            Free = 1,
            Premium = 2,
            Ultimate = 3,
            GodLevel = 4,
            Admin = 5
        }

        [Flags]
        public enum EmailPreferences
        {
            None = 0,
            ProductNews = 1,
            CryptoTips = 2,
            OurPortfolioUpdates = 4,
            Default = ProductNews | CryptoTips | OurPortfolioUpdates
        }

        public enum ConvThreadCategory
        {
            Unknown = 0,
            MainDashboard = 1,
            OfficialCoins = 2,
            FeatureRequests = 3
        }

        public enum ConvTagCode
        {
            Unknown = 0,
            Bullish = 1,
            Bear = 2
        }
    }
}
 