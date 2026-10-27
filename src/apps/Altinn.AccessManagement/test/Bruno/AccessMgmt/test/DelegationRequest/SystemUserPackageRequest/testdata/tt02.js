// Seed data for the SystemUserPackageRequest scenarios in TT02.
// TODO: replace the placeholder values with real TT02 entities before enabling the suite.
module.exports = {
    env: "tt02",
    package: "urn:altinn:accesspackage:jordbruk",
    publicSectorConsumer: {
        // KOMM, FYLK or STAT organisation that owns systemUser
        orgno: "313708640", // KOMM
        systemUser: {
          clientId: "TODO",
          partyUuid: "5ee02ad4-a24a-467e-96fc-aaf9d4dd69aa",
        },
    },
    otherPublicSectorConsumer: {
        // A second KOMM, FYLK or STAT organisation, owning a different system user
        orgno: "311166131", // STAT
        systemUser: {
            clientId: "TODO",
            partyUuid: "706bdc9f-c6ce-4b62-aca4-1da586fb3630",
        },
    },
    privateConsumer: {
        // Organisation of any other type (for example AS) that owns privateSystemUser
        orgno: "314242726",
        systemUser: {
            clientId: "TODO",
            partyUuid: "7ba107f5-19ae-4342-b8c2-ced6501a0adb"
        },
    },
    recipient: {
        // Organisation receiving the request
        orgno: "313180085",
        partyUuid: "ff8f47ae-bf03-420d-817b-5f81d36e947a",
        dagligleder: {
            pid: "12916999559",
            userId: 342984,
            partyId: 51198658,
            partyUuid: "efdb9336-7b66-462d-a147-243e4a0e886a",
        },
    },
    unknownOrgNo: "940000009",
};
