using PegaTrade.Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Core.Data.ServiceModels
{
    public class ImportedCoin
    {
        public string Symbol { get; set; }
        // Total price paid in the Symbol's currency. For example BTC-XRP, Total price paid would be .2 (BTC). In USD-XRP, Total could be $1000.
        public decimal TotalPricePaidInCurrency { get; set; }
        public decimal Shares { get; set; }
        public Types.Exchanges Exchange { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderType { get; set; }
    }

    internal class API_BitTrexImportResult
    {
        public class CoinResults
        {
            public string OrderUuid { get; set; }
            public string Exchange { get; set; }
            public string TimeStamp { get; set; }
            public string OrderType { get; set; }
            public string Limit { get; set; }
            public string Quantity { get; set; }
            public string QuantityRemaining { get; set; }
            public string Commission { get; set; }
            public string Price { get; set; }
            public string PricePerUnit { get; set; }
            public bool IsConditional { get; set; }
            public string Condition { get; set; }
            public bool ImmediateOrCancel { get; set; }
            public string Closed { get; set; }
        }

        public bool success { get; set; }
        public string message { get; set; }
        public IList<CoinResults> result { get; set; }
    }

    internal class API_GDaxImportResult
    {
        public int trade_id { get; set; }
        public string product_id { get; set; }
        public string price { get; set; }
        public string size { get; set; }
        public string order_id { get; set; }
        public DateTime created_at { get; set; }
        public string liquidity { get; set; }
        public string fee { get; set; }
        public bool settled { get; set; }
        public string side { get; set; }
    }

    internal class API_PolonixHistoricCoinPriceResult
    {
        public int date { get; set; }
        public double close { get; set; }
        public double weightedAverage { get; set; }
    }

    internal class API_EthplorerAddressLookup
    {
        internal class ETH
        {
            public double Balance { get; set; }
            public double TotalIn { get; set; }
            public double TotalOut { get; set; }
        }

        internal class TokenInfo
        {
            public string Address { get; set; }
            public string Name { get; set; }
            public double Decimals { get; set; }
            public string Symbol { get; set; }
            public string TotalSupply { get; set; }
            public string Owner { get; set; }
            //public int LastUpdated { get; set; }
            //public int IssuancesCount { get; set; }
            //public int HoldersCount { get; set; }
            // public object Price { get; set; } comes back as bool or Price-Class. Just ignore it.
            public double? TotalIn { get; set; }
            public double? TotalOut { get; set; }
            public string Description { get; set; }
        }

        internal class Token
        {
            public TokenInfo TokenInfo { get; set; }
            public double Balance { get; set; }
            // public long TotalIn { get; set; }
            // public long TotalOut { get; set; }
        }

        public string Address { get; set; }
        public ETH Eth { get; set; }
        // public long CountTxs { get; set; }
        public IList<Token> Tokens { get; set; }
    }
}
