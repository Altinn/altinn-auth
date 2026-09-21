# DIS product identity for Access Management

TL;DR (drafted with Codex, GPT-6; human reader: pending):

- Keep the DIS product slug `access-management` and namespace `product-access-management`.
- Use the alphanumeric syncroot name `accessmanagement` and publish under `accessmanagement/`.
- Configure the app's OTLP environment variables using Dialogporten's DIS pattern.

Status: proposed. Date: 2026-09-21.

The product rename is tracked by
[Altinn/altinn-platform #4114](https://github.com/Altinn/altinn-platform/issues/4114).
The platform's syncroot configuration accepts alphanumeric names. The core product
module needs an independent `syncroot_name` so the registry prefix can differ from
the product slug without changing the namespace or its access scopes.

Use `accessmanagement/syncroot:<environment>` for the bootstrap and
`accessmanagement/altinn-auth:main` for application manifests. Keep the existing
application name `access-management`. The publisher continues to publish the app
artifact before the syncroot and validates that the selected app overlay exists.

Use Dialogporten's shared collector endpoint, gRPC protocol, and pod UID resource
attribute. Set `OTEL_SERVICE_NAME` to the existing application name and put the
test sampling argument in the `at22` overlay.

Coordinate this change with [altinn-platform #4115](https://github.com/Altinn/altinn-platform/pull/4115)
and [core #322](https://github.com/dis-way/core/pull/322). The infrastructure changes
use teardown and recreation. Provision the new publisher, publish these artifacts,
and then switch core to the new syncroot. This does not migrate workloads or verify
telemetry delivery in a running cluster.
