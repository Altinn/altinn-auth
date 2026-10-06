# Changelog

## [0.2.0](https://github.com/Altinn/altinn-auth/compare/Altinn.Authorization.ABAC-v0.1.1...Altinn.Authorization.ABAC-v0.2.0) (2026-10-06)


### ⚠ BREAKING CHANGES

* **#4322:** Altinn.Common.PEP and Altinn.Authorization.ABAC no longer target net8.0. Consumers must be on .NET 9 or later.

### Bug Fixes

* **#4322:** drop net8.0 from published packages, embed symbols and reproducible builds ([#4323](https://github.com/Altinn/altinn-auth/issues/4323)) ([99e9396](https://github.com/Altinn/altinn-auth/commit/99e93960abaca3adf6b0f8771653c261a4b2a689))
* clear S2259 + S2955 null-deref findings ([#3150](https://github.com/Altinn/altinn-auth/issues/3150)) ([c24e8f0](https://github.com/Altinn/altinn-auth/commit/c24e8f0b66bf4d9444798b2610160b212961bbfc))
* combine rule decisions per the XACML 3.0 combining algorithm tables ([#3901](https://github.com/Altinn/altinn-auth/issues/3901)) ([b79b15c](https://github.com/Altinn/altinn-auth/commit/b79b15c2a6d89429efc7e72c114da619e82fff5a))

## [0.1.1](https://github.com/Altinn/altinn-auth/compare/Altinn.Authorization.ABAC-v0.1.0...Altinn.Authorization.ABAC-v0.1.1) (2025-11-04)


### Bug Fixes

* handle ValidateDecisionResult when user has no authlevel-claim ([#1272](https://github.com/Altinn/altinn-auth/issues/1272)) ([e5c204e](https://github.com/Altinn/altinn-auth/commit/e5c204e86ebebc2d08311c693e428553ca4af1cd))
* move package config to ABAC dir ([#1635](https://github.com/Altinn/altinn-auth/issues/1635)) ([ec9f74a](https://github.com/Altinn/altinn-auth/commit/ec9f74ad3c2e4d8b6986a249f94b4e0341efb741))

## [0.1.0](https://github.com/Altinn/altinn-auth/compare/Altinn.Authorization.ABAC-v0.0.8...Altinn.Authorization.ABAC-v0.1.0) (2025-09-18)


### Features

* prepare for release ([#1261](https://github.com/Altinn/altinn-auth/issues/1261)) ([b0572fb](https://github.com/Altinn/altinn-auth/commit/b0572fb4841112cea44511932204475f6ab20824))
