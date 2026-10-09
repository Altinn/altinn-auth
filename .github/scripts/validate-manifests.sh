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

# The two stages inside the manifests artifact, in the order Flux applies them. Each
# syncroot Kustomization selects ./<stage>/<env>.
STAGES=(pre-deploy deploy)

fail() {
  echo "::error::$*"
  exit 1
}

# Renders every overlay in a stage directory. base/ is the shared foundation, not an
# overlay, so it is only rendered through the overlays that reference it.
render_overlays() {
  local stage_dir="$1"

  [ -f "${stage_dir}base/kustomization.yaml" ] || fail "Missing ${stage_dir}base/kustomization.yaml"

  local found=0
  local overlay
  for overlay in "${stage_dir}"*/; do
    if [ "$(basename "${overlay}")" = "base" ]; then
      continue
    fi
    [ -f "${overlay}kustomization.yaml" ] || fail "Missing ${overlay}kustomization.yaml"
    kustomize build "${overlay}" > /dev/null
    echo "Renders: ${overlay}"
    found=1
  done
  [ "${found}" -eq 1 ] || fail "No overlays under ${stage_dir}"
}

# Every app that ships a manifests/ directory is validated, not just the one currently
# published: an app whose overlays never render is a deploy that breaks the day it is
# wired into the syncroot, long after the change that broke it was merged.
manifest_dirs=("${APPS_ROOT}"/*/manifests/)
[ ${#manifest_dirs[@]} -gt 0 ] || fail "No app manifest directories under ${APPS_ROOT}/*/manifests/"

for manifest_dir in "${manifest_dirs[@]}"; do
  for stage in "${STAGES[@]}"; do
    [ -d "${manifest_dir}${stage}" ] || fail "Missing ${manifest_dir}${stage}/"
    render_overlays "${manifest_dir}${stage}/"
  done
done

syncroot_dirs=("${APPS_ROOT}"/*/syncroot/)
[ ${#syncroot_dirs[@]} -gt 0 ] || fail "No app syncroot directories under ${APPS_ROOT}/*/syncroot/"

for syncroot_dir in "${syncroot_dirs[@]}"; do
  for dir in "${syncroot_dir}"*/; do
    env="$(basename "${dir}")"
    if [ "${env}" = "base" ]; then
      continue
    fi
    [ -f "${dir}kustomization.yaml" ] || fail "Missing ${dir}kustomization.yaml"
    rendered="$(kustomize build "${dir}")"
    # base hardcodes placeholders and every overlay is expected to patch them. An
    # overlay that forgets pulls another environment's artifact or renders another
    # environment's manifests, which fails silently in-cluster.
    for stage in "${STAGES[@]}"; do
      grep -qE "^ *path: \./${stage}/${env}\$" <<< "${rendered}" \
        || fail "${dir} does not select ./${stage}/${env}"
    done
    grep -qE "^ *tag: ${env}\$" <<< "${rendered}" \
      || fail "${dir} does not pin the OCIRepository to tag ${env}"
    echo "Selects ${env}: ${dir}"
  done
done
