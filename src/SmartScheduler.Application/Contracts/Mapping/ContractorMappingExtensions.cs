using SmartScheduler.Application.Contracts.DTOs;
using SmartScheduler.Domain.Contracts.Entities;
using SmartScheduler.Domain.Contracts.ValueObjects;

namespace SmartScheduler.Application.Contracts.Mapping;

/// <summary>
/// Extension methods for mapping between domain entities and DTOs.
/// </summary>
public static class ContractorMappingExtensions
{
    public static ContractorDto ToDto(this Contractor contractor)
    {
        return new ContractorDto
        {
            Id = contractor.Id,
            Name = contractor.Name,
            BaseLocation = contractor.BaseLocation.ToDto(),
            Rating = contractor.Rating,
            WorkingHours = contractor.WorkingHours.Select(wh => wh.ToDto()).ToList(),
            Skills = contractor.Skills.ToList(),
            Calendar = contractor.Calendar?.ToDto(),
            Availability = contractor.Availability,
            JobsToday = contractor.JobsToday,
            MaxJobsPerDay = contractor.MaxJobsPerDay,
            CurrentUtilization = contractor.CurrentUtilization,
            Timezone = contractor.Timezone,
            CreatedAt = contractor.CreatedAt,
            UpdatedAt = contractor.UpdatedAt
        };
    }

    public static GeoLocationDto ToDto(this GeoLocation geoLocation)
    {
        return new GeoLocationDto
        {
            Latitude = geoLocation.Latitude,
            Longitude = geoLocation.Longitude,
            Address = geoLocation.Address,
            City = geoLocation.City,
            State = geoLocation.State,
            PostalCode = geoLocation.PostalCode,
            Country = geoLocation.Country,
            FormattedAddress = geoLocation.FormattedAddress,
            PlaceId = geoLocation.PlaceId
        };
    }

    public static WorkingHoursDto ToDto(this WorkingHours workingHours)
    {
        return new WorkingHoursDto
        {
            DayOfWeek = workingHours.DayOfWeek,
            StartTime = workingHours.StartTime.ToString("HH:mm"),
            EndTime = workingHours.EndTime.ToString("HH:mm"),
            TimeZone = workingHours.TimeZone
        };
    }

    public static ContractorCalendarDto ToDto(this ContractorCalendar calendar)
    {
        return new ContractorCalendarDto
        {
            Holidays = calendar.Holidays.ToList(),
            Exceptions = calendar.Exceptions.Select(e => e.ToDto()).ToList(),
            DailyBreakMinutes = calendar.DailyBreakMinutes
        };
    }

    public static CalendarExceptionDto ToDto(this CalendarException exception)
    {
        return new CalendarExceptionDto
        {
            Date = exception.Date,
            Type = exception.Type.ToString(),
            WorkingHours = exception.WorkingHours?.ToDto()
        };
    }

    public static GeoLocation ToDomain(this GeoLocationDto dto)
    {
        return new GeoLocation(
            dto.Latitude,
            dto.Longitude,
            dto.Address,
            dto.City,
            dto.State,
            dto.PostalCode,
            dto.Country,
            dto.FormattedAddress,
            dto.PlaceId);
    }

    public static WorkingHours ToDomain(this WorkingHoursDto dto)
    {
        if (!TimeOnly.TryParse(dto.StartTime, out var startTime))
            throw new ArgumentException($"Invalid start time format: {dto.StartTime}", nameof(dto));
        
        if (!TimeOnly.TryParse(dto.EndTime, out var endTime))
            throw new ArgumentException($"Invalid end time format: {dto.EndTime}", nameof(dto));

        return new WorkingHours(dto.DayOfWeek, startTime, endTime, dto.TimeZone);
    }

    public static ContractorCalendar ToDomain(this ContractorCalendarDto dto)
    {
        return new ContractorCalendar(
            dto.Holidays.ToList(),
            dto.Exceptions.Select(e => e.ToDomain()).ToList(),
            dto.DailyBreakMinutes);
    }

    public static CalendarException ToDomain(this CalendarExceptionDto dto)
    {
        if (!Enum.TryParse<CalendarExceptionType>(dto.Type, out var type))
            throw new ArgumentException($"Invalid exception type: {dto.Type}", nameof(dto));

        return new CalendarException(
            dto.Date,
            type,
            dto.WorkingHours?.ToDomain());
    }
}

