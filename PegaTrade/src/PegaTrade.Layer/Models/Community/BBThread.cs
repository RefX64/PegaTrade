using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using PegaTrade.Layer.Models.Coins;

namespace PegaTrade.Layer.Models.Community
{
    public class BBThread
    {
        public int ThreadId { get; set; }
        public int UserId { get; set; }
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } // Do not change the name of this. Binding will mess up. Need it to be same as BBComent's Message property.
        public Types.ConvThreadCategory CategoryCode { get; set; }
        public int? OfficialCoinId { get; set; }
        public DateTime CreateDate { get; set; }
        public Types.ConvTagCode TagCode { get; set; }
        public bool IsClosed { get; set; }

        // Local
        public int TotalComments { get; set; }
        public string ThreadName { get; set; } // Helps generate ID for specific threads
        
        public bool ShowOfficialCoinNameOnThread { get; set; } // Used only for "All" thread. Shows Coin name. E.g. DemoUser ($Vechain)
        public OfficialCoin OfficialCoin { get; set; }

        public int CurrentLoggedInUserID { get; set; }

        public List<BBComment> ThreadComments { get; set; }
        public Account.PegaUser User { get; set; }
        public VoteResult VoteResult { get; set; }
    }
}
