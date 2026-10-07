using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

[UnitTest]
public class ConstantTranslationTests
{
    [Fact]
    public void TranslateField_PicksLanguageList_AndFallsBackToBokmal()
    {
        var definition = ActivityTypeConstants.AssignmentCreated;

        Assert.Equal("Role assignment created", definition.TranslateField("eng", "Name", definition.Entity.Name));
        Assert.Equal("Rolletildeling oppretta", definition.TranslateField("nno", "Name", definition.Entity.Name));
        Assert.Equal(definition.Entity.Name, definition.TranslateField("nob", "Name", definition.Entity.Name));
        Assert.Equal(definition.Entity.Name, definition.TranslateField(null, "Name", definition.Entity.Name));
        Assert.Equal(definition.Entity.Name, definition.TranslateField("xyz", "Name", definition.Entity.Name));
        Assert.Equal(definition.Entity.Description, definition.TranslateField("eng", "MissingField", definition.Entity.Description));
    }

    [Fact]
    public void ActivityTypeCatalog_HasCompleteEnglishAndNynorskTranslations()
    {
        foreach (var definition in ActivityTypeConstants.AllEntities())
        {
            foreach (var code in new[] { "eng", "nno" })
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(definition.TranslateField(code, "Name", fallback: null)),
                    $"{definition.Entity.Name}: missing {code} Name");
                Assert.False(
                    string.IsNullOrWhiteSpace(definition.TranslateField(code, "Description", fallback: null)),
                    $"{definition.Entity.Name}: missing {code} Description");
            }
        }
    }
}
