namespace MergeTable.Services
{
    public class TableService
    {
        private List<RecordDto> standardTable;
        private List<RecordDto> table;
        private List<RecordDto> mergedTable;

        public TableService()
        {
            standardTable = new List<RecordDto>
            {
                new RecordDto(1, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(10,0), new TimeOnly(10,15), new TimeSpan(0, 15, 0), "DURUŞ", "ÇAY MOLASI"),
                new RecordDto(2, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(12,0), new TimeOnly(12,30), new TimeSpan(0, 30, 0), "DURUŞ", "YEMEK MOLASI"),
                new RecordDto(3, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(15,0), new TimeOnly(15,15), new TimeSpan(0, 15, 0), "DURUŞ", "ÇAY MOLASI")
            };

            table = new List<RecordDto>
            {
                new RecordDto(1, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(7,30), new TimeOnly(8,30), new TimeSpan(1, 0, 0), "ÜRETİM", ""),
                new RecordDto(2, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(8,30), new TimeOnly(12,0), new TimeSpan(3, 30, 0), "ÜRETİM", ""),
                new RecordDto(3, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(12,0), new TimeOnly(13,0), new TimeSpan(1, 0, 0), "ÜRETİM", ""),
                new RecordDto(4, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(13,0), new TimeOnly(13, 45), new TimeSpan(0, 45, 0), "DURUŞ", "ARIZA"),
                new RecordDto(5, DateOnly.FromDateTime(DateTime.Now), new TimeOnly(13,45), new TimeOnly(17,30), new TimeSpan(3, 45, 0), "ÜRETİM", "")
            };

            mergedTable = new List<RecordDto>();
        }

        public IEnumerable<RecordDto> GetStandardTable() => standardTable;
        public IEnumerable<RecordDto> GetTable() => table;

        public IResult AddRecord(CreateRecordDto newRecord)
        {
            if (newRecord.startTime >= newRecord.endTime)
            {
                return Results.BadRequest("Start time should be earlier than end time");
            }

            if (newRecord.startTime.Minute + newRecord.startTime.Hour * 60 < 450)
            {
                return Results.BadRequest("Start time cannot be earlier than 7:30");
            }

            if (newRecord.endTime.Minute + newRecord.endTime.Hour * 60 > 1050)
            {
                return Results.BadRequest("End time cannot be later than 17:30");
            }

            var overlappingRecord = table.FirstOrDefault(r =>
                newRecord.startTime < r.endTime && newRecord.endTime > r.startTime  
            );

            if (overlappingRecord != null)
            {
                return Results.BadRequest($"Overlaps with record ID: {overlappingRecord.id}");
            }

            int lastId = table.Any() ? table.Max(r => r.id) : 0;

            RecordDto record = new RecordDto(
                lastId + 1,
                DateOnly.FromDateTime(DateTime.Now),
                newRecord.startTime,
                newRecord.endTime,
                newRecord.endTime - newRecord.startTime,
                newRecord.status,
                newRecord.comment
            );

            table.Add(record);
            table = table.OrderBy(r => r.startTime).ToList();

            return Results.CreatedAtRoute("GetRecord", new { id = record.id }, record);
        }

        public IResult DeleteRecord(int id)
        {
            table.RemoveAll(r => r.id == id);
            return Results.NoContent();
        }

        public IEnumerable<RecordDto> GetMergedTable()
        {
            mergedTable.Clear();
            //Working time is between 7:30 and 17:30. Total : 600+1 minutes
            int[,] dp = new int[2, 601];

            int c1 = 0;
            while (c1 < table.Count)
            {
                RecordDto currRecord = table[c1];
                //450 minutes = 7:30
                int start = currRecord.startTime.Hour * 60 + currRecord.startTime.Minute - 450;
                int end = currRecord.endTime.Hour * 60 + currRecord.endTime.Minute - 450;

                for (int i = start; i < end; i++)
                {
                    dp[0, i] = c1+1;
                }
                c1++;
            }

            int c2 = 0;
            while (c2 < standardTable.Count)
            {
                RecordDto currRecord = standardTable[c2];
                //450 minutes = 7:30
                int start = currRecord.startTime.Hour * 60 + currRecord.startTime.Minute - 450;
                int end = currRecord.endTime.Hour * 60 + currRecord.endTime.Minute - 450;

                for (int i = start; i < end; i++)
                {
                    dp[1, i] = c2+1;
                }
                c2++;
            }

            //0 MEANS NO DATA
            //1 MEANS FROM TABLE (index 0 value is different than 0)
            //2 MEANS FROM STANDARD TABLE (index 1 value is different than 0)
            int startIndex = 0;
            int preType = 0;
            int currentType = 0;
            int preIndex = 0;
            int currentIndex = 0;
            for (int i = 0; i < 601; i++)
            {
                if (dp[1, i] != 0)
                {
                    currentType = 2;
                    currentIndex = dp[1, i];
                }
                else if (dp[0, i] != 0)
                {
                    currentType = 1;
                    currentIndex = dp[0, i];
                }
                else
                {
                    currentType = 0;
                    currentIndex = 0;
                }

                if ((((currentType != preType) || (currentIndex != preIndex)) && i != 0) || i == 600)
                {
                    RecordDto? recordDto = null;
                    if (preType == 2)
                    {
                        recordDto = standardTable[dp[1, i - 1] - 1];
                    }
                    else if (preType == 1)
                    {
                        recordDto = table[dp[0, i - 1] - 1];
                    }
                    else
                    {
                        recordDto = new RecordDto(
                            -1,
                            DateOnly.FromDateTime(DateTime.Now),
                            TimeOnly.MinValue,
                            TimeOnly.MinValue,
                            TimeSpan.MinValue,
                            "NO DATA",
                            "-"
                        );
                    }

                    int duration = i - startIndex;
                    int startT = startIndex + 450;
                    int endT = i + 450;

                    mergedTable.Add(new RecordDto(
                        recordDto.id,
                        recordDto.date,
                        new TimeOnly(startT / 60, startT % 60),
                        new TimeOnly(endT / 60, endT % 60),
                        new TimeSpan(duration / 60, duration % 60, 0),
                        recordDto.status,
                        recordDto.comment
                    ));
                    startIndex = i;
                }
                preType = currentType;
                preIndex = currentIndex;
            }
            return mergedTable;
        }
    }
}
