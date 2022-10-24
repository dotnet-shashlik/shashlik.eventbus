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
        foreach (var s in queryString.Split('&'))
        {
            var queryData = s.Split('=');
            if (string.IsNullOrEmpty(queryData[0]))
            {
                continue;
            }

            if (queryData.Length < 2)
            {
                queryDic[queryData[0]] = string.Empty;
            }
            else
            {
                queryDic[queryData[0]] = queryData[1];
            }

            if (queryData[0] == query)
            {
                queryDic[queryData[0]] = value;
            }
        }

        if (!queryDic.ContainsKey(query))
        {
            queryDic[query] = value;
        }

        uri.Query = string.Join('&', queryDic.Select(x => $"{x.Key}={x.Value}"));
        return uri.Uri.PathAndQuery.ToString();
    }
}