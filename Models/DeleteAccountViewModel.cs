using System.ComponentModel.DataAnnotations;

namespace Abdullhak_Khalaf.Models
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال كلمة المرور.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}