using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("Coins", Schema = "PegaTrade")]
    public partial class Coins
    {
        [Key]
        [Column("CoinID")]
        public long CoinId { get; set; }
        [Required]
        [StringLength(10)]
        public string Symbol { get; set; }
        [Column(TypeName = "decimal(14, 4)")]
        public decimal Shares { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime OrderDate { get; set; }
        public short CoinCurrency { get; set; }
        [Column(TypeName = "decimal(18, 8)")]
        public decimal PricePerUnit { get; set; }
        [Column("TotalPricePaidUSD", TypeName = "decimal(12, 2)")]
        public decimal? TotalPricePaidUsd { get; set; }
        public short OrderType { get; set; }
        [StringLength(300)]
        public string Notes { get; set; }
        public short Exchange { get; set; }
        public short? SoldCoinCurrency { get; set; }
        [Column(TypeName = "decimal(18, 8)")]
        public decimal? SoldPricePerUnit { get; set; }
        [Column("TotalSoldPricePaidUSD", TypeName = "decimal(12, 2)")]
        public decimal? TotalSoldPricePaidUsd { get; set; }
        [Column("PortfolioID")]
        public int PortfolioId { get; set; }

        [ForeignKey("PortfolioId")]
        [InverseProperty("Coins")]
        public Portfolios Portfolio { get; set; }
    }
}
