using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.TestUtils;

/// <summary>
/// Helper class that seeds deterministic test data into an
/// <see cref="AppDbContext"/> instance. Intended for use during test
/// database initialization so tests run against a known dataset.
/// </summary>
public static class TestDataSeeds
{
    /// <summary>
    /// Assignment id for Paula's managing-director role on the Karlstad main unit. Pinned so an
    /// instance delegation can be hung off it (see <see cref="Exec"/>) to assert that subunits do
    /// not inherit the main unit's authorized instances.
    /// </summary>
    private static readonly Guid PaulaKarlstadMainAssignmentId = Guid.Parse("0196a0b1-0001-7001-8001-000000000099");

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

            // Paula is also a rightholder on the Karlstad main unit, carrying an instance delegation.
            // This is the path through which authorized instances surface (mirrors the Kaos/Josephine seed),
            // used to assert that the subunit does not inherit the main unit's instances.
            new()
            {
                Id = PaulaKarlstadMainAssignmentId,
                FromId = TestEntities.MainUnitKarlstad,
                ToId = TestEntities.PersonPaula,
                RoleId = RoleConstants.Rightholder,
            },
        ]);
        #endregion

        #region TestData Assignments and Delegations
        db.Assignments.AddRange(TestData.Assignments);
        db.AssignmentPackages.AddRange(TestData.AssignmentPackages);
        db.AssignmentInstances.AddRange(TestData.AssignmentInstances);

        // An instance delegated to Paula on the Karlstad MAIN unit. Used to assert that a subunit
        // does not inherit the main unit's authorized instances (roles/packages are inherited, instances are not).
        db.AssignmentInstances.Add(new AssignmentInstance
        {
            AssignmentId = PaulaKarlstadMainAssignmentId,
            ResourceId = TestData.SiriusSkattemelding.Id,
            InstanceId = "urn:altinn:instance-id:50208075/c3d4e5f6-a7b8-4c9d-8e0f-1a2b3c4d5e6f",
            PolicyPath = "sirius-skattemelding-v1/karlstad-main/delegationpolicy.xml",
            PolicyVersion = "1.0",
        });

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
