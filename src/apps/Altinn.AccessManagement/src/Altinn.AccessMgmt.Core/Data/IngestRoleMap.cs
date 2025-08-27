using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest RoleMap data
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestRoleMap(CancellationToken cancellationToken = default)
    {
        var roles = (await db.Roles.ToListAsync()).ToDictionary(t => t.Urn, t => t.Id);

        Guid GetRoleId(string urn, string name) =>
            roles.TryGetValue(urn, out var id)
                ? id
                : throw new KeyNotFoundException(string.Format("Role not found '{0}'", name));

        /*DAGL*/
        var roleDagl = GetRoleId("urn:altinn:external-role:ccr:daglig-leder", "daglig-leder");
        /*LEDE*/
        var roleLede = GetRoleId("urn:altinn:external-role:ccr:styreleder", "styreleder");
        /*INNH*/
        var roleInnh = GetRoleId("urn:altinn:external-role:ccr:innehaver", "innehaver");
        /*DTSO*/
        var roleDtso = GetRoleId("urn:altinn:external-role:ccr:deltaker-fullt-ansvar", "deltaker-fullt-ansvar");
        /*DTPR*/
        var roleDtpr = GetRoleId("urn:altinn:external-role:ccr:deltaker-delt-ansvar", "deltaker-delt-ansvar");
        /*KOMP*/
        var roleKomp = GetRoleId("urn:altinn:external-role:ccr:komplementar", "komplementar");
        /*BEST*/
        var roleBest = GetRoleId("urn:altinn:external-role:ccr:bestyrende-reder", "bestyrende-reder");
        /*BOBE*/
        var roleBobe = GetRoleId("urn:altinn:external-role:ccr:bostyrer", "bostyrer");
        /*REGN*/
        var roleRegn = GetRoleId("urn:altinn:external-role:ccr:regnskapsforer", "regnskapsforer");
        /*REVI*/
        var roleRevi = GetRoleId("urn:altinn:external-role:ccr:revisor", "revisor");
        /*KNUF*/
        var roleKnuf = GetRoleId("urn:altinn:external-role:ccr:kontaktperson-nuf", "kontaktperson-nuf");
        /*FFØR*/
        var roleFfor = GetRoleId("urn:altinn:external-role:ccr:forretningsforer", "forretningsforer");
        /*KEMN*/
        var roleKemn = GetRoleId("urn:altinn:external-role:ccr:kontaktperson-ados", "kontaktperson-ados");
        /*PRIV*/
        var rolePriv = GetRoleId("urn:altinn:role:privatperson", "privatperson");
        /*KOMK*/
        var roleKomk = GetRoleId("urn:altinn:external-role:ccr:kontaktperson-kommune", "kontaktperson-kommune");
        /*KONT*/
        var roleKont = GetRoleId("urn:altinn:external-role:ccr:kontaktperson", "kontaktperson");
        /*MEDL*/
        var roleMedl = GetRoleId("urn:altinn:external-role:ccr:styremedlem", "styremedlem");
        /*MVAG*/
        var roleMvag = GetRoleId("urn:altinn:external-role:ccr:mva-signerer", "mva-signerer");
        /*MVAU*/
        var roleMvau = GetRoleId("urn:altinn:external-role:ccr:mva-utfyller", "mva-utfyller");
        /*NEST*/
        var roleNest = GetRoleId("urn:altinn:external-role:ccr:nestleder", "nestleder");
        /*REPR*/
        var roleRepr = GetRoleId("urn:altinn:external-role:ccr:norsk-representant", "norsk-representant");
        /*SAM*/
        var roleSam = GetRoleId("urn:altinn:external-role:ccr:sameier", "sameier");
        /*SENS*/
        var roleSens = GetRoleId("urn:altinn:rolecode:SENS", "Sensitive-tjenester");
        /*SREVA*/
        var roleSreva = GetRoleId("urn:altinn:external-role:ccr:kontaktperson-revisor", "kontaktperson-revisor");

        /*A0212*/
        var roleA0212 = GetRoleId("urn:altinn:rolecode:A0212", "A0212");
        /*A0236*/
        var roleA0236 = GetRoleId("urn:altinn:rolecode:A0236", "A0236");
        /*A0237*/
        var roleA0237 = GetRoleId("urn:altinn:rolecode:A0237", "A0237");
        /*A0238*/
        var roleA0238 = GetRoleId("urn:altinn:rolecode:A0238", "A0238");
        /*A0239*/
        var roleA0239 = GetRoleId("urn:altinn:rolecode:A0239", "A0239");
        /*A0240*/
        var roleA0240 = GetRoleId("urn:altinn:rolecode:A0240", "A0240");
        /*A0241*/
        var roleA0241 = GetRoleId("urn:altinn:rolecode:A0241", "A0241");
        /*A0278*/
        var roleA0278 = GetRoleId("urn:altinn:rolecode:A0278", "A0278");
        /*A0282*/
        var roleA0282 = GetRoleId("urn:altinn:rolecode:A0282", "A0282");
        /*A0286*/
        var roleA0286 = GetRoleId("urn:altinn:rolecode:A0286", "A0286");
        /*A0293*/
        var roleA0293 = GetRoleId("urn:altinn:rolecode:A0293", "A0293");
        /*A0294*/
        var roleA0294 = GetRoleId("urn:altinn:rolecode:A0294", "A0294");
        /*A0298*/
        var roleA0298 = GetRoleId("urn:altinn:rolecode:A0298", "A0298");
        /*ADMAI*/
        var roleADMAI = GetRoleId("urn:altinn:rolecode:ADMAI", "ADMAI");
        /*APIADM*/
        var roleAPIADM = GetRoleId("urn:altinn:rolecode:APIADM", "APIADM");
        /*APIADMNUF*/
        var roleAPIADMNUF = GetRoleId("urn:altinn:rolecode:APIADMNUF", "APIADMNUF");
        /*ATTST*/
        var roleATTST = GetRoleId("urn:altinn:rolecode:ATTST", "ATTST");
        /*BOADM*/
        var roleBOADM = GetRoleId("urn:altinn:rolecode:BOADM", "BOADM");
        /*BOBEL*/
        var roleBOBEL = GetRoleId("urn:altinn:rolecode:BOBEL", "BOBEL");
        /*BOBES*/
        var roleBOBES = GetRoleId("urn:altinn:rolecode:BOBES", "BOBES");
        /*ECKEYROLE*/
        var roleECKEYROLE = GetRoleId("urn:altinn:rolecode:ECKEYROLE", "ECKEYROLE");
        /*EKTJ*/
        var roleEKTJ = GetRoleId("urn:altinn:rolecode:EKTJ", "EKTJ");
        /*HADM*/
        var roleHADM = GetRoleId("urn:altinn:rolecode:HADM", "HADM");
        /*HVASK*/
        var roleHVASK = GetRoleId("urn:altinn:rolecode:HVASK", "HVASK");
        /*KLADM*/
        var roleKLADM = GetRoleId("urn:altinn:rolecode:KLADM", "KLADM");
        /*KOMAB*/
        var roleKOMAB = GetRoleId("urn:altinn:rolecode:KOMAB", "KOMAB");
        /*LOPER*/
        var roleLOPER = GetRoleId("urn:altinn:rolecode:LOPER", "LOPER");
        /*PASIG*/
        var rolePASIG = GetRoleId("urn:altinn:rolecode:PASIG", "PASIG");
        /*PAVAD*/
        var rolePAVAD = GetRoleId("urn:altinn:rolecode:PAVAD", "PAVAD");
        /*PRIUT*/
        var rolePRIUT = GetRoleId("urn:altinn:rolecode:PRIUT", "PRIUT");
        /*REGNA*/
        var roleREGNA = GetRoleId("urn:altinn:rolecode:REGNA", "REGNA");
        /*SIGNE*/
        var roleSIGNE = GetRoleId("urn:altinn:rolecode:SIGNE", "SIGNE");
        /*SISKD*/
        var roleSISKD = GetRoleId("urn:altinn:rolecode:SISKD", "SISKD");
        /*UIHTL*/
        var roleUIHTL = GetRoleId("urn:altinn:rolecode:UIHTL", "UIHTL");
        /*UILUF*/
        var roleUILUF = GetRoleId("urn:altinn:rolecode:UILUF", "UILUF");
        /*UTINN*/
        var roleUTINN = GetRoleId("urn:altinn:rolecode:UTINN", "UTINN");
        /*UTOMR*/
        var roleUTOMR = GetRoleId("urn:altinn:rolecode:UTOMR", "UTOMR");

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
            var existing = await db.RoleMaps
                .FirstOrDefaultAsync(x => x.HasRoleId == rm.HasRoleId && x.GetRoleId == rm.GetRoleId, cancellationToken);

            if (existing == null)
            {
                db.RoleMaps.Add(rm);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
