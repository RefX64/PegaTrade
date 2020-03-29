using System;
using System.Collections.Generic;
using System.Text;

namespace PegaTrade.Layer.Models.Community
{
    public class VoteResult
    {
        public int TotalVotes { get; set; }
        public int TotalUpvotes { get; set; }
        public int TotalDownvote => TotalVotes - TotalUpvotes;
    }
}
