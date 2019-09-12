Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class JSONsettings
    Private job As JObject
    Private ReadOnly appData As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Private ReadOnly settings_path As String = Path.Combine(appData, "WFInfo\settings.json")
    Public delay As Integer = 1000
    Public delayDuplicate As Integer = 10000

    Public Sub New()
        Try
            job = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(settings_path))
            delay = job("delay").ToObject(Of Integer)
            delayDuplicate = job("delayDuplicate").ToObject(Of Integer)
        Catch ex As Exception
            job = New JObject()
            job("delay") = delay
            job("delayDuplicate") = delayDuplicate
            SaveFile()
        End Try
    End Sub

    Public Sub SaveFile()
        If Not Directory.Exists(Path.Combine(appData, "WFInfo")) Then
            Directory.CreateDirectory(Path.Combine(appData, "WFInfo"))
        End If
        File.WriteAllText(settings_path, JsonConvert.SerializeObject(job, Formatting.Indented))
    End Sub
End Class
