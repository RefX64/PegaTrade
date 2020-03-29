using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PegaTrade.Core.StaticLogic;
using PegaTrade.Core.StaticLogic.Models;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Tests.Helpers;
using PegaTrade.ViewModel.Coins;
using static NetJSON.NetJSON;

namespace PegaTrade.Tests.Tests
{
    [TestClass]
    public class PriceCalculationTests
    {
        [TestMethod]
        public void GetAllCoinsAndTestProfitLoss()
        {
            Types.CoinCurrency currency = Types.CoinCurrency.USD;

            List<CryptoCoin> coins = Deserialize<List<CryptoCoin>>(TestUtilities.File_ReadAllLinesSingle(TestUtilities.GeneratePath(@"TestFiles\Coins\Portfolio_Ref_12-13-17_BitTrexOnly.json")));
            List<MarketCoin> apiFetchedCoins = Deserialize<List<MarketCoin>>(TestUtilities.File_ReadAllLinesSingle(TestUtilities.GeneratePath(@"TestFiles\Coins\CoinMarketCap_12-13-17_getall_coins.json")));

            // Todo: for non CoinCurrency.USD items.
            // Dictionary<int, HistoricCoinPrice> historicPrices = new Dictionary<int, HistoricCoinPrice>();
            // if ((currency == Types.CoinCurrency.BTC || currency == Types.CoinCurrency.ETH) && coins.Any(x => x.OrderDate > DateTime.MinValue)) { historicPrices = await GetAllHistoricCoinPrices(); }

            List<CryptoCoin> updatedCoins = CryptoLogic.UpdateCoinsCurrentPrice(coins, apiFetchedCoins, new Dictionary<int, HistoricCoinPrice>(), useCombinedDisplay: true);

            List<CoinDisplay> displayModeCoins = updatedCoins.ToCoinDisplay(currency);

            CoinsVM vm = new CoinsVM(displayModeCoins)
            {
                DisplayCurrency = currency
            };

            //vm.CalculateCurrentHoldingProfit();
            //vm.CalculateSoldProfit();

            //Console.WriteLine(vm.CurrentHoldingMarketValue());
        }
    }
}
