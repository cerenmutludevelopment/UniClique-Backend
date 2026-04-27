using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Persistence.Contexts;

namespace UniCliqueBackend.Persistence.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly AppDbContext _context;

        public ReportRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Report report)
        {
            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();
        }

        public async Task<Report?> GetByIdAsync(Guid id)
        {
            return await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Report>> GetAllAsync(int pageNumber, int pageSize)
        {
            return await _context.Reports
                                 .OrderByDescending(r => r.CreatedAt)
                                 .Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }

        public async Task UpdateAsync(Report report)
        {
            _context.Reports.Update(report);
            await _context.SaveChangesAsync();
        }
    }
}
