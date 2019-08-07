namespace Newtonsoft.Json.Serialization.ContractResolverExtentions
{
    using System.Text.RegularExpressions;

    public class SnakeCasePropertyNamesContractResolver : DefaultContractResolver
    {
        protected internal Regex converter = new Regex(@"((?<=[a-z])(?<b>[A-Z])|(?<=[^_])(?<b>[A-Z][a-z]))");

        protected override string ResolvePropertyName(string propertyName)
        {
            return converter.Replace(propertyName, "_${b}").ToLower();
        }
    }
}