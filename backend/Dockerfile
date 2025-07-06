# --- ビルドステージ ---
# .NET SDK イメージをベースにする (F# が含まれているバージョンを選択)
# プロジェクトで使用している .NET のバージョンに合わせてください (例: 8.0)
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# プロジェクトファイル (.fsproj) をコピーして依存関係を復元
# backend/src に .fsproj があると仮定
COPY *.fsproj ./
RUN dotnet restore *.fsproj

# ソースコード全体をコピー
COPY . .

# アプリケーションをビルドして発行 (リリースモード、自己完結型ではない)
# backend/src/YourApp.fsproj がメインのプロジェクトファイルだと仮定
WORKDIR /app
RUN dotnet publish -c Release -o /app/publish --no-self-contained ./fsharp.fsproj

# --- 実行ステージ ---
# ASP.NET Core ランタイムイメージをベースにする (より軽量)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# ビルドステージから発行されたファイルのみをコピー
COPY --from=build /app/publish .
COPY config.toml /app/config.toml
COPY firebase_config.toml /app/firebase_config.toml
COPY data /app/data


# アプリケーションがリッスンするポート (Cloud Run は PORT 環境変数で指定されたポートを使う)
# デフォルトは 8080 だが、ASP.NET Core は通常 80 または 443 をリッスンしようとする
# 環境変数 ASPNETCORE_URLS で明示的に指定するか、
# Cloud Run が提供する PORT 環境変数を尊重するようにアプリケーションを構成する
# Kestrel はデフォルトで PORT 環境変数を尊重するので、通常は EXPOSE だけで良い
EXPOSE 8080

# コンテナ起動時に実行されるコマンド
# backend/src/YourApp.dll のように、発行された DLL ファイル名を指定
ENTRYPOINT ["dotnet", "fsharp.dll", "--server"]
