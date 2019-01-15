Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Management
Imports System.Security.Cryptography
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging
Imports System.Data.SQLite
Imports Tesseract
Public Class Main
    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As Integer)
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Integer
    Dim scTog As Integer = 0 ' Toggle to force a single screenshot
    Dim count As Integer = 0 ' Number of Pics in appData
    Dim Sess As Integer = 0  ' Number of Screenshots this session
    Dim PPM As Integer = 0   ' Potential Platinum Made this session
    Dim pCount As Integer = 0 ' Current plat price to scan (Used for passive plat checks)
    Dim CliptoImage As Image         ' Stored image
    Dim HKeyTog As Integer = 0       ' Toggle Var for setting the activation key
    Dim lbTemp As String             ' Stores the keychar
    Public devCheck As Boolean = False  ' Price alert (dev only) test feature that searches for cheaply listed parts
    Dim drag As Boolean = False      ' Toggle for the custom UI allowing it to drag
    Dim mouseX As Integer
    Dim mouseY As Integer
    Dim enablePPC As Boolean = True  ' Toggle that enables/disables passive platinum checks

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            '_________________________________________________________________________
            ' Refreshes the UI and moves it to the stored location
            '_________________________________________________________________________

            UpdateColors(Me)
            pbHome.Parent = pbSideBar
            pbHome.Location = New Point(0, 4)
            pbRelic.Parent = pbSideBar
            pbRelic.Location = New Point(0, 30)
            pbDonate.Parent = pbSideBar
            pbDonate.Location = New Point(0, 55)
            pbSettings.Parent = pbSideBar
            pbSettings.Location = New Point(0, 80)

            lbVersion.Text = "v" + My.Settings.Version 'The current version is stored in project properties
            Me.Location = New Point(My.Settings.StartX, My.Settings.StartY)
            Fullscreen = My.Settings.Fullscreen
            Me.MaximizeBox = False
            lbStatus.ForeColor = Color.Yellow
            Me.Refresh()
            Me.Activate()
            Me.Refresh()


            '_________________________________________________________________________
            'Readies the test folder for debug mode (Saves screenshots for debugging)
            '_________________________________________________________________________
            If (Not System.IO.Directory.Exists(appData + "\WFInfo\tests")) Then
                System.IO.Directory.CreateDirectory(appData + "\WFInfo\tests")
            End If
            count = GetMax(appData + "\WFInfo\tests\") + 1


            '_________________________________________________________________________
            ' Gets the xcsrf token from browser cookies for listing parts while in game
            '_________________________________________________________________________
            Try
                If getCookie() Then
                    getXcsrf()
                End If
            Catch ex As Exception
                addLog(ex.ToString)
            End Try


            '_________________________________________________________________________
            ' Sets up screenshot settings for fullscreen mode (Steam only, not fully supported)
            '_________________________________________________________________________
            If Fullscreen Then
                If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                    My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
                End If
            End If


            '_________________________________________________________________________
            'Refreshes the clipboard, causes issues later if you don't
            '_________________________________________________________________________
            If Clipboard.ContainsImage() Then
                Clipboard.GetImage()
                CliptoImage = Clipboard.GetImage()
            End If

            '_________________________________________________________________________
            'Mechanism to make sure I don't kill warframe.market (You can disable all passive checks via the website)
            '_________________________________________________________________________
            Dim enablePassives As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/enablepassivechecks")
            enablePassives = enablePassives.Remove(0, enablePassives.IndexOf("enabled = ") + 10)
            enablePassives = enablePassives.Remove(enablePassives.IndexOf(""""), enablePassives.Length - enablePassives.IndexOf(""""))


            '_________________________________________________________________________
            'Disables passive checks if user sets it in settings
            '_________________________________________________________________________
            If Not enablePassives = "true" Then
                enablePPC = False
            End If

        Catch ex As Exception
            addLog(ex.ToString)
        End Try
        If Not My.Settings.TargetAreaSet Then
            MsgBox("This is a beta version of WFInfo." & vbNewLine & "You must first set the target area." & vbNewLine & vbNewLine & "1.) Get to the Fissure Reward Screen with 4 players." & vbNewLine & "2.) Press " & My.Settings.HKey3Text & " to show the selection cursor." & vbNewLine & "3.) Click the upper left corner of the first reward." & vbNewLine & "4.) Click the lower right corner of the last reward.", MsgBoxStyle.SystemModal)
        End If
    End Sub

    Public Function getCookie()
        '_________________________________________________________________________
        'Checks FF cookie then Chrome Cookie, if it exists in neither returns false, true if found, also sets cookie
        '_________________________________________________________________________
        Dim found As Boolean = False
        Dim ChromePath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\Google\Chrome\User Data\Default\Cookies"

        If File.Exists(appData + "\Mozilla\Firefox\Profiles") Then
            Dim FFpath As String = Directory.GetDirectories(appData + "\Mozilla\Firefox\Profiles")(0) + "\cookies.sqlite"
            If File.Exists(FFpath) Then
                If Not checkCookie(FFpath, True) = True Then
                    If File.Exists(ChromePath) Then
                        If checkCookie(ChromePath) = True Then
                            found = True
                        End If
                    End If
                Else
                    found = True
                End If
            End If
        ElseIf File.Exists(ChromePath) Then
            If checkCookie(ChromePath) = True Then
                found = True
            End If
        End If
        Return found
    End Function

    Private Function checkCookie(path As String, Optional FireFox As Boolean = False)
        '_________________________________________________________________________
        'Decrypts cookie to get JWT and returns true if all goes well
        '_________________________________________________________________________
        Dim SQLconnect As New SQLiteConnection
        Dim SQLcommand As New SQLiteCommand

        SQLconnect.ConnectionString = "Data Source=" & path & ";"
        SQLconnect.Open()


        SQLcommand = SQLconnect.CreateCommand
        If FireFox Then
            SQLcommand.CommandText = "SELECT * FROM moz_cookies"
        Else
            SQLcommand.CommandText = "SELECT name,encrypted_value FROM Cookies"
        End If
        Dim SQLreader As SQLiteDataReader = SQLcommand.ExecuteReader()
        Dim cdmblk As String = " "
        Dim found As Boolean = False
        While SQLreader.Read
            If FireFox Then
                If SQLreader(3).contains("JWT") Then
                    cookie = "JWT=" + SQLreader(4) + "; cdmblk0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0"
                    found = True
                End If
            Else
                Dim encryptedData = SQLreader(1)
                If SQLreader(0).Contains("JWT") Then
                    Dim decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, Nothing, System.Security.Cryptography.DataProtectionScope.LocalMachine)
                    Dim plainText = System.Text.Encoding.ASCII.GetString(decodedData)
                    cookie = "JWT=" + plainText + "; cdmblk0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0"
                    found = True
                End If
            End If
        End While



        SQLcommand.Dispose()
        SQLconnect.Close()
        Return found
    End Function

    Private Function getXcsrf()
        '_________________________________________________________________________
        'Gets a fresh xcsrf token from warframe.market
        '_________________________________________________________________________
        Dim uri As New Uri("https://warframe.market")
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        Dim req As HttpWebRequest = HttpWebRequest.Create(uri)
        req.ContentType = "application/json"
        req.Method = "GET"
        req.Connection = "warframe.market:443 HTTP/1.1"
        req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0"
        req.Host = "warframe.market:443"
        req.Headers.Add("cookie", cookie)
        req.Headers.Add("X-Requested-With", "XMLHttpRequest")
        req.KeepAlive = True

        Dim response = req.GetResponse()
        Dim stream = response.GetResponseStream()
        Dim found As Boolean = False
        Dim reader As StreamReader = New StreamReader(stream)
        xcsrf = reader.ReadLine()
        Do Until xcsrf.Contains("csrf-token")
            xcsrf = reader.ReadLine()
            found = True
        Loop
        xcsrf = xcsrf.Substring(xcsrf.IndexOf("##"), 130)

        Return found
    End Function

    Private Sub btnDebug1_Click(sender As Object, e As EventArgs) Handles btnDebug1.Click
    End Sub

    Private Sub btnDebug2_Click(sender As Object, e As EventArgs) Handles btnDebug2.Click
    End Sub

    Private Sub BGWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles BGWorker.DoWork
        Try
            lbStatus.ForeColor = Color.Yellow
            If db Is Nothing Then
                db = New Data()
            Else
                OCR.ParseScreen()
            End If
            lbStatus.ForeColor = Color.Lime
        Catch ex As Exception
            lbStatus.ForeColor = Color.Red
            addLog(ex.ToString)
        End Try
    End Sub

    Private Sub KeyWatch_Tick(sender As Object, e As EventArgs) Handles tPB.Tick
        Try
            Dim Refresh As Integer = GetAsyncKeyState(HKey3)
            If GetAsyncKeyState(HKey3) And &H8000 Then
                If Not TargetSelector.Visible = True Then
                    TargetSelector.Show()
                End If
            End If

            If Not key1Tog Then
                '_________________________________________________________________________
                'Checks for new steam screenshots (using fullscreen mode) and starts main function if found
                '_________________________________________________________________________
                If Fullscreen Then
                    If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                        If Not My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First() Then
                            My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
                            If Not BGWorker.IsBusy Then
                                BGWorker.RunWorkerAsync()
                            End If
                        End If
                    End If
                End If


                '_________________________________________________________________________
                'watches for main hotkey and sctog starts the min function if pressed
                '_________________________________________________________________________
                If GetAsyncKeyState(HKey1) And &H8000 Then
                    If Not BGWorker.IsBusy Then
                        BGWorker.RunWorkerAsync()
                    End If
                End If
            End If
        Catch ex As Exception
            lbStatus.ForeColor = Color.Red
            addLog(ex.ToString)
        End Try
    End Sub

    Public Sub addLog(txt As String)
        '_________________________________________________________________________
        'Function for storing log market_data
        '_________________________________________________________________________
        appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Dim dateTime As String = "[" + System.DateTime.Now + "]"
        Dim logStore As String = ""
        If Not My.Computer.FileSystem.DirectoryExists(appData + "\WFInfo") Then
            Directory.CreateDirectory(appData + "\WFInfo")
        End If
        If My.Computer.FileSystem.FileExists(appData + "\WFInfo\WFInfo.log") Then
            logStore = My.Computer.FileSystem.ReadAllText(appData + "\WFInfo\WFInfo.log")
        Else
            File.Create(appData + "\WFInfo\WFInfo.log").Dispose()
        End If
        My.Computer.FileSystem.WriteAllText(appData + "\WFInfo\WFInfo.log",
        dateTime + vbNewLine + txt + vbNewLine + vbNewLine + logStore, False)
    End Sub

    Private Sub Main_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        If HKeyTog = 1 Then
            lbTemp = Chr(AscW(e.KeyChar)).ToString.ToUpper
        End If
    End Sub

    Private Sub btnSetKey_KeyPress(sender As Object, e As KeyPressEventArgs)
        If HKeyTog = 1 Then
            lbTemp = Chr(AscW(e.KeyChar)).ToString.ToUpper
        End If
    End Sub

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing

        My.Settings.StartX = Me.Location.X
        My.Settings.StartY = Me.Location.Y
        My.Settings.Save()
    End Sub

    Private Sub Main_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        '_________________________________________________________________________
        'Refreshes the application to stop graphical glitches caused by lockup
        'Starts the background timers
        '_________________________________________________________________________
        Me.Refresh()
        BGWorker.RunWorkerAsync()
    End Sub

    Private Sub pbSettings_Click(sender As Object, e As EventArgs) Handles pbSettings.Click
        Settings.Show()
    End Sub

    Private Sub pbSettings_MouseEnter(sender As Object, e As EventArgs) Handles pbSettings.MouseEnter
        pbSettings.Image = My.Resources.Settings_h
    End Sub

    Private Sub pbSettings_MouseLeave(sender As Object, e As EventArgs) Handles pbSettings.MouseLeave
        pbSettings.Image = My.Resources.Settings
    End Sub

    Private Sub pbHome_Click(sender As Object, e As EventArgs) Handles pbHome.Click
        Process.Start("https://sites.google.com/site/wfinfoapp/home")
    End Sub

    Private Sub pbHome_MouseEnter(sender As Object, e As EventArgs) Handles pbHome.MouseEnter
        pbHome.Image = My.Resources.home_h
    End Sub

    Private Sub pbHome_MouseLeave(sender As Object, e As EventArgs) Handles pbHome.MouseLeave
        pbHome.Image = My.Resources.home
    End Sub

    Private Sub pbRelic_Click(sender As Object, e As EventArgs) Handles pbRelic.Click
        If db.relic_data IsNot Nothing Then
            Relics.Load_Relic_Tree()
            Relics.Show()
        End If
    End Sub

    Private Sub pbRelic_MouseEnter(sender As Object, e As EventArgs) Handles pbRelic.MouseEnter
        pbRelic.Image = My.Resources.Relic_h
    End Sub

    Private Sub pbRelic_MouseLeave(sender As Object, e As EventArgs) Handles pbRelic.MouseLeave
        pbRelic.Image = My.Resources.Relic
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub pTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub pTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub pTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp
        drag = False
    End Sub

    Private Sub lbTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub lbTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub lbTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseUp
        drag = False
    End Sub

    Private Sub lbVersion_MouseDown(sender As Object, e As MouseEventArgs) Handles lbVersion.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub lbVersion_MouseMove(sender As Object, e As MouseEventArgs) Handles lbVersion.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub lbVersion_MouseUp(sender As Object, e As MouseEventArgs) Handles lbVersion.MouseUp
        drag = False
    End Sub

    Private Sub ButtonHide_Click(sender As Object, e As EventArgs) Handles btnHide.Click
        Me.Hide()
        trayIcon.Visible = True
    End Sub

    Private Sub trayIcon_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles trayIcon.MouseDoubleClick
        Me.Show()
        trayIcon.Visible = False
    End Sub

    Private Sub trayMenu_Opening(sender As Object, e As ToolStripItemClickedEventArgs) Handles trayMenu.ItemClicked
        If e.ClickedItem.Name = "trayExit" Then
            Me.Close()
        ElseIf e.ClickedItem.Name = "trayShow" Then
            Me.Show()
            trayIcon.Visible = False
        ElseIf e.ClickedItem.Name = "trayRelics" Then
            pbRelic_Click(pbRelic, Nothing)
        End If
    End Sub
End Class

Module Glob
    '_________________________________________________________________________
    'Global variables used for various things
    '_________________________________________________________________________

    ' StopWatch for Code Profiling
    Public clock As New Stopwatch()
    Public prev_time As Long = clock.Elapsed.Ticks



    Public db As Data
    Public qItems As New List(Of String)()
    Public HKey1 As Integer = My.Settings.HKey1
    Public HKey3 As Integer = My.Settings.HKey3
    Public HideShots As Boolean = False     ' Bool to hide screenshot notifications in fullscreen mode
    'Public Equipment As String               ' List of leveled equipment
    Public Fullscreen As Boolean = False
    Public key1Tog As Boolean = False
    Public key2Tog As Boolean = False
    Public key3Tog As Boolean = False
    Public Animate As Boolean = My.Settings.Animate
    'Public PassiveChecks As Boolean = My.Settings.PassiveChecks
    Public Messages As Boolean = My.Settings.Messages
    Public NewStyle As Boolean = My.Settings.NewStyle
    Public Debug As Boolean = My.Settings.Debug
    'Public DisplayPlatinum As Boolean = My.Settings.DisplayPlatinum
    Public DisplayNames As Boolean = My.Settings.DisplayNames
    Public appData As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Public textColor As Color = Color.FromArgb(177, 208, 217)
    Public textBrush As Brush = New SolidBrush(textColor)
    Public stealthColor As Color = Color.FromArgb(118, 139, 145)
    Public stealthBrush As Brush = New SolidBrush(stealthColor)
    Public commonColor As Color = Color.FromArgb(205, 127, 50)
    Public commonBrush As Brush = New SolidBrush(commonColor)
    Public uncommonColor As Color = Color.FromArgb(192, 192, 192)
    Public uncommonBrush As Brush = New SolidBrush(uncommonColor)
    Public rareColor As Color = Color.FromArgb(255, 215, 0)
    Public rareBrush As Brush = New SolidBrush(rareColor)
    Public bgColor As Color = Color.FromArgb(27, 27, 27)
    Public bgBrush As Brush = New SolidBrush(bgColor)
    Public cookie As String = ""
    Public xcsrf As String = ""

    Public Sub UpdateColors(f As Form)
        '_________________________________________________________________________
        'Updates the application colors for people who use custom colors
        '_________________________________________________________________________
        For Each c As Control In f.Controls
            If c.Name = "pTitle" Then
                c.BackColor = My.Settings.cTitleBar
                For Each c2 As Control In c.Controls
                    If TypeOf c2 Is Label Then c2.ForeColor = My.Settings.cText
                    If TypeOf c2 Is Button Then c2.BackColor = My.Settings.cTitleBar
                    If TypeOf c2 Is Button Then c2.ForeColor = My.Settings.cText
                    If c2.Name = "lbStatus" Then c2.ForeColor = Color.Lime
                Next
            Else
                If TypeOf c Is Label Then c.ForeColor = My.Settings.cText
                If TypeOf c Is Panel Then c.BackColor = My.Settings.cBackground
                If TypeOf c Is Label Then c.ForeColor = My.Settings.cText
                If TypeOf c Is Button Then c.ForeColor = My.Settings.cText
                If TypeOf c Is Button Then c.BackColor = My.Settings.cTitleBar
                If c.Name = "pbSideBar" Then c.BackColor = My.Settings.cSideBar
                If TypeOf c Is TextBox Then c.BackColor = My.Settings.cBackground
                If TypeOf c Is TextBox Then c.ForeColor = My.Settings.cText
                If c.Name.Contains("DropShadow") Then
                    c.ForeColor = Color.FromArgb(10, 10, 10)
                End If
                For Each c2 As Control In c.Controls
                    If TypeOf c2 Is Panel Then c2.BackColor = My.Settings.cBackground
                    If TypeOf c2 Is Label Then c2.ForeColor = My.Settings.cText
                    If TypeOf c2 Is Button Then c2.ForeColor = My.Settings.cText
                    If TypeOf c2 Is Button Then c2.BackColor = My.Settings.cTitleBar
                    If TypeOf c2 Is CheckBox Then c2.ForeColor = My.Settings.cText
                    If c2.Name = "pbSideBar" Then c2.BackColor = My.Settings.cSideBar
                    If c2.Name.Contains("DropShadow") Then
                        c2.ForeColor = Color.FromArgb(10, 10, 10)
                    End If
                    For Each c3 As Control In c2.Controls
                        If TypeOf c3 Is Panel Then c3.BackColor = My.Settings.cBackground
                        If TypeOf c3 Is Label Then c3.ForeColor = My.Settings.cText
                        If TypeOf c3 Is Button Then c3.ForeColor = My.Settings.cText
                        If TypeOf c3 Is Button Then c3.BackColor = My.Settings.cTitleBar
                        If TypeOf c3 Is CheckBox Then c3.ForeColor = My.Settings.cText
                        For Each c4 As Control In c3.Controls
                            If TypeOf c4 Is Panel Then c4.BackColor = My.Settings.cBackground
                            If TypeOf c4 Is Label Then c4.ForeColor = My.Settings.cText
                            If TypeOf c4 Is Button Then c4.ForeColor = My.Settings.cText
                            If TypeOf c4 Is Button Then c4.BackColor = My.Settings.cTitleBar
                            If TypeOf c4 Is CheckBox Then c4.ForeColor = My.Settings.cText
                            If c4.Name = "pbSideBar" Then c4.BackColor = My.Settings.cSideBar
                        Next
                    Next
                Next
            End If
        Next
    End Sub

    Public Function Tint(ByVal bmpSource As Bitmap, ByVal clrScaleColor As Color, ByVal sngScaleDepth As Single) As Bitmap

        Dim bmpTemp As New Bitmap(bmpSource.Width, bmpSource.Height) 'Create Temporary Bitmap To Work With

        Dim iaImageProps As New ImageAttributes 'Contains information about how bitmap and metafile colors are manipulated during rendering. 

        Dim cmNewColors As ColorMatrix 'Defines a 5 x 5 matrix that contains the coordinates for the RGBAW space

        cmNewColors = New ColorMatrix(New Single()() _
            {New Single() {1, 0, 0, 0, 0},
             New Single() {0, 1, 0, 0, 0},
             New Single() {0, 0, 1, 0, 0},
             New Single() {0, 0, 0, 1, 0},
             New Single() {clrScaleColor.R / 255 * sngScaleDepth, clrScaleColor.G / 255 * sngScaleDepth, clrScaleColor.B / 255 * sngScaleDepth, 0, 1}})

        iaImageProps.SetColorMatrix(cmNewColors) 'Apply Matrix

        Dim grpGraphics As Graphics = Graphics.FromImage(bmpTemp) 'Create Graphics Object and Draw Bitmap Onto Graphics Object

        grpGraphics.DrawImage(bmpSource, New Rectangle(0, 0, bmpSource.Width, bmpSource.Height), 0, 0, bmpSource.Width, bmpSource.Height, GraphicsUnit.Pixel, iaImageProps)

        Return bmpTemp

    End Function

    Public Function ReplaceFirst(text As String, search As String, replace As String)
        Dim pos As Integer = text.IndexOf(search)
        If pos < 0 Then
            Return text
        End If
        Return text.Substring(0, pos) + replace + text.Substring(pos + search.Length)
    End Function

    Public Function GetMax(ByVal sFolder As String) As Long
        '_________________________________________________________________________
        'Function that returns the number of files in a folder
        '_________________________________________________________________________
        Const sExt As String = ".jpg"
        Dim lVal As Long, sFile As String

        sFile = Dir(sFolder & "\*" & sExt)
        Do While Len(sFile)
            lVal = Val(sFile)
            If Not lVal = 0 Then
                If Len(sFile) = Len(lVal & sExt) Then
                    If lVal > GetMax Then GetMax = lVal
                End If
            End If
            sFile = Dir()
        Loop
        Return GetMax
    End Function

    Public Function LevDist(ByVal s As String,
                                    ByVal t As String) As Integer
        '_________________________________________________________________________
        'LevDist determines how "close" a jumbled name is to an actual name
        'For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
        '     https://en.wikipedia.org/wiki/Levenshtein_distance
        '_________________________________________________________________________
        s.Replace("*", "")
        t.Replace("*", "")
        s = s.ToLower
        t = t.ToLower
        Dim n As Integer = s.Length
        Dim m As Integer = t.Length
        Dim d(n + 1, m + 1) As Integer

        If n = 0 Then
            Return m
        End If

        If m = 0 Then
            Return n
        End If

        Dim i As Integer
        Dim j As Integer

        For i = 0 To n
            d(i, 0) = i
        Next

        For j = 0 To m
            d(0, j) = j
        Next

        For i = 1 To n
            For j = 1 To m

                Dim cost As Integer
                If t(j - 1) = s(i - 1) Then
                    cost = 0
                Else
                    cost = 1
                End If

                d(i, j) = Math.Min(Math.Min(d(i - 1, j) + 1, d(i, j - 1) + 1),
                                   d(i - 1, j - 1) + cost)
            Next
        Next

        Return d(n, m)
    End Function
End Module