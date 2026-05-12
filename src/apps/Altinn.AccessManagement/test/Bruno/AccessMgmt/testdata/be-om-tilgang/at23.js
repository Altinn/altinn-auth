module.exports = {
  env: "at22",
  resource_to_delegate: {
    resource_id_privatperson:
      "ttd-test-bruno-enkelttjeneste-privatperson-ressurs",
    resource_id_virksomhet:
      "ttd-test-bruno-enkelttjenestedelegering-virksomhet",
    app_id: "app_ttd_test-bruno-app-for-delegation",
    non_delegable_ressurs_id : "ttd-bruno-test-non-delegable-ressurs",
    skattetaten_ressurs: "ske-informasjon-om-trekkpaalegg",
    virksomhet_pacakge_urn : "urn:altinn:accesspackage:reindrift",
    innbygger_package_urn : "urn:altinn:accesspackage:innbygger-samliv",
    regn_package_urn : "urn:altinn:accesspackage:regnskapsforer-lonn",
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
  Bot_person_serviceowner : {
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
  acc_Pkg_bot_fra_person: {
    lastname: "BIE",
    pid: "27882848332",
    partyid: 50437724,
    userid: 20607186,
    partyuuid: "8450a308-391e-489a-8832-4826529692c9",
  },
  acc_Pkg_bot_til_person: {
    lastname: "BRIS",
    pid: "53848900334",
    partyid: 50916248,
    userid: 21017788,
    partyuuid: "de46e08e-67a9-4c7d-806a-3097352ba898",
  },
  acc_pkg_bot_fra_Org: {
    name: "VERTIKAL UNORMAL HEST BORETTSLAG",
    orgno: "310691011",
    partyid: 51247286,
    partyuuid: "edc8381a-629c-486b-bafb-24819cc455fa",
    styreleder: {
      name: "Økologisk Kategori",
      pid: "10899098280",
      partyid: 50771657,
      userid: 20574874,
      partyuuid: "7b877031-498f-4ca7-b51b-23abe90a6e61",
    },
    hovedadmin : {
      name: "PORSJONB",
      pid: "68844800952",
      partyid: 51034065,
      userid: 20051886,
      partyuuid: "070509a0-73db-4e59-afa4-a84e5cebe5d2",

    }
  },
 acc_pkg_bot_til_Org: {
    name: "VIRKELIG PUNKTLIG TIGER AS",
    orgno: "212755982",
    partyid: 51144625,
    partyuuid: "a58c54ea-a541-44ce-a04b-115668844e49",
    styreleder: {
      name: "Nett Scene",
      pid: "05888099818",
      partyid: 50318852,
      userid: 20553855,
      partyuuid: "76e312f6-5b4c-4982-bf43-5afc2f3897a2",
    },
    hovedadmin : {
      name: "Bolle",
      pid: "29870848283",
      partyid: 51034065,
      userid: 20051886,
      partyuuid: "070509a0-73db-4e59-afa4-a84e5cebe5d2",

    }
  },
  
};
