using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PegaTrade.Core.EntityFramework
{
    public partial class Exceptions
    {
        [Key]
        [Column("ExceptionID")]
        public int ExceptionId { get; set; }
        [Required]
        [StringLength(300)]
        public string Message { get; set; }
        [StringLength(300)]
        public string InnerMessage { get; set; }
        [Required]
        public string Source { get; set; }
        [StringLength(1000)]
        public string ExtraData { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Date { get; set; }
        public short SystemCode { get; set; }
    }
}
