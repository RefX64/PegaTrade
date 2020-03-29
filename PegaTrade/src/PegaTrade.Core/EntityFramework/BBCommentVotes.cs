using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("BBCommentVotes", Schema = "PegaTrade")]
    public partial class BBCommentVotes
    {
        [Key]
        [Column("VoteID")]
        public long VoteId { get; set; }
        public bool IsUpvote { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }
        [Column("CommentID")]
        public long CommentId { get; set; }

        [ForeignKey("CommentId")]
        [InverseProperty("BBCommentVotes")]
        public BBComments Comment { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("BBCommentVotes")]
        public Users User { get; set; }
    }
}
