{
  description = "Dotnet 10 Devshell";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
  };

  outputs = inputs @ {self, ...}:
    inputs.flake-parts.lib.mkFlake {inherit inputs;} {
      systems = [
        "x86_64-linux"
        "aarch64-linux"
        "aarch64-darwin"
      ];

      perSystem = {pkgs, ...}: let
        dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0_1xx-bin;
      in {
        devShells.default = pkgs.mkShell {
          name = "dotnet-shell";
          meta.description = "Dev environment for .NET 10 Development";

          packages = [
            dotnet-sdk

            pkgs.websocat
          ];

          env = {
            DOTNET_ROOT = "${dotnet-sdk}";
          };

          shellHook = ''
            if [ -z "$IN_NIX_SHELL" ]; then
              export IN_NIX_SHELL=1
              exec ${pkgs.fish}/bin/fish
            fi
          '';
        };
      };
    };
}
