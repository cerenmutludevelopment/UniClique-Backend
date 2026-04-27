namespace UniCliqueBackend.Application.DTOs.Admin.Report
{
    public class ResolveReportDto
    {
        public bool? IsActive { get; set; }
        public bool? IsBanned { get; set; }
        public bool? IsDeleted { get; set; }
        public string? AdminNote { get; set; }
    }
}
