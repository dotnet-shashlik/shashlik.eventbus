namespace Shashlik.EventBus;

/// <summary>
/// 主id生成器, 默认Y
/// </summary>
public interface IIdGenerator
{
    long NextId();
}