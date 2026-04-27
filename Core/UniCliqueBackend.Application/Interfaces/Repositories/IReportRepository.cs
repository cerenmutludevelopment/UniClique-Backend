using System.Collections.Generic;
using System.Threading.Tasks;
using UniCliqueBackend.Domain.Entities;

namespace UniCliqueBackend.Application.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task AddAsync(Report report);
        Task<Report?> GetByIdAsync(Guid id);
        Task<IEnumerable<Report>> GetAllAsync(int pageNumber, int pageSize);
        Task UpdateAsync(Report report);
    }
}
