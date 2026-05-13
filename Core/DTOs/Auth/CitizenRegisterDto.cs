using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.DTOs.Auth
{
    public class CitizenRegisterDto : IJsonOnDeserialized
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public int BlockId { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraJson { get; set; }

        public void OnDeserialized()
        {
            if (BlockId == 0 &&
                ExtraJson is not null &&
                ExtraJson.TryGetValue("block_id", out var blockId) &&
                blockId.TryGetInt32(out var blockValue))
            {
                BlockId = blockValue;
            }
        }
    }
}
