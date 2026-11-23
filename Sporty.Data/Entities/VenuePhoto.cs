using System;
using System.Collections.Generic;
using System.Text;

namespace Sporty.Data.Entities
{
    public class VenuePhoto
    {
        public int VenuePhotoId { get; set; }
        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
