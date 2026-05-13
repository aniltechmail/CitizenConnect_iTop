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
    public class AssignComplaintDto : IJsonOnDeserialized
    {
        [Required]
        public int DepartmentId { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraJson { get; set; }

        public void OnDeserialized()
        {
            if (DepartmentId == 0 &&
                ExtraJson is not null &&
                ExtraJson.TryGetValue("department_id", out var departmentId) &&
                departmentId.TryGetInt32(out var departmentValue))
            {
                DepartmentId = departmentValue;
            }
        }
    }
}
