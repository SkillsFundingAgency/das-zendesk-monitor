using FluentAssertions;
using Newtonsoft.Json.Serialization.ContractResolverExtentions;
using Xunit;

namespace SFA.DAS.Zendesk.Monitor.UnitTests
{
    public class SnakeCasePropertyResolverTests
    {
        private readonly SnakeCasePropertyNamesContractResolver sut = new SnakeCasePropertyNamesContractResolver();

        [Theory]
        [InlineData("Single", "single")]
        [InlineData("OneName", "one_name")]
        [InlineData("OneTwoNames", "one_two_names")]
        [InlineData("AName", "a_name")]
        [InlineData("Numeral1", "numeral1")]
        [InlineData("Numeral1Name", "numeral1_name")]
        [InlineData("NumeralName1", "numeral_name1")]
        [InlineData("CAPSName", "caps_name")]
        [InlineData("CAPSNameCAPS", "caps_name_caps")]
        [InlineData("CAPS1NameCAPS2", "caps1_name_caps2")]
        [InlineData("MiddleCAPSName", "middle_caps_name")]
        public void ResolvesName(string snake, string expected)
        {
            var actual = sut.GetResolvedPropertyName(snake);
            actual.Should().BeEquivalentTo(expected);
        }
    }
}