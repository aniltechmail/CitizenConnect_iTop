using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Complaint
{
    public class AssignComplaintDto
    {
        [Required]
        public int DepartmentId { get; set; }
    }
}
