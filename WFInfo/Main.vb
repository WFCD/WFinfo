Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging
Imports Tesseract
Public Class Main
    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer, ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer, ByVal dwExtraInfo As Integer)
    Public Declare Function GetAsyncKeyState Lib "user32" (ByVal vKey As Integer) As Integer
    Dim appData As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
    Dim scTog As Integer = 0 ' Toggle that only allows a single screen capture
    Dim count As Integer = 0 ' Number of Pics in appData
    Dim Sess As Integer = 0  ' Number of Screenshots this session
    Dim PPM As Integer = 0   ' Potential Platinum Made this session
    Dim pCount As Integer = 0 ' Current plat price to scan
    Dim CliptoImage As Image         ' Stored image
    Dim HKeyTog As Integer = 0       ' Toggle Var for setting the activation key
    Dim pbWait As Integer = 0        ' Variable to set to make the timer wait
    Dim lbTemp As String             ' Stores the keychar
    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer
    Dim enablePPC As Boolean = True
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateColors(Me)
        pbHome.Parent = pbSideBar
        pbHome.Location = New Point(0, 8)
        pbDonate.Parent = pbSideBar
        pbDonate.Location = New Point(0, 38)
        pbSettings.Parent = pbSideBar
        pbSettings.Location = New Point(0, 65)
        lbVersion.Text = "v" + My.Settings.Version
        Me.Location = New Point(My.Settings.StartX, My.Settings.StartY)
        Fullscreen = My.Settings.Fullscreen
        Me.MaximizeBox = False
        lbStatus.ForeColor = Color.Yellow
        Me.Refresh()
        If (Not System.IO.Directory.Exists(appData + "\WFInfo")) Then
            System.IO.Directory.CreateDirectory(appData + "\WFInfo")
        End If
        If (Not System.IO.Directory.Exists(appData + "\WFInfo\tests")) Then
            System.IO.Directory.CreateDirectory(appData + "\WFInfo\tests")
        End If
        count = GetMax(appData + "\WFInfo\tests\") + 1
        If Fullscreen Then
            If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
            End If
        End If
        If Clipboard.ContainsImage() Then
            Clipboard.GetImage()
            CliptoImage = Clipboard.GetImage()
        End If

        OnlineStatus.Navigate("https://sites.google.com/site/wfinfoapp/online")


        'Mechanism to make sure I don't kill warframe.market
        Dim enablePassives As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/enablepassivechecks")
        enablePassives = enablePassives.Remove(0, enablePassives.IndexOf("enabled = ") + 10)
        enablePassives = enablePassives.Remove(enablePassives.IndexOf(" "), enablePassives.Length - enablePassives.IndexOf(" "))

        If Not enablePassives = "true" Then
            enablePPC = False
        End If

    End Sub

    Private Async Sub tPB_Tick(sender As Object, e As EventArgs) Handles tPB.Tick
        Try
            If (Not key1Tog) And (Not key2Tog) Then
                Dim Refresh As Integer = GetAsyncKeyState(HKey1)
                Refresh = GetAsyncKeyState(HKey2)
                If Not Input.Visible = True Then
                    If GetAsyncKeyState(HKey2) Then
                        Input.Display()
                    End If
                End If
                If Not pbWait = 0 Then
                    pbWait -= 1
                Else
                    Dim keyState As Integer
                    If Fullscreen Then
                        If Not Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").Count = 0 Then
                            If Not My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First() Then
                                My.Settings.LastFile = Directory.GetFiles(My.Settings.LocStorage & "\760\remote\230410\screenshots").OrderByDescending(Function(f) New FileInfo(f).LastWriteTime).First()
                                keyState = 1
                            End If
                        End If
                    Else
                        keyState = GetAsyncKeyState(HKey1)
                    End If
                    If scTog = 0 Then
                        If Not keyState = 0 Then
                            scTog = 1
                        End If
                    Else
                        If Fullscreen = False Then
                            keyState = 0
                        End If
                        If keyState = 0 Then
                            lbStatus.ForeColor = Color.Yellow
                            tPPrice.Stop()
                            scTog = 0
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
                                Dim players As Integer = GetPlayers(Crop(CliptoImage))
                                If players > 4 Or players < 1 Then
                                    players = 4
                                End If
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
                                Dim unique As New List(Of String)()
                                For i = 0 To tList.Count - 1
                                    If Not LevDist(tList(i), "Blueprint") < 4 Then
                                        Dim guess As String = Names(check(tList(i)))
                                        unique.Add(guess)
                                        Else
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
                                        unique.Add(guess)
                                        End If
                                Next
                                qItems.Clear()
                                Dim HighestPlat As Integer = 0
                                Dim p As New List(Of String)()
                                Dim d As New List(Of String)()
                                For i = 0 To unique.Count - 1
                                    Dim guess As String = unique(i)
                                    If Not unique(i) = "Forma Blueprint" Then
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
                                        qItems.Add(vbNewLine & unique(i) & vbNewLine)
                                        p.Add(0)
                                        d.Add(0)
                                    End If
                                Next
                                If Not Fullscreen And Not NewStyle Then
                                    Tray.Clear()
                                    Tray.Display()
                                Else
                                    Tray.Clear()
                                    qItems.Clear()
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
                End If
            End If
        Catch ex As Exception
            lbStatus.ForeColor = Color.Orange
            addLog(ex.ToString)
            tPPrice.Start()
        End Try
    End Sub
    Public Function Crop(img As Image, Optional mode As Integer = 0, Optional pos As Integer = 1, Optional players As Integer = 0) As Image
        Dim startX As Integer
        Dim startY As Integer
        Dim height As Integer
        Dim width As Integer = 0.4 * img.Height
        Select Case mode
            Case 0
                startX = (img.Width / 2) - (width * 2)
                startY = img.Height * 0.457
                height = img.Height * 0.03
                width = width * 4
            Case 4
                startX = (img.Width / 2) - (width * 2) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 3
                startX = (img.Width / 2) - (1.5 * width) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 2
                startX = (img.Width / 2) - (width) + (width * pos)
                startY = img.Height * 0.425
                height = img.Height * 0.03
            Case 1
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
        Dim CropRect As New Rectangle(startX, startY, width, height)
        Dim OriginalImage = img
        Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
        Using grp = Graphics.FromImage(CropImage)
            grp.DrawImage(OriginalImage, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            Return CropImage
        End Using
    End Function

    Private Sub addLog(txt As String)
        Dim dateTime As String = "[" + System.DateTime.Now + "]"
        Dim logStore As String = ""
        If My.Computer.FileSystem.FileExists(appData + "\WFInfo\WFInfo.log") Then
            logStore = My.Computer.FileSystem.ReadAllText(appData + "\WFInfo\WFInfo.log")
        Else
            File.Create(appData + "\WFInfo\WFInfo.log").Dispose()
        End If
        My.Computer.FileSystem.WriteAllText(appData + "\WFInfo\WFInfo.log",
        dateTime + vbNewLine + txt + vbNewLine + vbNewLine + logStore, False)
    End Sub

    Private Function GetMax(ByVal sFolder As String) As Long
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
        Try
            Equipment = My.Settings.Equipment ' Load equipment string

            If Date.Today > My.Settings.LastUpdate.AddDays(7) Then
                lbStatus.ForeColor = Color.Yellow
                Dim duckString As String = ""
                Dim endpoint As String = New StreamReader(WebRequest.Create("http://warframe.wikia.com/wiki/Ducats/Prices").GetResponse().GetResponseStream()).ReadToEnd()
                Dim str1 As String = endpoint.Substring(endpoint.IndexOf("Blueprint/Crafted Value"))
                Dim str2 As String = str1.Substring(0, str1.IndexOf("</div>"))
                Dim str3 As String = str2.Substring(str2.IndexOf("Acquisition"))
                Dim strArray As String() = str3.Split(New String(0) {"Acquisition"}, StringSplitOptions.None)
                Dim index As Integer = 0
                While index < strArray.Length
                    Dim current As String = strArray(index)
                    If current.Contains("</a>") Then
                        Dim name As String = current.Substring(current.IndexOf(">") + 1, current.IndexOf("<")).Substring(0, current.Substring(current.IndexOf(">") + 1, current.IndexOf("<")).IndexOf("<"))
                        Dim ducats As String = current.Substring(current.IndexOf("sortkey") + 9, current.IndexOf("</span>") - current.IndexOf("sortkey") - 9)
                        Dim Relic = GetRelic(current, "<td>", ">").ToString().Replace("<br />", vbCrLf)
                        Dim vBool As Boolean = False
                        Dim vStr As String = ""
                        If Relic.Contains("Prime Vault") Then
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
            End If

        Catch ex As Exception
        End Try
        For Each str As String In My.Settings.DuckList.Split(vbNewLine)
            str.Replace(vbNewLine, "")
            Names.Add(str.Split(",")(0))
            Ducks.Add(str.Split(",")(1))
            Vaulted.Add(str.Split(",")(2))
        Next
        Names.Add("Forma Blueprint")
        Ducks.Add("0")

        lbStatus.ForeColor = Color.Lime
    End Sub

    Private Sub Main_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Refresh()
        UpdateList()
        tPPrice.Start()
    End Sub
    Private Function GetPlayers(img As Image) As Integer
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
    Private Function GetText(img As Image) As String
        Using img
            Dim engine As New TesseractEngine("", "eng")
            Dim page = engine.Process(prepare(img))
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
        Using img
            Dim engine As New TesseractEngine("", "eng")
            Dim page = engine.Process(prepare(img)).GetHOCRText(1)
            Return page
        End Using
    End Function
    Private Function prepare(img As Image) As Image
        Using img
            Dim X As Integer
            Dim Y As Integer
            Dim clr As Integer
            Dim bm As Bitmap = New Bitmap(img)
            For X = 0 To bm.Width - 1
                For Y = 0 To bm.Height - 1
                    clr = (CInt(bm.GetPixel(X, Y).R) +
                           bm.GetPixel(X, Y).G +
                           bm.GetPixel(X, Y).B) \ 3
                    bm.SetPixel(X, Y, Color.FromArgb(clr, clr, clr))
                Next Y
            Next X
            Return bm
        End Using
    End Function

    Private Function Sharpen(image As Image, strength As Integer) As Image
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

    Public Function KClean(guess As String)
        If Not guess.Contains("Carrier") And Not guess.Contains("Wyrm") And Not guess.Contains("Helios") Then
            If guess.Contains("Systems") Or guess.Contains("Chassis") Or guess.Contains("Neuroptics") Then
                guess = guess.Replace(" Blueprint", "")
            End If
        End If
        guess = guess.Replace("Band", "Collar Band").Replace("Buckle", "Collar Buckle").Replace("&amp;", "and")
        Return guess
    End Function


    Private Sub tPPrice_Tick(sender As Object, e As EventArgs) Handles tPPrice.Tick
        If Not bgPPrice.IsBusy And enablePPC Then
            bgPPrice.RunWorkerAsync()
        End If
    End Sub

    Private Sub bgPPrice_DoWork(sender As Object, e As DoWorkEventArgs) Handles bgPPrice.DoWork
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

    Public Function GetRelic(ByVal FullString As String, ByVal StartText As String, ByVal EndText As String)
        Dim startIndex As Integer = FullString.IndexOf(StartText) + StartText.Length
        Dim endIndex As Integer = FullString.IndexOf(EndText, startIndex)
        Return FullString.Substring(startIndex, endIndex - startIndex)
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles btnDebug1.Click
        Picker.Show()
    End Sub

    Private Sub tOnline_Tick(sender As Object, e As EventArgs) Handles tOnline.Tick
        OnlineStatus.Navigate("https://sites.google.com/site/wfinfoapp/online")
    End Sub

    Public Sub CheckUpdates()
        Dim curVersion As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/version")
        curVersion = curVersion.Remove(0, curVersion.IndexOf("version ") + 8)
        curVersion = curVersion.Remove(5, curVersion.Length - 5)


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
        If Messages Then
            Dim curMessage As String = New System.Net.WebClient().DownloadString("https://sites.google.com/site/wfinfoapp/message")
            curMessage = curMessage.Remove(0, curMessage.IndexOf("(message)") + 9)
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
        UpdateColors(Me)
    End Sub
End Class

Module Glob
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
    Public Commands As Boolean = My.Settings.Commands
    Public Messages As Boolean = My.Settings.Messages
    Public NewStyle As Boolean = My.Settings.NewStyle
    Public Debug As Boolean = My.Settings.Debug
    Public Function check(string1 As String) As Integer
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
    Public Function GetPlat(str As String, Optional getUser As Boolean = False, Optional getMod As Boolean = False) As String
        str = str.ToLower
        str = str.Replace(" ", "%5F").Replace(vbLf, "").Replace("*", "")
        Dim webClient As New System.Net.WebClient
        webClient.Headers.Add("platform", "pc")
        webClient.Headers.Add("language", "en")
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11
        Dim result As JObject

        If getUser Then
            result = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + str + "/orders"))
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

            If platCheck.Count = 0 Then
                Return 0
            End If
            Dim low As Integer = 999999
            Dim user As String
            For i = 0 To platCheck.Count - 1
                If (Not userCheck(i).Contains("XB1")) And (Not userCheck(i).Contains("PS4")) Then
                    If platCheck(i) < low Then
                        low = platCheck(i)
                        user = userCheck(i)
                    End If
                End If
            Next
            Clipboard.SetText(user)
            Return low & vbNewLine & "    User: " & user

        Else 'Not Single Pull

            result = JsonConvert.DeserializeObject(Of JObject)(webClient.DownloadString("https://api.warframe.market/v1/items/" + str + "/statistics"))
            Dim minList As New List(Of Integer)()
            Dim x As Integer = 9
            If x > result("payload")("statistics")("48hours").Count - 1 Then
                x = result("payload")("statistics")("48hours").Count - 1
            End If
            For i = 0 To x
                minList.Add(CInt(result("payload")("statistics")("48hours")(i)("min_price")))
            Next

            Dim total As Integer = 0
            For i = 0 To minList.Count - 1
                total += minList(i)
            Next

            Dim avg As Single = total / minList.Count

            Dim difference As Single = 999999
            Dim low As Integer = 0
            For i = 0 To minList.Count - 1
                If Math.Abs(avg - minList(i)) < difference Then
                    low = minList(i)
                    difference = Math.Abs(avg - minList(i))
                End If
            Next
            Return low
        End If
    End Function
    Public Sub UpdateColors(f As Form)
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