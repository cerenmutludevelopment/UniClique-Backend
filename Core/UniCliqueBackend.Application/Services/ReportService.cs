using System;
using System.Threading.Tasks;
using UniCliqueBackend.Application.DTOs.Report;
using UniCliqueBackend.Application.Interfaces.Repositories;
using UniCliqueBackend.Application.Interfaces.Services;
using UniCliqueBackend.Domain.Entities;
using UniCliqueBackend.Domain.Enums;

namespace UniCliqueBackend.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<bool> SubmitEventReportAsync(string reporterId, Guid eventId, CreateReportDto model)
        {
            if (!Guid.TryParse(reporterId, out var uid)) return false;

            var report = new Report
            {
                ReporterId = uid,
                Type = ReportType.EventReport,
                Subject = model.Subject,
                Description = model.Description,
                RelatedEntityId = eventId,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            return true;
        }

        public async Task<bool> SubmitUserReportAsync(string reporterId, Guid targetUserId, CreateReportDto model)
        {
            if (!Guid.TryParse(reporterId, out var uid)) return false;
            
            // Prevent self-reporting
            if (uid == targetUserId) return false;

            var report = new Report
            {
                ReporterId = uid,
                Type = ReportType.UserReport,
                Subject = model.Subject,
                Description = model.Description,
                RelatedEntityId = targetUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            return true;
        }

        public async Task<bool> SubmitGeneralReportAsync(string reporterId, CreateReportDto model)
        {
            if (!Guid.TryParse(reporterId, out var uid)) return false;

            var report = new Report
            {
                ReporterId = uid,
                Type = model.Type, // The user selects this from settings (Technical, UserReport, EventReport)
                Subject = model.Subject,
                Description = model.Description,
                RelatedEntityId = model.RelatedEntityId, // Optional
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            return true;
        }
    }
}
