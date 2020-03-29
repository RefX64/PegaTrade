using PegaTrade.Layer.Models.Account;
using PegaTrade.ViewModel.Community;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.ViewModel
{
    public class FullDashboardVM
    {
        public PegaUser User { get; set; }
        public ConversationsVM ConversationsVM { get; set; }
    }
}
