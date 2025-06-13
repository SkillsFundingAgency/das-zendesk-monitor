using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Collections.Generic;
using System.Net.Http;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public static class HttpRequestMessageExtensions
    {
        public static HttpRequestMessageAssertions Should(this HttpRequestMessage instance)
            => new HttpRequestMessageAssertions(instance);
    }

    public class HttpRequestMessageAssertions :
        ReferenceTypeAssertions<HttpRequestMessage, HttpRequestMessageAssertions>
    {
        public HttpRequestMessageAssertions(HttpRequestMessage instance) : base(instance)
        {
        }

        protected override string Identifier => "HttpRequestMessage";

        public AndConstraint<HttpRequestMessageAssertions> HavePayloadValidatedBy(
            JSchema schema, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(schema != null)
                .FailWith("You can't assert a null schema")
                .Then
                .Given(() => Subject.Content.ReadAsStringAsync().Result)
                .ForCondition(payload => !string.IsNullOrWhiteSpace(payload))
                .FailWith("Expected Content to contain data but was {0}", p => p)
                .Then
                .Given(payload => JToken.Parse(payload))
                .ForCondition(token => token != null)
                .FailWith("Expected Content to be valid JSON but was {0}", p => p)
                .Then
                .Given(token => { token.IsValid(schema, out IList<string> errors); return errors; })
                .ForCondition(errors => errors.Count == 0)
                .FailWith("Expected valid JSON, but found these errors: \n\t{0} \nin\n\t{2}\naccording to schema {1}",
                    errors => string.Join("\n\t", errors), _ => schema, _ => Subject.Content.ReadAsStringAsync().Result);

            return new AndConstraint<HttpRequestMessageAssertions>(this);
        }
    }
}