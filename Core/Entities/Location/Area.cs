using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Location
{
    public class Area
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ConstituencyId { get; set; }
        public Constituency Constituency { get; set; } = null!;

        public ICollection<Block> Blocks { get; set; } = new List<Block>();
    }
}
