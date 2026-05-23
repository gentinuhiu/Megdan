namespace Megdan.Web.Services
{
    public interface IRecaptchaService
    {
        Task<(bool IsSuccess, string ErrorMessage)> VerifyAsync(string token, string expectedAction);
    }
}