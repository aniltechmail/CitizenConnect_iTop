using Core.DTOs.ITop;

namespace Core.Interfaces.Services
{
    public interface IITopTicketAdapter
    {
        Task<ITopTicketCreateResult> CreateTicketAsync(
            ITopTicketCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<ITopTicketUpdateResult> UpdateTicketAsync(
            ITopTicketUpdateRequest request,
            CancellationToken cancellationToken = default);

        Task<ITopAttachmentCreateResult> CreateAttachmentAsync(
            ITopAttachmentCreateRequest request,
            CancellationToken cancellationToken = default);

        Task<ITopTicketUpdateResult> AddTicketLogAsync(
            ITopTicketLogRequest request,
            CancellationToken cancellationToken = default);
    }
}
