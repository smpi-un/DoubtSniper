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
        dotnet-sdk
        # dotnet-sdk_10
        dotnet-sdk_8
        # elmPackages.elm

        flutter # Flutter SDK
        dart # Dart SDK（Flutterに含まれるが明示的に指定）
        gtk3 # GTK3ライブラリ（FlutterのLinuxバックエンドに必要）
        pkg-config # 依存ライブラリの検出に必要
        libglvnd # OpenGLライブラリ
        mesa # Mesa 3Dグラフィックスライブラリ（OpenGL実装）
        libGLU # OpenGLユーティリティライブラリ
        glib # GTKで使用されるライブラリ
        gdk-pixbuf # 画像処理ライブラリ
        libxkbcommon
        cmake
        ninja
        xorg.libX11 # X11ライブラリ
        xorg.libXcursor # カーソル管理
        xorg.libXinerama # マルチモニター対応
        xorg.libXrandr # 画面解像度管理

        chromium # Chrome（FlutterのWebビルドやテスト用）

        # Google Cloud
        google-cloud-sdk
        firebase-tools
        # azure-functions-core-tools
      ];
      shellHook = ''
        echo "F# and elm environment is ready!"
        # flutter clean
        # rm -rf linux/build
        export FLUTTER_LINUX_BACKEND=x11
        export PKG_CONFIG_PATH="${pkgs.gtk3.dev}/lib/pkgconfig:${pkgs.libglvnd.dev}/lib/pkgconfig:${pkgs.glib.dev}/lib/pkgconfig:${pkgs.gdk-pixbuf.dev}/lib/pkgconfig:${pkgs.libxkbcommon.dev}/lib/pkgconfig:$PKG_CONFIG_PATH"
        export LIBGL_DRIVERS_PATH="${pkgs.mesa.drivers}/lib/dri"
        export LD_LIBRARY_PATH="${pkgs.libglvnd}/lib:${pkgs.mesa}/lib:${pkgs.gtk3}/lib:${pkgs.glib}/lib:${pkgs.gdk-pixbuf}/lib:${pkgs.libxkbcommon}/lib:$LD_LIBRARY_PATH"
      '';
    };
  };
}

