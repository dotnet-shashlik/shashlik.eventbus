// using System;
// using System.Linq;
// using System.Net;
// using System.Net.Sockets;
// using System.Threading;
// using System.Threading.Tasks;
// using FreeRedis;
// using Microsoft.Extensions.Logging;
//
// namespace Shashlik.EventBus.Utils;
//
// public class RedisWorkId
// {
//     private const int LeaseSeconds = 600; // 10分钟租约（统一用秒单位）
//     private const int LeaseCheckInterval = LeaseSeconds / 2; // 看门狗检查间隔
//     private const string KeyPrefix = "SHASHLIK:EVENTBUS:";
//
//     public RedisWorkId(RedisClient redisClient, ILogger<RedisWorkId> logger)
//     {
//         RedisClient = redisClient;
//         Logger = logger;
//     }
//
//     private RedisClient RedisClient { get; }
//     private ILogger<RedisWorkId> Logger { get; }
//
//     /// <summary>
//     /// 从0~workIdMax-1范围递增分配Worker ID
//     /// </summary>
//     /// <param name="workIdMax">最大Worker ID (建议≤128)</param>
//     /// <param name="cancellationToken">取消令牌</param>
//     /// <returns>分配到的Worker ID (0~127)</returns>
//     /// <exception cref="Exception">所有ID被占用时抛出</exception>
//     public ushort GetWorkerId(int workIdMax, CancellationToken cancellationToken)
//     {
//         // 生成唯一实例标识 (IP+端口+短GUID)
//         var instanceId = $"{Guid.NewGuid():N}";
//         var value = $"{instanceId}$$${DateTime.UtcNow:O}"; // 使用UTC时间避免时区问题
//
//         using var locker = RedisClient.Lock($"{KeyPrefix}LOCK", 30);
//         if (locker is null)
//             throw new NotSupportedException("Get worker id from redis occur error");
//         // 从0开始递增尝试所有可用ID
//         for (ushort workerId = 0; workerId < workIdMax; workerId++)
//         {
//             if (TryAcquireId(workerId, instanceId, value, KeyPrefix, cancellationToken, out var acquiredId))
//                 return acquiredId;
//         }
//
//         throw new Exception($"Redis中所有{workIdMax}个Worker ID均被占用，无法完成初始化");
//     }
//
//     private bool TryAcquireId(ushort workerId, string instanceId, string value, string keyPrefix,
//         CancellationToken cancellationToken, out ushort acquiredId)
//     {
//         acquiredId = workerId;
//         var redisKey = $"{keyPrefix}{workerId:D4}";
//
//         // 1. 尝试直接获取租约
//         if (RedisClient.SetNx(redisKey, value, TimeSpan.FromSeconds(LeaseSeconds)))
//         {
//             StartWatchDog(redisKey, instanceId, cancellationToken);
//             return true;
//         }
//
//         // 2. 检查现有租约
//         var existing = RedisClient.Get(redisKey);
//         if (string.IsNullOrEmpty(existing))
//             return false;
//
//         // 3. 验证租约格式 (instanceId$$$ISO8601)
//         var parts = existing.Split("$$$", 2);
//         if (parts.Length != 2 || !DateTime.TryParse(parts[1], out var leaseTime))
//         {
//             // 格式错误：尝试原子覆盖
//             if (RedisClient.GetSet(redisKey, value) == null)
//             {
//                 StartWatchDog(redisKey, instanceId, cancellationToken);
//                 return true;
//             }
//
//             return false;
//         }
//
//         // 4. 检查是否自身持有租约
//         if (parts[0] == instanceId)
//         {
//             StartWatchDog(redisKey, instanceId, cancellationToken);
//             return true;
//         }
//
//         // 5. 检查租约是否过期 (超过LeaseSeconds视为失效)
//         if ((DateTime.UtcNow - leaseTime).TotalSeconds > LeaseSeconds)
//         {
//             // 原子覆盖过期租约
//             var oldValue = RedisClient.GetSet(redisKey, value);
//             if (string.IsNullOrEmpty(oldValue) || oldValue.StartsWith(parts[0])) // 确保覆盖的是过期租约
//             {
//                 StartWatchDog(redisKey, instanceId, cancellationToken);
//                 return true;
//             }
//         }
//
//         return false;
//     }
//
//     private void StartWatchDog(string redisKey, string instanceId, CancellationToken cancellationToken)
//     {
//         SetInterval(() =>
//         {
//             var value = $"{instanceId}$$${DateTime.UtcNow:O}";
//             var existing = RedisClient.Get(redisKey);
//
//             // 只更新自己持有的租约
//             if (!string.IsNullOrEmpty(existing) &&
//                 existing.StartsWith(instanceId + "$$$", StringComparison.Ordinal))
//             {
//                 RedisClient.Set(redisKey, value, TimeSpan.FromSeconds(LeaseSeconds));
//                 Logger.LogDebug($"看门狗续约: {redisKey} = {value}");
//             }
//             else
//             {
//                 Logger.LogError($"看门狗检测到租约被抢占: {redisKey} (期望持有者={instanceId})");
//             }
//         }, TimeSpan.FromSeconds(LeaseCheckInterval), cancellationToken);
//     }
//
//     /// <summary>
//     ///     定时执行任务,不会立即执行
//     /// </summary>
//     /// <param name="action">要执行的表达式</param>
//     /// <param name="interval">间隔时间</param>
//     /// <param name="cancellationToken">撤销</param>
//     /// <return></return>
//     public static void SetInterval(Action action, TimeSpan interval, CancellationToken cancellationToken = default)
//     {
//         if (cancellationToken.IsCancellationRequested)
//             return;
//         if (interval <= TimeSpan.Zero)
//             throw new ArgumentException("invalid interval.", nameof(interval));
//         Task.Run(async () =>
//         {
//             using var timer = new PeriodicTimer(interval);
//             while (await timer.WaitForNextTickAsync(cancellationToken))
//                 action();
//         }, cancellationToken);
//     }
//
//     // 以下辅助方法保持不变（仅做安全增强）
//     public static string GetIp()
//     {
//         try
//         {
//             using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
//             socket.Connect("8.8.8.8", 65530);
//             return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString()
//                    ?? throw new InvalidOperationException("无法获取本机IP");
//         }
//         catch (Exception ex)
//         {
//             throw new InvalidOperationException("获取本机IP地址失败", ex);
//         }
//     }
//
//     public static int? GetListenPort(string? address)
//     {
//         if (string.IsNullOrWhiteSpace(address))
//             return null;
//
//         try
//         {
//             return new Uri(address).Port;
//         }
//         catch
//         {
//             return address.Split(':').LastOrDefault()?.Trim().ParseTo<int>();
//         }
//     }
// }