namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;

public class MessageViewModel
{
    /// <summary>
    /// 消息状态计数
    /// </summary>
    public Dictionary<string, int> StatusCount { get; set; } = new();

    /// <summary>
    /// 消息
    /// </summary>
    public List<MessageStorageModel> Messages { get; set; } = new();

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPage { get; set; }

    /// <summary>
    /// 搜索条件
    /// </summary>
    public string? EventName { get; set; }
}