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
    lastname: "ADAPTER",
    pid: "18919173986",
    partyid: 50422023,
    userid: 2215300,
    partyuuid: "ab9e642d-ffe3-4e6e-811e-305b5add04d7",
  },
  Bot_package_person_reject: {
    lastname: "PLATINA",
    pid: "06889774566",
    partyid: 50435470,
    userid: 1962309,
    partyuuid: "728e4901-035d-40db-9bdd-7dec07400ead",
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
