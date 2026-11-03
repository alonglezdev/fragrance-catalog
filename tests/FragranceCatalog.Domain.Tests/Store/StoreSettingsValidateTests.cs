using FragranceCatalog.Domain.Store;

namespace FragranceCatalog.Domain.Tests.Store;

public sealed class StoreSettingsValidateTests
{
    private const string ValidName = "Dolce Aroma";
    private const string ValidNumber = "523312345678";
    private const string ValidTemplate = "Hola, me interesa el {product} ({price})";

    [Theory]
    [InlineData(ValidNumber)]
    [InlineData("+52 (33) 1234-5678")]
    [InlineData("")]
    public void Validate_ValidValues_ReturnsNoErrors(string whatsAppNumber)
    {
        var errors = StoreSettings.Validate(ValidName, whatsAppNumber, ValidTemplate);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_BlankStoreName_ReturnsItsCode()
    {
        var errors = StoreSettings.Validate("  ", ValidNumber, ValidTemplate);

        errors.ShouldHaveSingleItem().Code.ShouldBe(StoreSettingsErrorCode.StoreNameRequired);
    }

    [Fact]
    public void Validate_StoreNameTooLong_ReturnsItsCode()
    {
        var errors = StoreSettings.Validate(new string('a', StoreSettings.MaxStoreNameLength + 1), ValidNumber, ValidTemplate);

        errors.ShouldHaveSingleItem().Code.ShouldBe(StoreSettingsErrorCode.StoreNameTooLong);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("1234567890123456")]
    [InlineData("52331234abcd")]
    public void Validate_InvalidWhatsAppNumber_ReturnsItsCodeAndValue(string whatsAppNumber)
    {
        var errors = StoreSettings.Validate(ValidName, whatsAppNumber, ValidTemplate);

        errors.ShouldBe([new StoreSettingsError(StoreSettingsErrorCode.InvalidWhatsAppNumber, whatsAppNumber)]);
    }

    [Fact]
    public void Validate_BlankTemplate_ReturnsItsCode()
    {
        var errors = StoreSettings.Validate(ValidName, ValidNumber, " ");

        errors.ShouldHaveSingleItem().Code.ShouldBe(StoreSettingsErrorCode.MessageTemplateRequired);
    }

    [Fact]
    public void Validate_TemplateTooLong_ReturnsItsCode()
    {
        var errors = StoreSettings.Validate(ValidName, ValidNumber, new string('a', StoreSettings.MaxMessageTemplateLength + 1));

        errors.ShouldHaveSingleItem().Code.ShouldBe(StoreSettingsErrorCode.MessageTemplateTooLong);
    }

    [Fact]
    public void Validate_TwoUnknownPlaceholders_ReturnsOneErrorForEach()
    {
        var errors = StoreSettings.Validate(ValidName, ValidNumber, "Hola {name}, quiero {color}");

        errors.ShouldBe(
        [
            new StoreSettingsError(StoreSettingsErrorCode.UnknownPlaceholder, "{name}"),
            new StoreSettingsError(StoreSettingsErrorCode.UnknownPlaceholder, "{color}"),
        ]);
    }

    [Fact]
    public void Validate_SeveralInvalidValues_ReturnsEveryError()
    {
        var errors = StoreSettings.Validate("", "abc", "Hola {name}");

        errors.Select(error => error.Code).ShouldBe(
        [
            StoreSettingsErrorCode.StoreNameRequired,
            StoreSettingsErrorCode.InvalidWhatsAppNumber,
            StoreSettingsErrorCode.UnknownPlaceholder,
        ]);
    }
}
