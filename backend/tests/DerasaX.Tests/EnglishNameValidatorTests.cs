using DerasaX.Application.Common;
using DerasaX.Domain.Exceptions;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Pure unit tests for <see cref="EnglishNameValidator"/> — no host/DB needed. Every account
/// creation/reset flow (SystemAdmin onboarding, SchoolAdmin provisioning) routes full-name
/// validation through this single helper.
/// </summary>
public class EnglishNameValidatorTests
{
    [Theory]
    [InlineData("Ahmed Hassan")]
    [InlineData("O'Brien-Smith")]
    [InlineData("Jean.Paul")]
    [InlineData("Mary Ann Lee")]
    [InlineData("  Trimmed Name  ")]
    public void Validate_accepts_english_names(string name)
    {
        var ex = Record.Exception(() => EnglishNameValidator.Validate(name));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("محمد أحمد")]     // Arabic
    [InlineData("12345")]         // digits-only
    [InlineData("John🙂")]        // emoji
    [InlineData("John@Doe")]      // symbol
    [InlineData("محمد123")]       // mixed script + digits
    public void Validate_rejects_non_english_names(string name)
    {
        Assert.Throws<ValidationException>(() => EnglishNameValidator.Validate(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_rejects_empty_or_whitespace_names(string? name)
    {
        Assert.Throws<BadRequestException>(() => EnglishNameValidator.Validate(name));
    }
}
