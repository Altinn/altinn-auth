variable "TAG" {
  default = "latest"
}

group "default" {
  targets = ["build"]
}

target "build" {
  name = "access-management-${app}"
  matrix = {
    app = ["api", "ffb"]
  }

  contexts = {
    aspnet-builder = "target:aspnet-builder"
    aspnet-runtime = "target:aspnet-runtime"
  }

  dockerfile = "src/apps/Altinn.AccessManagement/containers/Dockerfile.${app}"
  platforms  = ["linux/amd64"]
  tags       = ["ghcr.io/altinn/altinn-auth/access-management-${app}:${TAG}"]
}

target "aspnet-builder" {
  dockerfile = "containers/Dockerfile.aspnet-builder"
}

target "aspnet-runtime" {
  dockerfile = "containers/Dockerfile.aspnet-runtime"
}
