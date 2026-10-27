// Seed data for the SystemUserPackageRequest scenarios in AT22.
// TODO: replace the placeholder values with real AT22 entities before enabling the suite.
module.exports = {
    env: "at22",
    package: "urn:altinn:accesspackage:jordbruk",
    publicSectorConsumer: {
        // KOMM, FYLK or STAT organisation that owns systemUser
        orgno: "313708640", // KOMM
        systemUser: {
          clientId: "TODO",
          partyUuid: "c833a203-5c2d-433c-9e2b-d364e3137d15",
        },
    },
    otherPublicSectorConsumer: {
        // A second KOMM, FYLK or STAT organisation, owning a different system user
        orgno: "311166131", // STAT
        systemUser: {
            clientId: "TODO",
            partyUuid: "9aa23bc9-6e6e-4f79-81f8-c528f4c59535",
        },
    },
    privateConsumer: {
        // Organisation of any other type (for example AS) that owns privateSystemUser
        orgno: "314242726",
        systemUser: {
            clientId: "TODO",
            partyUuid: "171bd27b-31d2-409b-89b7-b88cb5b6bae3"
        },
    },
    recipient: {
        // Organisation receiving the request
        orgno: "313180085",
        partyUuid: "53d3e2a8-779a-49c9-91bb-340eb74a5491",
        dagligleder: {
            pid: "12916999559",
            userId: 21018261,
            partyId: 50947456,
            partyUuid: "d4a5b1bb-d5d1-4150-943a-76a8e9afb1fe",
        },
    },
    unknownOrgNo: "940000009",
};
