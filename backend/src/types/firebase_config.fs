module FirebaseConfig

open Tomlyn
open Tomlyn.Model
open System.IO

type FirebaseConfig = {
    ApiKey: string
    AuthDomain: string
    ProjectId: string
    StorageBucket: string
    MessagingSenderId: string
    AppId: string
    MeasurementId: string
}

let readFirebaseConfig (filePath: string) : FirebaseConfig =
    let tomlContent = File.ReadAllText(filePath)
    let model = Toml.Parse(tomlContent).ToModel()
    let firebaseTable = model.["firebase"] :?> TomlTable
    {
        ApiKey = firebaseTable.["api_key"].ToString()
        AuthDomain = firebaseTable.["auth_domain"].ToString()
        ProjectId = firebaseTable.["project_id"].ToString()
        StorageBucket = firebaseTable.["storage_bucket"].ToString()
        MessagingSenderId = firebaseTable.["messaging_sender_id"].ToString()
        AppId = firebaseTable.["app_id"].ToString()
        MeasurementId = firebaseTable.["measurement_id"].ToString()
    }


/// Realtime DatabaseのURLを生成します。
/// 形式: https://<project-id>-default-rtdb.firebaseio.com
let getRealtimeDatabaseUrl (config: FirebaseConfig) : string =
    if System.String.IsNullOrEmpty(config.ProjectId) then
        failwith "ProjectId is empty or null"
    sprintf "https://%s-default-rtdb.firebaseio.com" config.ProjectId

/// FirestoreのREST APIベースURLを生成します。
/// 形式: https://firestore.googleapis.com/v1/projects/<project-id>/databases/(default)/documents
let getFirestoreUrl (config: FirebaseConfig) : string =
    if System.String.IsNullOrEmpty(config.ProjectId) then
        failwith "ProjectId is empty or null"
    sprintf "https://firestore.googleapis.com/v1/projects/%s/databases/(default)/documents" config.ProjectId