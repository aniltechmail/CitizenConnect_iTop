namespace Core.Interfaces.Services
{
    public interface ISmsService
    {
        Task SendAsync(string mobileNumber, string message);
    }
}
