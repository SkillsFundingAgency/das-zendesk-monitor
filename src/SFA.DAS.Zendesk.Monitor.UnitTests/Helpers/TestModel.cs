using FluentValidation;

public class TestModel
{
    public string Name { get; set; } = string.Empty;
}

public class TestModelValidator : AbstractValidator<TestModel>
{
    public TestModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name must not be empty.");
    }
}