using System;
using System.Collections.Generic;
using System.Text;

namespace Sporty.Data.Entities
{
    public class GroupStatusCheckLog
    {
        public int Id { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
        public int UpdatedCount { get; set; }
    }
}
