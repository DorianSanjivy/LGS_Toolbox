using System.Collections;
using System.Collections.Generic;

namespace LGSToolbox
{
    public interface IStatsSink
    {
        IEnumerator Send(StatsSession session, IEnumerable<string> statKeys = null);
    }
}