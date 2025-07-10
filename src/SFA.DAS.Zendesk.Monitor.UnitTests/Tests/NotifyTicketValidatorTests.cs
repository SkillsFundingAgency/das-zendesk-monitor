using FluentAssertions;
using SFA.DAS.Zendesk.Monitor.UnitTests;
using Xunit;
using ZenWatchFunction;

namespace SFA.DAS.Zendesk.Monitor.UnitTests.Tests
{
    public class NotifyTicketValidatorTests
    {
        private readonly NotifyTicketValidator _validator = new NotifyTicketValidator();

        [Fact]
        public void Should_Return_Error_When_Id_Is_Zero()
        {
            var model = new NotifyTicket { Id = 0 };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(NotifyTicket.Id));
        }

        [Fact]
        public void Should_Pass_Validation_When_Id_Is_Positive()
        {
            var model = new NotifyTicket { Id = 12345 };

            var result = _validator.Validate(model);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Return_Single_Error_When_Id_Is_Zero()
        {
            var model = new NotifyTicket { Id = 0 };

            var result = _validator.Validate(model);

            result.Errors.Should().HaveCount(1);
            result.Errors[0].PropertyName.Should().Be(nameof(NotifyTicket.Id));
            result.Errors[0].ErrorMessage.Should().Contain("must not be empty");
        }
    }
}
