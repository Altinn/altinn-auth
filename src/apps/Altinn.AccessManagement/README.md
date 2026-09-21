# Altinn Access Management

TL;DR:

- Administers rights for apps, resources, and API schemes.
- DIS publishes under `accessmanagement/` and deploys into `product-access-management`.
- DIS telemetry uses the shared OTLP collector over gRPC.

## Getting started

Open `Altinn.AccessManagement.sln` and run the `Altinn.AccessManagement` project
(the browser opens the Swagger UI automatically).

Or from the command line:

```bash
dotnet run --project src/apps/Altinn.AccessManagement/src/Altinn.AccessManagement
```

## Local development

Local setup — containerised PostgreSQL (`just dev`), user secrets, and the
database bootstrap — is in the
[repository README](../../../README.md#local-development-environment). Access
Management uses the `authorizationdb` database (roles `platform_authorization` /
`platform_authorization_admin`).

## DIS publishing and telemetry

The [Flux publishing workflow](../../../.github/workflows/publish-flux-artifacts.yml)
publishes `accessmanagement/altinn-auth:main` from `deploy/`, then
`accessmanagement/syncroot:<environment>` from `flux/syncroot/`. The syncroot selects
`./environments/<environment>` in the app artifact and targets
`product-access-management`. The product slug is `access-management`; the publishing
identity and syncroot name use the alphanumeric `accessmanagement`. The workflow
only publishes from `main` and requires an app overlay for the selected environment;
currently only `at22` exists.

The `at22` overlay consumes [deploy/app](deploy/app), which contains the application
deployment. [deploy/base](deploy/base) is a separate demo resource set containing an
nginx deployment, identity, service, and route; it is not included by `at22`.

The app sets `OTEL_EXPORTER_OTLP_ENDPOINT` to
`http://otel-collector.monitoring.svc.cluster.local:4317`, uses `grpc`, and identifies
itself as `access-management` through `OTEL_SERVICE_NAME`. `POD_UID` comes from the
Downward API and supplies `k8s.pod.uid` in `OTEL_RESOURCE_ATTRIBUTES`. The `at22`
overlay sets `OTEL_TRACES_SAMPLER_ARG` to `1`, matching Dialogporten's test overlays.

Apply the publishing-identity change in
[altinn-platform #4115](https://github.com/Altinn/altinn-platform/pull/4115), publish
these artifacts, then apply [core #322](https://github.com/dis-way/core/pull/322).
Those infrastructure changes tear down and recreate the old product resources;
they do not migrate existing workloads. See the
[DIS product identity decision](docs/adr/0001-dis-product-identity.md) for the naming contract.

Validate locally with `./.github/scripts/validate-manifests.sh` and
`actionlint .github/workflows/publish-flux-artifacts.yml` from the repository root.

DIS documentation drafted with Codex; human reader: pending.
