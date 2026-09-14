#!/usr/bin/env bash
# Renders every kustomize overlay this repo publishes and checks that each syncroot
# overlay selects the environment it is named for.
#
# Everything is validated in full, not just one environment: the app manifests
# artifact carries a single tag that every environment reads, so a push made for one
# environment ships every other environment's overlay along with it.
set -euo pipefail
shopt -s nullglob

APPS_ROOT="${APPS_ROOT:-./src/apps}"
SYNCROOT_DIR="${SYNCROOT_WORKDIR:-./flux/syncroot}"

fail() {
  echo "::error::$*"
  exit 1
}

# Every app that ships a deploy/ directory is validated, not just the one currently
# published: an app whose overlays never render is a deploy that breaks the day it is
# wired into the syncroot, long after the change that broke it was merged.
app_dirs=("${APPS_ROOT}"/*/deploy/)
[ ${#app_dirs[@]} -gt 0 ] || fail "No app deploy directories under ${APPS_ROOT}/*/deploy/"

for app_dir in "${app_dirs[@]}"; do
  [ -f "${app_dir}base/kustomization.yaml" ] || fail "Missing ${app_dir}base/kustomization.yaml"

  app_overlays=("${app_dir}"environments/*/)
  [ ${#app_overlays[@]} -gt 0 ] || fail "No overlays under ${app_dir}environments/"

  for overlay in "${app_overlays[@]}"; do
    [ -f "${overlay}kustomization.yaml" ] || fail "Missing ${overlay}kustomization.yaml"
    kustomize build "${overlay}" > /dev/null
    echo "Renders: ${overlay}"
  done
done

for dir in "${SYNCROOT_DIR}"/*/; do
  env="$(basename "${dir}")"
  if [ "${env}" = "base" ]; then
    continue
  fi
  [ -f "${dir}kustomization.yaml" ] || fail "Missing ${dir}kustomization.yaml"
  rendered="$(kustomize build "${dir}")"
  # base hardcodes a path and every overlay is expected to patch it. An overlay that
  # forgets renders another environment's manifests, which fails silently in-cluster.
  grep -qE "^ *path: \./environments/${env}\$" <<< "${rendered}" \
    || fail "${dir} does not select ./environments/${env}"
  echo "Selects ./environments/${env}: ${dir}"
done
