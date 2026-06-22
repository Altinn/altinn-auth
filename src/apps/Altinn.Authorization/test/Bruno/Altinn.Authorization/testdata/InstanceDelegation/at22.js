module.exports = 
{
    "env": "at22",
    "resources": {
        "appInstanceDelegation": {
            "resourceId": "app_ttd_ttd-bruno-tilgangspakke-app",
            "org": "ttd",
            "app":  "ttd-bruno-tilgangspakke-app"
        }
    },
    "instances": {
        "p2p": {
            "instanceId": "7ff72acc-1714-4991-a1d1-a7e89bd5e62d",
            "brukerstyrtInstanceId": "7e6c9a38-13e2-4e5d-84ff-86a2903eb596",
            "from": "5daf2382-adee-4cd6-9995-3c5f3fc058af",
            "to": "bd90e954-efe5-4b65-be8a-f9fccbb36342"
        },
        "p2main": {
            "instanceId": "d1f6410e-e5f1-406f-b0f6-8605f95bc867",
            "from": "5daf2382-adee-4cd6-9995-3c5f3fc058af",
            "to": "f7e2eb77-6b5e-4aeb-9fe9-f261013401c9"
        },
        "p2mainAgent": {
            "instanceId": "cf3a907f-4385-44dc-ab50-d3f56d8e5c4e"
        },
        "p2sub": {
            "instanceId": "259dde98-8223-4e07-94c2-574a5a7c2a1c"
        },
        "main2p": {
            "instanceId": "6240aaf3-5795-4698-b1ac-1a18224eff22",
            "from": "01712d4f-cfb2-4d12-8aac-2eb7ad0d9c86",
            "to": "bd90e954-efe5-4b65-be8a-f9fccbb36342"
        },
        "main2sub": {
            "instanceId": "f4777946-ecf6-43fc-b740-7197980e97c2",
        },
        "main2mainAgent": {
            "instanceId": "e4d5b1bb-1d1e-43d2-b5f3-79edf62685ce"
        },
        "main2subAgent": {
            "instanceId": "7259eac0-6ef3-4458-9965-3822d4ddea40"
        },
        "sub2p": {
            "instanceId": "238dd66a-8357-4da9-969b-5c53f3246f38",
            "from": "e21a640b-9558-4d59-b47f-7b87d2cd9ee0",
            "to": "bd90e954-efe5-4b65-be8a-f9fccbb36342"
        },
        "sub2main": {
            "instanceId": "5b8a4dd5-f281-4c78-a05a-b9e3a8e47f07",
        },
        "sub2mainAgent": {
            "instanceId": "f59cc62f-45c0-4a3b-bb29-c5fc33f54b36",
        },
        "sub2sub": {
            "instanceId": "2a4d49e1-4b54-4079-9b69-73e37d528299",
        },
        "sub2subAgent": {
            "instanceId": "f59cc62f-45c0-4a3b-bb29-c5fc33f54b36"
        },
        "o2o": {
            "instanceId": "bd91ee77-cfd0-4797-8e3c-553bd8a14ee9",
            "from": "f7e2eb77-6b5e-4aeb-9fe9-f261013401c9",
            "to": "01712d4f-cfb2-4d12-8aac-2eb7ad0d9c86"
        }
    },
    "organizations": {
        "mobilBeskjedenTiger": {
            "partyUuid": "f7e2eb77-6b5e-4aeb-9fe9-f261013401c9",
            "name": "MOBIL BESKJEDEN TIGER AS",
            "organizationNumber": "310705071",
            "partyId": 51373625,
            "subunits": {
                "mobilBeskjedenTiger": {
                    "partyUuid": "2177ee69-fd11-4b38-904a-e0e2f1958f86",
                    "name": "MOBIL BESKJEDEN TIGER AS",
                    "organizationNumber": "314410343",
                    "partyId": 51690212,
                    "dagl": null
                }
            },
            "dagl": {
                "name": "HAKKE DYR",
                "personId": "29917997339",
                "userId": 20012883,
                "partyUuid": "bd90e954-efe5-4b65-be8a-f9fccbb36342",
                "partyId": 50127680
            },
            "agent": {
                "name": "KONKRET OTER",
                "personId": "21929899382",
                "userId": 20153145,
                "partyUuid": "1d9e6932-3bed-4ce1-a33f-30635ce1f664",
                "partyId": 50886700
            }
        },
        "legitimJusterbarTiger": {
            "partyUuid": "01712d4f-cfb2-4d12-8aac-2eb7ad0d9c86",
            "name": "LEGITIM JUSTERBAR TIGER AS",
            "organizationNumber": "311041371",
            "partyId": 51403263,
            "subunits": {
                "legitimJusterbarTiger": {
                    "partyUuid": "e21a640b-9558-4d59-b47f-7b87d2cd9ee0",
                    "name": "LEGITIM JUSTERBAR TIGER AS",
                    "organizationNumber": "315684617",
                    "partyId": 51806076,
                    "dagl": null,
                    "agent": {
                        "name": "ALLEHÅNDE GØYAL",
                        "personId": "63886300141",
                        "userId": 20230661,
                        "partyUuid": "2dfccbab-c3cd-486c-9e06-4ecf0fb28ade",
                        "partyId": 51090315
                    }
                }
            },
            "dagl": {
                "name": "ØVELSE EMPIRISK",
                "personId": "25884699358",
                "userId": 20012882,
                "partyUuid": "5daf2382-adee-4cd6-9995-3c5f3fc058af",
                "partyId": 51195601
            },
            "agent": {
                "name": "AKTIV BJØRN",
                "personId": "14828497872",
                "userId": 20044758,
                "partyUuid": "06a4f9dd-d385-4d5b-a459-6e3d7af74ca7",
                "partyId": 50167532
            }
        },
        "ivrigGildTigerAS": {
            "partyUuid": "87b0a6ff-d3e2-466e-a7d1-e52879425e43",
            "name": "IVRIG GILD TIGER AS",
            "organizationNumber": "311111043",
            "partyId": 51409428,
            "subunits": {
                "ivrigGildTigerAS": {
                    "partyUuid": "7d4602d6-5692-4f7b-b16a-91a380cdbc0c",
                    "name": "LEGITIM JUSTERBAR TIGER AS",
                    "organizationNumber": "314903331",
                    "partyId": 51735042,
                    "dagl": null
                },
                "agent": {
                    "name": "DIAMETER MOTLØS",
                    "personId": "21831799207",
                    "userId": 20359610,
                    "partyUuid": "495df8aa-1d04-4c9e-8d18-b6c3b3a01d07",
                    "partyId": 50331097
                }
            },
            "dagl": {
                "name": "SUPPORTER ARITMETISK",
                "personId": "19886098742",
                "userId": 20967956,
                "partyUuid": "ca094035-f9cb-48b9-8f34-a37af4b66e96",
                "partyId": 50972013
            }
        },
        "slakkAktivBilleIKS": {
            "partyUuid": "e5789232-f227-418d-a1c9-87a28fc25e7e",
            "name": "SLAKK AKTIV BILLE IKS",
            "organizationNumber": "314248333",
            "partyId": 51269977,
            "subunits": {
                "slakkAktivBilleIKS": {
                    "partyUuid": "3599450f-c15e-4a30-868f-0cf1f8f7ea29",
                    "name": "SLAKK AKTIV BILLE IKS",
                    "organizationNumber": "314612086",
                    "partyId": 51708559,
                    "dagl": null
                }
            },
            "dagl": {
                "name": "RØDFOTSULE AKSEPTABEL",
                "personId": "14878498454",
                "userId": 20763541,
                "partyUuid": "9ee440a2-8cb9-43da-a2d8-962db5b53ab2",
                "partyId": 50161361
            }
        }
    },
    "persons": {
        "ovelseEmpirisk": {
            "name": "ØVELSE EMPIRISK",
            "personId": "25884699358",
            "userId": 20012882,
            "partyUuid": "5daf2382-adee-4cd6-9995-3c5f3fc058af",
            "partyId": 51195601
        },
        "hakkeDyr": {
            "name": "HAKKE DYR",
            "personId": "29917997339",
            "userId": 20012883,
            "partyUuid": "bd90e954-efe5-4b65-be8a-f9fccbb36342",
            "partyId": 50127680
        },
        "juleferieStoyfri": {
            "name": "JULEFERIE STØYFRI",
            "personId": "23879198038",
            "userId": 20713386,
            "partyUuid": "944aef87-72f3-4a8c-ba61-f589dce69f68",
            "partyId": 50217741
        },
    }
}
