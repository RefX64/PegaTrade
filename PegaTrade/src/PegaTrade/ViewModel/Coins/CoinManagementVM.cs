using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PegaTrade.Layer.Models;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Helpers;

namespace PegaTrade.ViewModel.Coins
{
    public class CoinManagementVM
    {
        public CryptoCoin Coin { get; set; }
        public IEnumerable<SelectListItem> PortfolioList { get; set; }
        public int SelectedPortfolioID { get; set; }
        public bool IsCreateMode { get; set; }
        public ExchangeApiInfo ExchangeApiInfo { get; set; }
        public IEnumerable<SelectListItem> ExistingSavedImportAPIList { get; set; }

        // -- Mark as sold
        [Required]
        public DateTime SoldDate { get; set; }

        public IEnumerable<SelectListItem> AvailableCurrencyList => new List<SelectListItem>
        {
            new SelectListItem { Text = Types.CoinCurrency.USD.ToString(), Value = ((int)Types.CoinCurrency.USD).ToString() },
            new SelectListItem { Text = Types.CoinCurrency.BTC.ToString(), Value = ((int)Types.CoinCurrency.BTC).ToString() },
            new SelectListItem { Text = Types.CoinCurrency.ETH.ToString(), Value = ((int)Types.CoinCurrency.ETH).ToString() }
        };

        private List<Types.Exchanges> CurrentSupportedImportAPIExchanges { get; set; } = new List<Types.Exchanges>
        {
            Types.Exchanges.BitTrex,
            //Types.Exchanges.CoinBase,
            Types.Exchanges.GDax
        };
        public IEnumerable<SelectListItem> ImportAPIExchangeList() => CurrentSupportedImportAPIExchanges.ToSelectList(x => x.ToString(), x => ((int)x).ToString());

        private List<Types.Exchanges> CurrentSupportedCSVExchanges { get; set; } = new List<Types.Exchanges>
        {
            Types.Exchanges.Custom,
            Types.Exchanges.BitTrex,
            Types.Exchanges.Kraken,
            //Types.Exchanges.CoinBase,
            Types.Exchanges.GDax,
            Types.Exchanges.Binance
        };
        public IEnumerable<SelectListItem> CSVExchangesList() => CurrentSupportedCSVExchanges.ToSelectList(x => x.ToString(), x => ((int)x).ToString());
    }
}
