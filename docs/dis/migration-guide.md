# Migration guide: moving a service from platform to DIS

This guide covers the full lifecycle of migrating a running service from the platform cluster to DIS. It picks up after the app is already deployed to DIS (see [deploy-app-to-DIS.md](deploy-app-to-DIS.md)) and walks through the traffic shift and final cutover.

![Architecture overview](dis_and_platform.png)

**Current state:** APIM routes all traffic to the platform cluster Traefik. Apps on DIS use proxy services to forward inter-service calls to apps still running on the platform cluster. DIS accesses shared infrastructure (platform Key Vault, App Configuration) via RBAC; databases and storage via an open firewall rule.

---

## Overview

| Phase | Goal |
|-------|------|
| 1. Deploy and verify | App running on DIS, all infrastructure in place |
| 2. Shift traffic platform → DIS | Gradually move incoming traffic to the DIS instance |
| 3. Remove DIS proxy forwarding | Stop the DIS instance from forwarding calls to platform |
| 4. Switch backend in APIM | Cut API Management over to the DIS endpoint |
| 5. Remove app from platform | Decommission the platform-cluster deployment |

---

## Phase 1 — Deploy app to DIS and verify

Follow [deploy-app-to-DIS.md](deploy-app-to-DIS.md) for the full per-environment checklist. Before continuing to phase 2, confirm:

- [ ] Pod is Running: `kubectl get pods -n <namespace>`
- [ ] Health endpoint responds: `GET /health/` returns 200
- [ ] RBAC terraform applied; DIS managed identity has the required role assignments
- [ ] DIS proxy endpoints configured in `environment.env`
- [ ] Platform Key Vault / App Configuration access granted
- [ ] Smoke test passes in the target environment

---

## Phase 2 — Shift traffic from platform cluster to DIS (0 % → 100 %)

Traffic comes from two distinct directions and must be handled separately:

- **External/ingress traffic** arrives via APIM → platform Traefik → app. This is shifted at the APIM level (see below).
- **Internal traffic** comes from other platform-cluster apps calling this service by its in-cluster DNS name (e.g. `altinn-access-management.default.svc.cluster.local`). This is shifted via the **platform-cluster proxy** — a feature-flagged nginx proxy that sits in front of the app's Kubernetes Service and gradually routes calls to the DIS-hosted instance. See feature [#5001](https://github.com/Altinn/altinn-auth/issues/5001) for how the proxy works and the per-app tasks.

### 2a — Internal traffic (platform-cluster proxy)

Before shifting any external traffic, deploy the platform-side proxy for this service so internal callers keep working throughout the migration:

- [ ] Merge and deploy the proxy chart change with `proxy.<env>.enabled: true` — verify the proxy pod is healthy and can reach the DIS upstream directly
- [ ] Set `proxy.<env>.routeInternal: true` — the app's Service selector now points at the proxy pods; internal callers are hijacked. At `weight.dis: 0` (the default) all traffic loops back locally — this is a no-op for callers, just proves the switch works
- [ ] Raise `proxy.<env>.weight.dis` gradually (e.g. 10 → 50 → 100), watching the service at each step — this is what actually routes internal calls to DIS

> Roll back by setting `weight.dis` back to `0` (or `routeInternal` back to `false` to fully remove the proxy from the call path). The proxy does not need to be undeployed — `enabled: true` with `routeInternal: false` is harmless.

### 2b — External traffic (APIM)

Once internal traffic is stable at `weight.dis: 100`, shift external traffic at the APIM level.

APIM currently routes all traffic to the platform cluster Traefik. APIM also has a backend registered for the DIS Traefik — traffic weighting is adjusted there.

**Suggested increments:** 10 % → 25 % → 50 % → 75 % → 100 %

For each increment:

- [ ] Update the APIM backend weight to send `<weight> %` to the DIS Traefik and `<100 − weight> %` to the platform Traefik
- [ ] Apply the APIM change
- [ ] Monitor error rate and latency for the DIS instance
- [ ] Confirm no increase in errors before continuing to the next increment

> Stop and roll back (set DIS weight back to 0 % in APIM) if error rate rises or latency degrades.

Once weight reaches 100 %:

- [ ] All traffic is handled by DIS Traefik; platform instance is no longer receiving requests
- [ ] Let it run at 100 % for at least one full business day before proceeding

---

## Phase 3 — Remove DIS proxy forwarding to platform (100 % → 0 %)

While services were being migrated to DIS, the DIS cluster used proxy endpoints to forward calls to services still running on the platform cluster. Now that this service is live on DIS, reduce the DIS-to-platform proxy forwarding.

> This step applies per service, as each upstream dependency migrates to DIS. Remove all proxy forwarding for a given service only after that service itself has completed phase 2.

- [ ] Remove or disable the DIS proxy endpoint(s) for this service in `environment.env` for the relevant environment(s)
- [ ] Deploy the config change
- [ ] Verify the service continues to function without the proxy
- [ ] Once all services have migrated to DIS, the trusted proxies configuration (`Altinn__ClusterInfo__TrustedProxies`) can also be removed from the deployment manifests

---

## Phase 4 — Switch backend in APIM

After phase 2, APIM routes 100 % to DIS Traefik via weighted routing. This phase removes the platform backend from the APIM routing config entirely and sets DIS as the sole backend.

- [ ] Remove the platform Traefik backend weight from APIM (or set to 0 % and clean up)
- [ ] Set DIS Traefik as the sole backend for the service in APIM
- [ ] Apply the APIM change
- [ ] Verify API calls succeed end-to-end through APIM → DIS Traefik → DIS app

---

## Phase 5 — Remove app from platform cluster

Once all traffic is confirmed flowing through DIS and APIM points to DIS:

- [ ] Set `proxy.<env>.removeApp: true` in the platform-cluster Helm chart (requires `routeInternal: true` and `weight.dis: 100` already in place) — this removes the app's Deployment/DaemonSet and switches Traefik's IngressRoute to target the proxy Service in one atomic deploy, with no manual manifest deletion needed
- [ ] Verify the app continues to serve traffic after the removal (internal callers via the proxy → DIS, external callers via DIS Traefik)
- [ ] Remove the platform backend from APIM (optional cleanup)
- [ ] Revoke platform-cluster RBAC and managed-identity role assignments for the service (Terraform)
- [ ] Clean up platform Key Vault access for the service's managed identity (once all services have migrated, platform Key Vault entries for DIS identities can be removed)
- [ ] Update the per-environment deployment checklist in the epic to mark this environment done

---

## Rollback

At any phase before the platform deployment is removed (phase 5), rollback is:

1. Set the DIS backend weight back to 0 % in APIM (phase 2 rollback) — platform Traefik handles all traffic again
2. Restore the platform Traefik as sole APIM backend (phase 4 rollback)

After phase 5 is complete, rollback requires redeploying to the platform cluster.
