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

### 2. Grant the DIS service principals their role assignments

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

Where the app has terraform in Azure DevOps (Access Management, Authorization), append the DIS `servicePrincipalId` values to the existing `module "rbac_platform_app"` block in the app's `infra/main.tf`. If the app has a Key Vault, also add `Key Vault Secrets User` entries for the DIS principal IDs in the `key_vault_roles` list. Apply the terraform.

> **Not every app has terraform.** Resource Registry and Authentication have no `infra/main.tf` in Azure DevOps — their role assignments probably have to be added manually (Azure Portal, or a ticket to the platform team), mirroring what the platform-cluster identity already has.

### 3. Configure DIS proxy

The DIS cluster has proxies that forward requests to services still running in the platform cluster. **The app needs no proxy-specific configuration for this**: it keeps calling `http://altinn-<service>.default.svc.cluster.local/...`, exactly as in the platform cluster. In the DIS cluster that name is fronted by an `HTTPRoute` in the `default` namespace that splits the traffic between the legacy cluster (through the service's own nginx pod) and the migrated app in its own namespace.

Those routes and their weights live in [altinn-platform-dis-proxies](https://github.com/Altinn/altinn-platform-dis-proxies), under `dis-to-legacy/`:

| File | What it holds |
|---|---|
| `teams/autorisasjon/<service>/httproute.yaml` | the two backends: the nginx shim, and the migrated app in its DIS namespace. Already filled in for all four of our apps |
| `teams/autorisasjon/<service>/proxy/allowed-clients.yaml` | who may reach the service **through the shim** |
| `envs/<env>/autorisasjon/kustomization.yaml` | the weights, one file per environment, CODEOWNED by autorisasjon |

The weights are relative, not percentages — legacy first, DIS second. Shifting traffic is described in [migration-guide.md](migration-guide.md#phase-3--shift-dis-internal-traffic-from-legacy-to-dis).

The app also needs to trust the proxy by adding the trusted proxies configuration. This is read from the `clusterinfo` ConfigMap and exposed as `Altinn__ClusterInfo__TrustedProxies` — see the deployment manifest for how this is wired up. The ConfigMap lives in the app's `syncroot/base/0*-cluster-info.yaml` and its value is substituted from `${DIS_TRUSTED_PROXIES}` at the cluster level.

**The list must include the APIM address.** External traffic arrives as APIM → Traefik → app, so if APIM is not trusted the app resolves the wrong client IP and anything keyed on it (rate limiting, logging, IP-based checks) is wrong. Verify the effective value per environment, and pay particular attention to `tt02` and `prod`:

```bash
kubectl get configmap -n <namespace> clusterinfo -o jsonpath='{.data.trusted_proxies}'
```

If the APIM address is missing, the value comes from the DIS cluster configuration — ask the platform team to add it.

> Once all services have been migrated to DIS, the proxy endpoints and the DIS proxies in the trusted proxies configuration can be removed. APIM stays in front of the app, so its address must remain trusted.

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
3. Confirm the app can read **certificates** and not only secrets. `Key Vault Secrets User` covers a certificate fetched through the secrets API; an app that uses the certificate API also needs `Key Vault Certificate User`. Check the pod logs after startup for Key Vault authorization errors before calling this done.

> **Chicken-and-egg:** the DIS managed identity does not exist until after the first deployment. Deploy the app first (step 1), retrieve the principal IDs (step 2), then check and order access. Once all services have migrated to DIS, access to the platform Key Vault can be cleaned up.

### 7. Platform-cluster secrets: `accesstoken` and `altinn-appsettings-secret`

Every app mounts two secrets that exist in the platform cluster but are not created by our manifests:

| Secret | Mount |
|---|---|
| `altinn-appsettings-secret` | `/altinn-appsettings` |
| `accesstoken` | `/accesstoken` |

The pod stays in `ContainerCreating` until both exist in the DIS namespace:

```bash
kubectl get secret -n <namespace> accesstoken altinn-appsettings-secret
```

> ⚠️ **These should not be copied.** The target state is that the app reads these values from the platform Key Vault with its own DIS managed identity (step 6) — a copied secret is an unmanaged duplicate that drifts the moment it is rotated in the platform cluster. Copy only as a deliberate stopgap to get the pod started, and remove the copies once managed-identity access works.

If you do copy them as a stopgap:

```bash
for s in accesstoken altinn-appsettings-secret; do
  kubectl --context <platform-cluster> -n default get secret "$s" -o json \
    | jq 'del(.metadata.namespace, .metadata.uid, .metadata.resourceVersion,
               .metadata.creationTimestamp, .metadata.managedFields,
               .metadata.ownerReferences, .metadata.annotations)' \
    | kubectl --context <dis-cluster> -n <namespace> apply -f -
done
```

### 8. Azure App Configuration access (if needed)

If the app reads from platform App Configuration, access can be granted directly via Terraform without involving the platform team.

In the app's `infra/main.tf` (Azure DevOps), add an `App Configuration Data Reader` role assignment for **every** DIS principal ID on the App Configuration instance (`appconfaltinnauth001hub.azconfig.io`) — including the pre-deploy identities, which read configuration too: Access Management needs `am-init`, `am-api` and `am-ffb`. Apply the terraform. Where the app has no terraform, add the assignment manually as in step 2.

### 9. App-specific Key Vault access and secrets (if needed)

If the app has its own Key Vault and it was not covered in step 2, update the Key Vault terraform in Azure DevOps to grant the DIS managed identity the appropriate role. Apply the changes.

**A Key Vault provisioned by DIS starts empty.** Where the app's pre-deploy stage declares a `Vault` resource (Authentication's `authn-api-vault` in `manifests/pre-deploy/base/02-vault.yaml`), DIS creates the vault but no secrets. Every key referenced by the environment's `ExternalSecret` (`manifests/pre-deploy/<env>/01-secrets.yaml`) has to be added to that vault by hand after the first deploy, per environment — the app will not start until they are there. Then confirm the sync:

```bash
kubectl get externalsecret -n <namespace> <name>   # SecretSynced
kubectl get secret -n <namespace> <target name>
```

> **The two sides of a `data` entry use different separators.** `secretKey` is the Kubernetes key the app reads as configuration, so it keeps `__` (`OidcProviders__idporten__ClientSecret`). `remoteRef.key` is the Key Vault secret name, and Key Vault allows only letters, digits and `-` — so it uses `--` (`OidcProviders--idporten--ClientSecret`). A `remoteRef.key` containing `__` cannot exist in Key Vault and will never sync.
