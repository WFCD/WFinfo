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
            pbHome.Location = New Point(0, 8)
            pbDonate.Parent = pbSideBar
            pbDonate.Location = New Point(0, 38)
            pbSettings.Parent = pbSideBar
            pbSettings.Location = New Point(0, 65)
            pbRelic.Parent = pbSideBar
            pbRelic.Location = New Point(0, 92)
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

    Private Async Sub tPB_Tick(sender As Object, e As EventArgs) Handles tPB.Tick
        '_________________________________________________________________________
        'This is the main timer that triggers on hotkey and controls most function
        'The toggles make sure it runs only once per press
        '_________________________________________________________________________
        Try
            Dim Refresh As Integer = GetAsyncKeyState(HKey3)
            If GetAsyncKeyState(HKey3) And &H8000 Then
                If Not TargetSelector.Visible = True Then
                    TargetSelector.Show()
                End If
            End If

            If (Not key1Tog) And (Not key2Tog) And My.Settings.TargetAreaSet Then
                '_________________________________________________________________________
                'Refreshes the async state and checks and opens command function if hotkey is pressed
                '_________________________________________________________________________
                Refresh = GetAsyncKeyState(HKey1)
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
                        Dim screenshot As System.Drawing.Bitmap
                        Dim graph As Graphics
                        screenshot = New System.Drawing.Bitmap(My.Settings.RecSize.X, My.Settings.RecSize.Y, System.Drawing.Imaging.PixelFormat.Format32bppRgb)
                        graph = Graphics.FromImage(screenshot)
                        graph.CopyFromScreen(My.Settings.StartPoint.X, My.Settings.StartPoint.Y, 0, 0, My.Settings.RecSize, CopyPixelOperation.SourceCopy)
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
                                ' Modified
                                Dim guess As String = check(tList(i))
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
                                ' Modified
                                ' Dim guess As String = Names(check(GetText(img) + " Blueprint Blueprint"))
                                Dim guess As String = check(GetText(img) + " Blueprint")
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
                        Dim v As New List(Of Boolean)()
                        Dim n As New List(Of String)()
                        For i = 0 To finalList.Count - 1
                            Dim guess As String = finalList(i)
                            If Not finalList(i) = "Forma Blueprint" Then
                                Dim plat As String = ""
                                Dim ducat As String = ""
                                Dim job As JObject = Nothing
                                If ducat_plat.TryGetValue(guess, job) Then
                                    plat = job("plat").ToString()
                                    p.Add(plat)
                                    ducat = job("ducats").ToString()
                                    d.Add(ducat)
                                Else
                                    Dim plat_int As Integer = GetPlat(KClean(guess))
                                    plat = plat_int
                                    If plat_int > HighestPlat Then
                                        HighestPlat = plat_int
                                    End If
                                    If plat_int < 0 Then
                                        plat = "X"
                                    End If
                                    p.Add(plat)

                                    If plat = "X" OrElse plat = 0 Then
                                        ducat = plat
                                    Else
                                        ducat = ducat_plat(check(guess))("ducats").ToString()
                                    End If
                                    d.Add(ducat)
                                End If
                                guess = KClean(guess)

                                If guess.Length > 27 Then
                                    n.Add(guess.Substring(0, 27) & "...")
                                Else
                                    n.Add(guess)
                                End If

                                ' TODO: Add in "vaulted" to ducat_plat database
                                '       And probably should rename it to prime_parts or something
                                '       Then have check like this:
                                ' v.Add(job("vaulted").ToObject(Of Boolean))
                                v.Add(True)

                                If guess.Length > 27 Then
                                    qItems.Add(guess.Substring(0, 27) & "..." & vbNewLine & "    Ducks: " & ducat & "   Plat: " & plat & vbNewLine)
                                Else
                                    qItems.Add(guess & vbNewLine & "    Ducks: " & ducat & "   Plat: " & plat & vbNewLine)
                                End If
                            Else
                                n.Add(vbNewLine & KClean(guess))
                                p.Add(0)
                                d.Add(0)
                                v.Add(False)
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
                                Dim width As Integer = (CliptoImage.Width / 4)
                                Dim y As Integer = My.Settings.StartPoint.Y + (My.Settings.StartPoint.Y * 0.05)
                                Select Case players
                                    Case 4
                                        Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 0.25))
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i), v(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i), v(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i), v(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i), v(i))
                                        End Select
                                    Case 3
                                        Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 0.75))
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i), v(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i), v(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i), v(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i), v(i))
                                        End Select
                                    Case 2
                                        Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 1.25))
                                        Select Case i
                                            Case 0
                                                panel1.Display(x, y, p(i), d(i), v(i))
                                            Case 1
                                                panel2.Display(x, y, p(i), d(i), v(i))
                                            Case 2
                                                panel3.Display(x, y, p(i), d(i), v(i))
                                            Case 3
                                                panel4.Display(x, y, p(i), d(i), v(i))
                                        End Select
                                End Select
                            Next

                            If DisplayNames Then
                                'Each plaque has it's own panel/overlay
                                Dim plaque1 As New NamePlaque
                                Dim plaque2 As New NamePlaque
                                Dim plaque3 As New NamePlaque
                                Dim plaque4 As New NamePlaque
                                For i = 0 To players - 1
                                    Dim width As Integer = (CliptoImage.Width / 4)
                                    Dim y As Integer = My.Settings.StartPoint.Y + (My.Settings.RecSize.Y * 0.93)
                                    Dim w As Integer = (((CliptoImage.Width / 4) * 0.8) - My.Settings.StartPoint.X) + 125
                                    Dim x As Integer = 0
                                    Select Case players
                                        Case 4
                                            x = (width * i) + (width * 0.25) + (i * width * 0.005)
                                        Case 3
                                            x = (width * i) + (width * 0.75) + (i * width * 0.005)
                                        Case 2
                                            x = (width * i) + (width * 1.25) + (i * width * 0.005)
                                    End Select
                                    Select Case i
                                        Case 0
                                            plaque1.Display(x, y, w, n(i))
                                        Case 1
                                            plaque2.Display(x, y, w, n(i))
                                        Case 2
                                            plaque3.Display(x, y, w, n(i))
                                        Case 3
                                            plaque4.Display(x, y, w, n(i))
                                    End Select
                                Next
                                End if
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
            Else
                Refresh = GetAsyncKeyState(HKey1)

                If GetAsyncKeyState(HKey1) And &H8000 And scTog = 0 Then
                    scTog = 1
                    MsgBox("You must first set the target area!" & vbNewLine & vbNewLine & "1.) Get to the Fissure Reward Screen with 4 players." & vbNewLine & "2.) Press " & My.Settings.HKey3Text & " to show the selection cursor." & vbNewLine & "3.) Click the upper left corner of the first reward." & vbNewLine & "4.) Click the lower right corner of the last reward.", MsgBoxStyle.SystemModal)
                ElseIf Not GetAsyncKeyState(HKey1) And &H8000 And scTog = 1 Then
                    scTog = 0
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
        Dim height As Integer = img.Height * 0.1
        Dim width As Integer = (img.Width / 4)
        Select Case mode
            Case 0 'This mode is used to get the number of players
                startX = 0
                startY = img.Height - height
                height = img.Height - startY
                width = img.Width
            Case 4 '4 players for single lined parts
                startX = (img.Width / 4) * pos
                startY = img.Height - (height * 2)
            Case 3 '3 players for single lined parts
                startX = (img.Width / 4) * pos + ((img.Width / 4) * 0.5)
                startY = img.Height - (height * 2)
            Case 2 '2 players for single lined parts
                startX = (img.Width / 4) * (pos + 1)
                startY = img.Height - (height * 2)
            Case 1 'Case for multi-lined part names
                Select Case players
                    Case 4
                        startX = (img.Width / 4) * pos
                        startY = img.Height - (height * 2.65)
                        height = img.Height * 0.08
                    Case 3
                        startX = (img.Width / 4) * pos + ((img.Width / 4) * 0.5)
                        startY = img.Height - (height * 2.65)
                        height = img.Height * 0.08
                    Case 2
                        startX = (img.Width / 4) * (pos + 1)
                        startY = img.Height - (height * 2.65)
                        height = img.Height * 0.08
                End Select
        End Select


        '_________________________________________________________________________
        'Crops and returns the cropped image using the paramaters selected above
        '_________________________________________________________________________
        Dim CropRect As New Rectangle(startX, startY, width, height)
        Dim OriginalImage = img
        If Debug Then
            img.Save(appData & "\WFInfo\tests\" & "Original Test" & ".jpg", Imaging.ImageFormat.Jpeg)
        End If
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
        'My.Settings.Equipment = Equipment
        My.Settings.Save()
    End Sub

    Private Sub Load_Market_Items()
        Dim webClient As New System.Net.WebClient
        webClient.Headers.Add("platform", "pc")
        webClient.Headers.Add("language", "en")
        Dim m_i_temp As JObject = JsonConvert.DeserializeObject(Of JObject)(WebClient.DownloadString("https://api.warframe.market/v1/items"))
        market_items = New Dictionary(Of String, String)()
        For Each elem As JObject In m_i_temp("payload")("items")("en")
            Dim name As String = elem("item_name")
            If name.Contains("Prime ") Then
                market_items(elem("id")) = name + "|" + elem("url_name").ToString()
            End If
        Next
        File.WriteAllText(items_file_path, JsonConvert.SerializeObject(market_items, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Private Sub UpdateList()
        '_________________________________________________________________________
        'Function that retrieves parts and ducat prices from the wiki and stores the info in Names() and Ducks()
        '_________________________________________________________________________
        Try
            'Equipment = My.Settings.Equipment ' Load equipment string
            lbStatus.ForeColor = Color.Yellow
            Dim webClient As New System.Net.WebClient
            webClient.Headers.Add("platform", "pc")
            webClient.Headers.Add("language", "en")
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12


            If market_items Is Nothing AndAlso File.Exists(items_file_path) Then
                market_items = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(File.ReadAllText(items_file_path))
            End If

            If market_items Is Nothing Then
                Load_Market_Items()
            End If

            If ducat_plat Is Nothing AndAlso File.Exists(ducat_file_path) Then
                ducat_plat = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(ducat_file_path))
            End If

            Dim temp_bool = ducat_plat Is Nothing
            If Not temp_bool Then
                Dim timestamp As Date = DateTime.Parse(ducat_plat("timestamp"))
                Dim dayAgo As Date = Date.Now.AddDays(-1)
                temp_bool = timestamp < dayAgo
            End If

            If temp_bool Then
                Dim d_p_temp As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/tools/ducats"))
                ducat_plat = New JObject()
                For Each elem As JObject In d_p_temp("payload")("previous_day")
                    Dim item_name As String = ""
                    If Not market_items.TryGetValue(elem("item"), item_name) Then
                        Console.WriteLine("DURING DUCAT/PLAT LOAD: CAN'T FIND THIS ID -- " + elem("item").ToString())
                        Load_Market_Items()
                        item_name = market_items(elem("item"))
                    End If
                    item_name = item_name.Split("|")(0)
                    If Not item_name.Contains("Set") Then
                        ducat_plat(item_name) = New JObject()
                        ducat_plat(item_name)("ducats") = elem("ducats")
                        ducat_plat(item_name)("plat") = elem("wa_price")
                    End If
                Next

                ducat_plat("timestamp") = Date.Now.ToString("R")
                If Not ducat_plat.TryGetValue("Forma Blueprint", Nothing) Then
                    Dim job As New JObject()
                    job("ducats") = 0
                    job("plat") = 0
                    ducat_plat("Forma Blueprint") = job
                End If
                File.WriteAllText(ducat_file_path, JsonConvert.SerializeObject(ducat_plat, Newtonsoft.Json.Formatting.Indented))
            End If

            For Each elem As KeyValuePair(Of String, String) In market_items
                Dim name As String = elem.Value.Split("|")(0)
                Dim ducat As String = "0"
                Dim d_p As JObject = Nothing
                If ducat_plat.TryGetValue(name, d_p) Then
                    ducat = d_p("ducats")
                    'PlatPrices.Add(name & "," & d_p("plat").ToString())
                End If
                'Names.Add(name)
                'Ducks.Add(ducat)
            Next

            'Names.Add("Forma Blueprint")
            'Ducks.Add("0")

            Load_Relic_Data()

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
            img = RemoveNoise(Sharpen(prepare(ResizeImage(img, 1.1)), 6))
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
        guess = guess.Replace("&amp;", "and")
        Return guess
    End Function


    Private Sub tPPrice_Tick(sender As Object, e As EventArgs) Handles tPPrice.Tick
        '_________________________________________________________________________
        'Keeps the background passive price check running
        '_________________________________________________________________________
        If Not bgPPrice.IsBusy And enablePPC Then
            bgPPrice.RunWorkerAsync()
        End If
    End Sub

    Private Sub bgPPrice_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgPPrice.DoWork
        '_________________________________________________________________________
        'Process that passively checks parts for platinum prices
        'This speeds up searches as you no longer have to search every part in a fissure
        '_________________________________________________________________________

        ' TODO:
        '   Change this to add missing plat vals, and to check if need to reload plat vals
        '   Make sure that when reloading plat vals, also reload the "fix list"


        'Try
        '    Dim found As Boolean = False
        '    Dim price As Integer = 0
        '    For i = 0 To PlatPrices.Count - 1
        '        If PlatPrices(i).Contains(Names(pCount)) Then
        '            price = GetPlat(KClean(Names(pCount)))
        '            PlatPrices(i) = Names(pCount) & "," & price
        '            found = True
        '        End If
        '    Next
        '    If found = False Then
        '        price = GetPlat(KClean(Names(pCount)))
        '        PlatPrices.Add(Names(pCount) & "," & price)
        '    End If


        '    If pCount < Names.Count - 2 Then
        '        pCount += 1
        '    Else
        '        pCount = 0
        '    End If

        '    'This is the developer version of a function that checks for cheaply listed parts
        '    If devCheck Then
        '        Dim difference = GetPlat(KClean(Names(pCount)), getDif:=True)
        '        If difference >= 20 Then
        '            Tray.Clear()
        '            qItems.Add("-ALERT-" & vbNewLine & KClean(Names(pCount)) & vbNewLine & "Difference:  " & difference)
        '            Tray.ShowDialog()
        '            Tray.Dispose()
        '        End If
        '    End If
        'Catch ex As Exception
        '    addLog(ex.ToString)
        'End Try
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

    Private Sub pbRelic_Click(sender As Object, e As EventArgs) Handles pbRelic.Click
        If relic_data IsNot Nothing Then
            Load_Relic_Tree()
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
        Try
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
        Catch ex As exception
            tOnline.enabled = False
            tOnline.stop
        End Try
    End Sub

    Public Sub CheckUpdates()
        '_________________________________________________________________________
        'Checks for updates, did this really need an annotation? ;)
        '_________________________________________________________________________
        Try
            Dim curVersion As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/version")
            curVersion = curVersion.Remove(0, curVersion.IndexOf("text-align: center") + 29)
            curVersion = curVersion.Substring(0, curVersion.IndexOf("<"))


            If Not My.Settings.Version = curVersion Then
                If My.Settings.Version.Contains("b") Then
                    If My.Settings.Version.Replace("b", "") = curVersion Then
                        UpdateWindow.Display(curVersion)
                    End If
                Else
                    UpdateWindow.Display(curVersion)
                End If
            End If
        Catch ex As exception
        End Try

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
        TargetSelector.Show()
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

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint

    End Sub
End Class

Module Glob
    '_________________________________________________________________________
    'Global variables used for various things
    '_________________________________________________________________________
    Public qItems As New List(Of String)()
    Public HKey1 As Integer = My.Settings.HKey1
    Public HKey2 As Integer = My.Settings.HKey2
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
    Public market_items As Dictionary(Of String, String)                ' warframe.market item listing                  {<id>: "<name>|<url_name>", ...}
    Public items_file_path As String = Path.Combine(appData, "WFInfo\market_items.json")
    Public ducat_plat As JObject                                        ' contains warframe.market ducatonator listing  {<partName>: {"ducats": <ducat_val>,"plat": <plat_val>}, ...}
    Public ducat_file_path As String = Path.Combine(appData, "WFInfo\ducat_plat.json")
    Public relic_data As JObject                                        ' Contains relic_data from Warframe PC Drops    {<Era>: {"A1":{"vaulted": true,<rare1/uncommon[12]/common[123]>: <part>}, ...}, "Meso": ..., "Neo": ..., "Axi": ...}
    Public relic_file_path As String = Path.Combine(appData, "WFInfo\relic_data.json")
    Public hidden_nodes As JObject                                      ' Contains list of nodes to hide                {"Lith": ["A1","A2",...], "Meso": [...], "Neo": [...], "Axi": [...]}
    Public hidden_file_path As String = Path.Combine(appData, "WFInfo\hidden.json")
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
    Public cookie As String = ""
    Public xcsrf As String = ""
    Public Function check(string1 As String) As String
        '_________________________________________________________________________
        'Checks the levDist of a string and returns the index in Names() of the closest part
        '_________________________________________________________________________
        string1 = string1.Replace("*", "")
        Dim lowest As String = Nothing
        Dim low As Integer = 9999
        For Each prop As KeyValuePair(Of String, JToken) In ducat_plat
            Dim val As Integer = LevDist(prop.Key, string1)
            If val < low Then
                low = val
                lowest = prop.Key
            End If
        Next
        Return lowest
    End Function
    Public Function checkSet(string1 As String) As String
        '_________________________________________________________________________
        'Returns a string of a set given a part name
        '_________________________________________________________________________
        string1 = string1.ToLower()
        string1 = string1.Replace("*", "")
        Dim rStr As String = Nothing
        Dim low As Integer = 9999
        ' Modified
        For Each prop As KeyValuePair(Of String, JToken) In ducat_plat
            Dim str As String = prop.Key.ToLower
            str = Str.Replace("neuroptics", "")
            Str = Str.Replace("chassis", "")
            Str = Str.Replace("sytems", "")
            Str = Str.Replace("carapace", "")
            Str = Str.Replace("cerebrum", "")
            Str = Str.Replace("blueprint", "")
            Str = Str.Replace("harness", "")
            Str = Str.Replace("blade", "")
            Str = Str.Replace("pouch", "")
            Str = Str.Replace("barrel", "")
            Str = Str.Replace("receiver", "")
            Str = Str.Replace("stock", "")
            Str = Str.Replace("disc", "")
            Str = Str.Replace("grip", "")
            Str = Str.Replace("string", "")
            Str = Str.Replace("handle", "")
            Str = Str.Replace("ornament", "")
            Str = Str.Replace("wings", "")
            Str = Str.Replace("blades", "")
            Str = Str.Replace("hilt", "")
            str = RTrim(str)
            Dim val As Integer = LevDist(str, string1)
            If val < low Then
                low = val
                rStr = prop.Key
            End If
        Next
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

    Public Sub Load_Relic_Data()
        Dim request As WebRequest = Nothing
        If File.Exists(relic_file_path) Then
            request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
            request.Method = "HEAD"
            ' Move last_mod back one hour, so that it doesn't equal timestamp
            Dim last_mod As Date = DateTime.Parse(request.GetResponse().Headers.Get("Last-Modified")).AddHours(-1)
            relic_data = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(relic_file_path))
            Dim ignore As JToken = Nothing
            If relic_data.TryGetValue("timestamp", ignore) Then
                Dim timestamp As Date = DateTime.Parse(relic_data("timestamp"))
                If last_mod < timestamp Then
                    Return
                End If
            End If
        End If
        relic_data = New JObject()
        request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
        Dim response As WebResponse = request.GetResponse()
        relic_data("timestamp") = DateTime.Parse(response.Headers.Get("Last-Modified"))
        Dim drop_data As String = Nothing
        Using reader As New StreamReader(response.GetResponseStream(), System.Text.ASCIIEncoding.ASCII)
            drop_data = reader.ReadToEnd()
        End Using

        Dim first As Integer = drop_data.IndexOf("id=""relicRewards""")
        first = drop_data.IndexOf("<table>", first)
        Dim last As Integer = drop_data.IndexOf("</table>", first)
        Dim index As Integer = drop_data.IndexOf("<tr>", first)
        Dim tr_stop As Integer = 0
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            If sub_str.Contains("Relic") AndAlso sub_str.Contains("Intact") Then
                sub_str = Regex.Replace(sub_str, "<[^>]+>|\([^\)]+\)", "")
                Dim split As String() = sub_str.Split(" ")
                Dim era As String = split(0)
                Dim relic As String = split(1)
                Dim ignore As JObject = Nothing
                If Not relic_data.TryGetValue(era, ignore) Then
                    relic_data(era) = New JObject()
                End If
                relic_data(era)(relic) = New JObject()
                relic_data(era)(relic)("vaulted") = True
                Dim cmnNum As Integer = 1
                Dim uncNum As Integer = 1
                index = drop_data.IndexOf("<tr", tr_stop)
                tr_stop = drop_data.IndexOf("</tr>", index)
                sub_str = drop_data.Substring(index, tr_stop - index)
                While Not sub_str.Contains("blank-row")
                    sub_str = sub_str.Replace("<tr><td>", "").Replace("</td>", "").Replace("td>", "")
                    split = sub_str.Split("<")
                    If split(1).Contains("2.") Then
                        relic_data(era)(relic)("rare1") = split(0)
                    ElseIf split(1).Contains("11") Then
                        relic_data(era)(relic)("uncommon" + uncNum.ToString()) = split(0)
                        uncNum += 1
                    Else
                        relic_data(era)(relic)("common" + cmnNum.ToString()) = split(0)
                        cmnNum += 1
                    End If

                    index = drop_data.IndexOf("<tr", tr_stop)
                    tr_stop = drop_data.IndexOf("</tr>", index)
                    sub_str = drop_data.Substring(index, tr_stop - index)
                End While

            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Missions
        last = drop_data.IndexOf("id=""relicRewards""")
        index = drop_data.IndexOf("<tr>")
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Dim split As String() = sub_str.Split(" ")
                relic_data(split(0))(split(1))("vaulted") = False
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Special Rewards
        last = drop_data.IndexOf("id=""modByAvatar""")
        index = drop_data.IndexOf("id=""keyRewards""")
        index = drop_data.IndexOf("<tr>", index)
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Dim split As String() = sub_str.Split(" ")
                Dim ignore As JToken = Nothing
                If relic_data.TryGetValue(split(0), ignore) Then
                    relic_data(split(0))(split(1))("vaulted") = False
                End If
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        Save_Relic_Data()
    End Sub

    Public Sub Load_Relic_Tree()
        If Relics.RelicTree.Nodes(0).Nodes.Count > 1 Then
            Return
        End If
        For Each node As TreeNode In Relics.RelicTree.Nodes
            For Each relic As JProperty In relic_data(node.Text)
                Dim kid As New TreeNode(relic.Name)
                kid.Name = relic.Name
                node.Nodes.Add(kid)

                kid.Nodes.Add(relic.Value("rare1").ToString()).ForeColor = rareColor
                kid.Nodes.Add(relic.Value("uncommon1").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("uncommon2").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("common1").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common2").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common3").ToString()).ForeColor = commonColor
                Dim rtot As Double = 0
                Dim itot As Double = 0
                Dim rperc As Double = 0.1
                Dim iperc As Double = 0.02
                Dim count As Integer = 0
                For Each temp As TreeNode In kid.Nodes
                    If temp.Parent.FullPath = kid.FullPath Then
                        If Not ducat_plat.TryGetValue(temp.Text, Nothing) And temp.Text <> "Forma Blueprint" Then
                            If temp.Text.Contains("Kavasa") Then
                                If temp.Text.Contains("Kubrow") Then
                                    temp.Text = temp.Text.Replace("Kubrow ", "")
                                Else
                                    temp.Text = temp.Text.Replace("Prime", "Prime Collar")
                                End If
                            ElseIf Not temp.Text.Contains("Prime Blueprint") Then
                                temp.Text = temp.Text.Replace(" Blueprint", "")
                            End If
                            If Not ducat_plat.TryGetValue(temp.Text, Nothing) Then
                                Console.WriteLine("LOADING: MISSING PLAT -- " + temp.FullPath + " -- " + temp.Text)
                            End If
                        End If
                        If ducat_plat.TryGetValue(temp.Text, Nothing) Then
                            If count > 2 Then
                                rperc = 0.5 / 3.0
                                iperc = 0.76 / 3.0
                            ElseIf count > 0 Then
                                rperc = 0.2
                                iperc = 0.11
                            End If
                            Dim plat As Double = Double.Parse(ducat_plat(temp.Text)("plat"))
                            rtot += plat * rperc
                            itot += plat * iperc
                            count += 1
                        End If
                    End If
                Next
                rtot -= itot
                relic_data(node.Text)(relic.Name)("rad") = rtot
                relic_data(node.Text)(relic.Name)("int") = itot
                kid = kid.Clone()
                kid.Text = node.Text + " " + relic.Name
                Relics.RelicTree2.Nodes.Add(kid)
            Next
            node.Nodes.Add("Hidden").Name = "Hidden"
        Next
        Relics.RelicTree2.Nodes.Add("Hidden").Name = "Hidden"

        Load_Hidden_Nodes()

        Relics.RelicTree.TreeViewNodeSorter = Relics.Tree1Sorter
        Relics.RelicTree2.TreeViewNodeSorter = Relics.Tree2Sorter
        Relics.RelicTree.Sort()
        Relics.RelicTree2.Sort()
    End Sub

    Private Sub Load_Hidden_Nodes()
        If File.Exists(hidden_file_path) Then
            hidden_nodes = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(hidden_file_path))

            For Each node As TreeNode In Relics.RelicTree.Nodes
                For Each hide As JValue In hidden_nodes(node.Text)
                    Dim move As TreeNode = node.Nodes.Find(hide.Value, False)(0)
                    node.Nodes.Remove(move)
                    node.Nodes.Find("Hidden", False)(0).Nodes.Add(move)
                    For Each found As TreeNode In Relics.RelicTree2.Nodes.Find(hide.Value, False)
                        If found.Text.Equals(node.Text + " " + hide.Value) Then
                            Relics.RelicTree2.Nodes.Remove(found)
                            Relics.RelicTree2.Nodes.Find("Hidden", False)(0).Nodes.Add(found)
                        End If
                    Next

                Next
            Next
        Else
            hidden_nodes = New JObject()
            hidden_nodes("Lith") = New JArray()
            hidden_nodes("Meso") = New JArray()
            hidden_nodes("Neo") = New JArray()
            hidden_nodes("Axi") = New JArray()
            File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
        End If
    End Sub

    Public Sub Save_Relic_Data()
        File.WriteAllText(relic_file_path, JsonConvert.SerializeObject(relic_data, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Public Sub Find_Item(item_name As String, url As String)
        Dim webClient As New System.Net.WebClient
        webClient.Headers.Add("platform", "pc")
        webClient.Headers.Add("language", "en")
        Dim stats As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + url + "/statistics"))
        stats = stats("payload")("statistics_closed")("90days").Last

        Dim ducats As JObject = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + url))
        ducats = ducats("payload")("item")
        Dim id As String = ducats("id")
        For Each part As JObject In ducats("items_in_set")
            If part("id").ToString() = id Then
                ducats = part
                Exit For
            End If
        Next
        Dim ducat As String = Nothing
        If Not ducats.TryGetValue("ducats", ducat) Then
            ducat = "0"
        End If
        ducat_plat(item_name) = New JObject()
        ducat_plat(item_name)("ducats") = ducat
        ducat_plat(item_name)("plat") = stats("avg_price")
        File.WriteAllText(ducat_file_path, JsonConvert.SerializeObject(ducat_plat, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Public Function GetMarketData(name As String) As JObject
        Dim ret As JObject = Nothing
        If ducat_plat.TryGetValue(name, ret) Then
            Return ret
        End If

        For Each kvp As KeyValuePair(Of String, String) In market_items
            If kvp.Value.Contains(name) Then
                Dim split As String() = kvp.Value.Split("|")
                Find_Item(split(0), split(1))
                If ducat_plat.TryGetValue(name, ret) Then
                    Return ret
                End If
            End If
        Next

        Console.WriteLine("CANNOT FIND """ + name + """ IN MARKET ITEMS")
        ret = New JObject()
        ret("ducats") = 0
        ret("plat") = 0
        Return ret
    End Function

    Public Function GetPlat(str As String, Optional getUser As Boolean = False, Optional getMod As Boolean = False, Optional getID As Boolean = False, Optional getDif As Boolean = False) As Integer

        Dim partName As String = str
        partName = partName.Replace(vbLf, "").Replace("*", "")
        str = str.ToLower
        str = str.Replace(" ", "%5F").Replace(vbLf, "").Replace("*", "")

        Dim elem As JObject = Nothing
        If Not ducat_plat.TryGetValue(partName, elem) Then
            Dim partName2 As String = partName.Replace("and", "&")
            If Not ducat_plat.TryGetValue(partName2, elem) Then
                Find_Item(partName, str.Replace("&", "and"))
                If Not ducat_plat.TryGetValue(partName, elem) Then
                    Return 0
                End If
            End If
        End If
        Return elem("plat")
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
    Public Function prepare(img As Image) As Image
        Using img
            Dim X As Integer
            Dim Y As Integer
            Dim clr As Integer
            Dim bmp As Bitmap = New Bitmap(img)
            For X = 0 To bmp.Width - 1
                For Y = 0 To bmp.Height - 1
                    clr = (CInt(bmp.GetPixel(X, Y).R) +
                           bmp.GetPixel(X, Y).G +
                           bmp.GetPixel(X, Y).B) \ 3
                    bmp.SetPixel(X, Y, Color.FromArgb(clr, clr, clr))
                Next Y
            Next X
            Return bmp
        End Using
    End Function

    Public Function Sharpen(image As Image, strength As Integer) As Image
        Using image
            Dim fpixel, secpixel As Color
            Dim NewImg As Bitmap = New Bitmap(image)
            Dim CR, CB, CG As Integer
            Dim x, y As Integer
            For x = 0 To NewImg.Width - 2
                For y = 0 To NewImg.Height - 2
                    fpixel = NewImg.GetPixel(x, y)
                    secpixel = NewImg.GetPixel(x + 1, y)
                    Dim newR, newB, newG As Integer
                    newR = CInt(fpixel.R) - CInt(secpixel.R)
                    newB = CInt(fpixel.B) - CInt(secpixel.B)
                    newG = CInt(fpixel.G) - CInt(secpixel.G)
                    CR = CInt(newR * strength) + fpixel.R
                    CG = CInt(newG * strength) + fpixel.G
                    CB = CInt(newB * strength) + fpixel.B

                    If CR > 255 Then
                        CR = 255
                    End If
                    If CR < 0 Then
                        CR = 0

                    End If
                    If CB > 255 Then
                        CB = 255
                    End If
                    If CB < 0 Then
                        CB = 0
                    End If

                    If CG > 255 Then
                        CG = 255
                    End If

                    If CG < 0 Then
                        CG = 0
                    End If
                    NewImg.SetPixel(x, y, Color.FromArgb(CR, CG, CB))
                Next
            Next
            Return NewImg
        End Using
    End Function
    Public Function RemoveNoise(ByVal bmap As Bitmap) As Bitmap
        For x = 0 To bmap.Width - 1

            For y = 0 To bmap.Height - 1
                Dim pixel = bmap.GetPixel(x, y)

                If pixel.R < 162 AndAlso pixel.G < 162 AndAlso pixel.B < 162 Then
                    bmap.SetPixel(x, y, Color.Black)
                ElseIf pixel.R > 162 AndAlso pixel.G > 162 AndAlso pixel.B > 162 Then
                    bmap.SetPixel(x, y, Color.White)
                End If
            Next
        Next

        Return bmap
    End Function

    Public Function ReplaceFirst(text As String, search As String, replace As String)
        Dim pos As Integer = text.IndexOf(search)
        If pos < 0 Then
            Return text
        End If
        Return text.Substring(0, pos) + replace + text.Substring(pos + search.Length)
    End Function
End Module