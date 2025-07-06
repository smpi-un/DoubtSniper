

# frontend
https://grok.com/share/bGVnYWN5_f4305d6d-0fd9-4d38-a139-69dbcb3589ac
## Run Local
### Linux Desktop App
```sh
flutter run -d linux
```
### Web App
```sh
flutter run -d web-server --web-port=8080
```

## Deploy

```sh
flutter build web --dart-define=API_URL=https://doubt-sniper-backend-404704694046.asia-northeast1.run.app
firebase deploy --only hosting
```


# backend
## Run Local
dotnet run -- --server

## Deploy

```sh
gcloud builds submit --config cloudbuild.yaml
```
