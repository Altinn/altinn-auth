module.exports = {
  env: "at22",
  resource_to_delegate: {
    resource_id_privatperson:
      "ttd-test-bruno-enkelttjeneste-privatperson-ressurs",
    resource_id_virksomhet:
      "ttd-test-bruno-enkelttjenestedelegering-virksomhet",
    app_id: "app_ttd_test-bruno-app-for-delegation",
    skattetaten_ressurs: "ske-informasjon-om-trekkpaalegg",
    maskinporten_ressurs : "ttd-bruno-maskinporten-ressurs"
  },
  package_to_delegate: {
    package_id_privatperson: "urn:altinn:accesspackage:innbygger-samliv",
    package_id_privatperson_withdraw:
      "urn:altinn:accesspackage:innbygger-vapen",
    package_id_privatperson_reject:
      "urn:altinn:accesspackage:innbygger-byggesoknad",
    package_id_virksomhet: "urn:altinn:accesspackage:posttjenester",
    hadm: "urn:altinn:accesspackage:hovedadministrator",
    regn_not_assignable:
      "urn:altinn:accesspackage:regnskapsforer-med-signeringsrettighet",
    eksplisitt: "urn:altinn:accesspackage:eksplisitt",
    konkbotilg: "urn:altinn:accesspackage:konkursbo-tilgangsstyrer",
  },
  Bot_person: {
    lastname: "INGREDIENS",
    pid: "21866699620",
    partyid: 50911351,
    userid: 2055277,
    partyuuid: "877a433e-0c99-4b03-a75b-76ff364796d0",
  },
  Bot_from_person: {
    lastname: "KARIES",
    pid: "06920848050",
    partyid: 51356827,
    userid: 1918480,
    partyuuid: "68a6555a-1a8b-4067-bd3f-1fccceeba161",
  },
  Bot_Org_for_serviceowner: {
    name: "STERK DYP TIGER AS",
    org_no: "313025098",
    partyid: 51816948,
    partyuuid: "722e5c50-8483-423a-a050-c7d7bcd88dfd",
    dagligleder: {
      name: "Materialistisk Bygg",
      pid: "22816399142",
      partyid: 51437759,
      userid: 2264193,
      partyuuid: "b6c4a1c5-dec6-4ebb-9629-04ca045dfaca",
    },
  },
  Bot_person_serviceowner: {
    lastname: "STAMMOR",
    pid: "08826499630",
    partyid: 51207503,
    userid: 2417132,
    partyuuid: "d970396b-0cf9-49e8-8ee1-e2f07aa74ec5",
  },

  Bot_Org: {
    name: "PASSIV HANDLEKRAFTIG PUMA",
    org_no: "313657892",
    partyid: 51860826,
    partyuuid: "fa3c7224-3d9f-44f8-ab7a-59f82fb18ea1",
    innehaver: {
      name: "FROM ANDAKT",
      pid: "04907397659",
      partyid: 50773665,
      userid: 2439278,
      partyuuid: "de72e5f0-7c3c-46bc-8c8c-4ab0d25ea0a0",
    },
  },
   
  Bot_Org_SubUnits: {
    name: "FINTFØLENDE RASK PIGGSVIN",
    org_no: "210008802",
    partyid: 51448738,
    partyuuid: "983a844d-1fee-463e-b52e-95c6c030ee6d",
    dagligleder: {
      name: "FANTASTISK PIANOKRAKK",
      pid: "11896498713",
      partyid: 50965193,
      userid: 159893,
      partyuuid: "01668787-fba5-401e-a2a7-e084a4a2cfb5",
    },
    underenhet01: {
      name: "BLØT INITIATIVRIK FJELLREV",
      org_no: "211942932",
      partyid: 51473756,
      partyuuid: "252fd6e8-79f1-4c83-8d39-acd42b2be792"
    },
    underenhet02: {
      name: "NORMAL FAST FJELLREV",
      org_no: "211943092",
      partyid: 51473755,
      partyuuid: "1ca0274f-6f4b-4c15-864d-87842625543d"
    },
    underenhet03: {
      name: "URETTFERDIG SPETTETE FJELLREV",
      org_no: "311942921",
      partyid: 51722792,
      partyuuid: "c80d61e9-6b04-461d-b02c-c5b04f5f7750"
    },
    underenhet04: {
      name: "VOKAL PRESIS FJELLREV",
      org_no: "311942948",
      partyid: 51722795,
      partyuuid: "f69698b7-5a69-4332-a9cb-6827293d2b04"
    },
    underenhet05: {
      name: "FORETAKSOM KURSIV FJELLREV",
      org_no: "311942956",
      partyid: 51722796,
      partyuuid: "d62104cb-84e3-4a0c-aa00-cd4cc00516e8"
    },
    underenhet06: {
      name: "ANONYM ALVORLIG FJELLREV",
      org_no: "311942964",
      partyid: 51722797,
      partyuuid: "dc4673a7-d740-4eec-97c8-fab4a64146f3"
    },
    underenhet07: {
      name: "GLEMSOM EGOISTISK FJELLREV",
      org_no: "311942972",
      partyid: 51722798,
      partyuuid: "6ec145c8-1456-44ed-be90-b896baa95d70"
    },
    underenhet08: {
      name: "RASTLØS STILLE FJELLREV",
      org_no: "311942980",
      partyid: 51722800,
      partyuuid: "a881218d-75b6-4fa6-983f-bc6fc4bfe572"
    },
    underenhet09: {
      name: "BERIKENDE URIMELIG FJELLREV",
      org_no: "311942999",
      partyid: 51722799,
      partyuuid: "e5a95318-5287-485b-b53f-97cd82da3355"
    },
    underenhet10: {
      name: "SLITEN STRIDLYNT FJELLREV",
      org_no: "311943006",
      partyid: 51722801,
      partyuuid: "14eb214c-6aa5-4327-a428-fe0cc4d742f7"
    },
    underenhet11: {
      name: "UMAKE SKRAVLETE FJELLREV",
      org_no: "311943014",
      partyid: 51722802,
      partyuuid: "160ecf8f-b7bf-4f32-9049-563ff87220d6"
    },
    underenhet12: {
      name: "NETT SMIGRENDE FJELLREV",
      org_no: "311943022",
      partyid: 51722803,
      partyuuid: "414a7055-c059-4edc-8319-c855e772baf5"
    },
    underenhet13: {
      name: "BERØMT INNESLUTTET FJELLREV",
      org_no: "311943030",
      partyid: 51722804,
      partyuuid: "a657da6e-63eb-4aff-ad4c-dd5524d78a4e"
    },
    underenhet14: {
      name: "PLEIENDE UFORNUFTIG FJELLREV",
      org_no: "311943049",
      partyid: 51722805,
      partyuuid: "26438971-c05a-4249-a931-05ef7d1efdc9"
    },
    underenhet15: {
      name: "GILD NYTTIG FJELLREV",
      org_no: "311943057",
      partyid: 51722806,
      partyuuid: "f20c50d7-821a-4077-b7a2-db9bd029dc07"
    },
    underenhet16: {
      name: "BESKJEDEN UKJENT FJELLREV",
      org_no: "311943065",
      partyid: 51722807,
      partyuuid: "07a63925-4cbb-4772-916e-4e5ec687df89"
    },
    underenhet17: {
      name: "STADIG UKJENT FJELLREV",
      org_no: "311943073",
      partyid: 51722808,
      partyuuid: "04d3defc-5430-460d-abde-68ac47ed6178"
    },
    underenhet18: {
      name: "RUSTEN UAVHENGIG FJELLREV",
      org_no: "311943081",
      partyid: 51722809,
      partyuuid: "b565af9f-e453-4b74-95c0-77a3388b432c"
    },
    underenhet19: {
      name: "OPPBLÅST TYKKHUDET FJELLREV",
      org_no: "311943103",
      partyid: 51722810,
      partyuuid: "0e47ed1d-6dfc-49da-8048-26a8fb199031"
    },
  },

  Bot_Org_2: {
    name: "RIMELIG FAST HEST BORETTSLAG",
    org_no: "310413089",
    partyid: 51561311,
    partyuuid: "1a93a7b3-e3de-416b-bc43-4a4e9640327e",
    styreleder: {
      name: "Lysegul Femkant",
      pid: "14838799907",
      partyid: 50971139,
      userid: 1286384,
      partyuuid: "02f2032f-60a0-49d0-ab29-8bfa6494cb35",
    },
  },
  user_uten_rettighetshaver: {
    lastname: "SJOKOLADEKAKE",
    pid: "63881275025",
    partyid: 51339014,
    userid: 2032920,
    partyuuid: "82688bc7-20ec-4a86-8213-b57cd0b5072e",
  },
  serviceowner_digdir: {
    org: "digdir",
    orgno: "991825827",
  },
  Bot_package_person: {
    lastname: "KOLONI",
    pid: "47855701924",
    partyid: 51276862,
    userid: 2380631,
    partyuuid: "d1278502-6d99-408c-8663-8b03ee7c7d59",
  },
  Bot_package_person_withdraw: {
    lastname: "FAMILIEBARNEHAGE",
    pid: "13859797622",
    partyid: 50865622,
    userid: 2231579,
    partyuuid: "af504a64-5060-42aa-b84e-1f7c0e81923d",
  },
  Bot_package_person_reject: {
   lastname: "KALV",
    pid: "27920999617",
    partyid: 50886468,
    userid: 1457390,
    partyuuid: "000124da-184f-44bf-9618-d3dfeea3e23c",
  },
  Bot_package_from_person: {
    lastname: "JUVEL",
    pid: "11837198668",
    partyid: 51193191,
    userid: 2406118,
    partyuuid: "d6e9fa3b-3047-4ce0-ba4a-0bd0598c1ded",
  },
  Bot_package_Org_for_serviceowner: {
    name: "RASTLØS RESERVERT TIGER AS",
    org_no: "210774432",
    partyid: 51457661,
    partyuuid: "bd40a41b-94d5-49f1-a368-0b2d72f7723c",
    dagligleder: {
      name: "NYTTIG RIBBE",
      pid: "11826899389",
      partyid: 51203182,
      userid: 1271517,
      partyuuid: "3968fdd2-b13b-47d1-9fdc-ea9db7314676",
    },
  },
  Bot_package_Org: {
    name: "KREATIV SNÅL PIGGSVIN",
    org_no: "313696057",
    partyid: 51469173,
    partyuuid: "6d974ae6-f962-497e-bd1e-807b6ece37dc",
    innehaver: {
      name: "SMAL GÅSTOL",
      pid: "26867198260",
      partyid: 50627228,
      userid: 2411242,
      partyuuid: "d81601e7-b56e-452d-a5f2-5360f5e26f70",
    },
  },
    Bot_package_Org_B: {
    name: "UNGT URETTFERDIG TIGER AS",
    org_no: "211155892",
    partyid: 584976156,
    partyuuid: "982b2c89-c383-4329-939a-02bed2e76106",
    innehaver: {
      name: "SNAKKESALIG NATTHEGRE",
      pid: "02928498746",
      partyid: 50667533,
      userid: 2458450,
      partyuuid: "e2cc83b6-4342-41f4-b5c2-446e32165593",
    },
  }
};
