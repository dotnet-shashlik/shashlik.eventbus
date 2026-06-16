using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

public class ReceivedController : BaseDashboardController
{
    private readonly IMessageStorage _messageStorage;
    private readonly IOptionsMonitor<EventBusOptions> _optionsMonitor;

    public ReceivedController(IOptionsMonitor<EventBusDashboardOption> options, IMessageStorage messageStorage,
        IOptionsMonitor<EventBusOptions> optionsMonitor) : base(options)
    {
        _messageStorage = messageStorage;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<IActionResult> Index(DateTimeOffset? beginTime, DateTimeOffset? endTime,
        string? eventName, string? eventHandlerName, string? status,
        int pageSize = 20, int pageIndex = 1)
    {
        ViewBag.Title = "Received";
        ViewBag.Page = "Received";

        var now = DateTimeOffset.Now;
        var bt = beginTime ?? now.AddDays(-7);
        var et = endTime ?? now.AddDays(1);

        var model = new MessageViewModel();
        model.BeginTime = bt;
        model.EndTime = et;
        model.StatusCount = await _messageStorage.GetReceivedMessageStatusCountAsync(
            _optionsMonitor.CurrentValue.Environment,
            bt, et, CancellationToken.None);
        if (string.IsNullOrEmpty(status) && model.StatusCount.Keys.Count > 0)
        {
            status = model.StatusCount.Keys.First();
        }

        model.Messages = await _messageStorage.SearchReceivedAsync(_optionsMonitor.CurrentValue.Environment,
            bt, et, eventName, eventHandlerName, status,
            (pageIndex - 1) * pageSize,
            pageSize, CancellationToken.None);
        model.PageIndex = pageIndex;
        model.PageSize = pageSize;
        model.EventName = eventName;
        model.EventHandlerName = eventHandlerName;
        model.Status = status;
        var total = 0M;
        if (!string.IsNullOrEmpty(status))
        {
            total = model.StatusCount.ContainsKey(status) ? model.StatusCount[status] : total;
        }

        model.TotalPage = Convert.ToInt32(Math.Ceiling(total / pageSize));
        return View("Messages", model);
    }

    public async Task Retry(string[]? ids, [FromServices] IReceivedMessageRetryProvider receivedMessageRetryProvider)
    {
        if (ids == null)
        {
            return;
        }

        foreach (var id in ids)
        {
            try
            {
                await receivedMessageRetryProvider.RetryAsync(id, CancellationToken.None);
            }
            catch (Exception)
            {
                //
            }
        }
    }
}