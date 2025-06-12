using FluentAssertions;
using Xunit;
using ZenWatchFunction;
using FluentValidation;

namespace ZenWatchFunction.Tests
{
    public class NotifyTicketValidatorTests
    {
        private readonly NotifyTicketValidator _validator = new NotifyTicketValidator();

        [Fact]
        public void Should_Return_Error_When_Id_Is_Zero()
        {
            // Arrange
            var model = new NotifyTicket { Id = 0 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == nameof(NotifyTicket.Id));
        }

        [Fact]
        public void Should_Pass_Validation_When_Id_Is_Valid()
        {
            // Arrange
            var model = new NotifyTicket { Id = 12345 };

            // Act
            var result = _validator.Validate(model);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}