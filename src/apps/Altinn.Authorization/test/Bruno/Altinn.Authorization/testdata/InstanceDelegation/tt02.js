module.exports = 
{
    "env": "tt02",
    "resources": {
        "appInstanceDelegation": {
            "resourceId": "app_ttd_ttd-bruno-tilgangspakke-app",
            "org": "ttd",
            "app":  "ttd-bruno-tilgangspakke-app"
        }
    },
    "instances": {
        "p2p": {
            "instanceId": "3f7bfe70-ceb3-45c0-8650-905960f9a9f4",
            "brukerstyrtInstanceId": "cb0ac555-0286-461b-8612-2e53b0442864",
            "from": "f9b2750f-f2aa-442c-91b7-418e2f627a8b",
            "to": "07336d43-1f0e-40a8-a02b-2c6019013cc7"
        },
        "p2main": {
            "instanceId": "f87c5de4-bc52-4f06-924e-3e88f7884e2d"
        },
        "p2sub": {
            "instanceId": "6d5a2f37-91ed-4295-be17-ad30c8dba159"
        },
        "main2p": {
            "instanceId": "49986993-2cec-4e96-85a5-b07c5b7e4c5e",
            "from": "58c0acd2-793e-48f1-a19f-169c171066f4",
            "to": "07336d43-1f0e-40a8-a02b-2c6019013cc7"
        },
        "main2sub": {
            "instanceId": "87e3d468-0b02-4135-bd9d-efdcde977062",
        },
        "main2mainAgent": {
            "instanceId": "08e6439e-4524-4ba4-a41e-5caa4dc22431"
        },
        "main2subAgent": {
            "instanceId": "8d323e04-853f-4e8c-9937-17d9b4f16789"
        },
        "sub2p": {
            "instanceId": "3a875fb6-4610-4436-b179-df45bdbc9fa5",
            "from": "65018cec-9bd4-45ad-998d-357ac181eaf3",
            "to": "07336d43-1f0e-40a8-a02b-2c6019013cc7"
        },
        "sub2main": {
            "instanceId": "a427957b-bb32-4321-89a5-d4d9f8f23bef",
        },
        "sub2mainAgent": {
            "instanceId": "67900a4f-61cc-4b6e-ad20-4124f795136e",
        },
        "sub2sub": {
            "instanceId": "28b94645-5e5c-4cb0-868c-8bbaad9d250c",
        },
        "sub2subAgent": {
            "instanceId": "67900a4f-61cc-4b6e-ad20-4124f795136e",
        },
        "o2o": {
            "instanceId": "632fe488-1ed3-4bff-ba9c-56f1df0839fc",
            "from": "9d263473-4f08-459e-bf27-1de535c40607",
            "to": "58c0acd2-793e-48f1-a19f-169c171066f4"
        }
    },
    "organizations": {
        "mobilBeskjedenTiger": {
            "partyUuid": "9d263473-4f08-459e-bf27-1de535c40607",
            "name": "MOBIL BESKJEDEN TIGER AS",
            "organizationNumber": "310705071",
            "partyId": 51587157,
            "subunits": {
                "mobilBeskjedenTiger": {
                    "partyUuid": "4f849f61-c36f-470a-b0b5-924568e3e10a",
                    "name": "MOBIL BESKJEDEN TIGER AS",
                    "organizationNumber": "314410343",
                    "partyId": 51903584,
                    "dagl": null
                }
            },
            "dagl": {
                "name": "HAKKE DYR",
                "personId": "29917997339",
                "userId": 1424053,
                "partyUuid": "07336d43-1f0e-40a8-a02b-2c6019013cc7",
                "partyId": 51015472
            },
            "agent": {
                "name": "KONKRET OTER",
                "personId": "21929899382",
                "userId": 1990261,
                "partyUuid": "78cb60c0-5224-4d02-9d4b-29df6972dff9",
                "partyId": 51103813
            }
        },
        "legitimJusterbarTiger": {
            "partyUuid": "58c0acd2-793e-48f1-a19f-169c171066f4",
            "name": "LEGITIM JUSTERBAR TIGER AS",
            "organizationNumber": "311041371",
            "partyId": 51616797,
            "subunits": {
                "legitimJusterbarTiger": {
                    "partyUuid": "65018cec-9bd4-45ad-998d-357ac181eaf3",
                    "name": "LEGITIM JUSTERBAR TIGER AS",
                    "organizationNumber": "315684617",
                    "partyId": 52019430,
                    "dagl": null,
                    "agent": {
                        "name": "ALLEHÅNDE GØYAL",
                        "personId": "63886300141",
                        "userId": 1978193,
                        "partyUuid": "76178480-949c-4ce6-9870-64a4229f2192",
                        "partyId": 51340156
                    }
                },
            },
            "dagl": {
                "name": "ØVELSE EMPIRISK",
                "personId": "25884699358",
                "userId": 1424054,
                "partyUuid": "f9b2750f-f2aa-442c-91b7-418e2f627a8b",
                "partyId": 51443447
            },
            "agent": {
                "name": "AKTIV BJØRN",
                "personId": "14828497872",
                "userId": 1560376,
                "partyUuid": "177be44f-83ff-4ea5-9a16-46b24e458eab",
                "partyId": 50673021
            }
        },
        "ivrigGildTigerAS": {
            "partyUuid": "33388dcf-f785-40a5-80f8-536c01b459d6",
            "name": "IVRIG GILD TIGER AS",
            "organizationNumber": "311111043",
            "partyId": 51622964,
            "subunits": {
                "ivrigGildTigerAS": {
                    "partyUuid": "62f90166-9095-49e7-a476-bfb60b3600c9",
                    "name": "LEGITIM JUSTERBAR TIGER AS",
                    "organizationNumber": "314903331",
                    "partyId": 51948403,
                    "dagl": null
                },
                "agent": {
                    "name": "DIAMETER MOTLØS",
                    "personId": "21831799207",
                    "userId": 1903472,
                    "partyUuid": "6542bcb2-7a4c-4b79-b6fa-953213489533",
                    "partyId": 50850919
                }
            },
            "dagl": {
                "name": "SUPPORTER ARITMETISK",
                "personId": "19886098742",
                "userId": 2572394,
                "partyUuid": "fc8a6b0d-9553-4aad-8c96-d4c5f5452d00",
                "partyId": 51222916
            }
        },
        "slakkAktivBilleIKS": {
            "partyUuid": "16071f0d-18bb-4d49-bebc-167df5ebf3fd",
            "name": "SLAKK AKTIV BILLE IKS",
            "organizationNumber": "314248333",
            "partyId": 51483492,
            "subunits": {
                "slakkAktivBilleIKS": {
                    "partyUuid": "3edb1370-e03e-4ab7-af1b-f95369f9c5b7",
                    "name": "SLAKK AKTIV BILLE IKS",
                    "organizationNumber": "314612086",
                    "partyId": 51921926,
                    "dagl": null
                }
            },
            "dagl": {
                "name": "RØDFOTSULE AKSEPTABEL",
                "personId": "14878498454",
                "userId": 343084,
                "partyUuid": "61db9171-b8c1-4500-a401-3479db09196f",
                "partyId": 50833737
            }
        }
    },
    "persons": {
        "ovelseEmpirisk": {
            "name": "ØVELSE EMPIRISK",
            "personId": "25884699358",
            "userId": 1424054,
            "partyUuid": "f9b2750f-f2aa-442c-91b7-418e2f627a8b",
            "partyId": 51443447
        },
        "hakkeDyr": {
            "name": "HAKKE DYR",
            "personId": "29917997339",
            "userId": 1424053,
            "partyUuid": "07336d43-1f0e-40a8-a02b-2c6019013cc7",
            "partyId": 51015472
        },
        "juleferieStoyfri": {
            "name": "JULEFERIE STØYFRI",
            "personId": "23879198038",
            "userId": 2108706,
            "partyUuid": "9386b52b-0f02-4fe3-b102-dd2626503121",
            "partyId": 50996304
        }
    }
};