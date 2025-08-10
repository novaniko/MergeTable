namespace MergeTable;

public record class RecordDto(
    int id,
    DateOnly date,
    TimeOnly startTime,
    TimeOnly endTime,
    TimeSpan durationTime,
    string status,
    string comment
);
