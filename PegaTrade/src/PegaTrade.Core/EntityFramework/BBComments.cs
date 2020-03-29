using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("BBComments", Schema = "PegaTrade")]
    public partial class BBComments
    {
        public BBComments()
        {
            BBCommentVotes = new HashSet<BBCommentVotes>();
        }

        [Key]
        [Column("CommentID")]
        public long CommentId { get; set; }
        [Required]
        [StringLength(500)]
        public string Message { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }
        [Column("ThreadID")]
        public int ThreadId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }
        public short? TagCode { get; set; }

        [ForeignKey("ThreadId")]
        [InverseProperty("BBComments")]
        public BBThreads Thread { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("BBComments")]
        public Users User { get; set; }
        [InverseProperty("Comment")]
        public ICollection<BBCommentVotes> BBCommentVotes { get; set; }
    }
}
