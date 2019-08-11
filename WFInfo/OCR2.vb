Imports System.Text.RegularExpressions
Imports Tesseract

Public Class OCR2
    ' Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
    Public Const pixRwrdWid As Integer = 972
    Public Const pixRwrdHei As Integer = 235
    Public Const pixRwrdYDisp As Integer = 316
    Public Const pixRwrdLineHei As Integer = 44
    Public Const pixRwrdLineWid As Integer = 240

    ' Pixel measurement for player bars for player count
    '   Width is same as pixRwrdWid
    ' Public Const pixRareWid As Integer = pixRwrdWid
    '   Height is always 1px
    ' Public Const pixRareHei As Integer = 1
    '   Box is centered horizontally
    ' Public Const pixRareXDisp As Integer = ???
    Public Const pixRareYDisp As Integer = 58
    Public Const pixOverlayPos As Integer = 30

    Public Const pixProfWid As Integer = 48
    Public Const pixProfTotWid As Integer = 192
    ' Height is always 1px
    ' Public Const pixProfHei As Integer = 1
    Public Const pixProfXDisp As Integer = 93
    Public Const pixProfYDisp As Integer = 87
    Public Const pixProfXSpecial As Double = 117
    Public Const pixProfYSpecial As Double = 87

    ' Pixel measurements for detecting reward screen
    Public Const pixFissWid As Integer = 354
    Public Const pixFissHei As Integer = 45
    Public Const pixFissXDisp As Integer = 285
    Public Const pixFissYDisp As Integer = 43

    ' Colors for "VOIDFISSURE/REWARDS"

    Public uiColor As Color
    Public FissClr1 As Color = Color.FromArgb(189, 168, 101)    ' Vitruvian
    Public FissClr2 As Color = Color.FromArgb(150, 31, 35)      ' Stalker 
    Public FissClr3 As Color = Color.FromArgb(238, 193, 105)    ' Baruk
    Public FissClr4 As Color = Color.FromArgb(35, 200, 245)     ' Corpus
    Public FissClr5 As Color = Color.FromArgb(57, 105, 192)     ' Fortuna
    Public FissClr6 As Color = Color.FromArgb(255, 189, 102)    ' Grineer
    Public FissClr7 As Color = Color.FromArgb(36, 184, 242)     ' Lotus
    Public FissClr8 As Color = Color.FromArgb(140, 38, 92)      ' Nidus
    Public FissClr9 As Color = Color.FromArgb(20, 41, 29)       ' Orokin
    Public FissClr10 As Color = Color.FromArgb(9, 78, 106)      ' Tenno
    Public FissClr11 As Color = Color.FromArgb(2, 127, 217)     ' High contrast
    Public FissClr12 As Color = Color.FromArgb(255, 255, 255)   ' Legacy
    Public FissClr13 As Color = Color.FromArgb(158, 159, 167)   ' Equinox

    Public rarity As New List(Of Color) From {Color.FromArgb(180, 135, 110), Color.FromArgb(200, 200, 200), Color.FromArgb(212, 192, 120)}
    Public fissColors As Color() = {FissClr1, FissClr2, FissClr3, FissClr4, FissClr5, FissClr6, FissClr7, FissClr8, FissClr9, FissClr10, FissClr11, FissClr12, FissClr13}
    Public fissNames As String() = {"Vitruvian", "Stalker", "Baruk", "Corpus", "Fortuna", "Grineer", "Lotus", "Nidus", "Orokin", "Tenno", "Highcontrast", "Legacy", "Equinox"}

    ' Warframe window stats
    '   Warframe process
    Public WF_Proc As Process = Nothing
    '   Warframe window style (Fullscreen, Windowed, Borderless)
    Public currStyle As WindowStyle

    '   Warframe window bounds & points
    Public window As Rectangle = Nothing
    Public center As Point = Nothing

    ' Scaling adjustments
    Public dpiScaling As Double = 1.0
    Public screenScaling As Double = 1.0
    Public uiScaling As Double = 1.0
    Public totalScaling As Double = 1.0

    ' Tesseract engines for each reward
    ' 0 for relic window checks or any system checks
    ' 1-4 for players 1 to 4
    Public engine(4) As TesseractEngine
    Public tasks(3) As Task
    Public running As New List(Of Task)
    Public plyr_count As Integer = -1

    ' List of results found by OCR, didn't have a better place to put it
    Public foundText(3) As String
    Public tessText(3) As String
    Public foundRec(3) As Rectangle

    Public Shared debugFile As Bitmap = Nothing

    Public Enum WindowStyle
        FULLSCREEN
        BORDERLESS
        WINDOWED
    End Enum

    Public Sub New()
        For i As Integer = 0 To 4
            If i = 0 Then
                engine(i) = New TesseractEngine("", "englimited")
                engine(i).DefaultPageSegMode = PageSegMode.SingleLine
                engine(i).SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ/")
            Else
                engine(i) = New TesseractEngine("", "engbest")
                engine(i).DefaultPageSegMode = PageSegMode.SingleBlock
                engine(i).SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz&/")
            End If
            engine(i).SetVariable("load_system_dawg", False)
            engine(i).SetVariable("user_words_suffix", "prime-words")

        Next
    End Sub

    '----------------------------------------------------------------------
    ' Utility Functions
    '----------------------------------------------------------------------

    Public Overridable Function GetUiColor() As Boolean
        uiColor = Nothing

        Dim clr As Color = Nothing
        If debugFile IsNot Nothing Then
            Dim R As Integer = 0
            Dim G As Integer = 0
            Dim B As Integer = 0
            Dim width As Integer = pixProfWid * totalScaling / 2
            Dim x As Integer = pixProfXDisp * totalScaling + width / 2
            Dim y As Integer = pixProfYDisp * totalScaling
            For i As Integer = 0 To width - 1
                clr = debugFile.GetPixel(x + i, y)
                R += clr.R
                G += clr.G
                B += clr.B
            Next
            R /= width
            G /= width
            B /= width

            Dim detectedColor = Color.FromArgb(R, G, B)
            For Each knowColor In fissColors
                If ColorThreshold(detectedColor, knowColor, 20) Then
                    uiColor = knowColor
                    Main.addLog("FOUND COLOR: " & uiColor.ToString())
                    Return True
                End If
            Next

        Else
            Dim scalingMod As Double = totalScaling * 40 / My.Settings.Scaling

            Dim startX As Integer = CInt(pixProfXSpecial * scalingMod)
            Dim startY As Integer = CInt(pixProfYSpecial * scalingMod)
            Dim endX As Integer = CInt(pixProfXSpecial * scalingMod * 3)
            Dim endY As Integer = CInt(pixProfYSpecial * scalingMod * 3)

            Dim debugList As New List(Of Integer)
            Dim debugClrList As New List(Of String)
            Dim closestThresh As Integer = 999
            Dim closestColor As String = Nothing

            Using bmp As New Bitmap(endX - startX, endY - startY)
                Using graph As Graphics = Graphics.FromImage(bmp)
                    graph.CopyFromScreen(window.X + startX, window.Y + startY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy)
                End Using
                For y As Integer = 1 To bmp.Height
                    Dim newY As Integer = bmp.Height - y
                    Dim newX As Integer = bmp.Width * (newY / bmp.Height)
                    clr = bmp.GetPixel(newX, newY)
                    Dim minThresh As Integer = 999
                    Dim minColor As String = Nothing
                    Dim minInd As Integer = 0
                    For i As Integer = 0 To fissColors.Length - 1
                        Dim knowColor As Color = fissColors(i)
                        Dim tempThresh As Integer = ColorDifference(clr, knowColor)
                        If tempThresh < minThresh Then
                            minThresh = tempThresh
                            minColor = minThresh & "," & fissNames(i)
                            minInd = i
                        End If
                        If ColorThreshold(clr, knowColor, 10) Then
                            uiColor = knowColor
                            Main.addLog("FOUND COLOR " & clr.ToString() & " AT (" & (startX + newX) & ", " & (startY + newY) & ")")
                            Main.addLog("ESTIMATED SCALING: " & CInt(100 * (startX + newX) / (pixProfXSpecial * screenScaling)) & "%")
                            Main.addLog("USER INPUT SCALING: " & My.Settings.Scaling & "%")
                            Return True
                        End If
                    Next
                    minColor = String.Format("#{0:X2}{1:X2}{2:X2}", clr.R, clr.G, clr.B) & "(" & minColor & ")"
                    If minThresh < closestThresh Then
                        closestThresh = minThresh
                        closestColor = minInd & ", " & minColor
                    End If
                    debugList.Add(minThresh)
                    debugClrList.Add(minColor)
                Next
            End Using
            Main.addLog("UI COLOR NOT FOUND - CLOSEST: " & closestColor)
            Main.addLog("UI COLOR NOT FOUND - THRESHOLDS: " & String.Join(", ", debugClrList))
        End If
        Return False
    End Function

    Public Overridable Function FindUiColor() As Boolean

        uiColor = Nothing
        Dim width As Integer = pixProfWid * totalScaling / 2

        Using bmp As New Bitmap(width, 1)
            Dim clr As Color = Nothing
            Dim R As Integer = 0
            Dim G As Integer = 0
            Dim B As Integer = 0

            If debugFile IsNot Nothing Then
                Dim x As Integer = pixProfXDisp * totalScaling + width / 2
                Dim y As Integer = pixProfYDisp * totalScaling
                For i As Integer = 0 To width - 1
                    clr = debugFile.GetPixel(x + i, y)
                    R += clr.R
                    G += clr.G
                    B += clr.B
                Next
            Else
                Using graph As Graphics = Graphics.FromImage(bmp)
                    Dim x As Integer = window.Left + pixProfXDisp * totalScaling + width / 2
                    Dim y As Integer = window.Top + pixProfYDisp * totalScaling
                    graph.CopyFromScreen(x, y, 0, 0, New Size(width, 1), CopyPixelOperation.SourceCopy)
                    Main.addLog("Pulled UI Color from : (" & x & ", " & y & ") x " & width & "px")
                End Using
                Dim foundColors As String = ""
                For i As Integer = 0 To width - 1
                    clr = bmp.GetPixel(i, 0)
                    Dim foundAclr As Boolean = False
                    For j As Integer = 0 To fissColors.Length - 1
                        If ColorThreshold(clr, fissColors(j), 20) Then
                            If i <> 0 Then
                                foundColors &= ", "
                            End If
                            foundColors &= j
                            foundAclr = True
                            Exit For
                        End If
                    Next
                    If Not foundAclr Then
                        If i <> 0 Then
                            foundColors &= ", "
                        End If
                        foundColors &= "-1"
                    End If
                    R += clr.R
                    G += clr.G
                    B += clr.B
                Next
                Main.addLog("Colors found: " & foundColors)
            End If

            R /= width
            G /= width
            B /= width

            Dim detectedColor = Color.FromArgb(R, G, B)
            For Each knowColor In fissColors
                If ColorThreshold(detectedColor, knowColor, 20) Then
                    uiColor = knowColor
                    Return True
                End If
            Next

            Main.addLog("UI COLOR NOT FOUND")
        End Using
        Return False
    End Function

    Public Overridable Function CleanImage(image As Bitmap, Optional thresh As Integer = 10) As Bitmap
        For i As Integer = 0 To image.Width - 1
            For j As Integer = 0 To image.Height - 1
                Dim currentPixle = image.GetPixel(i, j)
                If ColorThreshold(currentPixle, uiColor, thresh) Then
                    image.SetPixel(i, j, Color.Black)
                Else
                    image.SetPixel(i, j, Color.White)
                End If
            Next
        Next
        Return image
    End Function

    Public Overridable Function GetDPIScaling() As Double
        Dim tempScaling As Double = 1

        Using form As New Form()
            Using g As Graphics = form.CreateGraphics()
                If g.DpiX <> 96 Then
                    tempScaling = g.DpiX / 96
                ElseIf g.DpiY <> 96 Then
                    tempScaling = g.DpiY / 96
                End If
            End Using
        End Using

        If tempScaling = 1 Then
            Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
                Dim desktop As IntPtr = g.GetHdc()
                tempScaling = GetDeviceCaps(desktop, DeviceCap.DESKTOPVERTRES) / GetDeviceCaps(desktop, DeviceCap.VERTRES)
            End Using
        End If

        If tempScaling <> dpiScaling Then
            dpiScaling = tempScaling
            Main.addLog("UPDATING DPI SCALING TO: " & dpiScaling)
            totalScaling = dpiScaling * uiScaling
        End If

        Return dpiScaling
    End Function

    Public Overridable Function GetUIScaling() As Double
        Dim tempScaling As Double = My.Settings.Scaling / 100
        '     All values are based on 1920x1080
        If window.Width / window.Height > 16 / 9 Then
            screenScaling = window.Height / 1080
        Else
            screenScaling = window.Width / 1920
        End If
        tempScaling *= screenScaling
        If tempScaling <> uiScaling Then
            uiScaling = tempScaling
            totalScaling = dpiScaling * uiScaling

            Main.addLog("UPDATING UI SCALING TO: " & uiScaling)
        End If
        Return uiScaling
    End Function

    Public Overridable Function GetWFProc() As Process
        For Each p As Process In Process.GetProcesses
            Try
                If p.MainWindowTitle = "Warframe" Then
                    Dim hr As New HandleRef(p, p.MainWindowHandle)
                    Dim tempRect As New RECT
                    GetWindowRect(hr, tempRect)

                    If tempRect.Left <> tempRect.Right AndAlso tempRect.Top <> tempRect.Bottom Then
                        If WF_Proc Is Nothing OrElse p.Handle <> WF_Proc.Handle Then
                            WF_Proc = p
                            UpdateCenter()
                        End If
                        Return WF_Proc
                    End If
                End If
            Catch ex As Exception
                Main.addLog("EXCEPTION DURING GetWFProc() with PROCESS " & p.Id & ": " & ex.ToString())
            End Try
        Next
        WF_Proc = Nothing
        If Debug And window = Nothing Then
            FakeUpdateCenter()
        End If
        Return Nothing
    End Function

    Public Overridable Function IsWFActive() As Boolean
        If WF_Proc Is Nothing OrElse WF_Proc.HasExited Then
            GetWFProc()
        End If
        If WF_Proc Is Nothing And Debug And window = Nothing Then
            FakeUpdateCenter()
        End If
        Return WF_Proc IsNot Nothing Or Debug
    End Function

    Public Overridable Sub FakeUpdateCenter()
        window = Screen.PrimaryScreen.Bounds

        ' Get Scaling
        GetDPIScaling()
        GetUIScaling()

        ' Get Window Points
        Dim horz_center As Integer = window.Left + (window.Width / 2)
        Dim vert_center As Integer = window.Top + (window.Height / 2)
        center = New Point(dpiScaling * horz_center, dpiScaling * vert_center)

        Main.addLog("UPDATED CENTER COORS: " & center.ToString())
    End Sub

    Public Overridable Sub UpdateCenter()
        If WF_Proc Is Nothing Then
            Return
        End If

        Dim hr As New HandleRef(WF_Proc, WF_Proc.MainWindowHandle)
        Dim tempRect As New RECT
        GetWindowRect(hr, tempRect)

        Dim tempWindow As New Rectangle(tempRect.Left, tempRect.Top, tempRect.Right - tempRect.Left, tempRect.Bottom - tempRect.Top)

        If tempWindow.Width <> window.Width OrElse tempWindow.Height <> window.Height Then
            window = tempWindow
            Main.addLog("UPDATED WINDOW AREA: " & window.ToString())

            Dim GWL_STYLE As Int32 = -16
            Dim WS_THICKFRAME As Long = 262144
            Dim WS_MAXIMIZE As Long = 16777216
            Dim WS_POPUP As Long = 2147483648
            Dim styles As Long = GetWindowLong(WF_Proc.MainWindowHandle, GWL_STYLE)
            If (styles And WS_THICKFRAME) <> 0 Then
                window = New Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38)
                Main.addLog("WINDOWED ADJUSTMENT: " & window.ToString())
                currStyle = WindowStyle.WINDOWED
            ElseIf (styles And WS_POPUP) <> 0 Then
                currStyle = WindowStyle.BORDERLESS
            Else
                currStyle = WindowStyle.FULLSCREEN
            End If

            If window.X < -20000 Or window.Y < -20000 Then
                WF_Proc = Nothing
                window = Nothing
            Else
                ' Get Scaling
                GetDPIScaling()
                GetUIScaling()

                ' Get Window Points
                Dim horz_center As Integer = window.Left + (window.Width / 2)
                Dim vert_center As Integer = window.Top + (window.Height / 2)
                center = New Point(dpiScaling * horz_center, dpiScaling * vert_center)

                Main.addLog("UPDATED CENTER COORS: " & center.ToString())
            End If
        End If
    End Sub

    Public Overridable Function DefaultParseText(bmp As Bitmap, plyr As Integer) As String
        Using bmp
            Using page As Page = engine(plyr).Process(bmp)
                Return page.GetText().Trim
            End Using
        End Using
    End Function

    Public Overridable Function ColorThreshold(test As Color, thresh As Color, Optional threshold As Integer = 10) As Boolean
        Return (Math.Abs(CInt(test.R) - thresh.R) < threshold) AndAlso (Math.Abs(CInt(test.G) - thresh.G) < threshold) AndAlso (Math.Abs(CInt(test.B) - thresh.B) < threshold)
    End Function

    Public Overridable Function ColorDifference(test As Color, thresh As Color) As Integer
        Return Math.Abs(CInt(test.R) - thresh.R) + Math.Abs(CInt(test.G) - thresh.G) + Math.Abs(CInt(test.B) - thresh.B)
    End Function

    Public Overridable Function HSVThreshold(test As Color, thresh As Color, Optional thresh_hue As Integer = 10, Optional thresh_sat As Double = 0.05, Optional thresh_brg As Double = 0.05) As Boolean
        Dim hue As Single = Math.Abs(test.GetHue() - thresh.GetHue())
        If hue > 2 * thresh_hue Then
            hue = Math.Abs(360 - hue)
        End If
        Return (hue < thresh_hue) AndAlso (Math.Abs(test.GetSaturation() - thresh.GetSaturation()) < thresh_sat) AndAlso (Math.Abs(test.GetBrightness() - thresh.GetBrightness()) < thresh_brg)
    End Function

    '_____________________________________________________
    '
    '    BEGIN RELIC REWARDS STUFF
    '_____________________________________________________

    Public Overridable Function IsRelicWindow() As Boolean
        Try

            If Not IsWFActive() Then
                Return False
            End If

            GetDPIScaling()
            GetUIScaling()

            If GetUiColor() Then


                Dim bnds As New Rectangle(window.Left + pixFissXDisp * totalScaling,
                                            window.Top + pixFissYDisp * totalScaling,
                                            pixFissWid * totalScaling,
                                            pixFissHei * totalScaling)

                Using bmp As New Bitmap(bnds.Width + 40, bnds.Height + 40)
                    Using graph As Graphics = Graphics.FromImage(bmp)
                        If debugFile IsNot Nothing Then
                            bnds = New Rectangle(pixFissXDisp * totalScaling, pixFissYDisp * totalScaling, bnds.Width, bnds.Height)
                            graph.DrawImage(debugFile, 0, 0, bnds, GraphicsUnit.Pixel)
                        Else
                            graph.CopyFromScreen(bnds.X, bnds.Y, 20, 20, New Size(bnds.Width, bnds.Height), CopyPixelOperation.SourceCopy)
                        End If
                    End Using

                    If Debug Then
                        bmp.Save(appData & "\WFInfo\debug\FISS-CHECK-" & My.Settings.DebugCount.ToString() & ".png")
                        My.Settings.DebugCount += 1
                    End If

                    Dim clr As Color = Nothing
                    Dim found As Boolean = False
                    For i As Integer = 0 To bmp.Width - 1
                        For j As Integer = 0 To bmp.Height - 1
                            clr = bmp.GetPixel(i, j)
                            If ColorThreshold(clr, uiColor, Math.Max(uiColor.R, Math.Max(uiColor.G, uiColor.B)) / 7 + 30) Then
                                found = True
                                bmp.SetPixel(i, j, Color.Black)
                            Else
                                bmp.SetPixel(i, j, Color.White)
                            End If
                        Next
                    Next

                    If Not found Then
                        Return False
                    End If

                    Dim ret As String = DefaultParseText(bmp, 0).Replace(" ", "")
                    ' Finds: YOID FILS S URE -> YOIDFILSSURE
                    Main.addLog("FISSURE CHECK FOUND: " & ret)
                    If LevDist(ret, "VOIDFISSURE") < 4 Then
                        Return True
                    End If
                End Using
            End If
        Catch ex As Exception

        End Try
        Return False
    End Function

    Public Overridable Function GetPlayers() As Integer
        Dim width As Integer = pixRwrdWid / 2 * totalScaling
        Dim lineHeight As Integer = pixRwrdLineHei * totalScaling / 2

        Dim left As Integer = center.X - width
        Dim top As Integer = center.Y - pixRwrdYDisp * totalScaling + pixRwrdHei * totalScaling - lineHeight - 1
        Using bmp As New Bitmap(width, lineHeight)
            Using graph As Graphics = Graphics.FromImage(bmp)
                If debugFile IsNot Nothing Then
                    graph.DrawImage(debugFile, 0, 0, New Rectangle(left, top, bmp.Width, bmp.Height), GraphicsUnit.Pixel)
                Else
                    graph.CopyFromScreen(left, top, 0, 0, New Size(bmp.Width, bmp.Height), CopyPixelOperation.SourceCopy)
                End If
            End Using


            Dim x As Integer = 0
            For i As Integer = 1 To 3
                Dim found As Boolean = False
                x = CInt(i / 4 * width) - bmp.Height / 4 - 1
                For y As Integer = 0 To bmp.Height - 1
                    For k As Integer = 0 To bmp.Height / 2

                        Dim currentPixle = bmp.GetPixel(x + k, y)
                        If ColorThreshold(currentPixle, uiColor, 30) Then
                            bmp.SetPixel(x + k, y, Color.Black)
                            If Debug Then
                                bmp.Save(appData & "\WFInfo\debug\PLYR-COUNT-" & My.Settings.RarebarCount.ToString() & ".png")
                                My.Settings.RarebarCount += 1
                            End If
                            Return 5 - i
                        Else
                            bmp.SetPixel(x + k, y, Color.White)
                        End If
                    Next
                Next
            Next
            If Debug Then
                bmp.Save(appData & "\WFInfo\debug\PLYR-COUNT-" & My.Settings.RarebarCount.ToString() & ".png")
                My.Settings.RarebarCount += 1
            End If
        End Using
        Return -1
    End Function

    Public Sub CheckImage()
        Dim ssFile = New OpenFileDialog()
        ssFile.Title = "Please select a relic screenshot"

        If ssFile.ShowDialog() = DialogResult.OK Then
            ParseFile(ssFile.FileName)
        End If
    End Sub

    Public Sub ParseFile(fileName As String)
        Main.addLog("PARSING FILE: " & fileName)
        debugFile = Bitmap.FromFile(fileName)

        window = New Rectangle(0, 0, debugFile.Width, debugFile.Height)

        ' Get DPI Scaling
        dpiScaling = 1.0
        GetUIScaling()

        ' Get Window Points
        Dim horz_center As Integer = window.Width / 2
        Dim vert_center As Integer = window.Height / 2
        center = New Point(horz_center, vert_center)

        If IsRelicWindow() Then
            ParseScreen()
        End If

        debugFile.Dispose()
        debugFile = Nothing
    End Sub

    Public Overridable Function GetPlayerImage(plyr As Integer, count As Integer) As Bitmap
        Dim padding As Integer = 8 * totalScaling
        Dim width As Integer = pixRwrdWid / 4 * totalScaling - padding
        Dim lineHeight As Integer = pixRwrdLineHei * totalScaling

        Dim left As Integer = center.X - (width + padding) * (count / 2 - plyr) + 2
        Dim top As Integer = center.Y - pixRwrdYDisp * totalScaling + pixRwrdHei * totalScaling - lineHeight - 1

        Dim ret As New Bitmap(width + 10, lineHeight + 10)
        If debugFile IsNot Nothing Then
            For x As Integer = 0 To width - 1
                For y As Integer = 0 To lineHeight - 1
                    ret.SetPixel(x + 5, y + 5, debugFile.GetPixel(left + x, top + y))
                Next
            Next
        Else
            Using graph As Graphics = Graphics.FromImage(ret)
                graph.CopyFromScreen(left, top, 5, 5, New Size(width, lineHeight), CopyPixelOperation.SourceCopy)
            End Using
        End If
        foundRec(plyr) = New Rectangle(New Point(left, top), New Size(width, lineHeight))
        Return CleanImage(ret, 30) 'Math.Min(uiColor.R, Math.Max(uiColor.G, uiColor.B)) / 7 + 30
    End Function

    Public ParsePlayer_timer() As Long = {0, 0, 0, 0}
    Public Overridable Sub ParsePlayer(plyr As Integer)
        ParsePlayer_timer(plyr) = clock.Elapsed.TotalMilliseconds
        Dim text As Bitmap = GetPlayerImage(plyr, plyr_count)
        If Debug Then
            text.Save(appData & "\WFInfo\debug\SCAN" & My.Settings.EtcCount.ToString() & "-PLYR-" & plyr & ".png")
        End If

        Dim result = DefaultParseText(text, plyr + 1)
        tessText(plyr) = result
        foundText(plyr) = db.GetPartName(result)

        ParsePlayer_timer(plyr) -= clock.Elapsed.TotalMilliseconds
        Main.addLog("PLAYER(" & plyr & ") PARSE-" & ParsePlayer_timer(plyr) & "ms")
    End Sub

    Public ParseScreen_timer As Long = 0
    Public Overridable Sub ParseScreen(Optional fromAuto As Boolean = False)
        Try
            ParseScreen_timer = clock.Elapsed.TotalMilliseconds

            If Not IsWFActive() Then
                Main.addLog("Warframe Process Not Found")
                Return
            End If

            GetDPIScaling()
            GetUIScaling()

            If fromAuto OrElse GetUiColor() Then


                ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                Main.addLog("GET COLOR & SCALING-" & ParseScreen_timer & "ms")
                ParseScreen_timer = clock.Elapsed.TotalMilliseconds

                plyr_count = GetPlayers()

                ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                Main.addLog("FOUND " & plyr_count & " PLAYERS-" & ParseScreen_timer & "ms")
                ParseScreen_timer = clock.Elapsed.TotalMilliseconds


                If debugFile IsNot Nothing Then
                    For i As Integer = 0 To plyr_count - 1
                        ParsePlayer(i)
                    Next
                Else
                    Main.addLog("STARTING MULTITHREADING PARSING")
                    For i As Integer = 0 To plyr_count - 1
                        Dim plyr As Integer = i
                        running.Add(Task.Factory.StartNew(Sub() ParsePlayer(plyr)))
                    Next

                    Task.WaitAll(running.ToArray())
                    running.Clear()
                    Main.addLog("MULTITHREADING PARSING COMPLETE")
                End If

                My.Settings.EtcCount += 1
                ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                Main.addLog("GET ALL PARTS-" & ParseScreen_timer & "ms")
                ParseScreen_timer = clock.Elapsed.TotalMilliseconds

                ' Display window true = seperate window
                ' Display window false = overlay
                If My.Settings.ResultWindow Then
                    'run window
                    Main.Instance.Invoke(Sub() RewardWindow.Display(foundText, plyr_count))
                    ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                    Main.addLog("DISPLAY WINDOW-" & ParseScreen_timer & "ms")
                    ParseScreen_timer = clock.Elapsed.TotalMilliseconds
                ElseIf My.Settings.NewOverlay Then

                    Dim wid As Integer = pixRwrdWid / 4 * uiScaling 'totalScaling
                    Dim top As Integer = (center.Y / dpiScaling) + pixOverlayPos * uiScaling 'totalScaling
                    Dim left As Integer = (center.X / dpiScaling) - wid * 2
                    left += wid * (4 - plyr_count) / 2
                    If Not My.Settings.Automate Then
                        For i = 0 To plyr_count - 1
                            Dim j As Integer = i
                            Main.Instance.Invoke(Sub() rwrdPanels2(j).tHide.Start())
                        Next
                    End If

                    For i = 0 To plyr_count - 1
                        Dim j As Integer = i
                        Main.Instance.Invoke(Sub() rwrdPanels2(j).ShowAtLocation(foundText(j), New Point(left, top), wid))
                        Main.addLog("DISPLAY NEW OVERLAY: " & foundText(i) & " @ (" & left & ", " & top & ")")
                        left += wid
                    Next

                    ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                    Main.addLog("DISPLAY OVERLAYS-" & ParseScreen_timer & "ms")
                    ParseScreen_timer = clock.Elapsed.TotalMilliseconds
                Else
                    'run overlay
                    ' Move over if you don't have all 4

                    Dim pad As Integer = pixRwrdHei * 0.05 * totalScaling 'padding to prevent it from looking off.
                    Dim top = center.Y - pixRwrdYDisp * totalScaling + pad 'from center to the top it's 248px
                    Dim right = center.X - (pixRwrdWid / 2 * totalScaling) - pad 'Going from the center you substract half of the width times the ui scale.
                    Dim offset = pixRwrdWid / 4 * totalScaling
                    right += offset * (4 - plyr_count) / 2
                    For i = 0 To plyr_count - 1
                        right += offset
                        Dim j As Integer = i
                        Main.Instance.Invoke(Sub() rwrdPanels(j).ShowLoading(right / dpiScaling, top / dpiScaling))
                        Main.Instance.Invoke(Sub() namePanels(j).ShowLoading((right + pad / 2) / dpiScaling, (top + pixRwrdHei * totalScaling) / dpiScaling, (offset - pad) / dpiScaling))
                        Main.addLog("DISPLAY OLD OVERLAY: " & foundText(i) & " @ (" & right & ", " & top & ")")
                    Next

                    For i = 0 To plyr_count - 1
                        Try
                            Dim plat As Double = db.market_data(foundText(i))("plat")
                            Dim ducat As Double = db.market_data(foundText(i))("ducats").ToString()
                            Dim vaulted As Boolean = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
                            Dim j As Integer = i
                            rwrdPanels(j).Invoke(Sub() rwrdPanels(j).LoadText(plat.ToString("N1"), ducat, vaulted))
                            namePanels(j).Invoke(Sub() namePanels(j).LoadText(foundText(j)))
                        Catch ex As Exception
                            Main.addLog("Something went wrong displaying overlay nr:" & i & ": " & ex.ToString)
                        End Try
                    Next
                    ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
                    Main.addLog("DISPLAY OVERLAYS-" & ParseScreen_timer & "ms")
                    ParseScreen_timer = clock.Elapsed.TotalMilliseconds
                End If


            Else
                Main.Instance.Invoke(Sub() Main.Instance.lbStatus.Text = "ERROR(UI color not found)")
                Main.Instance.Invoke(Sub() Main.Instance.lbStatus.ForeColor = Color.Red)
            End If

        Catch ex As Exception

        End Try

        Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds
        If debugFile IsNot Nothing Then
            screenBounds = window
        End If

        Dim ss_area As New Rectangle(center.X - pixRwrdWid * totalScaling / 2,
                                    center.Y - pixRwrdYDisp * totalScaling,
                                    pixRwrdWid * totalScaling,
                                    pixRwrdHei * totalScaling)


        Dim vf_area As New Rectangle(window.Left + pixFissXDisp * totalScaling,
                                    window.Top + pixFissYDisp * totalScaling,
                                    pixFissWid * totalScaling,
                                    pixFissHei * totalScaling)

        Dim debugRet As New Bitmap(CInt(screenBounds.Width * dpiScaling), CInt(screenBounds.Height * dpiScaling))
        Using graph As Graphics = Graphics.FromImage(debugRet)
            Dim screenSize As New Size(screenBounds.Width * dpiScaling, screenBounds.Height * dpiScaling)
            If debugFile IsNot Nothing Then
                graph.DrawImage(debugFile, 0, 0)
            Else
                graph.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, screenSize, CopyPixelOperation.SourceCopy)
            End If

            If Debug Then
                debugRet.Save(appData & "\WFInfo\debug\SSCLEAN-" & My.Settings.SSCount.ToString() & ".png")
            End If
            Dim print As String =
                "Tried looking at " & ss_area.ToString & vbCrLf &
                "Screen resolution: " & screenBounds.Size.ToString & vbCrLf &
                "Screen center: " & center.ToString & vbCrLf &
                "Screen bounds: " & window.ToString & vbCrLf &
                "UI scaling: " & uiScaling & vbTab & vbTab & " Windows scaling: " & dpiScaling
            Dim font As New Font("Tahoma", (screenBounds.Height / 120.0))
            Dim printBounds As SizeF = graph.MeasureString(print, font, graph.MeasureString(print, font).Width)
            Dim textbox = New Rectangle(ss_area.Right, ss_area.Bottom + 3, printBounds.Width, printBounds.Height)

            Dim print2 As String =
                "Tried looking at " & vf_area.ToString & vbCrLf &
                "Screen top-left: " & (New Point(window.X, window.Y)).ToString & vbCrLf &
                "UI scaling: " & uiScaling & vbTab & vbTab & " Windows scaling: " & dpiScaling

            Dim printBounds2 As SizeF = graph.MeasureString(print2, font, graph.MeasureString(print2, font).Width)
            Dim textbox2 = New Rectangle((vf_area.Left + vf_area.Right) / 2, vf_area.Bottom + 3, printBounds2.Width, printBounds2.Height)

            graph.DrawRectangle(New Pen(Brushes.DeepPink), ss_area)                  'The area that it tried to read from
            For i As Integer = 0 To plyr_count - 1
                Dim elem = foundRec(i)
                graph.DrawRectangle(New Pen(Brushes.HotPink), elem)             'Draws a box around each text box
                Dim printBoundsRelic As SizeF = graph.MeasureString(foundText(i), font)
                Dim rewardBox = New Rectangle(elem.Left + 3, elem.Bottom + 3, printBoundsRelic.Width + 4, printBoundsRelic.Height)
                graph.FillRectangle(Brushes.Black, rewardBox)                   'Black background for reward box
                graph.DrawString(foundText(i), font, Brushes.HotPink, rewardBox) 'Debug text ontop of screenshot

                printBoundsRelic = graph.MeasureString(tessText(i), font)
                rewardBox = New Rectangle(elem.Left + 3, rewardBox.Bottom + 3, printBoundsRelic.Width + 4, printBoundsRelic.Height)
                graph.FillRectangle(Brushes.Black, rewardBox)                   'Black background for reward box
                graph.DrawString(tessText(i), font, Brushes.HotPink, rewardBox) 'Debug text ontop of screenshot
            Next
            graph.FillRectangle(Brushes.Black, textbox)                         'Black background for text box
            graph.DrawString(print, font, Brushes.Red, textbox)                 'Debug text ontop of screenshot
            graph.DrawRectangle(New Pen(Brushes.Red), vf_area)                  'The area that it tried to read from
            graph.FillRectangle(Brushes.Black, textbox2)                        'Black background for text box
            graph.DrawString(print2, font, Brushes.Red, textbox2)               'Debug text ontop of screenshot

            debugRet.Save(appData & "\WFInfo\debug\SSFUL-" & My.Settings.SSCount.ToString() & ".png")
            My.Settings.SSCount += 1

        End Using
        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Main.addLog("PRINT DEBUG-" & ParseScreen_timer & "ms")
    End Sub

End Class
