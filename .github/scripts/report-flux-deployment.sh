#!/usr/bin/env bash
set -euo pipefail

# The event file is data; never interpolate the dispatch payload into shell code.
if [[ "$GITHUB_EVENT_NAME" != repository_dispatch ]]; then
  echo '::error::Expected a repository_dispatch event.'
  exit 1
fi

if ! jq -e '
  .client_payload as $event |
  $event.involvedObject.kind == "Kustomization" and
  $event.involvedObject.name == "deploy" and
  .action == ("Kustomization/deploy." + $event.involvedObject.namespace)
' "$GITHUB_EVENT_PATH" >/dev/null; then
  echo '::error::Dispatch does not identify a final deploy Kustomization.'
  exit 1
fi

namespace=$(jq -r '.client_payload.involvedObject.namespace' "$GITHUB_EVENT_PATH")
case "$namespace" in
  product-access-management) app=Altinn.AccessManagement; slug=access-management ;;
  product-authentication) app=Altinn.Authentication; slug=authentication ;;
  product-authorization) app=Altinn.Authorization; slug=authorization ;;
  product-resource-registry) app=Altinn.ResourceRegistry; slug=resource-registry ;;
  *) echo '::error::Unknown application namespace.'; exit 1 ;;
esac

outcome=$(jq -r '[.client_payload.severity, .client_payload.reason] | join("/")' "$GITHUB_EVENT_PATH")
case "$outcome" in
  info/ReconciliationSucceeded) desired_state=success ;;
  error/BuildFailed|error/ValidationFailed|error/HealthCheckFailed|error/PruneFailed|error/ReconciliationFailed|error/ArtifactFailed)
    desired_state=failure ;;
  *) echo 'Ignoring event that does not report a reconciliation outcome.'; exit 0 ;;
esac

environment=$(jq -er '.client_payload.metadata.env | strings' "$GITHUB_EVENT_PATH")
if [[ ! "$environment" =~ ^[a-z0-9]+$ ]] ||
   [[ ! -f "src/apps/$app/syncroot/$environment/kustomization.yaml" ]]; then
  echo '::error::Expected metadata.env to identify a configured application environment.'
  exit 1
fi

# originRevision is read from `org.opencontainers.image.revision` of the OCI artifact.
origin=$(jq -er '.client_payload.metadata.originRevision | strings' "$GITHUB_EVENT_PATH")
if [[ ! "$origin" =~ ^([^[:space:]]+@sha1:)?([[:xdigit:]]{40})$ ]]; then
  echo '::error::Expected originRevision to contain a full Git SHA, optionally prefixed with <ref>@sha1:.'
  exit 1
fi
sha=${BASH_REMATCH[2],,}
target="$slug/$environment"

# Only complete the newest deployment request for this environment. An older
# revision's delayed callback must not deactivate a newer successful deployment.
deployment=$(gh api --method GET "repos/$GITHUB_REPOSITORY/deployments" \
  -f environment="$target" -f task=deploy -f per_page=1 --jq '.[0] // empty')
if [[ -z "$deployment" ]]; then
  echo "No deployment exists for $target; nothing to update."
  exit 0
fi
if ! jq -e --arg sha "$sha" --arg target "$target" '
  .sha == $sha and .environment == $target and .task == "deploy"
' <<< "$deployment" >/dev/null; then
  echo "Ignoring callback that does not match the newest deployment for $target."
  exit 0
fi

id=$(jq -er '.id | select(type == "number" and . > 0 and floor == .)' <<< "$deployment")
state=$(gh api "repos/$GITHUB_REPOSITORY/deployments/$id/statuses" \
  --jq '.[0].state // ""')
case "$state" in
  ''|queued|pending|in_progress|success|failure|error) ;;
  *) echo "Deployment $id already has status $state; nothing to update."; exit 0 ;;
esac
if [[ "$state" == "$desired_state" ]]; then
  echo "Deployment $id already has status $state; nothing to update."
  exit 0
fi

if [[ "$desired_state" == success ]]; then
  description='Flux successfully reconciled the application deployment.'
else
  reason=${outcome#error/}
  description="Flux could not reconcile the application deployment: $reason."
fi

gh api --method POST "repos/$GITHUB_REPOSITORY/deployments/$id/statuses" \
  -f state="$desired_state" \
  -f description="$description" \
  -f log_url="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID" \
  --silent
echo "Marked deployment $id ($target at $sha) $desired_state."
