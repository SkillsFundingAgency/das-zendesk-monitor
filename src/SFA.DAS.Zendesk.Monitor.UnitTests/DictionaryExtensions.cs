using System.Collections.Generic;
using System;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public static class DictionaryExtensions
    {
        public static T GetOrAdd<T>(this Dictionary<long, T> c, long key, Func<T> createNew)
        {
            if (!c.TryGetValue(key, out var ticketComments))
            {
                ticketComments = createNew();
                c[key] = ticketComments;
            }

            return ticketComments;
        }
    }

    /*
     * Watcher marks ticket as sharing before sending to middleware
     * Watcher marks ticket as shared after successfully sending to middleware
     * Watcher leaves ticket as sharing after unsuccessfully sending to middleware
     *
     */
}