using Ardalis.SmartEnum;
using LanguageExt;
using static LanguageExt.Prelude;

namespace SFA.DAS.Zendesk.Monitor.Zendesk
{
    public static class SmartEnumValues
    {
        public static Option<T> ParseIgnoringCase<T>(string value) where T : SmartEnum<T, int> =>
            SmartEnum<T, int>.TryFromName(value, ignoreCase: true, out var reason) ? Some(reason) : None;
    }
}