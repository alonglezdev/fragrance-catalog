using FragranceCatalog.Domain.Store;

namespace FragranceCatalog.Domain.Tests.Store;

public sealed class StoreSettingsTests
{
    private const string ValidTemplate = "Hola, me interesa el {product} ({price})";

    [Fact]
    public void CreateDefault_NewStore_HasProvisionalNameAndNoWhatsApp()
    {
        var settings = StoreSettings.CreateDefault();

        settings.Id.ShouldBe(StoreSettings.SingletonId);
        settings.StoreName.ShouldBe("Dolce Aroma");
        settings.WhatsAppNumber.ShouldBeEmpty();
        settings.HasWhatsAppNumber.ShouldBeFalse();
        settings.WhatsAppMessageTemplate.ShouldBe(ValidTemplate);
    }

    [Fact]
    public void Update_FormattedPhoneNumber_StoresDigitsOnly()
    {
        var settings = StoreSettings.CreateDefault();

        settings.Update("Dolce Aroma", "+52 (33) 1234-5678", ValidTemplate);

        settings.WhatsAppNumber.ShouldBe("523312345678");
        settings.HasWhatsAppNumber.ShouldBeTrue();
    }

    [Fact]
    public void Update_EmptyPhoneNumber_DisablesWhatsApp()
    {
        var settings = StoreSettings.CreateDefault();
        settings.Update("Dolce Aroma", "523312345678", ValidTemplate);

        settings.Update("Dolce Aroma", "", ValidTemplate);

        settings.HasWhatsAppNumber.ShouldBeFalse();
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("1234567890123456")]
    [InlineData("52 33 ABC 5678")]
    public void Update_InvalidPhoneNumber_ThrowsArgumentException(string whatsAppNumber)
    {
        var settings = StoreSettings.CreateDefault();

        Should.Throw<ArgumentException>(() => settings.Update("Dolce Aroma", whatsAppNumber, ValidTemplate));
    }

    [Fact]
    public void Update_TemplateWithEveryPlaceholder_IsAccepted()
    {
        var settings = StoreSettings.CreateDefault();

        settings.Update("Dolce Aroma", "", "{brand} {product} {size} {price}");

        settings.WhatsAppMessageTemplate.ShouldBe("{brand} {product} {size} {price}");
    }

    [Fact]
    public void Update_TemplateWithUnknownPlaceholder_ThrowsArgumentException()
    {
        var settings = StoreSettings.CreateDefault();

        Should.Throw<ArgumentException>(() => settings.Update("Dolce Aroma", "", "Hola {name}, quiero {product}"));
    }

    [Fact]
    public void Update_TemplateLongerThanLimit_ThrowsArgumentOutOfRangeException()
    {
        var settings = StoreSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update("Dolce Aroma", "", new string('a', 501)));
    }

    [Fact]
    public void Update_StoreNameLongerThanLimit_ThrowsArgumentOutOfRangeException()
    {
        var settings = StoreSettings.CreateDefault();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.Update(new string('a', 81), "", ValidTemplate));
    }

    [Fact]
    public void Update_BlankStoreName_ThrowsArgumentException()
    {
        var settings = StoreSettings.CreateDefault();

        Should.Throw<ArgumentException>(() => settings.Update("  ", "", ValidTemplate));
    }

    [Fact]
    public void FindUnknownPlaceholders_MixedPlaceholders_ReturnsOnlyUnknownOnes()
    {
        var unknown = StoreSettings.FindUnknownPlaceholders("{product} {name} {price} {name} {color}");

        unknown.ShouldBe(["{name}", "{color}"]);
    }
}
