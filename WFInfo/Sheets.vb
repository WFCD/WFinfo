Imports Google.Apis.Auth.OAuth2
Imports Google.Apis.Services
Imports Google.Apis.Sheets.v4
Imports Google.Apis.Sheets.v4.Data

Class Sheets

    Private cred As GoogleCredential
    Private ReadOnly sheetID = "1uAbqfwBYrcqlWCJad4juankmu9-_UqzBmd7XtrrrwkM"
    Private service As SheetsService

    Public Sub New()
        cred = GoogleCredential.FromJson(My.Resources.google_creds).CreateScoped({SheetsService.Scope.SpreadsheetsReadonly})
        service = New SheetsService(New BaseClientService.Initializer() With
        {
            .HttpClientInitializer = cred,
            .ApplicationName = "WFInfo"
        })

    End Sub

    Public Function GetSheet(range As String) As IList(Of IList(Of Object))
        Dim response As ValueRange = service.Spreadsheets.Values.Get(sheetID, range).Execute()
        Return response.Values
    End Function
End Class
