using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PegaTrade.ViewModel
{
    public class PartialToFullVM
    {
        public string Title { get; set; }
        public string PartialViewName { get; set; }
        public object ViewModel { get; set; }
    }
}
