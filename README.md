

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
firebase deploy --only hosting
```


# backend

## Deploy

```sh
gcloud builds submit --config cloudbuild.yaml
```
