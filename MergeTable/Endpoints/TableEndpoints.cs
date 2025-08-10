using MergeTable.Services;
namespace MergeTable.Endpoints
{
    public static class TableEndpoints
    {
        public static void MapTableEndpoints(this WebApplication app)
        {
            app.MapGet("standardTable", (TableService service) => service.GetStandardTable());
            app.MapGet("table", (TableService service) => service.GetTable());
            app.MapPost("table", (TableService service, CreateRecordDto dto) => service.AddRecord(dto));
            app.MapDelete("table/{id}", (TableService service, int id) =>
            {
                service.DeleteRecord(id);
                return Results.NoContent();
            });
            app.MapGet("mergedTable", (TableService service) => service.GetMergedTable());
        }
    }
}
