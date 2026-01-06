using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

/// <summary>
/// Partial StaticDataIngest
/// </summary>
public static partial class StaticDataIngest 
{
    /// <summary>
    /// Ingest RoleMap data
    /// </summary>
    public static async Task IngestRoleMap(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        /*DAGL*/
        var roleDagl = RoleConstants.ManagingDirector.Id; // GetRoleId("urn:altinn:external-role:ccr:daglig-leder", "daglig-leder");
        /*LEDE*/
        var roleLede = RoleConstants.ChairOfTheBoard.Id; // GetRoleId("urn:altinn:external-role:ccr:styreleder", "styreleder");
        /*INNH*/
        var roleInnh = RoleConstants.Innehaver.Id; // GetRoleId("urn:altinn:external-role:ccr:innehaver", "innehaver");
        /*DTSO*/
        var roleDtso = RoleConstants.ParticipantFullResponsibility.Id; // GetRoleId("urn:altinn:external-role:ccr:deltaker-fullt-ansvar", "deltaker-fullt-ansvar");
        /*DTPR*/
        var roleDtpr = RoleConstants.ParticipantSharedResponsibility.Id; // GetRoleId("urn:altinn:external-role:ccr:deltaker-delt-ansvar", "deltaker-delt-ansvar");
        /*KOMP*/
        var roleKomp = RoleConstants.GeneralPartner.Id; // GetRoleId("urn:altinn:external-role:ccr:komplementar", "komplementar");
        /*BEST*/
        var roleBest = RoleConstants.ManagingShipowner.Id; // GetRoleId("urn:altinn:external-role:ccr:bestyrende-reder", "bestyrende-reder");
        /*BOBE*/
        var roleBobe = RoleConstants.EstateAdministrator.Id; // GetRoleId("urn:altinn:external-role:ccr:bostyrer", "bostyrer");
        /*REGN*/
        var roleRegn = RoleConstants.Accountant.Id; // GetRoleId("urn:altinn:external-role:ccr:regnskapsforer", "regnskapsforer");
        /*REVI*/
        var roleRevi = RoleConstants.Auditor.Id; // GetRoleId("urn:altinn:external-role:ccr:revisor", "revisor");
        /*KNUF*/
        var roleKnuf = RoleConstants.ContactPersonNUF.Id; // GetRoleId("urn:altinn:external-role:ccr:kontaktperson-nuf", "kontaktperson-nuf");
        /*FFØR*/
        var roleFfor = RoleConstants.BusinessManager.Id; // GetRoleId("urn:altinn:external-role:ccr:forretningsforer", "forretningsforer");
        /*KEMN*/
        var roleKemn = RoleConstants.ContactPersonInAdministrativeUnit.Id; // GetRoleId("urn:altinn:external-role:ccr:kontaktperson-ados", "kontaktperson-ados");
        /*PRIV*/
        var rolePriv = RoleConstants.PrivatePerson.Id; // GetRoleId("urn:altinn:role:privatperson", "privatperson");
        /*KOMK*/
        var roleKomk = RoleConstants.ContactPersonInMunicipality.Id; // GetRoleId("urn:altinn:external-role:ccr:kontaktperson-kommune", "kontaktperson-kommune");
        /*KONT*/
        var roleKont = RoleConstants.ContactPerson.Id; // GetRoleId("urn:altinn:external-role:ccr:kontaktperson", "kontaktperson");
        /*MEDL*/
        var roleMedl = RoleConstants.BoardMember.Id; // GetRoleId("urn:altinn:external-role:ccr:styremedlem", "styremedlem");
        /*MVAG*/
        var roleMvag = RoleConstants.VatFormSigner.Id; // GetRoleId("urn:altinn:external-role:ccr:mva-signerer", "mva-signerer");
        /*MVAU*/
        var roleMvau = RoleConstants.VatFormCompleter.Id; // GetRoleId("urn:altinn:external-role:ccr:mva-utfyller", "mva-utfyller");
        /*NEST*/
        var roleNest = RoleConstants.DeputyLeader.Id; // GetRoleId("urn:altinn:external-role:ccr:nestleder", "nestleder");
        /*REPR*/
        var roleRepr = RoleConstants.NorwegianRepresentativeForeignEntity.Id; // GetRoleId("urn:altinn:external-role:ccr:norsk-representant", "norsk-representant");
        /*SAM*/
        var roleSam = RoleConstants.CoOwners.Id; // GetRoleId("urn:altinn:external-role:ccr:sameier", "sameier");
        /*SENS*/
        var roleSens = RoleConstants.Sens.Id; // GetRoleId("urn:altinn:rolecode:sens", "Sensitive-tjenester");
        /*SREVA*/
        var roleSreva = RoleConstants.RegisteredAuditor.Id; // GetRoleId("urn:altinn:external-role:ccr:kontaktperson-revisor", "kontaktperson-revisor");
        /*A0212*/
        var roleA0212 = RoleConstants.PrimaryIndustryAndFoodstuff.Id; // GetRoleId("urn:altinn:rolecode:A0212", "A0212");
        /*A0236*/
        var roleA0236 = RoleConstants.MailArchive.Id; // GetRoleId("urn:altinn:rolecode:A0236", "A0236");
        /*A0237*/
        var roleA0237 = RoleConstants.A0237.Id; // GetRoleId("urn:altinn:rolecode:A0237", "A0237");
        /*A0238*/
        var roleA0238 = RoleConstants.AssistantAuditor.Id; // GetRoleId("urn:altinn:rolecode:A0238", "A0238");
        /*A0239*/
        var roleA0239 = RoleConstants.AccountantWithSigningRights.Id; // GetRoleId("urn:altinn:rolecode:A0239", "A0239");
        /*A0240*/
        var roleA0240 = RoleConstants.AccountantWithoutSigningRights.Id; // GetRoleId("urn:altinn:rolecode:A0240", "A0240");
        /*A0241*/
        var roleA0241 = RoleConstants.AccountantSalary.Id; // GetRoleId("urn:altinn:rolecode:A0241", "A0241");
        /*A0278*/
        var roleA0278 = RoleConstants.PlanningAndConstruction.Id; // GetRoleId("urn:altinn:rolecode:A0278", "A0278");
        /*A0282*/
        var roleA0282 = RoleConstants.A0282.Id; // GetRoleId("urn:altinn:rolecode:A0282", "A0282");
        /*A0286*/
        var roleA0286 = RoleConstants.A0286.Id; // GetRoleId("urn:altinn:rolecode:A0286", "A0286");
        /*A0293*/
        var roleA0293 = RoleConstants.A0293.Id; // GetRoleId("urn:altinn:rolecode:A0293", "A0293");
        /*A0294*/
        var roleA0294 = RoleConstants.A0294.Id; // GetRoleId("urn:altinn:rolecode:A0294", "A0294");
        /*A0298*/
        var roleA0298 = RoleConstants.A0298.Id; // GetRoleId("urn:altinn:rolecode:A0298", "A0298");
        /*ADMAI*/
        var roleADMAI = RoleConstants.AccessManager.Id; // GetRoleId("urn:altinn:rolecode:ADMAI", "ADMAI");
        /*APIADM*/
        var roleAPIADM = RoleConstants.APIADM.Id; // GetRoleId("urn:altinn:rolecode:APIADM", "APIADM");
        /*APIADMNUF*/
        var roleAPIADMNUF = RoleConstants.APIADMNUF.Id; // GetRoleId("urn:altinn:rolecode:APIADMNUF", "APIADMNUF");
        /*ATTST*/
        var roleATTST = RoleConstants.Attst.Id; // GetRoleId("urn:altinn:rolecode:ATTST", "ATTST");
        /*BOADM*/
        var roleBOADM = RoleConstants.Boadm.Id; // GetRoleId("urn:altinn:rolecode:BOADM", "BOADM");
        /*BOBEL*/
        var roleBOBEL = RoleConstants.Bobel.Id; // GetRoleId("urn:altinn:rolecode:BOBEL", "BOBEL");
        /*BOBES*/
        var roleBOBES = RoleConstants.Bobes.Id; // GetRoleId("urn:altinn:rolecode:BOBES", "BOBES");
        /*ECKEYROLE*/
        var roleECKEYROLE = RoleConstants.Eckeyrole.Id; // GetRoleId("urn:altinn:rolecode:ECKEYROLE", "ECKEYROLE");
        /*EKTJ*/
        var roleEKTJ = RoleConstants.Ektj.Id; // GetRoleId("urn:altinn:rolecode:EKTJ", "EKTJ");
        /*HADM*/
        var roleHADM = RoleConstants.Hadm.Id; // GetRoleId("urn:altinn:rolecode:HADM", "HADM");
        /*HVASK*/
        var roleHVASK = RoleConstants.Hvask.Id; // GetRoleId("urn:altinn:rolecode:HVASK", "HVASK");
        /*KLADM*/
        var roleKLADM = RoleConstants.Kladm.Id; // GetRoleId("urn:altinn:rolecode:KLADM", "KLADM");
        /*KOMAB*/
        var roleKOMAB = RoleConstants.Komab.Id; // GetRoleId("urn:altinn:rolecode:KOMAB", "KOMAB");
        /*LOPER*/
        var roleLOPER = RoleConstants.Loper.Id; // GetRoleId("urn:altinn:rolecode:LOPER", "LOPER");
        /*PASIG*/
        var rolePASIG = RoleConstants.Pasig.Id; // GetRoleId("urn:altinn:rolecode:PASIG", "PASIG");
        /*PAVAD*/
        var rolePAVAD = RoleConstants.Pavad.Id; // GetRoleId("urn:altinn:rolecode:PAVAD", "PAVAD");
        /*PRIUT*/
        var rolePRIUT = RoleConstants.Priut.Id; // GetRoleId("urn:altinn:rolecode:PRIUT", "PRIUT");
        /*REGNA*/
        var roleREGNA = RoleConstants.Regna.Id; // GetRoleId("urn:altinn:rolecode:REGNA", "REGNA");
        /*SIGNE*/
        var roleSIGNE = RoleConstants.Signe.Id; // GetRoleId("urn:altinn:rolecode:SIGNE", "SIGNE");
        /*SISKD*/
        var roleSISKD = RoleConstants.Siskd.Id; // GetRoleId("urn:altinn:rolecode:SISKD", "SISKD");
        /*UIHTL*/
        var roleUIHTL = RoleConstants.Uihtl.Id; // GetRoleId("urn:altinn:rolecode:UIHTL", "UIHTL");
        /*UILUF*/
        var roleUILUF = RoleConstants.Uiluf.Id; // GetRoleId("urn:altinn:rolecode:UILUF", "UILUF");
        /*UTINN*/
        var roleUTINN = RoleConstants.Utinn.Id; // GetRoleId("urn:altinn:rolecode:UTINN", "UTINN");
        /*UTOMR*/
        var roleUTOMR = RoleConstants.Utomr.Id; // GetRoleId("urn:altinn:rolecode:UTOMR", "UTOMR");

        var roleMaps = new List<RoleMap>()
        {
            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0212 },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleA0212 },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0236 },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleA0236 },

            new RoleMap() { HasRoleId = roleRevi, GetRoleId = roleA0237 },

            new RoleMap() { HasRoleId = roleRevi, GetRoleId = roleA0238 },

            new RoleMap() { HasRoleId = roleRegn, GetRoleId = roleA0239 },

            new RoleMap() { HasRoleId = roleRegn, GetRoleId = roleA0240 },

            new RoleMap() { HasRoleId = roleRegn, GetRoleId = roleA0241 },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0278 },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleA0278 },

            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleA0282 },

            new RoleMap() { HasRoleId = roleSens, GetRoleId = roleA0286 },

            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0293 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0293 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0293 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0293 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0293 },

            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0294 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0294 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0294 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0294 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0294 },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleA0298 },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleA0298 },

            new RoleMap() { HasRoleId = roleBest,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleBobe,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleDagl,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleDtpr,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleDtso,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleFfor,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleInnh,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleKemn,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleKnuf,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleKomk,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleKomp,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleLede,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = rolePriv,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleRepr,  GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleSam,   GetRoleId = roleADMAI },
            new RoleMap() { HasRoleId = roleSreva, GetRoleId = roleADMAI },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleAPIADM },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleAPIADM },

            new RoleMap() { HasRoleId = roleKnuf, GetRoleId = roleAPIADMNUF },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleMvag, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleATTST },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleATTST },

            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleBOADM },

            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleBOBEL },

            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleBOBES },

            new RoleMap() { HasRoleId = roleBest,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleBobe,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleDagl,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleDtpr,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleDtso,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleInnh,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleKemn,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleKnuf,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleKomp,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleLede,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleRepr,  GetRoleId = roleECKEYROLE },
            new RoleMap() { HasRoleId = roleSreva, GetRoleId = roleECKEYROLE },

            new RoleMap() { HasRoleId = roleSens, GetRoleId = roleEKTJ },
            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleHADM },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleHADM },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleHADM },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleHADM },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleHADM },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleHADM },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleHVASK },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleHVASK },

            new RoleMap() { HasRoleId = roleBest,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleBobe,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleDagl,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleDtpr,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleDtso,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleInnh,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleKomp,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleLede,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleRepr,  GetRoleId = roleKLADM },
            new RoleMap() { HasRoleId = roleSreva, GetRoleId = roleKLADM },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleKOMAB },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleKOMAB },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleMvag, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleLOPER },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleLOPER },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleKnuf, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = rolePASIG },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = rolePASIG },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = rolePAVAD },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = rolePAVAD },

            new RoleMap() { HasRoleId = rolePriv, GetRoleId = rolePRIUT },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleMedl, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleMvag, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleMvau, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleREGNA },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleREGNA },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleSIGNE },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleSIGNE },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleMvag, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleSISKD },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleSISKD },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleUIHTL },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleUIHTL },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleUILUF },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleUILUF },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleKemn, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleKont, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleMedl, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleUTINN },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleUTINN },

            new RoleMap() { HasRoleId = roleBest, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleBobe, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleDagl, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleDtpr, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleDtso, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleFfor, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleInnh, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleKomk, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleKomp, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleLede, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleMedl, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleNest, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = rolePriv, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleRepr, GetRoleId = roleUTOMR },
            new RoleMap() { HasRoleId = roleSam,  GetRoleId = roleUTOMR }
        };

        // Upsert RoleMap data
        foreach (var rm in roleMaps)
        {
            var existing = await dbContext.RoleMaps
                .FirstOrDefaultAsync(x => x.HasRoleId == rm.HasRoleId && x.GetRoleId == rm.GetRoleId, cancellationToken);

            if (existing == null)
            {
                dbContext.RoleMaps.Add(rm);
            }
        }

        await dbContext.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
