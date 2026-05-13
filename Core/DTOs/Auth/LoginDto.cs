using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Identifier { get; set; } = string.Empty; // phone or email

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
