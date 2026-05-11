using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Location
{
    public class Constituency
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int DistrictId { get; set; }
        public District District { get; set; } = null!;

        public ICollection<Area> Areas { get; set; } = new List<Area>();
    } 
}
