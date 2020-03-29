using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    [Table("ApiDetails", Schema = "PegaTrade")]
    public partial class ApiDetails
    {
        [Column("ID")]
        public int Id { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public short Exchange { get; set; }
        public short ApiAction { get; set; }
        [Required]
        [StringLength(150)]
        public string ApiPublic { get; set; }
        [Required]
        [StringLength(150)]
        public string ApiPrivate { get; set; }
        [StringLength(150)]
        public string ApiThirdKey { get; set; }
        [Column(TypeName = "date")]
        public DateTime DateAdded { get; set; }
        [Column("UserID")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("ApiDetails")]
        public Users User { get; set; }
    }
}
