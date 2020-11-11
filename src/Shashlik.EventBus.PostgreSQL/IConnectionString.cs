using System;
using System.Collections.Generic;
using System.Text;

namespace Shashlik.EventBus.PostgreSQL
{
    public interface IConnectionString
    {
        string ConnectionString { get; }
    }
}
