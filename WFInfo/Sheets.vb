Imports Google.Apis.Auth.OAuth2
Imports Google.Apis.Services
Imports Google.Apis.Sheets.v4
Imports Google.Apis.Sheets.v4.Data

Class Sheets

    Private cred As GoogleCredential
    Private ReadOnly sheetID = "1uAbqfwBYrcqlWCJad4juankmu9-_UqzBmd7XtrrrwkM"
    Private ReadOnly range = "prices!A1:H1"

    Public Sub New()
        cred = GoogleCredential.FromJson(My.Resources.google_creds).CreateScoped({SheetsService.Scope.SpreadsheetsReadonly})


        Dim test As SheetsService = TestRun()
        Dim response As ValueRange = test.Spreadsheets.Values.Get(sheetID, range).Execute()
        For Each row In response.Values
            For Each cell In row
                Console.WriteLine(cell.ToString())
            Next
        Next
    End Sub

    Public Function TestRun() As SheetsService
        Return New SheetsService(New BaseClientService.Initializer() With
            {
                .HttpClientInitializer = cred,
                .ApplicationName = "WFInfo"
            })
    End Function

End Class
