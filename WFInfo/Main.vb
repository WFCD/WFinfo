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
    Dim appData As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
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
            pbHome.Location = New Point(0, 8)
            pbDonate.Parent = pbSideBar
            pbDonate.Location = New Point(0, 38)
            pbSettings.Parent = pbSideBar
            pbSettings.Location = New Point(0, 65)
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

            '_________________________________________________________________________
            'UpdateStatus is a keep-alive function for analytics
            '_________________________________________________________________________
            UpdateStatus()
        Catch ex As Exception
            addLog(ex.ToString)
        End Try
    End Sub

    Public Function getCookie()
        '_________________________________________________________________________
        'Checks FF cookie then Chrome Cookie, if it exists in neither returns false, true if found, also sets cookie
        '_________________________________________________________________________
        Dim found As Boolean = False
        Dim FFpath As String = Directory.GetDirectories(appData + "\Mozilla\Firefox\Profiles")(0) + "\cookies.sqlite"
        Dim ChromePath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\Google\Chrome\User Data\Default\Cookies"

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

    Private Async Sub tPB_Tick(sender As Object, e As EventArgs) Handles tPB.Tick
        '_________________________________________________________________________
        'This is the main timer that triggers on hotkey and controls most function
        'The toggles make sure it runs only once per press
        '_________________________________________________________________________
        Try
            If (Not key1Tog) And (Not key2Tog) Then
                '_________________________________________________________________________
                'Refreshes the async state and checks and opens command function if hotkey is pressed
                '_________________________________________________________________________
                Dim Refresh As Integer = GetAsyncKeyState(HKey1)
                Refresh = GetAsyncKeyState(HKey2)
                If Not Input.Visible = True Then
                    If GetAsyncKeyState(HKey2) And &H8000 Then
                        Input.Display()
                    End If
                End If
                '_________________________________________________________________________
                'Checks for new screenshots (using fullscreen mode) and starts main function if found
                '_________________________________________________________________________
                If Fullscreen Then
                    If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                        If Not My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First() Then
                            My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
                            scTog = 1
                        End If
                    End If
                End If


                '_________________________________________________________________________
                'watches for main hotkey and sctog starts the min function if pressed
                '_________________________________________________________________________
                If scTog = 0 Then
                    If GetAsyncKeyState(HKey1) And &H8000 Then
                        scTog = 1
                    End If
                Else
                    lbStatus.ForeColor = Color.Yellow ' lbStatus is for showing the status color yellow = processing and sometimes error
                    tPPrice.Stop()
                    scTog = 0


                    '_________________________________________________________________________
                    'Stores the screenshot from clipboard (or screenshot file if fullscreen)
                    '_________________________________________________________________________
                    If Fullscreen Then
                        CliptoImage = New System.Drawing.Bitmap(My.Settings.LastFile)
                    Else
                        Dim bounds As Rectangle
                        Dim screenshot As System.Drawing.Bitmap
                        Dim graph As Graphics
                        bounds = Screen.PrimaryScreen.Bounds
                        screenshot = New System.Drawing.Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb)
                        graph = Graphics.FromImage(screenshot)
                        graph.CopyFromScreen(0, 0, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy)
                        CliptoImage = screenshot
                    End If


                    Try
                        '_________________________________________________________________________
                        'Gets the number of players using OCR and the names underneath fissure rewards
                        '_________________________________________________________________________
                        Dim players As Integer = GetPlayers(Crop(CliptoImage))
                        If players > 4 Or players < 1 Then
                            players = 4
                        End If


                        '_________________________________________________________________________
                        'Gets text of the cropped images (part tet) using OCR and stores them in tList
                        'Stores clipped images if debug is enbled
                        '_________________________________________________________________________
                        Dim tList As New List(Of String)()
                        For i = 0 To players - 1
                            Dim img As Image = Crop(CliptoImage, players, i)
                            If Not img Is Nothing Then
                                pbDebug.Image = img
                            End If
                            If Debug Then
                                Dim nextFile As Integer = GetMax(appData & "\WFInfo\tests\") + 1
                                img.Save(appData & "\WFInfo\tests\" & nextFile & ".jpg", Imaging.ImageFormat.Jpeg)
                            End If
                            tList.Add(GetText(Crop(CliptoImage, players, i)))
                        Next


                        '_________________________________________________________________________
                        'Gets the final guess (using Levenshtein distance) using the OCR text
                        'compared to a list that has been retrieved by the wiki
                        '_________________________________________________________________________
                        Dim finalList As New List(Of String)()
                        For i = 0 To tList.Count - 1

                            'Blueprint means that it is a multi-line part name
                            If Not LevDist(tList(i), "Blueprint") < 4 Then
                                Dim guess As String = Names(check(tList(i)))
                                finalList.Add(guess)
                            Else
                                'Since it's multi-line you need to use mode 1 of the crop function 
                                'This gets one line higher than the usual
                                Dim img As Image = Crop(CliptoImage, 1, i, players)
                                If Not img Is Nothing Then
                                    pbDebug.Image = img
                                End If
                                If Debug Then
                                    Dim nextFile As Integer = GetMax(appData & "\WFInfo\tests\") + 1
                                    img.Save(appData & "\WFInfo\tests\" & nextFile & ".jpg", Imaging.ImageFormat.Jpeg)
                                End If
                                Dim guess As String = Names(check(GetText(img) + " Blueprint"))
                                img.Dispose()
                                finalList.Add(guess)
                            End If
                        Next

                        qItems.Clear() 'qItems is for people using the tray instead of the overlay


                        '_________________________________________________________________________
                        'Retrieves the platinum and ducat prices using warframe.market 
                        'And the ducat list we pulled from the wiki when the application launched
                        '_________________________________________________________________________
                        '
                        'Stores them in p() and d()
                        '
                        'This also stores the text to display in the tray (if used) in qItems
                        '_________________________________________________________________________
                        Dim HighestPlat As Integer = 0
                        Dim p As New List(Of String)()
                        Dim d As New List(Of String)()
                        For i = 0 To finalList.Count - 1
                            Dim guess As String = finalList(i)
                            If Not finalList(i) = "Forma Blueprint" Then
                                Dim plat As String = ""
                                For j = 0 To PlatPrices.Count - 1
                                    If PlatPrices(j).Contains(guess) Then
                                        plat = PlatPrices(j).Split(",")(1)
                                        Exit For
                                    End If
                                Next
                                If plat = "" Then
                                    plat = GetPlat(KClean(guess))
                                    PlatPrices.Add(guess & "," & plat)
                                End If
                                If Not plat = "Unknown" Then
                                    If CType(plat, Integer) > HighestPlat Then
                                        HighestPlat = CType(plat, Integer)
                                    End If
                                End If

                                p.Add(plat)
                                d.Add(Ducks(check(guess)))

                                If KClean(guess).Length > 27 Then
                                    qItems.Add(KClean(guess).Substring(0, 27) & "..." & vbNewLine & "    Ducks: " & Ducks(check(guess)) & "   Plat: " & plat & vbNewLine)
                                Else
                                    qItems.Add(KClean(guess) & vbNewLine & "    Ducks: " & Ducks(check(guess)) & "   Plat: " & plat & vbNewLine)
                                End If
                            Else
                                qItems.Add(vbNewLine & finalList(i) & vbNewLine)
                                p.Add(0)
                                d.Add(0)
                            End If
                        Next


                        '_________________________________________________________________________
                        'Displays the information using either newstyle(overlay) or old(tray)
                        '_________________________________________________________________________
                        If Not Fullscreen And Not NewStyle Then
                            Tray.Clear()
                            Tray.Display()
                        Else
                            Tray.Clear()
                            qItems.Clear()

                            'Each part has it's own panel/overlay
                            Dim panel1 As New Overlay
                            Dim panel2 As New Overlay
                            Dim panel3 As New Overlay
                            Dim panel4 As New Overlay
                            For i = 0 To players - 1
                                Dim width As Integer = 0.4 * Screen.PrimaryScreen.Bounds.Height
                                Select Case players
                                    Case 4
                                        Dim x As Integer = (Screen.PrimaryScreen.Bounds.Width / 2) - (width * 2) + (width * i) + (width * 0.62)
                                        Dim y As Integer = Screen.PrimaryScreen.Bounds.Height * 0.174
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i))
                                        End Select
                                    Case 3
                                        Dim x As Integer = (Screen.PrimaryScreen.Bounds.Width / 2) - (1.5 * width) + (width * i) + (width * 0.62)
                                        Dim y As Integer = Screen.PrimaryScreen.Bounds.Height * 0.174
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i))
                                        End Select
                                    Case 2
                                        Dim x As Integer = (Screen.PrimaryScreen.Bounds.Width / 2) - (width) + (width * i) + (width * 0.62)
                                        Dim y As Integer = Screen.PrimaryScreen.Bounds.Height * 0.174
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i))
                                        End Select
                                End Select
                            Next
                        End If


                        '_________________________________________________________________________
                        'Readies the program for the next run and updates the session information
                        '_________________________________________________________________________
                        count += 1
                        Sess += 1
                        PPM += HighestPlat
                        lbStatus.ForeColor = Color.Lime
                        lbChecks.Text = "Checks this Session:              " & Sess
                        lbPPM.Text = "Platinum this Session:          " & PPM
                        tPPrice.Start()
                    Catch ex As Exception
                        lbStatus.ForeColor = Color.Orange
                        qItems.Clear()
                        qItems.Add(vbNewLine + "Something went wrong!")
                        Tray.Clear()
                        Tray.Display()
                        addLog(ex.ToString)
                        tPPrice.Start()
                    End Try
                End If
            End If
        Catch ex As Exception
            lbStatus.ForeColor = Color.Orange
            addLog(ex.ToString)
            tPPrice.Start()
        End Try
    End Sub
    Public Function Crop(img As Image, Optional mode As Integer = 0, Optional pos As Integer = 1, Optional players As Integer = 0) As Image
        '_________________________________________________________________________
        'Function used to crop the part names and usernames for player count
        '_________________________________________________________________________
        Dim startX As Integer
        Dim startY As Integer
        Dim height As Integer
        Dim width As Integer = 0.4 * img.Height
        Select Case mode
            Case 0 'This mode is used to get the number of players
                startX = (img.Width / 2) - (width * 2)
                startY = img.Height * 0.457
                height = img.Height * 0.03
                width = width * 4
            Case 4 '4 players for single lined parts
                startX = (img.Width / 2) - (width * 2) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 3 '3 players for single lined parts
                startX = (img.Width / 2) - (1.5 * width) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 2 '2 players for single lined parts
                startX = (img.Width / 2) - (width) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 1 'Case for multi-lined part names
                Select Case players
                    Case 4
                        startX = (img.Width / 2) - (width * 2) + (width * pos)
                        startY = img.Height * 0.4
                        height = img.Height * 0.03
                    Case 3
                        startX = (img.Width / 2) - (1.5 * width) + (width * pos)
                        startY = img.Height * 0.4
                        height = img.Height * 0.03
                    Case 2
                        startX = (img.Width / 2) - (width) + (width * pos)
                        startY = img.Height * 0.4
                        height = img.Height * 0.03
                End Select
        End Select


        '_________________________________________________________________________
        'Crops and returns the cropped image using the paramaters selected above
        '_________________________________________________________________________
        Dim CropRect As New Rectangle(startX, startY, width, height)
        Dim OriginalImage = img
        Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
        Using grp = Graphics.FromImage(CropImage)
            grp.DrawImage(OriginalImage, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            Return CropImage
        End Using
    End Function

    Public Sub addLog(txt As String)
        '_________________________________________________________________________
        'Function for storing log data
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

    Private Function GetMax(ByVal sFolder As String) As Long
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
        '_________________________________________________________________________
        'Stores settings and this was the start of me wanting to store information
        'So that users could generate a chart/graph showing how much platinum ducats they make over time
        '_________________________________________________________________________
        Dim tempPPMPD As String = ""
        Dim tempChecksPD As String = ""
        Dim isToday As Boolean = False
        For Each str As String In My.Settings.PPMPD.Split(vbNewLine)
            If Not str = "" Then
                If Not DateAdd(DateInterval.Day, 15, CType(str.Split("|")(0), Date)) < Today Then
                    If CType(str.Split("|")(0), Date) = Today Then
                        isToday = True
                        tempPPMPD &= vbNewLine & Today & "|" & str.Split("|")(1) + PPM
                    Else
                        tempPPMPD &= vbNewLine & str
                    End If
                End If
            End If
        Next
        If Not isToday Then
            tempPPMPD &= vbNewLine & Today & "|" & PPM
        End If

        isToday = False

        For Each str As String In My.Settings.ChecksPD.Split(vbNewLine)
            If Not str = "" Then
                If Not DateAdd(DateInterval.Day, 15, CType(str.Split("|")(0), Date)) > Today Then
                    If CType(str.Split("|")(0), Date) = Today Then
                        isToday = True
                        tempChecksPD &= vbNewLine & Today & "|" & str.Split("|")(1) + Sess
                    Else
                        tempChecksPD &= vbNewLine & str
                    End If
                End If
            End If
        Next
        If Not isToday Then
            tempChecksPD &= vbNewLine & Today & "|" & Sess
        End If
        My.Settings.PPMPD = tempPPMPD
        My.Settings.ChecksPD = tempChecksPD
        My.Settings.StartX = Me.Location.X
        My.Settings.StartY = Me.Location.Y
        My.Settings.Equipment = Equipment
        My.Settings.Save()
    End Sub
    Private Sub UpdateList()
        '_________________________________________________________________________
        'Function that retrieves parts and ducat prices from the wiki and stores the info in Names() and Ducks()
        '_________________________________________________________________________
        Try
            Equipment = My.Settings.Equipment ' Load equipment string

            lbStatus.ForeColor = Color.Yellow
            Dim duckString As String = ""
            Dim endpoint As String = New StreamReader(WebRequest.Create("http://warframe.wikia.com/wiki/Ducats/Prices/All").GetResponse().GetResponseStream()).ReadToEnd()
            Dim str1 As String = endpoint.Substring(endpoint.IndexOf("> Ducat Value"))
            Dim str2 As String = str1.Substring(0, str1.IndexOf("</div>"))
            Dim str3 As String = str2.Substring(str2.IndexOf("Acquisition"))
            Dim strArray As String() = str3.Split(New String(0) {"Acquisition"}, StringSplitOptions.None)
            Dim index As Integer = 0
            While index < strArray.Length
                Dim current As String = strArray(index)
                If current.Contains("</a>") Then
                    Dim name As String = current.Substring(current.IndexOf(">") + 1, current.IndexOf("<")).Substring(0, current.Substring(current.IndexOf(">") + 1, current.IndexOf("<")).IndexOf("<"))
                    Dim ducats As String = current.Substring(current.IndexOf("<b>") + 3)
                    ducats = ducats.Substring(0, ducats.IndexOf("<"))
                    Dim vStr As String = ""
                    Dim vBool As Boolean = False
                    If current.Contains("Prime Vault") Then
                        vBool = True
                        vStr = "*"
                    End If
                    If Not name.Contains("Carrier") And Not name.Contains("Wyrm") And Not name.Contains("Helios") Then
                        If name.Contains("Systems") Or name.Contains("Chassis") Or name.Contains("Neuroptics") Then
                            name += " Blueprint"
                        End If
                    End If
                    duckString += vStr & name & "," & ducats & "," & vBool & vbNewLine
                End If
                index += 1
            End While
            duckString = duckString.Remove(duckString.Length - 2, 1)
            My.Settings.DuckList = duckString
            My.Settings.LastUpdate = Date.Today
            My.Settings.Save()

            For Each str As String In My.Settings.DuckList.Split(vbNewLine)
                str.Replace(vbNewLine, "")
                Names.Add(str.Split(",")(0))
                Ducks.Add(str.Split(",")(1))
                Vaulted.Add(str.Split(",")(2))
            Next
            Names.Add("Forma Blueprint")
            Ducks.Add("0")

            lbStatus.ForeColor = Color.Lime
            tPPrice.Start()
        Catch ex As Exception
            addLog(ex.ToString)
        End Try
    End Sub

    Private Sub Main_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        '_________________________________________________________________________
        'Refreshes the application to stop graphical glitches caused by lockup
        'Starts the background timers
        '_________________________________________________________________________
        Me.Refresh()
        BGWorker.RunWorkerAsync()
        tPPrice.Enabled = True
        tPPrice.Start()
    End Sub
    Private Function GetPlayers(img As Image) As Integer
        '_________________________________________________________________________
        'Gets the number of seperate strings(players) in an image
        '_________________________________________________________________________
        Using img
            Dim wb As New WebBrowser
            wb.ScriptErrorsSuppressed = True
            wb.Navigate("about:blank")
            Dim doc As HtmlDocument = wb.Document.OpenNew(True)
            doc.Write(GetHOCR(img))
            Dim first As Boolean = True
            Dim prevDist As Integer
            Dim count As Integer = 1
            For Each element As HtmlElement In doc.All
                If element.GetAttribute("className") = "ocrx_word" Then
                    If first = False Then
                        If Not element.InnerText = "" Then
                            If prevDist + 100 < element.GetAttribute("title").Split(" ")(1) And element.InnerText.Length > 2 Then
                                count += 1
                                prevDist = element.GetAttribute("title").Split(" ")(1)
                            End If
                        End If
                    Else
                        If Not element.InnerText = "" Then
                            If element.InnerText.Length > 2 Then
                                prevDist = element.GetAttribute("title").Split(" ")(1)
                                first = False
                            End If
                        End If
                    End If
                End If
            Next
            Return count
        End Using
    End Function
    Public Shared Function ResizeImage(ByVal img As Image, multi As Double) As Image
        '_________________________________________________________________________
        'Used to improve OCR accuracy by blowing the image up
        '_________________________________________________________________________
        Return New Bitmap(img, New Size(img.Width * multi, img.Height * multi))
    End Function
    Private Function GetText(img As Image) As String
        '_________________________________________________________________________
        'Retrives the text from a cropped image
        '_________________________________________________________________________
        Using img
            Dim engine As New TesseractEngine("", "eng")
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            engine.DefaultPageSegMode = Tesseract.PageSegMode.SingleLine
            Dim page = engine.Process(ResizeImage(img, 1.1))
            Dim result As String = Regex.Replace(page.GetText(), "[^A-Za-z0-9\-_ /]", "")
            If Debug Then
                Dim nextFile As Integer = GetMax(appData & "\WFInfo\tests\")
                My.Computer.FileSystem.WriteAllText(appData + "\WFInfo\tests\" & nextFile & ".txt",
                result, False)
            End If
            Return result
        End Using
    End Function
    Private Function GetHOCR(img As Image) As String
        '_________________________________________________________________________
        'Retrieves the text information (location, type, etc) with OCR of an image
        '_________________________________________________________________________
        Using img
            Dim engine As New TesseractEngine("", "eng")
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-.")
            engine.DefaultPageSegMode = Tesseract.PageSegMode.SingleLine
            Dim page = engine.Process(ResizeImage(img, 1.1)).GetHOCRText(1)
            Return page
        End Using
    End Function

    Public Function KClean(guess As String)
        '_________________________________________________________________________
        'Prepares the part names for use with the Warframe.Market API
        '_________________________________________________________________________
        If Not guess.Contains("Carrier") And Not guess.Contains("Wyrm") And Not guess.Contains("Helios") Then
            If guess.Contains("Systems") Or guess.Contains("Chassis") Or guess.Contains("Neuroptics") Then
                guess = guess.Replace(" Blueprint", "")
            End If
        End If
        guess = guess.Replace("Band", "Collar Band").Replace("Buckle", "Collar Buckle").Replace("&amp;", "and")
        Return guess
    End Function


    Private Sub tPPrice_Tick(sender As Object, e As EventArgs) Handles tPPrice.Tick
        '_________________________________________________________________________
        'Keeps the background passive price check running
        '_________________________________________________________________________
        If Not bgPPrice.IsBusy And enablePPC And PassiveChecks Then
            bgPPrice.RunWorkerAsync()
        End If
    End Sub

    Private Sub bgPPrice_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgPPrice.DoWork
        '_________________________________________________________________________
        'Process that passively checks parts for platinum prices
        'This speeds up searches as you no longer have to search every part in a fissure
        '_________________________________________________________________________
        Try
            Dim found As Boolean = False
            Dim price As Integer = 0
            For i = 0 To PlatPrices.Count - 1
                If PlatPrices(i).Contains(Names(pCount)) Then
                    price = GetPlat(KClean(Names(pCount)))
                    PlatPrices(i) = Names(pCount) & "," & price
                    found = True
                End If
            Next
            If found = False Then
                price = GetPlat(KClean(Names(pCount)))
                PlatPrices.Add(Names(pCount) & "," & price)
            End If


            If pCount < Names.Count - 2 Then
                pCount += 1
            Else
                pCount = 0
            End If

            'This is the developer version of a function that checks for cheaply listed parts
            If devCheck Then
                Dim difference = GetPlat(KClean(Names(pCount)), getDif:=True)
                If difference >= 20 Then
                    Tray.Clear()
                    qItems.Add("-ALERT-" & vbNewLine & KClean(Names(pCount)) & vbNewLine & "Difference:  " & difference)
                    Tray.ShowDialog()
                    Tray.Dispose()
                End If
            End If
        Catch ex As Exception
            addLog(ex.ToString)
        End Try
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

    Private Sub pbDonate_Click(sender As Object, e As EventArgs) Handles pbDonate.Click
        Process.Start("https://sites.google.com/site/wfinfoapp/donate")
    End Sub

    Private Sub pbDonate_MouseEnter(sender As Object, e As EventArgs) Handles pbDonate.MouseEnter
        pbDonate.Image = My.Resources.Donate_h
    End Sub

    Private Sub pbDonate_MouseLeave(sender As Object, e As EventArgs) Handles pbDonate.MouseLeave
        pbDonate.Image = My.Resources.Donate
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

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnDebug1.Click
        '_________________________________________________________________________
        'Used to test how a message posted on the website will display
        '_________________________________________________________________________
        Dim curMessage As String = InputBox("Message String")
        curMessage = curMessage.Remove(0, curMessage.IndexOf("(message)") + 9)
        curMessage = curMessage.Remove(curMessage.IndexOf("(/message)"), curMessage.Length - curMessage.IndexOf("(/message)"))


        My.Settings.LastMessage = curMessage
        curMessage = curMessage.Replace("vbNewLine", vbNewLine)
        qItems.Add(vbNewLine & curMessage)
        Tray.Display()
    End Sub

    Private Sub tOnline_Tick(sender As Object, e As EventArgs) Handles tOnline.Tick
        UpdateStatus()
    End Sub

    Private Sub UpdateStatus()
        '_________________________________________________________________________
        'Keep-alive function for google analytics
        '_________________________________________________________________________
        Dim uri As New Uri("http://www.google-analytics.com/collect")
        Dim req As WebRequest = WebRequest.Create(uri)
        Dim pData As String = "v=1&tid=UA-97839771-1&cid=" & HID() & "&t=pageview&dp=Online"
        Dim pDataBytes = System.Text.Encoding.UTF8.GetBytes(pData)
        req.ContentType = "application/json"
        req.Method = "POST"
        req.ContentLength = pDataBytes.Length

        Dim stream = req.GetRequestStream()
        stream.Write(pDataBytes, 0, pDataBytes.Length)
        stream.Close()

        Dim response = req.GetResponse().GetResponseStream()

        Dim reader As New StreamReader(response)
        Dim res = reader.ReadToEnd()
        reader.Close()
        response.Close()
    End Sub

    Public Sub CheckUpdates()
        '_________________________________________________________________________
        'Checks for updates, did this really need an annotation? ;)
        '_________________________________________________________________________
        Dim curVersion As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/version")
        curVersion = curVersion.Remove(0, curVersion.IndexOf("text-align: center") + 29)
        curVersion = curVersion.Substring(0, curVersion.IndexOf("<"))


        If Not My.Settings.Version = curVersion Then
            UpdateWindow.Display(curVersion)
        End If

    End Sub

    Private Sub tUpdate_Tick(sender As Object, e As EventArgs) Handles tUpdate.Tick
        If My.Settings.CheckUpdates = True Then
            CheckUpdates()
        End If
        tUpdate.Enabled = False
        tUpdate.Stop()
    End Sub

    Private Sub tMessages_Tick(sender As Object, e As EventArgs) Handles tMessages.Tick
        '_________________________________________________________________________
        'Checks for messages on the website and displays them in a tray if new
        '_________________________________________________________________________
        If Messages Then
            Dim curMessage As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/message")
            curMessage = curMessage.Remove(0, curMessage.IndexOf("content=""(message)") + 18)
            curMessage = curMessage.Remove(curMessage.IndexOf("(/message)"), curMessage.Length - curMessage.IndexOf("(/message)"))


            If Not My.Settings.LastMessage = curMessage Then
                My.Settings.LastMessage = curMessage
                curMessage = curMessage.Replace("vbNewLine", vbNewLine)
                qItems.Add(vbNewLine & curMessage)
                Tray.Display()
            End If
        End If
    End Sub

    Private Sub btnDebug2_Click(sender As Object, e As EventArgs) Handles btnDebug2.Click
        '_________________________________________________________________________
        'Another debug button, this one was used when changing analytics
        '_________________________________________________________________________
        Dim uri As New Uri("http://www.google-analytics.com/collect")
        Dim req As WebRequest = WebRequest.Create(uri)
        Dim pData As String = "v=1&tid=UA-97839771-1&cid=" & HID() & "&t=pageview&dp=Online"
        Dim pDataBytes = System.Text.Encoding.UTF8.GetBytes(pData)
        req.ContentType = "application/json"
        req.Method = "POST"
        req.ContentLength = pDataBytes.Length

        Dim stream = req.GetRequestStream()
        stream.Write(pDataBytes, 0, pDataBytes.Length)
        stream.Close()

        Dim response = req.GetResponse().GetResponseStream()

        Dim reader As New StreamReader(response)
        Dim res = reader.ReadToEnd()
        reader.Close()
        response.Close()
    End Sub

    Private Function HID() As String
        '_________________________________________________________________________
        'Function that gets hardware ID used for uniquely identifying computers for analytics
        '_________________________________________________________________________

        ' Get the Windows Management Instrumentation object.
        Dim wmi As Object = GetObject("WinMgmts:")

        ' Get the "base boards" (mother boards).
        Dim serial_numbers As String = ""
        Dim mother_boards As Object =
        wmi.InstancesOf("Win32_BaseBoard")
        For Each board As Object In mother_boards
            serial_numbers &= ", " & board.SerialNumber
        Next board
        If serial_numbers.Length > 0 Then serial_numbers =
        serial_numbers.Substring(2)
        serial_numbers = GenSHA(serial_numbers) 'Encrypts serial number for safe transfer

        Return serial_numbers
    End Function

    Public Shared Function GenSHA(ByVal inputString) As String
        '_________________________________________________________________________
        'Function to encrypt HID for safe transfer on the net
        '_________________________________________________________________________
        Dim Sha256 As SHA256 = SHA256Managed.Create()
        Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(inputString)
        Dim hash As Byte() = Sha256.ComputeHash(bytes)
        Dim stringBuilder As New System.Text.StringBuilder()
        For i As Integer = 0 To hash.Length - 1
            stringBuilder.Append(hash(i).ToString("X2"))
        Next
        Return stringBuilder.ToString()
    End Function

    Private Sub BGWorker_DoWork(sender As Object, e As DoWorkEventArgs) Handles BGWorker.DoWork
        UpdateList()
    End Sub
End Class

Module Glob
    '_________________________________________________________________________
    'Global variables used for various things
    '_________________________________________________________________________
    Public qItems As New List(Of String)()
    Public HKey1 As Integer = My.Settings.HKey1
    Public HKey2 As Integer = My.Settings.HKey2
    Public Names As New List(Of String)()   ' List of Part Names
    Public Ducks As New List(Of String)()   ' Duck price associated with part
    Public Vaulted As New List(Of String)()   ' Is the part vaulted? True / False
    Public PlatPrices As New List(Of String)()   ' Stored list of plat prices
    Public HideShots As Boolean = False     ' Bool to hide screenshot notifications in fullscreen mode
    Public Equipment As String               ' List of leveled equipment
    Public Fullscreen As Boolean = False
    Public key1Tog As Boolean = False
    Public key2Tog As Boolean = False
    Public Animate As Boolean = My.Settings.Animate
    Public PassiveChecks As Boolean = My.Settings.PassiveChecks
    Public Messages As Boolean = My.Settings.Messages
    Public NewStyle As Boolean = My.Settings.NewStyle
    Public Debug As Boolean = My.Settings.Debug
    Public cookie As String = ""
    Public xcsrf As String = ""
    Public Function check(string1 As String) As Integer
        '_________________________________________________________________________
        'Checks the levDist of a string and returns the index in Names() of the closest part
        '_________________________________________________________________________
        string1 = string1.Replace("*", "")
        Dim Compare As New List(Of Integer)()
        For Each str As String In Names
            Compare.Add(LevDist(str, string1))
        Next
        Dim low As Integer = 9999
        Dim ind As Integer = 0
        For i = 0 To Compare.Count - 1
            If Compare(i) < low Then
                low = Compare(i)
                ind = i
            End If
        Next
        Return ind
    End Function
    Public Function checkSet(string1 As String) As String
        '_________________________________________________________________________
        'Returns a string of a set given a part name
        '_________________________________________________________________________
        string1 = string1.ToLower()
        string1 = string1.Replace("*", "")
        Dim Compare As New List(Of Integer)()
        For Each str As String In Names
            str = str.ToLower
            str = str.Replace("neuroptics", "")
            str = str.Replace("chassis", "")
            str = str.Replace("sytems", "")
            str = str.Replace("carapace", "")
            str = str.Replace("cerebrum", "")
            str = str.Replace("blueprint", "")
            str = str.Replace("harness", "")
            str = str.Replace("blade", "")
            str = str.Replace("pouch", "")
            str = str.Replace("barrel", "")
            str = str.Replace("receiver", "")
            str = str.Replace("stock", "")
            str = str.Replace("disc", "")
            str = str.Replace("grip", "")
            str = str.Replace("string", "")
            str = str.Replace("handle", "")
            str = str.Replace("ornament", "")
            str = str.Replace("wings", "")
            str = str.Replace("blades", "")
            str = str.Replace("hilt", "")
            str = RTrim(str)
            Compare.Add(LevDist(str, string1))
        Next
        Dim low As Integer = 9999
        Dim ind As Integer = 0
        For i = 0 To Compare.Count - 2
            If Compare(i) < low Then
                low = Compare(i)
                ind = i
            End If
        Next
        Dim rStr As String = Names(ind)
        rStr = rStr.ToLower
        rStr = rStr.Replace("neuroptics", "")
        rStr = rStr.Replace("chassis", "")
        rStr = rStr.Replace("sytems", "")
        rStr = rStr.Replace("carapace", "")
        rStr = rStr.Replace("cerebrum", "")
        rStr = rStr.Replace("blueprint", "")
        rStr = rStr.Replace("harness", "")
        rStr = rStr.Replace("blade", "")
        rStr = rStr.Replace("pouch", "")
        rStr = rStr.Replace("head", "")
        rStr = rStr.Replace("barrel", "")
        rStr = rStr.Replace("receiver", "")
        rStr = rStr.Replace("stock", "")
        rStr = rStr.Replace("disc", "")
        rStr = rStr.Replace("grip", "")
        rStr = rStr.Replace("string", "")
        rStr = rStr.Replace("handle", "")
        rStr = rStr.Replace("ornament", "")
        rStr = rStr.Replace("wings", "")
        rStr = rStr.Replace("blades", "")
        rStr = rStr.Replace("hilt", "")
        rStr = RTrim(rStr) & " set"
        rStr = StrConv(rStr, VbStrConv.ProperCase)
        Return rStr
    End Function
    Public Function LevDist(ByVal s As String,
                                    ByVal t As String) As Integer
        '_________________________________________________________________________
        'LevDist determines how "close" a jumbled name is to an actual name
        'For example: Nuvo Prime is closer to Nova Prime (2) then Ash Prime (4)
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
    Public Function GetPlat(str As String, Optional getUser As Boolean = False, Optional getMod As Boolean = False, Optional getID As Boolean = False, Optional getDif As Boolean = False) As String
        '_________________________________________________________________________
        'Retrieves a plat price of a part or set via warframe.market
        '_________________________________________________________________________

        '_________________________________________________________________________
        'Prepare the name for the API
        '_________________________________________________________________________
        Dim partName As String = str
        partName = partName.Replace(vbLf, "").Replace("*", "")
        str = str.ToLower
        str = str.Replace(" ", "%5F").Replace(vbLf, "").Replace("*", "")


        '_________________________________________________________________________
        'Make the request
        '_________________________________________________________________________
        Dim webClient As New System.Net.WebClient
        webClient.Headers.Add("platform", "pc")
        webClient.Headers.Add("language", "en")
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12
        Dim result As JObject
        result = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + str + "/orders"))


        '_________________________________________________________________________
        'If ingame and online, and selling, add the price and name to a list
        '_________________________________________________________________________
        Dim platCheck As New List(Of Integer)()
        Dim userCheck As New List(Of String)()
        For i = 0 To result("payload")("orders").Count - 1
            If result("payload")("orders")(i)("user")("status") = "ingame" Then
                If result("payload")("orders")(i)("order_type") = "sell" Then
                    platCheck.Add(result("payload")("orders")(i)("platinum"))
                    userCheck.Add(result("payload")("orders")(i)("user")("ingame_name"))
                End If
            End If
        Next


        '_________________________________________________________________________
        'Compare the prices to get the lowest price listed
        '_________________________________________________________________________
        If platCheck.Count = 0 Then
            Return 0
        End If
        Dim low As Integer = 999999
        Dim user As String = ""
        Dim minUsers(4) As String
        Dim minPrices(4) As Integer
        Dim total As Integer = 0
        For x = 0 To 4
            low = 999999
            For i = 0 To platCheck.Count - 1
                If (Not userCheck(i).Contains("XB1")) And (Not userCheck(i).Contains("PS4")) And (Not minUsers.Contains(userCheck(i))) Then
                    If platCheck(i) < low Then
                        low = platCheck(i)
                        user = userCheck(i)
                    End If
                End If
            Next
            minUsers(x) = user
            minPrices(x) = low
            total += low
        Next
        user = minUsers(0)
        low = total / 5


        '_________________________________________________________________________
        'If you toggle getUser:
        'Returns the username of the lowest seller and copies a buy message to clipboard
        '_________________________________________________________________________
        If getUser Then
            Clipboard.SetText("/w " & user & " Hi, I would like to buy your " & partName & " for " & low & " Platinum.")
            Return low & vbNewLine & "    User: " & user

        ElseIf getID Then 'Used to list parts on warframe.market, returns the ID of the searched part
            result = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + str))
            Return result("payload")("item")("id")
        ElseIf getDif Then 'Gets the difference between the lowest price and the next cheapest (Dev tool for cheap listings)
            platCheck.Clear()
            userCheck.Clear()
            For i = 0 To result("payload")("orders").Count - 1
                If result("payload")("orders")(i)("user")("status") = "ingame" Then
                    If (result("payload")("orders")(i)("order_type") = "sell") And (result("payload")("orders")(i)("user")("region") = "en") Then
                        platCheck.Add(result("payload")("orders")(i)("platinum"))
                        userCheck.Add(result("payload")("orders")(i)("user")("ingame_name"))
                    End If
                End If
            Next

            If platCheck.Count = 0 Then
                Return 0
            End If

            Dim firstLow As String = ""
            For x = 0 To 1
                low = 999999
                For i = 0 To platCheck.Count - 1
                    If (Not userCheck(i).Contains("XB1")) And (Not userCheck(i).Contains("PS4")) And (Not userCheck(i) = firstLow) Then
                        If platCheck(i) < low Then
                            low = platCheck(i)
                            user = userCheck(i)
                        End If
                    End If
                Next
                firstLow = user
            Next
            Dim difference As Integer = Math.Abs(minPrices(0) - low)
            Return difference
        Else 'Not Single Pull
            Return low
        End If
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
End Module