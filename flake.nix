{
  description = "F# development environment using Nix Flakes";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs }: 
  let 
    system = "x86_64-linux"; # 適宜変更
    pkgs = import nixpkgs { inherit system; };
  in {
    devShells.${system}.default = pkgs.mkShell {
      packages = with pkgs; [
        # dotnet-sdk
        dotnet-sdk_10
        elmPackages.elm
      ];
      shellHook = ''
        echo "F# and elm environment is ready!"
      '';
    };
  };
}

