using Core.DTOs.ITop;

namespace Core.Interfaces.Services
{
    public interface IITopTicketAdapter
    {
        Task<ITopTicketCreateResult> CreateTicketAsync(
            ITopTicketCreateRequest request,
            CancellationToken cancellationToken = default);
    }
}
