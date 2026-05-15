using Core.DTOs.Complaint;
using Core.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IComplaintService
    {
        Task<ComplaintResponseDto> SubmitComplaintAsync(Guid citizenId,SubmitComplaintDto dto);
        Task<ComplaintResponseDto> GetByIdAsync(Guid id);
        Task<IEnumerable<ComplaintResponseDto>> GetMyComplaintsAsync(Guid citizenId);
        Task<PagedComplaintsDto> GetAllComplaintsAsync(ComplaintStatus? status,int? departmentId,int? blockId,int page,int pageSize);
        Task<ComplaintResponseDto> AssignDepartmentAsync(Guid complaintId,AssignComplaintDto dto,Guid assignedById);
        Task<ComplaintResponseDto> AssignAgentAsync(Guid complaintId,Guid agentId,Guid assignedById);
        Task<ComplaintResponseDto> UpdateStatusAsync(Guid complaintId,UpdateComplaintStatusDto dto,Guid updatedById);
        Task<ComplaintMediaResponseDto> UploadMediaAsync(Guid complaintId,IFormFile file,Guid uploadedById);
        Task<ComplaintMessageResponseDto> SendMessageAsync(Guid complaintId,SendComplaintMessageDto dto,Guid senderId,bool isInternalUser);
        Task<IEnumerable<ComplaintMessageResponseDto>> GetMessagesAsync(Guid complaintId);
        Task<ComplaintMessageResponseDto> MarkMessageAsReadAsync(Guid complaintId,Guid messageId);
        Task<ComplaintFeedbackResponseDto> SubmitFeedbackAsync(Guid complaintId,SubmitFeedbackDto dto,Guid submittedById,bool isInternalUser);
        Task<ComplaintFeedbackResponseDto> GetFeedbackAsync(Guid complaintId);
    }
}
