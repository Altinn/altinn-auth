module.exports = {
  env: "tt02",
  resources: {
    clientDelgResourceId: "devtest_gar_bruno_client_resource",
  },
  REGN_Organisasjon: {
    name: "MOTSTANDSDYKTIG VASSEN TIGER AS",
    orgno: "314243994",
    partyId: 51450558,
    partyUuid: "4a06214d-b261-4695-b33a-0771a995b503",
    dagligleder: {
      name: "FORSTÅELSESFULL BYGD",
      pid: "08885399984",
      userId: 1260388,
      partyId: 51395522,
      partyUuid: "1b490ad0-778b-42d7-a6a6-30b9ea0d726c",
    },
    REGN_Org_Clients: {
      client_A: {
        name: "AKADEMISK RU TIGER AS",
        orgno: "213368532",
        partyId: 51489644,
        partyUuid: "71b12257-2311-492e-802d-b5a9cff96708",
        dagligleder: {
          name: "HALV BEHANDLING",
          pid: "02826899428",
          userId: 2469326,
          partyId: 51180221,
          partyUuid: "e546521c-2a13-4e0e-a691-c77eb3c81337",
        },
      },
      client_B: {
        name: "ALFABETISK INITIATIVRIK TIGER AS",
        orgno: "313360210",
        partyId: 51840240,
        partyUuid: "8758a530-988e-4a42-8fea-b75b67f91ed5",
        dagligleder: {
          name: "DJERV ABONNEMENT",
          pid: "20824699322",
          userId: 2408994,
          partyId: 51212734,
          partyUuid: "d791a1a3-94a5-46ba-9bdb-f99e2da5409b",
        },
      },
    },
    // HYBELKANIN INNSIKTSFULL, alive in the register. The previous agent, GRANITT KREATIV,
    // is registered as deceased and lives on as deceasedPerson for the negative tests.
    REGN_Agent: {
      name: "INNSIKTSFULL HYBELKANIN",
      etternavn: "HYBELKANIN",
      personidentity: "14906198453",
      userId: 1490338,
      partyId: 50951114,
      userUuid: "0791979b-b0ee-4b50-bd23-16a964da925c",
    },
  },
  // GRANITT KREATIV, registered as deceased (2020-12-22). Used only to verify that
  // agent registration and rightholder registration reject a deceased person.
  deceasedPerson: {
    name: "KREATIV GRANITT",
    etternavn: "GRANITT",
    personidentity: "08919574934",
    userId: 1465828,
    partyId: 50441038,
    userUuid: "01f7a70d-2619-4c50-8ff4-efd7ae6c8960",
  },
  REVI_Organisasjon: {
    name: "OVERFLADISK LANG TIGER AS",
    orgno: "310267511",
    partyId: 51449365,
    partyUuid: "03b14d35-4b8c-44cd-9c71-dc740d8585c2",
    dagligleder: {
      name: "ROBUST SERVIETT",
      pid: "25848395245",
      userId: 339925,
      partyId: 50743388,
      partyUuid: "998b0ee8-57d6-42c3-a36c-a775bbf93210",
    },
  },
  innbygger_package_to_delegate: {
      package_urn: "urn:altinn:accesspackage:innbygger-samliv",
    },
    privatPerson: {
      name: "TRIST PAPPA",
      pid: "18834599313",
      userId: 2184955,
      partyId: 51425503,
      partyUuid: "a4c0369b-2261-4123-ac03-e0028a64d265",
    },
    // Fixtures for the v2 client delegation scenarios in test/EnduserAPI/ClientDelegationsV2.
    clientDelegationV2: {
      // Single resource whose policy lets a dagligleder delegate it onwards.
      singleResource: "tilgangspakke_delegering_ressurs",
      packages: {
        regnskapsforerLonn: "urn:altinn:accesspackage:regnskapsforer-lonn",
        regnskapsforerMedSignering: "urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet",
        ansvarligRevisor: "urn:altinn:accesspackage:ansvarlig-revisor",
        klientadministrator: "urn:altinn:accesspackage:klientadministrator",
        skattegrunnlag: "urn:altinn:accesspackage:skattegrunnlag",
        forretningsforerEiendom: "urn:altinn:accesspackage:forretningsforer-eiendom",
        tjenesterNuf: "urn:altinn:accesspackage:tjenester-nuf",
        fforTilgangsstyrerNufNotDelegable: "urn:altinn:accesspackage:ffor-tilgangsstyrer-nuf",
      },
      // Facilitator with forretningsforer relations to NUF clients.
      forretningsforerNuf: {
        facilitator: {
          name: "OPPBLÅST UNG MINK ANS",
          orgno: "314240200",
          partyUuid: "47a62cca-4840-438f-be18-26bd2aea29a7",
          dagligleder: {
            name: "ANSTENDIG ARTERIE",
            pid: "15847099396",
            userId: 160682,
            partyId: 51209497,
            partyUuid: "b8b84060-cb50-42f2-8b3d-39abfc76616a",
          },
        },
        nufClient: {
          name: "SAKTE FRISK STRUTS AB",
          orgno: "311762966",
          partyUuid: "f407a32c-a33e-4d94-b6ce-58b58b9c563f",
        },
      },
      // Forretningsforer client of the systemuser-clientdelegation facilitator whose
      // unit type (BBL) is outside the ESEK/BRL scope of forretningsforer-eiendom.
      forretningsforerBblClient: {
        name: "USELVSTENDIG FLAT PUMA BBL",
        orgno: "210815872",
        partyUuid: "bd95521a-17e9-4817-8207-735ef015bf53",
      },
      unknown: {
        partyUuid: "11111111-1111-1111-1111-111111111111",
        packageUrn: "urn:altinn:accesspackage:bruno-finnes-ikke",
        resourceRefId: "bruno-finnes-ikke",
        roleCode: "bruno-finnes-ikke",
      },
    },
};
