using System;
using System.ComponentModel.DataAnnotations;

namespace PegaTrade.Layer.Models.Coins
{
    public class ExchangeApiInfo
    {
        [Required]
        [StringLength(150, MinimumLength = 10)]
        public string ApiPublic { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 10)]
        public string ApiPrivate { get; set; }

        [MaxLength(150)]
        public string ApiThirdKey { get; set; }

        [Required]
        public Types.Exchanges Exchange { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        public int Id { get; set; }
        public Types.ApiAction ApiAction { get; set; }
        public DateTime DateAdded { get; set; }
        public int UserId { get; set; }

        public int PortfolioID { get; set; }
    }
}
