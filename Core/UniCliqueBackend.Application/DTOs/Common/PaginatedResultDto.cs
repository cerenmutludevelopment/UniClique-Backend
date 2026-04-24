using System.Collections.Generic;

namespace UniCliqueBackend.Application.DTOs.Common
{
    public class PaginatedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public string? NextCursor { get; set; }
        public bool HasNextPage { get; set; }
    }
}
