variable "TAG" {
  default = "latest"
}

group "default" {
  targets = []
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

  dockerfile-inline = "FROM aspnet-runtime\nENV I_AM_TEST=true"
  platforms  = ["linux/amd64"]
  tags       = ["ghcr.io/altinn/altinn-auth/authorization-${app}:${TAG}"]
}

target "aspnet-builder" {
  dockerfile = "containers/Dockerfile.aspnet-builder"
}

target "aspnet-runtime" {
  dockerfile = "containers/Dockerfile.aspnet-runtime"
}
