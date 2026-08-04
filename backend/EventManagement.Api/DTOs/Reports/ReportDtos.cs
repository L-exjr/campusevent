namespace EventManagement.Api.DTOs.Reports;

public sealed record ReportSummaryResponse(
    int TotalEvents,
    int TotalRegistrations,
    decimal OverallAttendanceRate);

public sealed record EventReportResponse(
    Guid EventId,
    string EventTitle,
    int RegistrationCount,
    int AttendanceCount,
    decimal AttendanceRate);

public sealed record EventReportListItemResponse(
    Guid EventId,
    string EventTitle,
    Guid OrganizerId,
    string OrganizerName,
    int RegistrationCount,
    int AttendanceCount,
    decimal AttendanceRate);

public sealed record OrganizerReportResponse(
    Guid OrganizerId,
    string OrganizerName,
    int EventCount,
    int RegistrationCount);
