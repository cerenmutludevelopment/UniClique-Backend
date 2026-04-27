using System;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Report;

namespace UniCliqueBackend.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<bool> SubmitEventReportAsync(string reporterId, Guid eventId, CreateReportDto model);
        Task<bool> SubmitUserReportAsync(string reporterId, Guid targetUserId, CreateReportDto model);
        Task<bool> SubmitGeneralReportAsync(string reporterId, CreateReportDto model);
    }
}
