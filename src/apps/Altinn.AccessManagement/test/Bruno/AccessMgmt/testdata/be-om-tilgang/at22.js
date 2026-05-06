module.exports = {
  env: "at22",
  resource_to_delegate: {
    resource_id_privatperson:
      "ttd-test-bruno-enkelttjeneste-privatperson-ressurs",
    resource_id_virksomhet:
      "ttd-test-bruno-enkelttjenestedelegering-virksomhet",
    app_id: "app_ttd_test-bruno-app-for-delegation",
    skattetaten_ressurs: "ske-informasjon-om-trekkpaalegg",
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
    konkbotilg: "urn:altinn:accesspackage:konkursbo-tilgangsstyrer"
  },
  Bot_person: {
    lastname: "INGREDIENS",
    pid: "21866699620",
    partyid: 50786406,
    userid: 20420567,
    partyuuid: "5632f664-c7c1-46ce-968c-5ca50883967a",
  },
  Bot_from_person: {
    lastname: "KARIES",
    pid: "06920848050",
    partyid: 51107242,
    userid: 20388113,
    partyuuid: "4f61b87e-8f3f-4c34-8036-7bf403ad7bf7",
  },
  Bot_Org_for_serviceowner: {
    name: "STERK DYP TIGER AS",
    org_no: "313025098",
    partyid: 51603580,
    partyuuid: "b6220906-4437-48dd-b219-779c3cc6aaf5",
    dagligleder: {
      name: "Materialistisk Bygg",
      pid: "22816399142",
      partyid: 51189860,
      userid: 20080164,
      partyuuid: "0e251053-2cf3-4cdb-ae3c-fde6931389fa",
    },
  },
  Bot_person_serviceowner: {
    lastname: "STAMMOR",
    pid: "08826499630",
    partyid: 50956332,
    userid: 20915572,
    partyuuid: "bf02757f-419d-4a81-9c83-f224731d1fe6",
  },

  Bot_Org: {
    name: "PASSIV HANDLEKRAFTIG PUMA",
    org_no: "313657892",
    partyid: 51647456,
    partyuuid: "24ad99ed-5e92-48f1-a969-4f1e8c886e40",
    innehaver: {
      name: "FROM ANDAKT",
      pid: "04907397659",
      partyid: 50712216,
      userid: 20082861,
      partyuuid: "0eb7144c-1ff1-4657-a1d7-2b127830f52d",
    },
  },

  Bot_Org_2: {
    name: "RIMELIG FAST HEST BORETTSLAG",
    org_no: "310413089",
    partyid: 51347779,
    partyuuid: "584f3e33-1dc6-4e72-ad4b-6d4d1e39dea2",
    styreleder: {
      name: "Lysegul Femkant",
      pid: "14838799907",
      partyid: 50167981,
      userid: 20281547,
      partyuuid: "38c5f772-3544-4850-9cab-e4c81077c56c",
    },
  },
  user_uten_rettighetshaver: {
    lastname: "SJOKOLADEKAKE",
    pid: "63881275025",
    partyid: 51089153,
    userid: 20230679,
    partyuuid: "2dfe42c3-0c03-4684-be9a-fa3b0a451725",
  },
  serviceowner_digdir: {
    org: "digdir",
    orgno: "991825827",
  },
  Bot_package_person: {
    lastname: "KOLONI",
    pid: "47855701924",
    partyid: 51026392,
    userid: 20159085,
    partyuuid: "1ee055c5-53ae-4e35-8c47-1ab1d30cbc29",
  },
  Bot_package_person_withdraw: {
    lastname: "ADAPTER",
    pid: "18919173986",
    partyid: 50465896,
    userid: 20785410,
    partyuuid: "a38a3b1e-8c8f-4fd4-8789-38f45837b1b3",
  },
  Bot_package_person_reject: {
    lastname: "PLATINA",
    pid: "06889774566",
    partyid: 50479506,
    userid: 20936786,
    partyuuid: "c37f0e2e-afaa-4aec-860f-18c70f18d349",
  },
  Bot_package_from_person: {
    lastname: "JUVEL",
    pid: "11837198668",
    partyid: 50941784,
    userid: 20332321,
    partyuuid: "4386762e-2b50-4208-a5ca-bfc0e28be176",
  },
  Bot_package_Org_for_serviceowner: {
    name: "RASTLØS RESERVERT TIGER AS",
    org_no: "210774432",
    partyid: 51244126,
    partyuuid: "ecee2f11-d9e8-436d-a188-d69e4fb51de3",
    dagligleder: {
      name: "NYTTIG RIBBE",
      pid: "11826899389",
      partyid: 50951925,
      userid: 20119526,
      partyuuid: "1676408c-c492-4878-a0f9-219f132c0315",
    },
  },
  Bot_package_Org: {
    name: "KREATIV SNÅL PIGGSVIN",
    org_no: "313696057",
    partyid: 51255656,
    partyuuid: "8b0d56b5-2927-4775-9121-a005a30d4cae",
    innehaver: {
      name: "SMAL GÅSTOL",
      pid: "26867198260",
      partyid: 50633584,
      userid: 20058157,
      partyuuid: "09718393-861f-40c9-96b0-0252cf20c1ff",
    },
  },
  Bot_package_Org_B: {
    name: "UNGT URETTFERDIG TIGER AS",
    org_no: "211155892",
    partyid: 51248079,
    partyuuid: "0d5879ee-414c-4c63-99ae-7e9629fbeb5d",
    innehaver: {
      name: "SNAKKESALIG NATTHEGRE",
      pid: "02928498746",
      partyid: 50655446,
      userid: 20423314,
      partyuuid: "56c73e24-37f4-4fc5-8fa2-6ed307f66810",
    },
  },
};
