namespace MergeTable;

public record class CreateRecordDto(
    TimeOnly startTime,
    TimeOnly endTime,
    string status,
    string comment
);