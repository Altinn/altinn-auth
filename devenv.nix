{
  pkgs,
  lib,
  config,
  inputs,
  ...
}:
let
  dotnet =
    let
      combined =
        with pkgs.dotnetCorePackages;
        combinePackages [
          sdk_10_0
          sdk_9_0
          sdk_8_0
        ];

      copied = pkgs.runCommand "dotnet" { } ''
        mkdir -p "$out"/share/dotnet
        cp -aL "${combined}"/share/dotnet/. "$out"/share/dotnet/

        cp -aL "${combined}"/nix-support "$out"/nix-support

        mkdir -p "$out"/bin
        ln -s "$out"/share/dotnet/dotnet "$out"/bin/dotnet
      '';
    in
    copied;
in
{
  # https://devenv.sh/packages/
  packages = [
    pkgs.prek
    pkgs.docker-compose
    pkgs.pnpm
    pkgs.just
    pkgs.powershell
    pkgs.nodejs
  ];

  languages.dotnet.enable = true;
  languages.dotnet.package = dotnet;
}
