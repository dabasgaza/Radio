using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.DTOs
{
    public record CoverageDto
    {
        public int CoverageId { get; init; }
        public int CorrespondentId { get; init; }
        public string CorrespondentName { get; init; } = string.Empty;
        public int? GuestId { get; init; }
        public string? GuestName { get; init; }
        public string? Location { get; init; }
        public string? Topic { get; init; }
        public DateTime? ScheduledTime { get; init; }
        public DateTime? ActualTime { get; init; }
    }

}
