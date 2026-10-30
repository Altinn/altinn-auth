# Changelog

## 1.0.0 (2026-10-30)


### Features

* **#3817:** PIP & PDP | Update PIP.GetAllDelegationChanges to Include Client-Delegated Resources ([#3834](https://github.com/Altinn/altinn-auth/issues/3834)) ([4f69a01](https://github.com/Altinn/altinn-auth/commit/4f69a012f1f0464336272fb13ee253f029ef384b))
* **#4127:** measure duplicate audit events by default, and never let the measurement fail a decision ([#4290](https://github.com/Altinn/altinn-auth/issues/4290)) ([50c70ad](https://github.com/Altinn/altinn-auth/commit/50c70adcd87c7d3e2955d3586bd3c2811d3217c5))
* **#4127:** measure duplicate authorization events before they are queued for the audit log ([#4130](https://github.com/Altinn/altinn-auth/issues/4130)) ([aa7a54b](https://github.com/Altinn/altinn-auth/commit/aa7a54b0f65e56e538a238b8e02699896b12fd5a))
* add caller dimension to PDP decision metric ([#3934](https://github.com/Altinn/altinn-auth/issues/3934)) ([17b1a64](https://github.com/Altinn/altinn-auth/commit/17b1a64302c2db71a1d87af123506cfe22180f38))
* **authorization:** add access restriction and via-party to external Authorize API ([#4309](https://github.com/Altinn/altinn-auth/issues/4309)) ([c32e5e7](https://github.com/Altinn/altinn-auth/commit/c32e5e7cbbb195257629e58c70cf9845c8944844))
* **pdp:** open access-restriction to all authorize and decision callers ([#4726](https://github.com/Altinn/altinn-auth/issues/4726)) ([c71ee71](https://github.com/Altinn/altinn-auth/commit/c71ee71a5c84e3190b33da0a0209bbcd45ab4906))


### Bug Fixes

* address code scanning findings in the AccessManagement vertical ([#3951](https://github.com/Altinn/altinn-auth/issues/3951)) ([bca1f0c](https://github.com/Altinn/altinn-auth/commit/bca1f0c1c1716280873d337039da92358ffa4f51))
* cache policy documents as bytes and parse per read ([#3959](https://github.com/Altinn/altinn-auth/issues/3959)) ([5fe935e](https://github.com/Altinn/altinn-auth/commit/5fe935e9c02f97abe9e718c4f1dbefdc5c252faf))
* charset ([#4398](https://github.com/Altinn/altinn-auth/issues/4398)) ([a70aa1e](https://github.com/Altinn/altinn-auth/commit/a70aa1e9237d8ccb26e4c6753a803bac75a60133))
* continue cleanup started in previous commit ([#4403](https://github.com/Altinn/altinn-auth/issues/4403)) ([21de2f9](https://github.com/Altinn/altinn-auth/commit/21de2f9fc89e7b21a910be5505f75b5cab9c006f))
* docker deps ([#4142](https://github.com/Altinn/altinn-auth/issues/4142)) ([aa7876b](https://github.com/Altinn/altinn-auth/commit/aa7876ba1cf35b05b1120686c64dcd792a5112a4))
* drop the deleted Migration folder from the Authorization Dockerfile ([#3845](https://github.com/Altinn/altinn-auth/issues/3845)) ([878c3f0](https://github.com/Altinn/altinn-auth/commit/878c3f06ea375492f95d76c3726224a6e67269f9))
* Rename deployment from 'authorization' to 'authz-api' ([587b2ee](https://github.com/Altinn/altinn-auth/commit/587b2eec9235caf6c68e8981a9162187373775b2))
* tidy authz k8 manifests ([#4728](https://github.com/Altinn/altinn-auth/issues/4728)) ([34700f4](https://github.com/Altinn/altinn-auth/commit/34700f4a28f5726fb1d45ec3e561efea9e52a1de))
* update docker bake file for authorization ([#4739](https://github.com/Altinn/altinn-auth/issues/4739)) ([0a905aa](https://github.com/Altinn/altinn-auth/commit/0a905aafe57880c4a749d481c20eb888b03395dc))
* Update Dockerfile to publish path to Altinn.Authorization ([6228127](https://github.com/Altinn/altinn-auth/commit/6228127628a909a54b68aff6875fcb7d33a6a717))
* update manifest URL in 01-oci-repository.yaml ([680392e](https://github.com/Altinn/altinn-auth/commit/680392e68a992810ff7960b710ae54e2a879b8e2))
* use correct selector labels for apps ([#4738](https://github.com/Altinn/altinn-auth/issues/4738)) ([61b345c](https://github.com/Altinn/altinn-auth/commit/61b345c0dcff98b615b9f5b384612f76f00f2242))
