using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("Portfolios", Schema = "PegaTrade")]
    public partial class Portfolios
    {
        public Portfolios()
        {
            Coins = new HashSet<Coins>();
        }

        [Key]
        [Column("PortfolioID")]
        public int PortfolioId { get; set; }
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        public short DisplayType { get; set; }
        public bool IsDefault { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("Portfolios")]
        public Users User { get; set; }
        [InverseProperty("Portfolio")]
        public ICollection<Coins> Coins { get; set; }
    }
}
