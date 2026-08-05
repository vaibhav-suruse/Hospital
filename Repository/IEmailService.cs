namespace WebApplicationSampleTest2.Repository
{
    public interface IEmailService
    {
        void SendOTP(string toEmail, string otp, string purpose);
    }
}
