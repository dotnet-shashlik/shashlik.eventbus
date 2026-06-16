using Microsoft.AspNetCore.Http.Extensions;

namespace Shashlik.EventBus.Dashboard;

public class UrlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public UrlService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentUrl(string query, string value)
    {
        var uri = new UriBuilder(_httpContextAccessor.HttpContext!.Request.GetDisplayUrl());
        var queryString = uri.Query;
        if (queryString.StartsWith('?'))
        {
            queryString = queryString[1..];
        }

        var queryDic = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(queryString))
        {
            foreach (var pair in queryString.Split('&'))
            {
                var idx = pair.IndexOf('=');
                if (idx < 0)
                {
                    if (!string.IsNullOrEmpty(pair))
                        queryDic[Uri.UnescapeDataString(pair)] = string.Empty;
                    continue;
                }

                var key = Uri.UnescapeDataString(pair[..idx]);
                var val = Uri.UnescapeDataString(pair[(idx + 1)..]);
                queryDic[key] = val;
            }
        }

        queryDic[query] = value;

        uri.Query = string.Join('&', queryDic.Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return uri.Uri.PathAndQuery;
    }
}