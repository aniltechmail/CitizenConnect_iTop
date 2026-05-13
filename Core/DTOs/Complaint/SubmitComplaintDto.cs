using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.DTOs.Complaint
{
    public class SubmitComplaintDto : IJsonOnDeserialized
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int BlockId { get; set; }

        [Range(1, 4)]
        public int Priority { get; set; } = 3;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraJson { get; set; }

        public void OnDeserialized()
        {
            if (ExtraJson is null)
                return;

            if (CategoryId == 0 &&
                ExtraJson.TryGetValue("category_id", out var categoryId) &&
                categoryId.TryGetInt32(out var categoryValue))
            {
                CategoryId = categoryValue;
            }

            if (BlockId == 0 &&
                ExtraJson.TryGetValue("block_id", out var blockId) &&
                blockId.TryGetInt32(out var blockValue))
            {
                BlockId = blockValue;
            }
        }
    }
}
