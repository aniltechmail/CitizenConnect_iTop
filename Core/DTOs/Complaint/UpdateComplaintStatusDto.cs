using Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Complaint
{
    public class UpdateComplaintStatusDto
    {
        [Required]
        public ComplaintStatus Status { get; set; }

        public string? Remarks { get; set; }
    }
}
