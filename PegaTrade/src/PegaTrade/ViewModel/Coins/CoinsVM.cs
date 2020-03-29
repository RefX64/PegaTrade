using System.Collections.Generic;
using System.Linq;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Core.StaticLogic.Models;
using PegaTrade.Layer.Models;

namespace PegaTrade.ViewModel.Coins
{
    public class CoinsVM
    {
        public Types.CoinCurrency DisplayCurrency { get; set; }
        public List<CoinDisplay> AllCoins { private get; set; }
        public List<CoinDisplay> HoldingCoins => AllCoins.Where(x => x.OrderType == Types.OrderType.Buy).ToList();
        public List<CoinDisplay> SoldCoins => AllCoins.Where(x => x.OrderType == Types.OrderType.Sell).OrderByDescending(x => x.OrderDate).ToList();

        public string CurrentHoldingProfitCSS { get; set; }
        public string SoldProfitCSS { get; set; }
        public bool ViewOtherUser { get; set; }

        public CoinsVM() { }
        
        public CoinsVM(List<CoinDisplay> coins)
        {
            InitializeCoinsVM(coins);
        }

        public decimal GetTotalPriceOf(Types.PriceValue priceType)
        {
            switch (priceType)
            {
                case Types.PriceValue.InitialBoughtValue:
                case Types.PriceValue.CurrentValue:
                case Types.PriceValue.CurrentProfitLoss:
                    return HoldingCoins.Sum(x => x.GetPrice(priceType));

                case Types.PriceValue.InitialSoldValue:
                case Types.PriceValue.SoldEndValue:
                case Types.PriceValue.SoldProfitLoss:
                    return SoldCoins.Sum(x => x.GetPrice(priceType));
            }

            return 0;
        }

        public void GenerateProfitLossCSS()
        {
            CurrentHoldingProfitCSS = GetTotalPriceOf(Types.PriceValue.CurrentProfitLoss) > 0 ? "price-rose-text" : "price-fell-text";
            SoldProfitCSS = GetTotalPriceOf(Types.PriceValue.SoldProfitLoss) > 0 ? "price-rose-text" : "price-fell-text";
        }

        private void InitializeCoinsVM(List<CoinDisplay> coins)
        {
            AllCoins = coins;
            if (!AllCoins.IsNullOrEmpty()) { GenerateProfitLossCSS(); }
        }
    }
}
