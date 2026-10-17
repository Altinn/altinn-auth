# Changelog

## 1.0.0 (2026-10-16)


### Features

* **#2114:** expose viaRoles on connection sub-connections ([#3895](https://github.com/Altinn/altinn-auth/issues/3895)) ([0df6cf8](https://github.com/Altinn/altinn-auth/commit/0df6cf8a1e48e7c75e3f3db4618c721d16735527))
* **#3690:** tilgangsstyrer for privatperson skal bare kunne administrere utgående tilganger ([#3700](https://github.com/Altinn/altinn-auth/issues/3700)) ([b0a5721](https://github.com/Altinn/altinn-auth/commit/b0a57217b2fc13b002ee3028856d802891b56b17))
* **#3714:** revoke assignments when a system user is deleted ([#4014](https://github.com/Altinn/altinn-auth/issues/4014)) ([5318281](https://github.com/Altinn/altinn-auth/commit/531828114b98a53a74518a0a6af9b573cbb92c51))
* **#3815:** Include Client-Delegated Resources in ConnectionQuery and AuthorizedParties response ([#3855](https://github.com/Altinn/altinn-auth/issues/3855)) ([b09ec72](https://github.com/Altinn/altinn-auth/commit/b09ec72d2050c2ef4c222abe2b56c3431444becf))
* **#3817:** PIP & PDP | Update PIP.GetAllDelegationChanges to Include Client-Delegated Resources ([#3834](https://github.com/Altinn/altinn-auth/issues/3834)) ([4f69a01](https://github.com/Altinn/altinn-auth/commit/4f69a012f1f0464336272fb13ee253f029ef384b))
* **#3974:** add serviceowner resource delegation endpoints ([#4026](https://github.com/Altinn/altinn-auth/issues/4026)) ([43aa9ed](https://github.com/Altinn/altinn-auth/commit/43aa9ed8b1b48a62850fe8c591416f77f8daf25c))
* **#3979:** Activitylog - Initial Triggers and APIs ([#4185](https://github.com/Altinn/altinn-auth/issues/4185)) ([ec32a18](https://github.com/Altinn/altinn-auth/commit/ec32a18f22268b386a19ead9b5bbcf8cc5d5ca61))
* **#4321:** Count request for subunits ([#4337](https://github.com/Altinn/altinn-auth/issues/4337)) ([44176b4](https://github.com/Altinn/altinn-auth/commit/44176b44b70687a6aaab5e4d32180bc2d1121c14))
* **access-management:** allow some system users to send package access requests ([#4195](https://github.com/Altinn/altinn-auth/issues/4195)) ([b14ee7a](https://github.com/Altinn/altinn-auth/commit/b14ee7a24431526c2b5c3cd520f141cae453492d))
* add a match mode to the client delegation clients endpoints ([#3914](https://github.com/Altinn/altinn-auth/issues/3914)) ([1c99ef7](https://github.com/Altinn/altinn-auth/commit/1c99ef7bcda04d73b56c73f0fe7cb2b88cad7de8))
* add API endpoints to GET and DELETE ID-porten authorizations ([#3752](https://github.com/Altinn/altinn-auth/issues/3752)) ([e41928c](https://github.com/Altinn/altinn-auth/commit/e41928cb7134e22534054f456f53c52dc1db4c32))
* add boilerplate manifest files ([#4011](https://github.com/Altinn/altinn-auth/issues/4011)) ([4984b0c](https://github.com/Altinn/altinn-auth/commit/4984b0cebbba98b4ab6c26b2c553496a84b09535))
* **authorization:** add access restriction and via-party to external Authorize API ([#4309](https://github.com/Altinn/altinn-auth/issues/4309)) ([c32e5e7](https://github.com/Altinn/altinn-auth/commit/c32e5e7cbbb195257629e58c70cf9845c8944844))
* delegate single resources to agents in the v2 client delegation API ([#3800](https://github.com/Altinn/altinn-auth/issues/3800)) ([8be0c81](https://github.com/Altinn/altinn-auth/commit/8be0c81ad601bc2263f9e7bcfe9c0df962498b7b))
* enforce system user minimum authentication level obligation in PEP ([#3955](https://github.com/Altinn/altinn-auth/issues/3955)) ([1d94b77](https://github.com/Altinn/altinn-auth/commit/1d94b771f3cea6201cb97c0030d4a5f233ec634f)), closes [#3281](https://github.com/Altinn/altinn-auth/issues/3281)
* flatten the my/clientproviders response ([#3833](https://github.com/Altinn/altinn-auth/issues/3833)) ([7d0e7ae](https://github.com/Altinn/altinn-auth/commit/7d0e7aea9564859f554f4bc4e1c770f59dbb10f7))
* let agents remove received client-delegated resources ([#3822](https://github.com/Altinn/altinn-auth/issues/3822)) ([30c47af](https://github.com/Altinn/altinn-auth/commit/30c47af06e57e0a571a342891a4e116032e317be))
* link delegation resources to their originating assignment resource ([#3772](https://github.com/Altinn/altinn-auth/issues/3772)) ([7a3a098](https://github.com/Altinn/altinn-auth/commit/7a3a09890ffe61bf2a938e81983f70059aea1f11))
* move the body-carrying v2 client delegation deletes to POST ([#3824](https://github.com/Altinn/altinn-auth/issues/3824)) ([269492f](https://github.com/Altinn/altinn-auth/commit/269492f842dc1572382a424d8fd5cb55496ee825))
* patch AM Dockerfile ([#4328](https://github.com/Altinn/altinn-auth/issues/4328)) ([c6aa5d0](https://github.com/Altinn/altinn-auth/commit/c6aa5d00b3895a5db23c5757be2aef9f71ec71f0))
* return the resource registry id on v2 client delegation resources ([#3829](https://github.com/Altinn/altinn-auth/issues/3829)) ([e032fd1](https://github.com/Altinn/altinn-auth/commit/e032fd1e774667231b2df7ed0e64f7e880426ae8))
* show client delegated resources in the connections api ([#3882](https://github.com/Altinn/altinn-auth/issues/3882)) ([ef7cd1d](https://github.com/Altinn/altinn-auth/commit/ef7cd1d4672455212187974b24bb56fd4e7f17f1))
* version the client delegation API and read delegated resources in v2 ([#3788](https://github.com/Altinn/altinn-auth/issues/3788)) ([b5df277](https://github.com/Altinn/altinn-auth/commit/b5df277eab8106be463776b374336e33b46d20f5))


### Bug Fixes

* **#1243:** add real descriptions to seven CCR roles ([#3940](https://github.com/Altinn/altinn-auth/issues/3940)) ([6d6aafe](https://github.com/Altinn/altinn-auth/commit/6d6aafe96afc98d0f3f487aa987cf97ff6f73d69))
* **#1873:** correct the Bostyrer role title ([#3939](https://github.com/Altinn/altinn-auth/issues/3939)) ([9d6a6e0](https://github.com/Altinn/altinn-auth/commit/9d6a6e0216fc2d40dc2094868c4ed270f5097650))
* **#3663:** correct nynorsk titles for areas and access packages ([#3902](https://github.com/Altinn/altinn-auth/issues/3902)) ([ae1d1ba](https://github.com/Altinn/altinn-auth/commit/ae1d1ba22f0e42ac7f8e3690b8028aa39198dc29))
* **#3789:** correct null role and agentAddedAt in v1 clientdelegation responses ([#3790](https://github.com/Altinn/altinn-auth/issues/3790)) ([9d73b4a](https://github.com/Altinn/altinn-auth/commit/9d73b4a708965a0c3b78ff9abe2b33b00380f28e))
* **#4035:** check Delegable in the serviceowner resource rights lookup ([#4037](https://github.com/Altinn/altinn-auth/issues/4037)) ([1bd7288](https://github.com/Altinn/altinn-auth/commit/1bd7288df37e9fc8cdd3c24f12e8f850606616b7))
* **#4044:** remove every delegation when a client is removed from an agent ([#4045](https://github.com/Altinn/altinn-auth/issues/4045)) ([2b6f83e](https://github.com/Altinn/altinn-auth/commit/2b6f83eb6bcf9db265fe4ddc86c504a3c4373c16))
* **#4122:** accept the org claim for providers without an organization number ([#4123](https://github.com/Altinn/altinn-auth/issues/4123)) ([ad1c9ed](https://github.com/Altinn/altinn-auth/commit/ad1c9ed1ec16b2f419484986f28d27e096a55317))
* **#4381:** repair CI after the slnx move and the dotnet format run ([#4382](https://github.com/Altinn/altinn-auth/issues/4382)) ([eaefa62](https://github.com/Altinn/altinn-auth/commit/eaefa6248c1dc85901224c3c02cbea3bd71af73a))
* **#4594:** remove the BOM that stops the Access Management Bruno run ([#4595](https://github.com/Altinn/altinn-auth/issues/4595)) ([7599446](https://github.com/Altinn/altinn-auth/commit/75994463e36917bac5e58d7da1d185dba671648d))
* address code scanning findings in the AccessManagement vertical ([#3951](https://github.com/Altinn/altinn-auth/issues/3951)) ([bca1f0c](https://github.com/Altinn/altinn-auth/commit/bca1f0c1c1716280873d337039da92358ffa4f51))
* align policy cache keys and serve each read a fresh policy instance ([#3957](https://github.com/Altinn/altinn-auth/issues/3957)) ([357cbe1](https://github.com/Altinn/altinn-auth/commit/357cbe1158b8b10abcc4e273b70f7eea20fc4f6b))
* allow dis sp to read from az app configuration ([#4139](https://github.com/Altinn/altinn-auth/issues/4139)) ([64bf594](https://github.com/Altinn/altinn-auth/commit/64bf594f0c220350dbe24047a3dd4818e69f89e7))
* authorizedparties include-flags honored when orgCode/resource filter is used ([#3760](https://github.com/Altinn/altinn-auth/issues/3760)) ([b5b6f4e](https://github.com/Altinn/altinn-auth/commit/b5b6f4e715aee142aa3ff8efdd579df58ee51041))
* charset ([#4398](https://github.com/Altinn/altinn-auth/issues/4398)) ([a70aa1e](https://github.com/Altinn/altinn-auth/commit/a70aa1e9237d8ccb26e4c6753a803bac75a60133))
* client list package filter only matches packages the client holds ([#3865](https://github.com/Altinn/altinn-auth/issues/3865)) ([e067463](https://github.com/Altinn/altinn-auth/commit/e06746358064381bf6d9dc00193dcb49ceff1345))
* correct entity-type guards and CreateRecipient branching in notification handlers ([#3917](https://github.com/Altinn/altinn-auth/issues/3917)) ([849131c](https://github.com/Altinn/altinn-auth/commit/849131cd62237a608ae10ce04cf775730b359bc9)), closes [#3916](https://github.com/Altinn/altinn-auth/issues/3916)
* docker deps ([#4142](https://github.com/Altinn/altinn-auth/issues/4142)) ([aa7876b](https://github.com/Altinn/altinn-auth/commit/aa7876ba1cf35b05b1120686c64dcd792a5112a4))
* fixfix ([#4136](https://github.com/Altinn/altinn-auth/issues/4136)) ([da28a24](https://github.com/Altinn/altinn-auth/commit/da28a2425a5b8b5949e2fa10435e513881ab28b4))
* harden null handling in basic-auth composer and consent-events lookup ([#3536](https://github.com/Altinn/altinn-auth/issues/3536)) ([0ea0130](https://github.com/Altinn/altinn-auth/commit/0ea013044e434c372c322cbf4cae1c9ed5a2b559))
* identity ([#4137](https://github.com/Altinn/altinn-auth/issues/4137)) ([ce44f73](https://github.com/Altinn/altinn-auth/commit/ce44f73af8d7a960f68eae60b49506bebc0934f4))
* log the actual api version in request telemetry ([#3843](https://github.com/Altinn/altinn-auth/issues/3843)) ([ba3eb6f](https://github.com/Altinn/altinn-auth/commit/ba3eb6f18cd1b66d813afaa4feb2c1c449469cd3))
* match delegation packages on delegation and package id when deleting ([#3849](https://github.com/Altinn/altinn-auth/issues/3849)) ([385a133](https://github.com/Altinn/altinn-auth/commit/385a133a8dd6dbc06087f043d701694cddabc460))
* pin MessagePack to 2.5.302 to clear CVE-2026-48109 in the AppHost ([#3538](https://github.com/Altinn/altinn-auth/issues/3538)) ([b3c8804](https://github.com/Altinn/altinn-auth/commit/b3c88043dfe78469ae8365f2df85460d1633a1a9))
* point v2 client delegation validation errors at the renamed query parameters ([#3809](https://github.com/Altinn/altinn-auth/issues/3809)) ([5f6eb9d](https://github.com/Altinn/altinn-auth/commit/5f6eb9d02c1d0b3f8264c03818a56a66d57be83d))
* register api versioning in the standalone enduser host ([#3831](https://github.com/Altinn/altinn-auth/issues/3831)) ([ff50ffe](https://github.com/Altinn/altinn-auth/commit/ff50ffe75cffec76bec2aa041f600fa9388c98f4))
* reject persisting derived Expired consent status and event type ([#3743](https://github.com/Altinn/altinn-auth/issues/3743)) ([62ca5d6](https://github.com/Altinn/altinn-auth/commit/62ca5d6572ee61d78308ad96f6d48341b1419296))
* restore the configuration block in the AccessManagement prod and tt02 tfvars ([#3897](https://github.com/Altinn/altinn-auth/issues/3897)) ([6907166](https://github.com/Altinn/altinn-auth/commit/69071660d8ee7cdff2488e2eee88a4adf3fccd3e))
* serialize StaticDataIngest with an advisory lock to stop pk_entity races ([#3409](https://github.com/Altinn/altinn-auth/issues/3409)) ([cb7b6bb](https://github.com/Altinn/altinn-auth/commit/cb7b6bb2f9e1b774fca8f71133ca1828c69bd715))
* show via and via role on indirect resource and instance rights ([#3894](https://github.com/Altinn/altinn-auth/issues/3894)) ([478f2cd](https://github.com/Altinn/altinn-auth/commit/478f2cd4bb677b01a4986b940d8c0331d90b2234))
* stop DelegationMetadataRepositoryMock from randomly simulating failed inserts ([#3711](https://github.com/Altinn/altinn-auth/issues/3711)) ([866ab48](https://github.com/Altinn/altinn-auth/commit/866ab48c3381dabf476453a8cbf2380161ad64be))
* tf format for am ([#3921](https://github.com/Altinn/altinn-auth/issues/3921)) ([532de86](https://github.com/Altinn/altinn-auth/commit/532de86c2689faab960109a078f9976806ccb7d4))
* throw exception if list of resources are empty ([#3915](https://github.com/Altinn/altinn-auth/issues/3915)) ([460c5a3](https://github.com/Altinn/altinn-auth/commit/460c5a3b03c0866e18784b3d1c462bb3d896c431))
* update mail notification title from "du har fått fullmakt i Altinn" to "Dere har blitt fratatt fullmakt i Altinn" for instance delegation ([#4107](https://github.com/Altinn/altinn-auth/issues/4107)) ([b535946](https://github.com/Altinn/altinn-auth/commit/b53594693b3c0babcb3373f75fa9b9dfbcaa4d25))
* workload identity ([#4140](https://github.com/Altinn/altinn-auth/issues/4140)) ([4d15b8e](https://github.com/Altinn/altinn-auth/commit/4d15b8e2faafc9ba8eb38c81d9810b24c85fa9fc))
* workload identity ([#4141](https://github.com/Altinn/altinn-auth/issues/4141)) ([959760b](https://github.com/Altinn/altinn-auth/commit/959760bf9210f3777b9b552b4472f275d707163d))


### Performance Improvements

* index consentrequest on the party columns the events feed filters on ([#4015](https://github.com/Altinn/altinn-auth/issues/4015)) ([3debd41](https://github.com/Altinn/altinn-auth/commit/3debd417a7084d3fd38da833eaa31e315d79c59f))
