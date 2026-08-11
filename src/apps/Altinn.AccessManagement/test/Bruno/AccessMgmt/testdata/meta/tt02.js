module.exports = {
  env: "tt02",
  packages: {
    skattegrunnlag: {
      id: "4c859601-9b2b-4662-af39-846f4117ad7a",
      urn: "urn:altinn:accesspackage:skattegrunnlag",
      name: "Skattegrunnlag",
    },
    kunstOgUnderholdning: {
      id: "75fa5863-3368-4ac6-9a4b-48f595e483ad",
      urn: "urn:altinn:accesspackage:kunst-og-underholdning",
      name: "Kunst og underholdning",
      nnName: "Kunst og underhaldning",
      enName: "Arts and entertainment",
    },
    skattNaering: {
      id: "1dba50d6-f604-48e9-bd41-82321b13e85c",
      urn: "urn:altinn:accesspackage:skatt-naering",
      name: "Skatt næring",
      resourceProviderCode: "ttd",
    },
    regnskapsforerMedSignering: {
      id: "955d5779-3e2b-4098-b11d-0431dc41ddbe",
      urn: "urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet",
      name: "Regnskapsfører med signeringsrettighet",
    },
    revisorattesterer: {
      id: "1886712b-e077-445a-ab3f-8c8bdebccc67",
      urn: "urn:altinn:accesspackage:revisorattesterer",
      name: "Revisorattesterer",
      resourceName: "Bruno automatisert test ressurs for direktedelegering",
      resourceRefId: "devtest_gar_bruno_direct_resource",
    },
    infrastruktur: {
      id: "75978efe-2437-421e-8c77-dd61925c7ba4",
      urn: "urn:altinn:accesspackage:infrastruktur",
      name: "Infrastruktur",
    },
  },
  areas: {
    kulturOgFrivillighet: {
      id: "5996ba37-6db0-4391-8918-b1b0bd4b394b",
      name: "Kultur og frivillighet",
    },
  },
  organizationSubtypes: {
    utbg: {
      id: "99a54a28-52d3-4608-9298-94081bb3f3d2",
      name: "UTBG",
    },
    rev: {
      id: "2d9371de-576b-42fc-94b9-7e80b2467982",
      name: "REV",
    },
  },
  groups: {
    bransje: {
      id: "3757643a-316d-4d0e-a52b-4dc7cdebc0b4",
      name: "Bransje",
    },
  },
  roles: {
    deltakerDeltAnsvar: {
      id: "18baa914-ac43-4663-9fa4-6f5760dc68eb",
      code: "deltaker-delt-ansvar",
      urn: "urn:altinn:external-role:ccr:deltaker-delt-ansvar",
      name: "Deltaker delt ansvar",
      nnName: "Deltakar delt ansvar",
      enName: "Participant with Shared Responsibility",
      legacyRoleCode: "dtpr",
      isKeyRole: true,
      providerCode: "sys-ccr",
      providerName: "Enhetsregisteret",
    },
    dagligLeder: {
      id: "55bd7d4d-08dd-46ee-ac8e-3a44d800d752",
      code: "daglig-leder",
      variant: "AS",
      packageWithResources: {
        id: "0195efb8-7c80-7642-b9b8-c748ee4fecd4",
        name: "Tinglysing eiendom",
      },
    },
    revisor: {
      id: "f76b997a-9bd8-4f7b-899f-fcd85d35669f",
      code: "revisor",
      variant: "ENK",
      packageNames: ["Ansvarlig revisor", "Revisormedarbeider"],
    },
  },
};
