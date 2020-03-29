using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("BBThreadVotes", Schema = "PegaTrade")]
    public partial class BBThreadVotes
    {
        [Key]
        [Column("VoteID")]
        public long VoteId { get; set; }
        public bool IsUpvote { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }
        [Column("ThreadID")]
        public int ThreadId { get; set; }

        [ForeignKey("ThreadId")]
        [InverseProperty("BBThreadVotes")]
        public BBThreads Thread { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("BBThreadVotes")]
        public Users User { get; set; }
    }
}
