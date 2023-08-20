namespace rvinowise.twitter

open System
open System.Collections.Generic
open System.Configuration
open System.IO
open System.Text.RegularExpressions
open Google.Apis.Auth.OAuth2
open Google.Apis.Services
open Microsoft.Extensions.Configuration
open OpenQA.Selenium
open SeleniumExtras.WaitHelpers
open Xunit
open canopy.classic
open Dapper
open FSharp.Data

open Google.Apis.Sheets.v4



module Export_scores_to_googlesheet =
    
    let _scopes = [SheetsService.Scope.Spreadsheets]
    let _applicationName = "web-bot"
    let _spreadsheetId = Settings.googlesheet_twitter_score
    let stream = new FileStream(
        "google_api_secret.json", 
        FileMode.Open, FileAccess.Read
    ) 
        
    let credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
    let service = new SheetsService(
        BaseClientService.Initializer(
            HttpClientInitializer = credential, ApplicationName = _applicationName 
        )
    )
    
    
    let clean_sheet sheet_id =
        let range = "twitter_score!A1:Y";
        let dataValueRange = Data.ValueRange()
        dataValueRange.Range <- range
        updateData.Add(dataValueRange);
        service.Spreadsheets.Values.Clear(requestBody, _spreadsheetId).Execute();
    
    let input_scores_to_sheet () =
        let data: IList<IList<obj>> = ([|
                [|"a1":>obj;"b1":>obj;"c1":>obj|] :> IList<_>;
                [|"a2":>obj;"b2":>obj;"c2":>obj|] :> IList<_>;
                [|"a3":>obj;"b3":>obj;"c3":>obj|] :> IList<_>;
            |] :> IList<_>)
        let range = "twitter_score!A1:Y";
        let valueInputOption = "USER_ENTERED";
        let updateData = List<Data.ValueRange>();
        let dataValueRange = Data.ValueRange()
        dataValueRange.Range <- range
        dataValueRange.Values <- data
        updateData.Add(dataValueRange);

        let requestBody = Data.BatchUpdateValuesRequest();
        requestBody.ValueInputOption <- valueInputOption;
        requestBody.Data <- new List<Data.ValueRange>(dataValueRange)

        let response = service.Spreadsheets.Values.BatchUpdate(requestBody, _spreadsheetId).Execute();

        response
    
    let update_googlesheet_with_last_scores
        sheet_id
        scores
        =
        clean_sheet sheet_id  
        input_scores_to_sheet sheet_id  
    
    
    [<Fact>]
    let ``try update_googlesheet_with_last_scores``() =
        update_googlesheet_with_last_scores
            Settings.googlesheet_twitter_score
            
