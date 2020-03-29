using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using PegaTrade.Core.StaticLogic.Helper;
using PegaTrade.Layer.Models.Coins;
using PegaTrade.Helpers;
using PegaTrade.Layer.Models;
using PegaTrade.ViewModel.Coins;

namespace PegaTrade.ViewModel
{
    public class PortfolioVM
    {
        public CoinsVM CoinsVM { get; set; }
        public Layer.Models.Account.ViewUser ViewUser { get; set; }

        #region Main display type
        public List<Portfolio> Portfolios { get; set; }
        public int SelectedPortfolio { get; set; }
        public bool ViewOtherUser { get; set; }
        #endregion

        #region CRUD Portfolio
        public Portfolio Portfolio { get; set; }
        public bool IsCreateMode { get; set; }
        #endregion
        
        public IEnumerable<SelectListItem> PortfolioList => Portfolios.ToSelectList(x => x.Name, x => x.PortfolioId.ToString());

        public IEnumerable<SelectListItem> PortfolioDisplayTypeList()
        {
            return Utilities.EnumToList<Types.PortfolioDisplayType>().ToSelectList(x => x.ToString(), x => ((int)x).ToString());
        }
    }
}
