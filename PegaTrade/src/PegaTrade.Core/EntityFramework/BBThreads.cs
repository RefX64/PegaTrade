using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("BBThreads", Schema = "PegaTrade")]
    public partial class BBThreads
    {
        public BBThreads()
        {
            BBComments = new HashSet<BBComments>();
            BBThreadVotes = new HashSet<BBThreadVotes>();
        }

        [Key]
        [Column("ThreadID")]
        public int ThreadId { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }
        [StringLength(150)]
        public string Title { get; set; }
        [Required]
        [StringLength(500)]
        public string Message { get; set; }
        public short CategoryCode { get; set; }
        [Column("OfficialCoinID")]
        public int? OfficialCoinId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime CreateDate { get; set; }
        public short? TagCode { get; set; }
        public bool IsClosed { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("BBThreads")]
        public Users User { get; set; }
        [InverseProperty("Thread")]
        public ICollection<BBComments> BBComments { get; set; }
        [InverseProperty("Thread")]
        public ICollection<BBThreadVotes> BBThreadVotes { get; set; }
    }
}
