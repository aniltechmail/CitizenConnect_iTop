using Core.Entities.Complaint;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Repositories
{
    public interface IComplaintRepository
    {
        Task<Complaint?> GetByIdAsync(Guid id);
        Task<Complaint?> GetByRefNumberAsync(string refNumber);
        Task<IEnumerable<Complaint>> GetByCitizenIdAsync(Guid citizenId);
        Task<IEnumerable<Complaint>> GetAllAsync(
            ComplaintStatus? status,
            int? departmentId,
            int? blockId,
            int page,
            int pageSize
        );
        Task<int> GetTotalCountAsync(
            ComplaintStatus? status,
            int? departmentId,
            int? blockId
        );
        Task<Complaint> CreateAsync(Complaint complaint);
        Task UpdateAsync(Complaint complaint);
        Task AddMediaAsync(ComplaintMedia media);
        Task AddMessageAsync(ComplaintMessage message);
        Task AddITopMappingAsync(ComplaintITopMapping mapping);
        Task<string> GenerateRefNumberAsync();
        Task<ComplaintITopMapping?> GetITopMappingByComplaintIdAsync(Guid complaintId);
        Task UpdateITopMappingAsync(ComplaintITopMapping mapping);
    }
}
