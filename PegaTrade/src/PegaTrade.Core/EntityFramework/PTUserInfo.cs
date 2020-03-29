using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("PTUserInfo", Schema = "PegaTrade")]
    public partial class PTUserInfo
    {
        [Key]
        [Column("UserID")]
        public int UserId { get; set; }
        [StringLength(50)]
        public string Location { get; set; }
        [StringLength(50)]
        public string Website { get; set; }
        [StringLength(400)]
        public string Bio { get; set; }
        [StringLength(150)]
        public string FavoriteCoins { get; set; }
        public byte SubscriptionLevel { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? SubscriptionExpireDate { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("PTUserInfo")]
        public Users User { get; set; }
    }
}
