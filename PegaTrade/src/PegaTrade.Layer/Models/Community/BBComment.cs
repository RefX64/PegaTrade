using System;
using System.ComponentModel.DataAnnotations;

namespace PegaTrade.Layer.Models.Community
{
    public class BBComment
    {
        public long CommentId { get; set; }
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } // Do not change the name of this. Binding will mess up. Need it to be same as BBThread's Message property.
        public int UserId { get; set; }
        public int ThreadId { get; set; }
        public DateTime CreateDate { get; set; }
        public Types.ConvTagCode TagCode { get; set; }

        public Account.PegaUser User { get; set; }
        public VoteResult VoteResult { get; set; }

        // Local
        public int CurrentLoggedInUserID { get; set; }
    }
}
