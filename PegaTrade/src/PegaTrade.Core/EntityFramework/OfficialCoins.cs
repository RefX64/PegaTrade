using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("OfficialCoins", Schema = "PegaTrade")]
    public partial class OfficialCoins
    {
        [Key]
        [Column("OfficialCoinID")]
        public int OfficialCoinId { get; set; }
        [Required]
        [StringLength(10)]
        public string Symbol { get; set; }
        [Required]
        [StringLength(20)]
        public string Name { get; set; }
    }
}
