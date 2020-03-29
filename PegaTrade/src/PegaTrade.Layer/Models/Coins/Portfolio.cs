using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.Layer.Models.Coins
{
    public class Portfolio
    {
        public int PortfolioId { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }

        public Types.PortfolioDisplayType DisplayType { get; set; }
        public bool IsDefault { get; set; }
        public int UserId { get; set; }

        public string OwnerUsername { get; set; }
    }
}
