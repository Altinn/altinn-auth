# Deploy app to DIS

Apps currently run in the platform cluster. This guide covers onboarding an app to DIS and the per-environment deployment checklist.

## One-time onboarding

> **Namespace:** If the app needs its own namespace in the DIS cluster, order it from the platform team by opening an issue in [altinn-platform](https://github.com/Altinn/altinn-platform/issues) before starting the steps below.
>
> When the platform team handles the request they will:
>
> 1. Create the dedicated namespace
> 2. Create a syncroot in the namespace pointing to the **Altinn OCI registry** (not GitHub OCI)
>
>
> We then publish our FluxCD artifacts to that syncroot. This differs from the default setup where artifacts go to the GitHub OCI registry.

### 1. Add app to publish-syncroot.yml

Add the vertical name to the `vertical` options in [`.github/workflows/publish-syncroot.yml`](../../.github/workflows/publish-syncroot.yml):

```yaml
options:
  - Altinn.AccessManagement
  - Altinn.Authorization
  - Altinn.NewApp          # add this
```

### 2. Add Dockerfile and docker-bake.hcl

Add a `Dockerfile` and `docker-bake.hcl` at `src/apps/<NewApp>/`. The bake file references the shared base images in [`containers/`](../../containers/) as `aspnet-builder` and `aspnet-runtime` targets — the app-specific Dockerfile content is minimal. Use the existing apps as reference.

### 3. Add syncroot

Create `src/apps/<NewApp>/syncroot/` with a `base/` directory containing:

- `01-oci-repository.yaml` — `OCIRepository` pointing to the app's manifest OCI image (tag set to `fake` in base; patched per env)
- `02-deployment.yaml` — `Kustomization` for the deploy stage (and `02-pre-deployment.yaml` + `03-deployment.yaml` if the app has a pre-deploy job)
- `kustomization.yaml` — lists the above resources

Then add a `<env>/kustomization.yaml` overlay for each environment (`at22`, `at23`, `at24`, `yt01`, `tt02`, `prod`). Each overlay patches the OCI tag to the environment name and the Kustomization path to `./deploy/<env>` (and `./pre-deploy/<env>` if applicable).

See [`Altinn.Authorization/syncroot/`](../../src/apps/Altinn.Authorization/syncroot/) (no pre-deploy) or [`Altinn.AccessManagement/syncroot/`](../../src/apps/Altinn.AccessManagement/syncroot/) (with pre-deploy) for reference.

### 4. Add manifests

Create `src/apps/<NewApp>/manifests/deploy/` with a `base/` directory containing the Kubernetes resource declarations (`ApplicationIdentity`, `Deployment`, `Service`, `HTTPRoute`, etc.) and a `kustomization.yaml` that lists them. Add a `<env>/kustomization.yaml` overlay for each environment with any environment-specific patches (image tag, config, replica count).

If the app has a pre-deploy job (e.g. DB migrations), add `manifests/pre-deploy/` with the same base/overlay structure.

See [`Altinn.Authorization/manifests/`](../../src/apps/Altinn.Authorization/manifests/) or [`Altinn.AccessManagement/manifests/`](../../src/apps/Altinn.AccessManagement/manifests/) for reference.

---

## Per-environment deployment checklist

### 1. Deploy app to DIS environment

Deploy the app, then confirm the pod is running:

```bash
kubectl get pods -n <namespace>
```

Wait for `Running` status before continuing.

### 2. Update RBAC terraform for DIS service principals

DIS creates an `ApplicationIdentity` resource per app. After the first deployment the `servicePrincipalId` is populated in its status. There are two per app: the app itself and the pre-deploy job — both are needed.

```bash
kubectl get applicationidentity -n <namespace> <name> -o yaml
# look for: status.servicePrincipalId
```

To list all identities in the namespace at once:

```bash
kubectl get applicationidentity -n <namespace> \
  -o jsonpath='{range .items[*]}{.metadata.name}: {.status.servicePrincipalId}{"\n"}{end}'
```

Append the DIS `servicePrincipalId` values to the existing `module "rbac_platform_app"` block in the app's `infra/main.tf` (Azure DevOps). If the app has a Key Vault, also add `Key Vault Secrets User` entries for the DIS principal IDs in the `key_vault_roles` list. Apply the terraform.

### 3. Configure DIS proxy

The DIS cluster has proxies that forward requests to services still running in the platform cluster. Configure the app to route calls to platform-cluster services through the appropriate DIS proxy endpoints.

The app also needs to trust the proxy by adding the trusted proxies configuration. This is read from the `clusterinfo` ConfigMap and exposed as `Altinn__ClusterInfo__TrustedProxies` — see the deployment manifest for how this is wired up.

> Once all services have been migrated to DIS, the proxy endpoints and trusted proxies configuration can be removed.

**Platform-to-DIS direction:** there is a matching proxy on the platform-cluster side — a feature-flagged nginx proxy in each service's platform-cluster Helm chart ([altinn-studio-ops](https://github.com/Altinn/altinn-studio-ops)) that lets platform-cluster callers reach DIS-hosted services without changing their configured endpoints. This proxy must be deployed before shifting internal traffic in phase 2 of the migration. See feature [#5001](https://github.com/Altinn/altinn-auth/issues/5001) for the per-service proxy tasks and the general migration runbook.

### 4. Database firewall (if the app uses a database)

DIS runs in its own network. If the app connects to a PostgreSQL database, the database's firewall must allow inbound connections from the DIS core subnet.

1. Check whether the DIS core subnet is already listed in the database's firewall rules (Azure Portal → PostgreSQL flexible server → Networking).
2. If it is not listed, open an issue in [altinn-platform](https://github.com/Altinn/altinn-platform/issues) requesting a firewall opening for the DIS core subnet. The app will fail to connect until this is in place.

### 5. Storage Account firewall (if the app uses Storage queues or blobs)

The same applies to Azure Storage accounts. If the app reads from or writes to a Storage Account (queues, blobs), the DIS core subnet must be allowed in the storage account's firewall.

1. Check whether the DIS core subnet is listed under the storage account's networking rules (Azure Portal → Storage account → Networking).
2. If it is not listed, open an issue in [altinn-platform](https://github.com/Altinn/altinn-platform/issues) requesting the network rule to be added.

### 6. Platform Key Vault access (if needed)

If the app reads secrets from the platform Key Vault, the DIS workload identity must have the `Key Vault Secrets User` role on that Key Vault.

1. Check whether the principal IDs from step 2 already have this role (Azure Portal → Key Vault → Access control (IAM)).
2. If they do not, create a ticket to the platform team providing the `principalId` values. The app may fail to start until access is granted.

> **Chicken-and-egg:** the DIS managed identity does not exist until after the first deployment. Deploy the app first (step 1), retrieve the principal IDs (step 2), then check and order access. Once all services have migrated to DIS, access to the platform Key Vault can be cleaned up.

### 7. Azure App Configuration access (if needed)

If the app reads from platform App Configuration, access can be granted directly via Terraform without involving the platform team.

In the app's `infra/main.tf` (Azure DevOps), add an `App Configuration Data Reader` role assignment for the DIS principal IDs on the App Configuration instance. Apply the terraform.

### 8. App-specific Key Vault access (if needed)

If the app has its own Key Vault and it was not covered in step 2, update the Key Vault terraform in Azure DevOps to grant the DIS managed identity the appropriate role. Apply the changes.
