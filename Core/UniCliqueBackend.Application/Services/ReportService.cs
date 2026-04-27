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
        private readonly IUserRepository _userRepository;
        private readonly IEventRepository _eventRepository;

        public ReportService(
            IReportRepository reportRepository, 
            IUserRepository userRepository,
            IEventRepository eventRepository)
        {
            _reportRepository = reportRepository;
            _userRepository = userRepository;
            _eventRepository = eventRepository;
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

        public async Task<System.Collections.Generic.IEnumerable<ReportDto>> GetAllReportsAsync(int pageNumber, int pageSize)
        {
            var reports = await _reportRepository.GetAllAsync(pageNumber, pageSize);
            
            var dtoList = new System.Collections.Generic.List<ReportDto>();
            foreach(var r in reports)
            {
                dtoList.Add(new ReportDto
                {
                    Id = r.Id,
                    Subject = r.Subject,
                    Description = r.Description,
                    Type = r.Type,
                    RelatedEntityId = r.RelatedEntityId,
                    ReportedById = r.ReporterId.ToString(),
                    CreatedAt = r.CreatedAt,
                    IsResolved = r.IsResolved
                });
            }
            return dtoList;
        }

        public async Task<bool> ResolveReportAsync(Guid reportId, UniCliqueBackend.Application.DTOs.Admin.Report.ResolveReportDto model, string adminId)
        {
            var report = await _reportRepository.GetByIdAsync(reportId);
            if (report == null) return false;

            report.IsResolved = true;
            report.ResolvedAt = DateTime.UtcNow;
            report.AdminNote = model.AdminNote;

            Guid? targetUserId = null;

            if (report.Type == ReportType.UserReport)
            {
                targetUserId = report.RelatedEntityId;
            }
            else if (report.Type == ReportType.EventReport && report.RelatedEntityId.HasValue)
            {
                var evt = await _eventRepository.GetByIdAsync(report.RelatedEntityId.Value);
                if (evt != null)
                {
                    targetUserId = evt.OwnerId;
                }
            }

            if (targetUserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(targetUserId.Value);
                if (user != null)
                {
                    if (model.IsActive.HasValue) user.IsActive = model.IsActive.Value;
                    if (model.IsBanned.HasValue) user.IsBanned = model.IsBanned.Value;
                    if (model.IsDeleted.HasValue)
                    {
                        user.IsDeleted = model.IsDeleted.Value;
                        user.DeletedAt = model.IsDeleted.Value ? DateTime.UtcNow : null;
                    }
                    await _userRepository.UpdateAsync(user);
                }
            }

            await _reportRepository.UpdateAsync(report);
            return true;
        }
    }
}
