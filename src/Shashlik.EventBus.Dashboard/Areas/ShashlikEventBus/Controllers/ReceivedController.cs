using Microsoft.AspNetCore.Mvc;
using Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Models;

namespace Shashlik.EventBus.Dashboard.Areas.ShashlikEventBus.Controllers;

public class ReceivedController : BaseDashboardController
{
    private readonly IMessageStorage _messageStorage;

    public ReceivedController(IMessageStorage messageStorage)
    {
        _messageStorage = messageStorage;
    }

    public async Task<IActionResult> Index(string? eventName, string? status, int pageSize = 20, int pageIndex = 1)
    {
        ViewBag.Title = "Received";
        ViewBag.Page = "Received";
        var model = new MessageViewModel();
        model.StatusCount = await _messageStorage.GetReceivedMessageStatusCountAsync(CancellationToken.None);
        if (string.IsNullOrEmpty(status) && model.StatusCount.Keys.Count > 0)
        {
            status = model.StatusCount.Keys.First();
        }

        model.Messages = await _messageStorage.SearchReceived(eventName, string.Empty, status,
            (pageIndex - 1) * pageSize,
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

    public async Task Retry(long[]? ids, [FromServices] IReceivedMessageRetryProvider receivedMessageRetryProvider)
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