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

DIS creates an `ApplicationIdentity` resource per app with the `principalId` in its status. There are two per app: the app itself and the pre-deploy job — both are needed.

```bash
kubectl get applicationidentity -n <namespace> \
  -o jsonpath='{range .items[*]}{.metadata.name}: {.status.principalId}{"\n"}{end}'
```

In the app's `infra/main.tf` (in Azure DevOps), add a `module "rbac_dis_app"` block for these principal IDs, mirroring the existing `module "rbac_platform_app"`. If the app has a Key Vault, also add `Key Vault Secrets User` entries for the DIS principal IDs in the `key_vault_roles` list. Apply the terraform.

### 3. Configure DIS proxy

The DIS cluster has proxies that forward requests to services still running in the platform cluster. Configure the app to route calls to platform-cluster services through the appropriate DIS proxy endpoints.

The app also needs to trust the proxy by adding the trusted proxies configuration. This is read from the `clusterinfo` ConfigMap and exposed as `Altinn__ClusterInfo__TrustedProxies` — see the deployment manifest for how this is wired up.

> Once all services have been migrated to DIS, the proxy endpoints and trusted proxies configuration can be removed.

### 4. Platform Key Vault and App Configuration access (if needed)

If the app reads secrets from the platform Key Vault or uses platform App Configuration, access must be ordered from the platform team. Provide them with the `principalId` values from step 2.

> **Chicken-and-egg:** the DIS managed identity does not exist until after the first deployment. Deploy the app first (step 1), retrieve the principal IDs (step 2), then order access. The app may fail to start until access is granted. Once all services have migrated to DIS, access to the platform Key Vault can be cleaned up.

### 5. App-specific Key Vault access (if needed)

If the app was not covered in step 2, update the Key Vault terraform in Azure DevOps to grant the app's managed identity the appropriate role. Apply the changes.
