`#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeSql;
using Shashlik.EventBus;
using Shashlik.EventBus.Utils;

const string connStr = "Host=localhost;Port=5433;Username=pguser;Password=123123;Database=eventbus_perf;IncludeErrorDetail=true";
const string environment = "perf-test";
const int totalPublishedRows = 5_000_000;
const int totalReceivedNoDelayRows = 2_000_000;
const int totalReceivedMostDelayRows = 2_000_000;

long _nextId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 10000;
long NextId() => Interlocked.Increment(ref _nextId);
const int delayRetrySecond = 60;
const int maxFailedRetryCount = 10;
const int retryQueryCount = 100;

var fsql = new FreeSqlBuilder()
    .UseConnectionString(DataType.PostgreSQL, connStr)
    .UseAutoSyncStructure(true)
    .UseNoneCommandParameter(true)
    .Build();

fsql.Aop.CommandBefore += (_, e) =>
{
    e.Command.CommandTimeout = 300;
};

Console.WriteLine("=== Syncing table structure (CodeFirst) ===");
fsql.CodeFirst.SyncStructure<RelationDbMessageStoragePublishedModel>();
fsql.CodeFirst.SyncStructure<RelationDbMessageStorageReceivedModel>();
Console.WriteLine("Table structure synced.\n");

try
{
    await RunPublishedTest(fsql);
    await RunReceivedNoDelayTest(fsql);
    await RunReceivedMostDelayTest(fsql);
    await RunDeleteExpiresTest(fsql);
}
finally
{
    fsql.Dispose();
}

async Task RunPublishedTest(IFreeSql freeSql)
{
    var pubCount = await freeSql.Select<RelationDbMessageStoragePublishedModel>().CountAsync();
    if (pubCount < totalPublishedRows)
    {
        if (pubCount > 0)
        {
            Console.WriteLine($"=== Published table: clearing {pubCount:N0} partial rows before full insert ===");
            await freeSql.Delete<RelationDbMessageStoragePublishedModel>().ExecuteAffrowsAsync();
        }
        Console.WriteLine($"=== Published table: inserting {totalPublishedRows:N0} rows ===");
        await BulkInsertPublished(freeSql, totalPublishedRows);
    }
    else
    {
        Console.WriteLine($"=== Published table: already has {pubCount:N0} rows, skipping insert ===");
    }

    var total = await freeSql.Select<RelationDbMessageStoragePublishedModel>().CountAsync();
    Console.WriteLine($"Published table total rows: {total:N0}\n");

    Console.WriteLine("=== GetPublishedMessagesOfNeedRetryAsync performance test ===");
    for (int i = 0; i < 5; i++)
    {
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.UtcNow.GetLongDate();

        var sql = freeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount)
            .ToSql();

        Console.WriteLine($"  [Run {i + 1}] SQL:\n    {sql}");

        var sw = Stopwatch.StartNew();
        var results = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount)
            .ToListAsync();
        sw.Stop();

        Console.WriteLine($"  [Run {i + 1}] Rows returned: {results.Count}, Elapsed: {sw.ElapsedMilliseconds}ms");
    }

    var explainSql = freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.Environment == environment &&
                    r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                    r.CreateTimeTicks < DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate() &&
                    r.RetryCount < maxFailedRetryCount &&
                    (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < DateTimeOffset.UtcNow.GetLongDate()))
        .OrderByDescending(r => r.CreateTimeTicks)
        .Limit(retryQueryCount)
        .ToSql();
    await ExplainAnalyze(freeSql, explainSql);
}

async Task RunReceivedNoDelayTest(IFreeSql freeSql)
{
    var tableName = "eventbus_received_message_nodelay";
    var tempFsql = CreateTempReceivedFreeSql(tableName);

    var recvCount = await tempFsql.Select<PerfReceivedModel>().CountAsync();
    if (recvCount < totalReceivedNoDelayRows)
    {
        if (recvCount > 0)
        {
            Console.WriteLine($"\n=== Received (no-delay skew) table [{tableName}]: clearing {recvCount:N0} partial rows ===");
            await tempFsql.Delete<PerfReceivedModel>().ExecuteAffrowsAsync();
        }
        Console.WriteLine($"\n=== Received (no-delay skew) table [{tableName}]: inserting {totalReceivedNoDelayRows:N0} rows ===");
        await BulkInsertReceivedNoDelay(tempFsql, totalReceivedNoDelayRows, environment);
    }
    else
    {
        Console.WriteLine($"\n=== Received (no-delay skew) table [{tableName}]: already has {recvCount:N0} rows ===");
    }

    var total = await tempFsql.Select<PerfReceivedModel>().CountAsync();
    Console.WriteLine($"Received (no-delay) total rows: {total:N0}\n");

    Console.WriteLine($"=== GetReceivedMessagesOfNeedRetryAsync - no-delay skew test [{tableName}] ===");
    for (int i = 0; i < 5; i++)
    {
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.UtcNow.GetLongDate();

        var sw = Stopwatch.StartNew();
        var normalQuery = tempFsql.Select<PerfReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == false &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount);

        var delayMax = now + delayRetrySecond + (int)(delayRetrySecond * 0.2);
        var delayQuery = tempFsql.Select<PerfReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == true &&
                        r.DelayAtTicks <= delayMax &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount);

        var results = await normalQuery
            .UnionAll(delayQuery)
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount)
            .ToListAsync();
        sw.Stop();

        Console.WriteLine($"  [Run {i + 1}] Rows returned: {results.Count}, Elapsed: {sw.ElapsedMilliseconds}ms");
    }

    await ExplainAnalyze(freeSql, tempFsql.Select<PerfReceivedModel>()
        .Where(r => r.Environment == environment &&
                    r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                    r.IsDelay == false &&
                    r.CreateTimeTicks < DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate() &&
                    r.RetryCount < maxFailedRetryCount &&
                    (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < DateTimeOffset.UtcNow.GetLongDate()))
        .OrderByDescending(r => r.CreateTimeTicks)
        .Limit(retryQueryCount)
        .ToSql());

    tempFsql.Dispose();
}

async Task RunReceivedMostDelayTest(IFreeSql freeSql)
{
    var tableName = "eventbus_received_message_mostdelay";
    var tempFsql = CreateTempReceivedFreeSql(tableName);

    var recvCount = await tempFsql.Select<PerfReceivedModel>().CountAsync();
    if (recvCount < totalReceivedMostDelayRows)
    {
        if (recvCount > 0)
        {
            Console.WriteLine($"\n=== Received (most-delay skew) table [{tableName}]: clearing {recvCount:N0} partial rows ===");
            await tempFsql.Delete<PerfReceivedModel>().ExecuteAffrowsAsync();
        }
        Console.WriteLine($"\n=== Received (most-delay skew) table [{tableName}]: inserting {totalReceivedMostDelayRows:N0} rows ===");
        await BulkInsertReceivedMostDelay(tempFsql, totalReceivedMostDelayRows, environment);
    }
    else
    {
        Console.WriteLine($"\n=== Received (most-delay skew) table [{tableName}]: already has {recvCount:N0} rows ===");
    }

    var total = await tempFsql.Select<PerfReceivedModel>().CountAsync();
    Console.WriteLine($"Received (most-delay) total rows: {total:N0}\n");

    Console.WriteLine($"=== GetReceivedMessagesOfNeedRetryAsync - most-delay skew test [{tableName}] ===");
    for (int i = 0; i < 5; i++)
    {
        var createTimeLimit = DateTimeOffset.UtcNow.AddSeconds(-delayRetrySecond).GetLongDate();
        var now = DateTimeOffset.UtcNow.GetLongDate();

        var sw = Stopwatch.StartNew();
        var normalQuery = tempFsql.Select<PerfReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == false &&
                        r.CreateTimeTicks < createTimeLimit &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount);

        var delayMax = now + delayRetrySecond + (int)(delayRetrySecond * 0.2);
        var delayQuery = tempFsql.Select<PerfReceivedModel>()
            .Where(r => r.Environment == environment &&
                        r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                        r.IsDelay == true &&
                        r.DelayAtTicks <= delayMax &&
                        r.RetryCount < maxFailedRetryCount &&
                        (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < now))
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount);

        var results = await normalQuery
            .UnionAll(delayQuery)
            .OrderByDescending(r => r.CreateTimeTicks)
            .Limit(retryQueryCount)
            .ToListAsync();
        sw.Stop();

        Console.WriteLine($"  [Run {i + 1}] Rows returned: {results.Count}, Elapsed: {sw.ElapsedMilliseconds}ms");
    }

    var delayMaxExplain = DateTimeOffset.UtcNow.GetLongDate() + delayRetrySecond + (int)(delayRetrySecond * 0.2);
    await ExplainAnalyze(freeSql, tempFsql.Select<PerfReceivedModel>()
        .Where(r => r.Environment == environment &&
                    r.Status.In(MessageStatus.Scheduled, MessageStatus.Failed) &&
                    r.IsDelay == true &&
                    r.DelayAtTicks <= delayMaxExplain &&
                    r.RetryCount < maxFailedRetryCount &&
                    (!r.IsLocking || r.LockEndTicks == null || r.LockEndTicks < DateTimeOffset.UtcNow.GetLongDate()))
        .OrderByDescending(r => r.CreateTimeTicks)
        .Limit(retryQueryCount)
        .ToSql());

    tempFsql.Dispose();
}

async Task RunDeleteExpiresTest(IFreeSql freeSql)
{
    const int retryFailedMax = 10;
    var now = DateTimeOffset.UtcNow.GetLongDate();
    var pastExpire = DateTimeOffset.UtcNow.AddHours(-1).GetLongDate();

    Console.WriteLine("\n=== DeleteExpiresAsync performance test ===");

    var succeededCount = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.Status == MessageStatus.Succeeded && r.ExpireTimeTicks == null)
        .CountAsync();
    var failedCount = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.Status == MessageStatus.Failed && r.RetryCount >= retryFailedMax && r.ExpireTimeTicks == null)
        .CountAsync();

    Console.WriteLine($"  Published: {succeededCount:N0} SUCCEEDED rows without expire time");
    Console.WriteLine($"  Published: {failedCount:N0} FAILED+maxRetry rows without expire time");

    Console.WriteLine($"  Setting ExpireTimeTicks on SUCCEEDED rows (sample 500K)...");
    var sw = Stopwatch.StartNew();
    var idsToExpire = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.Status == MessageStatus.Succeeded && r.ExpireTimeTicks == null)
        .Limit(500_000)
        .ToListAsync(r => r.Id);
    sw.Stop();
    Console.WriteLine($"  Selected {idsToExpire.Count:N0} ids in {sw.ElapsedMilliseconds}ms");

    sw.Restart();
    const int updateBatchSize = 5000;
    for (int i = 0; i < idsToExpire.Count; i += updateBatchSize)
    {
        var batchIds = idsToExpire.Skip(i).Take(updateBatchSize).ToList();
        await freeSql.Update<RelationDbMessageStoragePublishedModel>()
            .Where(r => batchIds.Contains(r.Id))
            .Set(r => r.ExpireTimeTicks, pastExpire)
            .ExecuteAffrowsAsync();
        if ((i + updateBatchSize) % 100_000 == 0)
            Console.WriteLine($"  Updated {Math.Min(i + updateBatchSize, idsToExpire.Count):N0}/{idsToExpire.Count:N0}");
    }
    sw.Stop();
    Console.WriteLine($"  Set ExpireTimeTicks on {idsToExpire.Count:N0} rows in {sw.ElapsedMilliseconds}ms");

    var expiredTotal = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
        .CountAsync();
    Console.WriteLine($"  Total expired SUCCEEDED rows: {expiredTotal:N0}");

    Console.WriteLine($"\n  --- Testing SUCCEEDED delete query (select ids only, limit 1000) ---");
    var succeededSql = freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
        .Limit(1000)
        .ToSql(r => r.Id);
    Console.WriteLine($"  SQL: {succeededSql}");

    for (int i = 0; i < 5; i++)
    {
        sw.Restart();
        var ids = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
            .Limit(1000)
            .ToListAsync(r => r.Id);
        sw.Stop();
        Console.WriteLine($"  [Run {i + 1}] Selected {ids.Count} ids in {sw.ElapsedMilliseconds}ms");
    }

    await ExplainAnalyze(freeSql, freeSql.Select<RelationDbMessageStoragePublishedModel>()
        .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
        .Limit(1000)
        .ToSql(r => r.Id));

    Console.WriteLine($"\n  --- Simulating full DeleteExpiresAsync (published only) ---");
    const int batchSize = 1000;
    var totalDeleted = 0;
    var batchCount = 0;
    sw.Restart();

    while (true)
    {
        var ids = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Succeeded)
            .Limit(batchSize)
            .ToListAsync(r => r.Id);

        var idsFailed = await freeSql.Select<RelationDbMessageStoragePublishedModel>()
            .Where(r => r.ExpireTimeTicks != null && r.ExpireTimeTicks < now && r.Status == MessageStatus.Failed &&
                        r.RetryCount >= retryFailedMax)
            .Limit(batchSize)
            .ToListAsync(r => r.Id);
        if (idsFailed.Count > 0)
            ids.AddRange(idsFailed);
        if (ids.Count == 0) break;

        var deleted = await freeSql.Delete<RelationDbMessageStoragePublishedModel>()
            .Where(r => ids.Contains(r.Id))
            .ExecuteAffrowsAsync();
        totalDeleted += deleted;
        batchCount++;

        if (batchCount % 50 == 0)
            Console.WriteLine($"  Deleted {totalDeleted:N0} rows after {batchCount} batches ({sw.ElapsedMilliseconds}ms)");

        if (ids.Count < batchSize) break;
    }

    sw.Stop();
    Console.WriteLine($"  DeleteExpiresAsync total: {totalDeleted:N0} rows deleted in {batchCount} batches, {sw.ElapsedMilliseconds}ms");

    var remaining = await freeSql.Select<RelationDbMessageStoragePublishedModel>().CountAsync();
    Console.WriteLine($"  Published table remaining rows: {remaining:N0}");
}

IFreeSql CreateTempReceivedFreeSql(string tableName)
{
    var localFsql = new FreeSqlBuilder()
        .UseConnectionString(DataType.PostgreSQL, connStr)
        .UseAutoSyncStructure(true)
        .UseNoneCommandParameter(true)
        .Build();

    localFsql.Aop.CommandBefore += (_, e) =>
    {
        e.Command.CommandTimeout = 300;
    };

    localFsql.CodeFirst.ConfigEntity<PerfReceivedModel>(e =>
    {
        e.Name(tableName);
    });
    localFsql.CodeFirst.SyncStructure<PerfReceivedModel>();
    return localFsql;
}

async Task BulkInsertPublished(IFreeSql freeSql, int count)
{
    const int batchSize = 5000;
    var baseTime = DateTimeOffset.UtcNow.AddHours(-2);
    var inserted = 0;
    var sw = Stopwatch.StartNew();

    while (inserted < count)
    {
        var batch = Math.Min(batchSize, count - inserted);
        var items = new List<RelationDbMessageStoragePublishedModel>(batch);

        for (int i = 0; i < batch; i++)
        {
            var idx = inserted + i;
            var createTime = baseTime.AddSeconds(-idx * 0.01);
            var isRetryCandidate = idx % 100 == 0;
            var isDelay = idx % 20 == 0;

            items.Add(new RelationDbMessageStoragePublishedModel
            {
                Id = NextId(),
                MsgId = $"perf-pub-{idx:D10}",
                Environment = environment,
                EventName = $"PerfEvent_{idx % 50}",
                EventBody = $"{{\"data\":\"perf-test-{idx}\"}}",
                CreateTimeTicks = createTime.GetLongDate(),
                IsDelay = isDelay,
                DelayAtTicks = isDelay ? createTime.AddHours(1).GetLongDate() : null,
                ExpireTimeTicks = null,
                EventItems = "{}",
                Status = isRetryCandidate ? MessageStatus.Scheduled : MessageStatus.Succeeded,
                RetryCount = 0,
                IsLocking = false,
                LockEndTicks = null,
            });
        }

        await freeSql.Insert<RelationDbMessageStoragePublishedModel>()
            .AppendData(items)
            .ExecuteAffrowsAsync();

        inserted += batch;
        if (inserted % 100_000 == 0)
        {
            Console.WriteLine($"  Inserted {inserted:N0}/{count:N0} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    sw.Stop();
    Console.WriteLine($"  Total inserted {inserted:N0} rows in {sw.ElapsedMilliseconds}ms\n");
}

async Task BulkInsertReceivedNoDelay(IFreeSql freeSql, int count, string env)
{
    const int batchSize = 5000;
    var baseTime = DateTimeOffset.UtcNow.AddHours(-2);
    var inserted = 0;
    var sw = Stopwatch.StartNew();

    while (inserted < count)
    {
        var batch = Math.Min(batchSize, count - inserted);
        var items = new List<PerfReceivedModel>(batch);

        for (int i = 0; i < batch; i++)
        {
            var idx = inserted + i;
            var createTime = baseTime.AddSeconds(-idx * 0.01);
            var isRetryCandidate = idx % 100 == 0;

            items.Add(new PerfReceivedModel
            {
                Id = NextId(),
                MsgId = $"perf-recv-nd-{idx:D10}",
                Environment = env,
                EventName = $"PerfEvent_{idx % 50}",
                EventHandlerName = $"PerfHandler_{idx % 30}",
                EventBody = $"{{\"data\":\"perf-recv-nd-{idx}\"}}",
                CreateTimeTicks = createTime.GetLongDate(),
                IsDelay = false,
                DelayAtTicks = null,
                ExpireTimeTicks = null,
                EventItems = "{}",
                Status = isRetryCandidate ? MessageStatus.Scheduled : MessageStatus.Succeeded,
                RetryCount = 0,
                IsLocking = false,
                LockEndTicks = null,
            });
        }

        await freeSql.Insert<PerfReceivedModel>()
            .AppendData(items)
            .ExecuteAffrowsAsync();

        inserted += batch;
        if (inserted % 100_000 == 0)
        {
            Console.WriteLine($"  Inserted {inserted:N0}/{count:N0} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    sw.Stop();
    Console.WriteLine($"  Total inserted {inserted:N0} rows in {sw.ElapsedMilliseconds}ms\n");
}

async Task BulkInsertReceivedMostDelay(IFreeSql freeSql, int count, string env)
{
    const int batchSize = 5000;
    var baseTime = DateTimeOffset.UtcNow.AddHours(-2);
    var inserted = 0;
    var sw = Stopwatch.StartNew();

    while (inserted < count)
    {
        var batch = Math.Min(batchSize, count - inserted);
        var items = new List<PerfReceivedModel>(batch);

        for (int i = 0; i < batch; i++)
        {
            var idx = inserted + i;
            var createTime = baseTime.AddSeconds(-idx * 0.01);
            var isRetryCandidate = idx % 100 == 0;
            var isDelay = idx % 5 != 0;

            items.Add(new PerfReceivedModel
            {
                Id = NextId(),
                MsgId = $"perf-recv-md-{idx:D10}",
                Environment = env,
                EventName = $"PerfEvent_{idx % 50}",
                EventHandlerName = $"PerfHandler_{idx % 30}",
                EventBody = $"{{\"data\":\"perf-recv-md-{idx}\"}}",
                CreateTimeTicks = createTime.GetLongDate(),
                IsDelay = isDelay,
                DelayAtTicks = isDelay ? createTime.AddHours(2).GetLongDate() : null,
                ExpireTimeTicks = null,
                EventItems = "{}",
                Status = isRetryCandidate ? MessageStatus.Scheduled : MessageStatus.Succeeded,
                RetryCount = 0,
                IsLocking = false,
                LockEndTicks = null,
            });
        }

        await freeSql.Insert<PerfReceivedModel>()
            .AppendData(items)
            .ExecuteAffrowsAsync();

        inserted += batch;
        if (inserted % 100_000 == 0)
        {
            Console.WriteLine($"  Inserted {inserted:N0}/{count:N0} ({sw.ElapsedMilliseconds}ms)");
        }
    }

    sw.Stop();
    Console.WriteLine($"  Total inserted {inserted:N0} rows in {sw.ElapsedMilliseconds}ms\n");
}

async Task ExplainAnalyze(IFreeSql freeSql, string sql)
{
    Console.WriteLine($"\n  --- EXPLAIN ANALYZE ---");
    try
    {
        var explainSql = $"EXPLAIN ANALYZE {sql}";
        var rows = await freeSql.Ado.QueryAsync<string>(explainSql);
        foreach (var row in rows)
        {
            Console.WriteLine($"  {row}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  EXPLAIN ANALYZE failed: {ex.Message}");
    }
    Console.WriteLine();
}

[FreeSql.DataAnnotations.Table(Name = "eventbus_received_message_perf")]
[FreeSql.DataAnnotations.Index("ix_perf_received_msg_id_handler", "MsgId,EventHandlerName", IsUnique = true)]
[FreeSql.DataAnnotations.Index("ix_perf_received_create_time", "CreateTimeTicks DESC,Status,IsDelay")]
[FreeSql.DataAnnotations.Index("ix_perf_received_expire_time", "Status,ExpireTimeTicks")]
[FreeSql.DataAnnotations.Index("ix_perf_received_delay", "IsDelay,DelayAtTicks")]
public class PerfReceivedModel
{
    [System.ComponentModel.DataAnnotations.Key]
    public long Id { get; set; }
    public string MsgId { get; set; }
    public string Environment { get; set; }
    public string EventName { get; set; }
    public string EventHandlerName { get; set; }
    public string EventBody { get; set; }
    public long CreateTimeTicks { get; set; }
    public bool IsDelay { get; set; }
    public long? DelayAtTicks { get; set; }
    public long? ExpireTimeTicks { get; set; }
    public string EventItems { get; set; }
    public string Status { get; set; }
    public int RetryCount { get; set; }
    public bool IsLocking { get; set; }
    public long? LockEndTicks { get; set; }
}
