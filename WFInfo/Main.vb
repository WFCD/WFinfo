Imports System.ComponentModel
Imports System.Drawing.Imaging
Imports System.IO

Public Class Main
    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As Integer)
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Integer
    Dim CliptoImage As Image         ' Stored image
    Dim drag As Boolean = False      ' Toggle for the custom UI allowing it to drag
    Dim mouseX As Integer
    Dim mouseY As Integer
    Public version As String = Nothing

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            '_________________________________________________________________________
            ' Refreshes the UI and moves it to the stored location
            '_________________________________________________________________________

            UpdateColors(Me)

            If version Is Nothing Then
                version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
                version = version.Substring(0, version.LastIndexOf("."))
            End If

            lbVersion.Text = "v" + version
            Me.Location = My.Settings.MainLoc
            'Fullscreen = My.Settings.Fullscreen
            Me.MaximizeBox = False
            Me.Refresh()
            Me.Activate()
            Me.Refresh()
            clock.Restart()

            '_________________________________________________________________________
            'Readies the test folder for debug mode (Saves screenshots for debugging)
            '_________________________________________________________________________
            If (Not Directory.Exists(appData + "\WFInfo\tests")) Then
                Directory.CreateDirectory(appData + "\WFInfo\tests")
            End If


            '_________________________________________________________________________
            ' Gets the xcsrf token from browser cookies for listing parts while in game
            '_________________________________________________________________________
            'Try
            '    If getCookie() Then
            '        getXcsrf()
            '    End If
            'Catch ex As Exception
            '    addLog(ex.ToString)
            'End Try


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

        Catch ex As Exception
            addLog(ex.ToString)
        End Try
    End Sub

    Private DoWork_timer As Long = 0
    Private Sub DoWork()
        DoWork_timer = clock.Elapsed.TotalMilliseconds
        Try
            If (Glob.db IsNot Nothing) Then
                Dim elapsed As TimeSpan = Glob.clock.Elapsed
                Me.DoWork_timer = CLng(Math.Round(elapsed.TotalMilliseconds))
                Me.lbStatus.Text = "Getting Reward Info..."
                OCR.ParseScreen()
                elapsed = Glob.clock.Elapsed
                Me.DoWork_timer = CLng(Math.Round(elapsed.TotalMilliseconds - CDbl(Me.DoWork_timer)))
                Me.lbStatus.Text = "Rewards Shown (" & DoWork_timer & "ms)"
            Else
                Glob.db = New Data()
                OCR.ForceUpdateCenter()
                Invoke(Sub() lbMarketDate.Text = Glob.db.market_data("timestamp").ToString().Substring(5, 11))
                Invoke(Sub() lbEqmtDate.Text = Glob.db.eqmt_data("timestamp").ToString().Substring(5, 11))
                Invoke(Sub() lbWikiDate.Text = Glob.db.eqmt_data("rqmts_timestamp").ToString().Substring(5, 11))
                Relics.Load_Relic_Tree()
                Equipment.Load_Eqmt_Tree()
                For i As Integer = 0 To 3
                    rwrdPanels(i) = New Overlay()
                Next
                For i As Integer = 0 To 8
                    relicPanels(i) = New Overlay()
                Next
                Invoke(Sub() lbStatus.Text = "Data Loaded")
            End If
        Catch ex As Exception
            Me.lbStatus.Text = "ERROR (ParseScreen)"
            Me.lbStatus.ForeColor = Color.Red
            Me.addLog(ex.ToString())
        End Try
    End Sub

    Private Sub KeyWatch_Tick(sender As Object, e As EventArgs) Handles tPB.Tick
        Try
            If Not key1Tog Then

                '_________________________________________________________________________
                'Checks for new steam screenshots (using fullscreen mode) and starts main function if found
                '_________________________________________________________________________
                If Fullscreen Then
                    If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                        If Not My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First() Then
                            My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
                            DoWork()
                        End If
                    End If
                End If


                '_________________________________________________________________________
                'watches for main hotkey and sctog starts the min function if pressed
                '_________________________________________________________________________
                If GetAsyncKeyState(HKey1) And &H8000 Then
                    DoWork()
                End If
            End If
        Catch ex As Exception
            Invoke(Sub() lbStatus.Text = "ERROR (KeyWatch)")
            Invoke(Sub() lbStatus.ForeColor = Color.Red)
            addLog(ex.ToString)
        End Try
    End Sub

    Public Sub addLog(txt As String)
        '_________________________________________________________________________
        'Function for storing log data
        '_________________________________________________________________________

        If version Is Nothing Then
            version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
            version = version.Substring(0, version.LastIndexOf("."))
        End If

        appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        Dim dateTime As String = "[" + System.DateTime.Now + " " + version + "]"
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

    Private Sub Main_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing

        My.Settings.MainLoc = Me.Location
        My.Settings.Save()
    End Sub

    Private Sub Main_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        '_________________________________________________________________________
        'Refreshes the application to stop graphical glitches caused by lockup
        'Starts the background timers
        '_________________________________________________________________________
        Me.Refresh()
        Me.CreateControl()
        Task.Factory.StartNew(Sub() DoWork())
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
        If db IsNot Nothing AndAlso db.relic_data IsNot Nothing Then
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

    Private Sub pbEqmt_MouseEnter(sender As Object, e As EventArgs) Handles pbEqmt.MouseEnter
        pbEqmt.Image = My.Resources.foundry_h
    End Sub

    Private Sub pbEqmt_MouseLeave(sender As Object, e As EventArgs) Handles pbEqmt.MouseLeave
        pbEqmt.Image = My.Resources.foundry
    End Sub

    Private Sub pbEqmt_Click(sender As Object, e As EventArgs) Handles pbEqmt.Click
        If db IsNot Nothing AndAlso db.eqmt_data IsNot Nothing Then
            Equipment.Load_Eqmt_Tree()
            Equipment.Show()
        End If
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
        ElseIf e.ClickedItem.Name = "trayEquipment" Then
            pbEqmt_Click(pbEqmt, Nothing)
        End If
    End Sub

    Private tUpdate_Count As Integer = 1
    Private Sub tUpdate_Tick(sender As Object, e As EventArgs) Handles tUpdate.Tick ' Happens every 5min
        Try

            ' Every hour, check db Data
            If tUpdate_Count = 0 Then
                If db IsNot Nothing AndAlso db.Update() Then
                    Relics.Reload_Data()
                    Equipment.RefreshData()
                End If
            End If
            ' Every 5min update the relic_area
            OCR.ForceUpdateCenter()
        Catch ex As Exception
            Invoke(Sub() lbStatus.Text = "ERROR (Updating DB)")
            Invoke(Sub() lbStatus.ForeColor = Color.Red)
            addLog(ex.ToString)
        End Try
        tUpdate_Count = (tUpdate_Count + 1) Mod 12
    End Sub

    Private Async Sub lbMarket_Click(sender As Object, e As EventArgs) Handles lbMarket.Click
        lbMarketDate.Text = "Loading..."
        Await Task.Run(Sub() db.ForceMarketUpdate())
        lbMarketDate.Text = db.market_data("timestamp").ToString().Substring(5, 11)
        Equipment.Refresh()
        Relics.Refresh()
    End Sub

    Private Async Sub lbEqmt_Click(sender As Object, e As EventArgs) Handles lbEqmt.Click
        lbEqmtDate.Text = "Loading..."
        lbWikiDate.Text = "Loading..."
        Await Task.Run(Sub() db.ForceEqmtUpdate())
        lbEqmtDate.Text = db.eqmt_data("timestamp").ToString().Substring(5, 11)
        lbWikiDate.Text = db.eqmt_data("rqmts_timestamp").ToString().Substring(5, 11)
        Equipment.Refresh()
        Relics.Refresh()
    End Sub

    Private Async Sub lbWiki_Click(sender As Object, e As EventArgs) Handles lbWiki.Click
        lbWikiDate.Text = "Loading..."
        Await Task.Run(Sub() db.ForceWikiUpdate())
        lbWikiDate.Text = db.eqmt_data("rqmts_timestamp").ToString().Substring(5, 11)
        Equipment.Refresh()
    End Sub

    Private Sub tAutomate_Tick(ByVal sender As Object, ByVal e As EventArgs) Handles tAutomate.Tick
        If (Glob.db IsNot Nothing AndAlso rwrdPanels(0) IsNot Nothing AndAlso OCR.isWFActive()) Then
            If (OCR.IsRelicWindow()) Then
                If (Not rwrdPanels(0).Visible) Then
                    Me.tAutomate.Interval = 3000
                    MyBase.Invoke(Sub() Me.DoWork())
                End If
            ElseIf (rwrdPanels(0).Visible) Then
                For i As Integer = 0 To 3
                    rwrdPanels(i).Hide()
                Next
            Else
                Me.tAutomate.Interval = 1000
            End If
        Else
            Me.tAutomate.Interval = 5000
        End If
    End Sub
End Class

Module Glob
    '_________________________________________________________________________
    'Global variables used for various things
    '_________________________________________________________________________

    ' StopWatch for Code Profiling
    Public clock As New Stopwatch()

    Public db As Data
    Public qItems As New List(Of String)()
    Public HKey1 As Integer = My.Settings.HKey1
    Public HideShots As Boolean = False     ' Bool to hide screenshot notifications in fullscreen mode
    'Public Equipment As String               ' List of leveled equipment
    Public Fullscreen As Boolean = False
    Public key1Tog As Boolean = False
    Public Automate As Boolean = My.Settings.Automate
    Public Animate As Boolean = My.Settings.Animate
    Public Debug As Boolean = My.Settings.Debug
    Public appData As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Public textColor As Color = Color.FromArgb(177, 208, 217)
    Public textBrush As Brush = New SolidBrush(textColor)
    Public stealthColor As Color = Color.FromArgb(80, 100, 100)
    Public stealthBrush As Brush = New SolidBrush(stealthColor)
    Public subdueColor As Color = Color.FromArgb(120, 140, 140)
    Public subdueBrush As Brush = New SolidBrush(subdueColor)
    Public commonColor As Color = Color.FromArgb(205, 127, 50)
    Public commonBrush As Brush = New SolidBrush(commonColor)
    Public uncommonColor As Color = Color.FromArgb(192, 192, 192)
    Public uncommonBrush As Brush = New SolidBrush(uncommonColor)
    Public rareColor As Color = Color.FromArgb(255, 215, 0)
    Public rareBrush As Brush = New SolidBrush(rareColor)
    Public bgColor As Color = Color.FromArgb(27, 27, 27)
    Public bgBrush As Brush = New SolidBrush(bgColor)

    Public culture As System.Globalization.CultureInfo = New System.Globalization.CultureInfo("en")

    Public rwrdPanels(4) As Overlay
    Public relicPanels(9) As Overlay

    Public ReplacementList As Char(,)
    Public WithEvents globHook As New GlobalHook()

    Public Function getCookie()
        '_________________________________________________________________________
        'Checks FF cookie then Chrome Cookie, if it exists in neither returns false, true if found, also sets cookie
        '_________________________________________________________________________
        Dim found As Boolean = False
        'Dim ChromePath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\Google\Chrome\User Data\Default\Cookies"

        'If File.Exists(appData + "\Mozilla\Firefox\Profiles") Then
        '    Dim FFpath As String = Directory.GetDirectories(appData + "\Mozilla\Firefox\Profiles")(0) + "\cookies.sqlite"
        '    If File.Exists(FFpath) Then
        '        If Not checkCookie(FFpath, True) = True Then
        '            If File.Exists(ChromePath) Then
        '                If checkCookie(ChromePath) = True Then
        '                    found = True
        '                End If
        '            End If
        '        Else
        '            found = True
        '        End If
        '    End If
        'ElseIf File.Exists(ChromePath) Then
        '    If checkCookie(ChromePath) = True Then
        '        found = True
        '    End If
        'End If
        Return found
    End Function

    Private Function checkCookie(path As String, Optional FireFox As Boolean = False)
        '_________________________________________________________________________
        'Decrypts cookie to get JWT and returns true if all goes well
        '_________________________________________________________________________
        'Dim SQLconnect As New SQLiteConnection
        'Dim SQLcommand As New SQLiteCommand

        'SQLconnect.ConnectionString = "Data Source=" & path & ";"
        'SQLconnect.Open()


        'SQLcommand = SQLconnect.CreateCommand
        'If FireFox Then
        '    SQLcommand.CommandText = "SELECT * FROM moz_cookies"
        'Else
        '    SQLcommand.CommandText = "SELECT name,encrypted_value FROM Cookies"
        'End If
        'Dim SQLreader As SQLiteDataReader = SQLcommand.ExecuteReader()
        'Dim cdmblk As String = " "
        'Dim found As Boolean = False
        'While SQLreader.Read
        '    If FireFox Then
        '        If SQLreader(3).contains("JWT") Then
        '            cookie = "JWT=" + SQLreader(4) + "; cdmblk0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0"
        '            found = True
        '        End If
        '    Else
        '        Dim encryptedData = SQLreader(1)
        '        If SQLreader(0).Contains("JWT") Then
        '            Dim decodedData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, Nothing, System.Security.Cryptography.DataProtectionScope.LocalMachine)
        '            Dim plainText = System.Text.Encoding.ASCII.GetString(decodedData)
        '            cookie = "JWT=" + plainText + "; cdmblk0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0,0:0:0:0:0:0:0:0:0:0:0:0:0:0"
        '            found = True
        '        End If
        '    End If
        'End While



        'SQLcommand.Dispose()
        'SQLconnect.Close()
        'Return found
        Return False
    End Function

    Private Function getXcsrf()
        '_________________________________________________________________________
        'Gets a fresh xcsrf token from warframe.market
        '_________________________________________________________________________
        'Dim uri As New Uri("https://warframe.market")
        'ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        'Dim req As HttpWebRequest = HttpWebRequest.Create(uri)
        'req.ContentType = "application/json"
        'req.Method = "GET"
        'req.Connection = "warframe.market:443 HTTP/1.1"
        'req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0"
        'req.Host = "warframe.market:443"
        'req.Headers.Add("cookie", cookie)
        'req.Headers.Add("X-Requested-With", "XMLHttpRequest")
        'req.KeepAlive = True

        'Dim response = req.GetResponse()
        'Dim stream = response.GetResponseStream()
        'Dim found As Boolean = False
        'Dim reader As StreamReader = New StreamReader(stream)
        'xcsrf = reader.ReadLine()
        'Do Until xcsrf.Contains("csrf-token")
        '    xcsrf = reader.ReadLine()
        '    found = True
        'Loop
        'xcsrf = xcsrf.Substring(xcsrf.IndexOf("##"), 130)

        'Return found
        Return False
    End Function

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

    Public Function LevDist2(ByVal str1 As String, ByVal str2 As String, Optional ByVal limit As Integer = -1) As Integer
        Dim num As Integer
        Dim maxY As Boolean
        Dim temp As Integer
        Dim maxX As Boolean
        Dim s As String = str1.ToLower()
        Dim t As String = str2.ToLower()
        Dim n As Integer = s.Length
        Dim m As Integer = t.Length
        If (Not (n = 0 Or m = 0)) Then
            Dim d(n + 1 + 1 - 1, m + 1 + 1 - 1) As Integer
            Dim activeX As List(Of Integer) = New List(Of Integer)()
            Dim activeY As List(Of Integer) = New List(Of Integer)()
            d(0, 0) = 1
            activeX.Add(0)
            activeY.Add(0)
            Dim currX As Integer = 0
            Dim currY As Integer = 0
            Do
                currX = activeX(0)
                activeX.RemoveAt(0)
                currY = activeY(0)
                activeY.RemoveAt(0)

                temp = d(currX, currY)
                If limit <> -1 AndAlso temp > limit Then
                    Return temp
                End If

                maxX = currX = n
                maxY = currY = m
                If (Not maxX) Then
                    temp = d(currX, currY) + 1
                    If (temp < d(currX + 1, currY) OrElse d(currX + 1, currY) = 0) Then
                        d(currX + 1, currY) = temp
                        Glob.AddElement(d, activeX, activeY, currX + 1, currY)
                    End If
                End If
                If (Not maxY) Then
                    temp = d(currX, currY) + 1
                    If (temp < d(currX, currY + 1) OrElse d(currX, currY + 1) = 0) Then
                        d(currX, currY + 1) = temp
                        Glob.AddElement(d, activeX, activeY, currX, currY + 1)
                    End If
                End If
                If Not maxX And Not maxY Then
                    temp = d(currX, currY) + Glob.GetDifference(s(currX), t(currY))
                    If (temp < d(currX + 1, currY + 1) OrElse d(currX + 1, currY + 1) = 0) Then
                        d(currX + 1, currY + 1) = temp
                        Glob.AddElement(d, activeX, activeY, currX + 1, currY + 1)
                    End If
                End If
            Loop While Not (maxX And maxY)
            num = d(n, m) - 1
        Else
            num = n + m
        End If
        Return num
    End Function

    Private Sub AddElement(ByRef d As Integer(,), ByRef xList As List(Of Integer), ByRef yList As List(Of Integer), x As Integer, y As Integer)
        Dim loc As Integer = 0
        Dim temp As Integer = d(x, y)
        While loc < xList.Count AndAlso temp > d(xList(loc), yList(loc))
            loc = loc + 1
        End While
        If (loc = xList.Count) Then
            xList.Add(x)
            yList.Add(y)
            Return
        End If
        xList.Insert(loc, x)
        yList.Insert(loc, y)
    End Sub

    Private Function GetDifference(c1 As Char, c2 As Char) As Integer
        If c1 = c2 Or c1 = "?"c Or c2 = "?"c Then
            Return 0
        End If

        For i As Integer = 0 To ReplacementList.GetLength(0) - 1
            If (c1 = Glob.ReplacementList(i, 0) Or c2 = Glob.ReplacementList(i, 0)) AndAlso
               (c1 = Glob.ReplacementList(i, 1) Or c2 = Glob.ReplacementList(i, 1)) Then
                Return 0
            End If
        Next

        Return 1
    End Function
End Module