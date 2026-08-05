

namespace WebApplicationSampleTest2.Models
{
    public class ForgotPasswordModel
    {
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
        public bool IsEmailVerified { get; set; }
    }
}
