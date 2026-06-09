using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

public class PublishedController : BaseDashboardController
{
    private readonly IMessageStorage _messageStorage;
    private readonly IOptionsMonitor<EventBusOptions> _optionsMonitor;

    public PublishedController(IOptionsMonitor<EventBusDashboardOption> options, IMessageStorage messageStorage,
        IOptionsMonitor<EventBusOptions> optionsMonitor) :
        base(options)
    {
        _messageStorage = messageStorage;
        _optionsMonitor = optionsMonitor;
    }


    public async Task<IActionResult> Index(DateTimeOffset beginTime, DateTimeOffset endTime, string? eventName,
        string? status, int pageSize = 20, int pageIndex = 1)
    {
        ViewBag.Title = "Published";
        ViewBag.Page = "Published";
        var model = new MessageViewModel();
        model.StatusCount =
            await _messageStorage.GetPublishedMessageStatusCountsAsync(_optionsMonitor.CurrentValue.Environment,
                beginTime, endTime, CancellationToken.None);
        if (string.IsNullOrEmpty(status) && model.StatusCount.Keys.Count > 0)
        {
            status = model.StatusCount.Keys.First();
        }

        model.Messages = await _messageStorage.SearchPublishedAsync(_optionsMonitor.CurrentValue.Environment,
            beginTime, endTime, eventName, status, (pageIndex - 1) * pageSize,
            pageSize, CancellationToken.None);
        model.PageIndex = pageIndex;
        model.PageSize = pageSize;
        model.EventName = eventName;
        var total = 0M;
        if (!string.IsNullOrEmpty(status))
        {
            total = model.StatusCount.ContainsKey(status) ? model.StatusCount[status] : total;
        }

        model.TotalPage = Convert.ToInt32(Math.Ceiling(total / pageSize));
        return View("Messages", model);
    }

    public async Task Retry(string[]? ids, [FromServices] IPublishedMessageRetryProvider publishedMessageRetryProvider)
    {
        if (ids == null)
        {
            return;
        }

        foreach (var id in ids)
        {
            try
            {
                await publishedMessageRetryProvider.RetryAsync(id, CancellationToken.None);
            }
            catch (Exception)
            {
                //
            }
        }
    }
}