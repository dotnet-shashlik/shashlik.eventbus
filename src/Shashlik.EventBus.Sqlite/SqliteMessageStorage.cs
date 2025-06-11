using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Sqlite;

public class SqliteMessageStorage : RelationDbMessageStorageBase
{
    public SqliteMessageStorage(IFreeSqlFactory freeSqlFactory, IOptionsMonitor<EventBusSqliteOptions> options) :
        base(freeSqlFactory)
    {
        Options = options;
    }

    private IOptionsMonitor<EventBusSqliteOptions> Options { get; }

    public override async Task<string> SaveReceivedAsync(MessageStorageModel message,
        CancellationToken cancellationToken = default)
    {
        var entity = ToReceivedSaveObject(message);

        string sql = $@"
INSERT OR IGNORE INTO
	""{Options.CurrentValue.ReceivedTableName}""
(
	""msgId"",
	""environment"",
	""eventName"",
	""eventHandlerName"",
	""eventBody"",
	""createTime"",
	""isDelay"",
	""delayAt"",
	""expireTime"",
	""eventItems"",
	""status"",
	""retryCount"",
	""isLocking"",
	""lockEnd""
)
VALUES
(
	@MsgId,
	@Environment,
	@EventName,
	@EventHandlerName,
	@EventBody,
	@CreateTime,
	@IsDelay,
	@DelayAt,
	@ExpireTime,
	@EventItems,
	@Status,
	@RetryCount,
	@IsLocking,
	@LockEnd
)
";

        await FreeSql.Ado.ExecuteNonQueryAsync(sql, entity, cancellationToken);
        var id = FreeSql.Select<RelationDbMessageStorageReceivedModel>()
            .Where(r => r.MsgId == message.MsgId && r.EventHandlerName == message.EventHandlerName)
            .First(r => r.Id);
        message.Id = id.ToString();
        return id.ToString();
    }
}