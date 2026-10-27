variable "TAG" {
  default = "latest"
}

group "default" {
  targets = ["build"]
}

target "build" {
  name = "authorization-${app}"
  matrix = {
    app = ["api"]
  }

  contexts = {
    aspnet-builder = "target:aspnet-builder"
    aspnet-runtime = "target:aspnet-runtime"
  }

  dockerfile = "src/apps/Altinn.Authorization/containers/Dockerfile.${app}"
  platforms  = ["linux/amd64"]
  tags       = ["ghcr.io/altinn/altinn-auth/authorization-${app}:${TAG}"]
}

target "aspnet-builder" {
  dockerfile = "containers/Dockerfile.aspnet-builder"
}

target "aspnet-runtime" {
  dockerfile = "containers/Dockerfile.aspnet-runtime"
}
