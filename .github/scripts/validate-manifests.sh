#!/usr/bin/env bash
# Renders every kustomize overlay this repo publishes and checks that each syncroot
# overlay selects the environment it is named for.
#
# Everything is validated in full, not just one environment: the pull request workflow
# runs this with no target environment, and an overlay that is only rendered when
# someone dispatches its environment breaks long after the change that broke it.
set -euo pipefail
shopt -s nullglob

APPS_ROOT="${APPS_ROOT:-./src/apps}"

# The two stages inside the manifests artifact, in the order Flux applies them.
OPT_STAGES=(pre-deploy)
STAGES=(pre-deploy deploy)
SYNCROOT_ENVIRONMENTS=(at22 at23 at24 tt02 yt01 prod)
ERRORS=0

fail() {
  echo "::error::$*"
  ERRORS=$((ERRORS + 1))
}

success() {
  echo "$*"
}

# Renders every overlay in a stage directory. base/ is the shared foundation, not an
# overlay, so it is only rendered through the overlays that reference it.
render_overlays() {
  local stage_dir="$1"

  [ -f "${stage_dir}base/kustomization.yaml" ] || fail "Missing ${stage_dir}base/kustomization.yaml"

  local overlay
  for overlay in "${stage_dir}"*/; do
    if [ "$(basename "${overlay}")" = "base" ]; then
      continue
    fi

    if [[ ! -f "${overlay}kustomization.yaml" ]]; then
      fail "Missing ${overlay}kustomization.yaml"
      continue
    fi
    
    if kustomize build "${overlay}" > /dev/null; then
      success "Rendered ${overlay}"
    else
      fail "${overlay} failed to render"
    fi
  done
}

# Every app that ships a manifests/ directory is validated, not just the one currently
# published: an app whose overlays never render is a deploy that breaks the day it is
# wired into the syncroot, long after the change that broke it was merged.
manifest_dirs=("${APPS_ROOT}"/*/manifests/)
[ ${#manifest_dirs[@]} -gt 0 ] || fail "No app manifest directories under ${APPS_ROOT}/*/manifests/"

for manifest_dir in "${manifest_dirs[@]}"; do
  for stage in "${STAGES[@]}"; do
    if [[ ! " ${OPT_STAGES[@]} " =~ " ${stage} " && ! -d "${manifest_dir}${stage}" ]]; then
      fail "Missing ${manifest_dir}${stage}/"
    fi

    if [[ -d "${manifest_dir}${stage}/" ]]; then
      render_overlays "${manifest_dir}${stage}/"
    fi
  done
done

syncroot_dirs=("${APPS_ROOT}"/*/syncroot/)

for syncroot_dir in "${syncroot_dirs[@]}"; do
  for env in "${SYNCROOT_ENVIRONMENTS[@]}"; do
    if ! [ -f "${syncroot_dir}${env}/kustomization.yaml" ]; then
      fail "Missing ${syncroot_dir}${env}/kustomization.yaml"
      continue
    fi
  done

  for dir in "${syncroot_dir}"*/; do
    env="$(basename "${dir}")"
    if [ "${env}" = "base" ]; then
      continue
    fi
    if ! [ -f "${dir}kustomization.yaml" ]; then
      fail "Missing ${dir}kustomization.yaml"
      continue
    fi
    
    rendered="$(kustomize build "${dir}")"
    # Each environment overlay must select its own OCI artifact tag.
    if ! grep -qE "^ *tag: ${env}\$" <<< "${rendered}"; then
      fail "${dir} does not pin the OCIRepository to tag ${env}"
      continue
    fi
  done
done

if [ "${ERRORS}" -ne 0 ]; then
  exit 1
fi
