using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;

namespace Altinn.AccessManagement.TestUtils;

/// <summary>
/// Helper class that seeds deterministic test data into an
/// <see cref="AppDbContext"/> instance. Intended for use during test
/// database initialization so tests run against a known dataset.
/// </summary>
public static class TestDataSeeds
{
    /// <summary>
    /// Applies a minimal set of test entities to the provided <see cref="AppDbContext"/>
    /// and saves the changes. This method is safe to call during test database
    /// bootstrapping and is idempotent if called on a fresh database.
    /// </summary>
    /// <param name="db">The <see cref="AppDbContext"/> to seed.</param>
    public static async Task Exec(AppDbContext db)
    {
        #region Resource Types
        db.ResourceTypes.Add(TestData.TestResourceType);
        db.ResourceTypes.Add(TestData.CorrespondenceResourceType);
        #endregion

        #region Entities
        db.Entities.AddRange([
            TestEntities.PersonPaula,
            TestEntities.PersonOrjan,
            TestEntities.MainUnitNordis,
            TestEntities.OrganizationNordisAS,
            TestEntities.OrganizationVerdiqAS,
            TestEntities.SystemUserClient,
            TestEntities.SystemUserStandard,
            TestEntities.OrganizationOkernBorettslag,
            TestEntities.OrganizationNufExampleNUF,
            TestEntities.SIUserMarius,
            TestEntities.EmailUserMarius,
            TestEntities.EmailUserHarryPotter,
            TestEntities.EduUserHermioneGranger,
            TestEntities.UserRonWeasley,
            TestEntities.MainUnitKarlstad,
            TestEntities.SubunitKarlstad,
            TestEntities.OrganizationOrsta,
            TestEntities.PersonKasper
        ]);
        #endregion

        #region TestData Entities
        db.Entities.AddRange([
            TestData.HanSoloEnterprise,
            TestData.HanSolo,
            TestData.BenSolo,
            TestData.LeiaOrgana,
            TestData.LukeSkyWalker,
            TestData.BakerJohnsen,
            TestData.SvendsenAutomobil,
            TestData.FredriksonsFabrikk,
            TestData.NAV,
            TestData.RegnskapNorge,
            TestData.MittRegnskap,
            TestData.RpcAS,
            TestData.StorMektigTenesteeier,
            TestData.LarsBakke,
            TestData.HildeStrand,
            TestData.KnutVik,
            TestData.MortenDahl,
            TestData.GreteHolm,
            TestData.ArneLund,
            TestData.SiljeHaugen,
            TestData.EinarBerg,
            TestData.ToneKvam,
            TestData.BjornMoe,
            TestData.RandiLie,
            TestData.VegardSolberg,
            TestData.IngerNygard,
            TestData.AstridJohansen,
            TestData.TrondLarsen,
            TestData.MaritEriksen,
            TestData.GeirPedersen,
            TestData.OddHalvorsen,
            TestData.LivKristiansen,
            TestData.SteinarAndreassen,
            TestData.HelgeNilsen,
            TestData.MalinEmilie,
            TestData.Thea,
            TestData.JosephineYvonnesdottir,
            TestData.BodilFarmor,
            TestData.DumboAdventures,
            TestData.MilleHundefrisor,
            TestData.Milena,
            TestData.KaosMagicDesignAndArts,
            TestData.JinxArcane,
            TestData.AlexTheArtist
        ]);
        #endregion

        db.Providers.AddRange([
            TestData.ServiceOwnerNAV,
            ]);

        #region Assignments
        db.Assignments.AddRange([
            new()
            {
                FromId = TestEntities.OrganizationNordisAS,
                ToId = TestEntities.MainUnitNordis,
                RoleId = RoleConstants.HasAsRegistrationUnitBEDR,
            },
            new()
            {
                FromId = TestEntities.OrganizationOrsta,
                ToId = TestEntities.PersonKasper,
                RoleId = RoleConstants.ManagingDirector,
            },
            new()
            {
                FromId = TestEntities.MainUnitKarlstad,
                ToId = TestEntities.PersonPaula,
                RoleId = RoleConstants.ManagingDirector,
            },
            new()
            {
                FromId = TestEntities.SubunitKarlstad,
                ToId = TestEntities.PersonPaula,
                RoleId = RoleConstants.ManagingDirector,
            },
        ]);
        #endregion

        #region TestData Assignments and Delegations
        db.Assignments.AddRange(TestData.Assignments);
        db.AssignmentPackages.AddRange(TestData.AssignmentPackages);
        db.AssignmentInstances.AddRange(TestData.AssignmentInstances);
        db.AssignmentResources.AddRange(TestData.AssignmentResources);
        //// db.Delegations.AddRange(TestData.Delegations);
        #endregion

        #region Resources
        db.Resources.Add(TestData.MattilsynetBakeryService);
        db.Resources.Add(TestData.SiriusSkattemelding);
        db.Resources.Add(TestData.NavSykepengerDialog);
        db.Resources.Add(TestData.DiheOmsetningsoppgaveAlkohol);
        db.Resources.Add(TestData.NavSykepengerSykmelding);
        db.Resources.Add(TestData.SkattResource);
        db.Resources.Add(TestData.TestdirektoratetCorrespondenceService);
        db.Resources.AddRange(TestData.MvaResource);

        #endregion

        await db.SaveChangesAsync();
    }
}
