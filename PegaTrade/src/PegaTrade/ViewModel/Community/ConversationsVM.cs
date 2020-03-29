using PegaTrade.Layer.Models.Account;
using PegaTrade.Layer.Models.Community;
using System.Collections.Generic;

namespace PegaTrade.ViewModel.Community
{
    public class ConversationsVM
    {
        public PegaUser CurrentUser { get; set; }
        public List<BBThread> Threads { get; set; }
        public BBThread CurrentThread { get; set; }
        public bool IsCreateCommentMode { get; set; } // We're wanting to create a new comment instead of a new thread. 

        public bool HideCreateNewPost { get; set; }
    }
}
