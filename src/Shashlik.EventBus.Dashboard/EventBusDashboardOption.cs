using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shashlik.EventBus.Dashboard
{
    /// <summary>
    /// 面板选项
    /// </summary>
    public class EventBusDashboardOption
    {
        private string _urlPrefix = "/eventBus";

        /// <summary>
        /// 面板路由前缀
        /// </summary>
        public string UrlPrefix
        {
            get => _urlPrefix;
            set
            {
                _urlPrefix = value;
                if (!_urlPrefix.StartsWith('/'))
                {
                    _urlPrefix = '/' + _urlPrefix;
                }
            }
        }
    }
}
