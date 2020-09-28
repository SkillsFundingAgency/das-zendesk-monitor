using System;
using System.Collections.Generic;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class EnumValues
    {
        public static IEnumerable<T> ListEnum<T>() where T : struct, Enum
            => (T[])Enum.GetValues(typeof(T));
    }
}