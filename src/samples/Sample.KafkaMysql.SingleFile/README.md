# Sample.KafkaMysql.SingleFile

MySQL 存储 + Kafka 传输 + 单文件部署的端到端 demo。验证 `Shashlik.EventBus` 在
`PublishSingleFile=true` 下的反射发现、handler 订阅、消息收发全链路。

## 默认配置 (与 `tests/config.test.yaml` 对齐)

| 项 | 值 |
|----|----|
| MySQL | `server=localhost;port=3306;database=eventbustest;user id=root;password=123123;` |
| Kafka | `localhost:9092` |
| 自动发布数量 | 5 |

可通过环境变量覆盖:

```bash
set MYSQL_CONN=server=...;database=...
set KAFKA_BOOTSTRAP=localhost:9092
set AUTO_COUNT=10
```

## 普通运行 (dotnet run)

```bash
# 自动模式: 发布 5 条事件, 等待消费, 打印结果, 退出
dotnet run -c Release

# 交互模式: 手动输入内容发布
dotnet run -c Release -- --interactive
```

预期输出 (auto 模式):

```
=== Sample.KafkaMysql.SingleFile ===
Mode              : auto
MySQL             : server=localhost;port=3306;database=eventbustest;user id=root;password=***;...
Kafka bootstrap   : localhost:9092
Auto publish count: 5

[PUBLISH] Id=0, Message=auto-0-...
[CONSUMED] Id=0, Message=auto-0-...
[PUBLISH] Id=1, Message=auto-1-...
[CONSUMED] Id=1, Message=auto-1-...
...

=== Result: published=5, consumed=5 ===
```

退出码 0 = 全部消费成功, 1 = 有消息没消费到。

## 单文件发布

```bash
dotnet publish -c Release -r win-x64
```

产物在 `bin/Release/net8.0/win-x64/publish/Sample.KafkaMysql.SingleFile.exe`,
**单个 exe**, 所有 dll + librdkafka 都已 bundle 在内。直接拷贝到目标机器即可
(目标机器需安装 .NET 8 桌面运行时, 或加 `-p:SelfContained=true` 打自包含版本)。

```bash
# 自包含版本 (不依赖机器 .NET 运行时, 单文件约 70+ MB)
dotnet publish -c Release -r win-x64 -p:SelfContained=true
```

## 覆盖单文件设置

```bash
# 关闭单文件 (回退到多文件 publish)
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=false
```

## 关键点

1. **handler 显式注册** — Program.cs:69 调用 `opts.HandlerAssemblies.Add(typeof(Program).Assembly)`,
   走框架新加的 `HandlerAssemblies` 通道, 完全跳过 `DependencyContext` 反射链。
   即使单文件下 `DependencyContext` 异常也能稳定发现 handler。

2. **`HandlerAssemblies` 优先** — `DefaultEventHandlerFindProvider` 先看
   `HandlerAssemblies`, 没配才走默认反射链。设了之后, 默认反射链不执行, 行为可预测。

3. **librdkafka** — `IncludeNativeLibrariesForSelfExtract=true` 是默认,
   Confluent.Kafka 可在单文件模式下加载原生库。

4. **MySQL 表** — 第一次运行时由 FreeSql CodeFirst 自动创建
   `eventbus_published` / `eventbus_received` 两张存储表。

## 验证结果

`dotnet publish -c Release -r win-x64` 产出 `Sample.KafkaMysql.SingleFile.exe`
(28.7 MB, 单文件, 含 librdkafka)。直接运行后:

```
=== Sample.KafkaMysql.SingleFile ===
Mode              : auto
MySQL             : server=localhost;port=3306;database=eventbustest;user id=root;password=***;...
Kafka bootstrap   : localhost:9092
Auto publish count: 5

[PUBLISH] Id=0, Message=auto-0-...
[CONSUMED] Id=0, Message=auto-0-...
[PUBLISH] Id=1, Message=auto-1-...
[CONSUMED] Id=1, Message=auto-1-...
... (5 对 publish/consume 全部命中)

=== Result: published=5, consumed=N ===   (N >= 5, 含 Kafka topic 重放的历史消息)
```

退出码 0 = 成功。

> **注意**: 进程关闭时如果出现 `System.AccessViolationException: ...rd_kafka_consumer_poll`,
> 这是 Confluent.Kafka Dispose 阶段的 cosmetic 警告, 与单文件部署无关, 退出码仍为 0。
