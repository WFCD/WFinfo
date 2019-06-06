Imports System.Text.RegularExpressions
Imports Tesseract

Public Class OCR2
    ' Pixel measurements for reward screen @ 1920 x 1080 with 100% scale https://docs.google.com/drawings/d/1Qgs7FU2w1qzezMK-G1u9gMTsQZnDKYTEU36UPakNRJQ/edit
    Public Const pixRwrdWid As Integer = 968
    Public Const pixRwrdHei As Integer = 235
    Public Const pixRwrdYDisp As Integer = 185
    Public Const pixRwrdLineHei As Integer = 44
    Public Const pixRwrdLineWid As Integer = 240

    ' Pixel measurement for player bars for player count
    '   Width is same as pixRwrdWid
    ' Public Const pixRareWid As Integer = pixRwrdWid
    '   Height is always 1px
    ' Public Const pixRareHei As Integer = 1
    '   Box is centered horizontally
    ' Public Const pixRareXDisp As Integer = ???
    Public Const pixRareYDisp As Integer = 195

    Public Const pixProfWid As Integer = 48
    Public Const pixProfTotWid As Integer = 192
    ' Height is always 1px
    ' Public Const pixProfHei As Integer = 1
    Public Const pixProfXDisp As Integer = 93
    Public Const pixProfYDisp As Integer = 86

    ' Pixel measurements for detecting reward screen
    Public Const pixFissWid As Integer = 354
    Public Const pixFissHei As Integer = 45
    Public Const pixFissXDisp As Integer = 285
    Public Const pixFissYDisp As Integer = 43

    ' Colors for "VOIDFISSURE/REWARDS"

    Public uiColor As Color
    Public FissClr1 As Color = Color.FromArgb(189, 168, 101)    ' default
    Public FissClr2 As Color = Color.FromArgb(153, 31, 35)      ' stalker
    Public FissClr3 As Color = Color.FromArgb(238, 193, 105)    ' baruk
    Public FissClr4 As Color = Color.FromArgb(35, 201, 245)     ' corpus
    Public FissClr5 As Color = Color.FromArgb(57, 105, 192)     ' fortuna
    Public FissClr6 As Color = Color.FromArgb(255, 189, 102)    ' grineer
    Public FissClr7 As Color = Color.FromArgb(36, 184, 242)     ' lotus
    Public FissClr8 As Color = Color.FromArgb(140, 38, 92)      ' nidus
    Public FissClr9 As Color = Color.FromArgb(20, 41, 29)       ' orokin
    Public FissClr10 As Color = Color.FromArgb(9, 78, 106)      ' tenno

    Public rarity As New List(Of Color) From {Color.FromArgb(120, 70, 60), Color.FromArgb(80, 83, 89), Color.FromArgb(117, 109, 75)}
    Public fissColors As Color() = {FissClr1, FissClr2, FissClr3, FissClr4, FissClr5, FissClr6, FissClr7, FissClr8, FissClr9, FissClr10}

    ' Warframe window stats
    '   Warframe process
    Public WF_Proc As Process = Nothing
    '   Warframe window style (Fullscreen, Windowed, Borderless)
    Public currStyle As WindowStyle

    '   Warframe window bounds & points
    Public window As Rectangle = Nothing
    Public center As Point = Nothing

    ' Scaling adjustments
    Public dpiScaling As Double = -1.0
    Public uiScaling As Double = -1.0
    Public totalScaling As Double = -1.0

    ' Tesseract engines for each reward
    ' 0 for relic window checks or any system checks
    ' 1-4 for players 1 to 4
    Public engine(4) As TesseractEngine
    Public tasks(3) As Task
    Public running As New List(Of Task)
    Public plyr_count As Integer = -1

    ' List of results found by OCR, didn't have a better place to put it
    Public foundText(3) As String
    Public foundRec(3) As Rectangle


    Public Enum WindowStyle
        FULLSCREEN
        BORDERLESS
        WINDOWED
    End Enum

    Public Enum DeviceCap
        VERTRES = 10
        DESKTOPVERTRES = 117
    End Enum

    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure

    <DllImport("user32.dll")>
    Public Shared Function GetWindowRect(ByVal hWnd As HandleRef, ByRef lpRect As RECT) As Boolean
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Int16) As Int32
    End Function

    <DllImport("dwmapi.dll", PreserveSig:=False)>
    Public Shared Sub DwmEnableComposition(bEnable As Boolean)
    End Sub

    <DllImport("gdi32.dll")>
    Public Shared Function GetDeviceCaps(hdc As IntPtr, nIndex As Integer) As Integer
    End Function

    Public Sub New()
        For i As Integer = 0 To 4
            engine(i) = New TesseractEngine("", "eng")
            If i = 0 Then
                engine(i).DefaultPageSegMode = PageSegMode.SingleLine
                engine(i).SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ/")
            Else
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

    Public Overridable Sub GetUiColor(Optional bmp As Bitmap = Nothing)
        Dim reset As Boolean = bmp Is Nothing
        uiColor = Nothing
        Dim width As Integer = pixProfWid * totalScaling / 2
        If bmp Is Nothing Then
            bmp = New Bitmap(width, 1)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(pixProfXDisp * totalScaling + width / 2, pixProfYDisp * totalScaling, 0, 0, New Size(width, 1), CopyPixelOperation.SourceCopy)
            End Using
        End If
        If Debug Then
            bmp.Save(appData & "\WFInfo\debug\COLOR_CHECK-" & My.Settings.ColorcheckCount.ToString() & ".png")
            My.Settings.ColorcheckCount += 1
        End If
        Dim clr As Color = Nothing
        Dim R, G, B As Integer

        For i As Integer = 0 To width - 1
            clr = bmp.GetPixel(i, 0)
            R += clr.R
            G += clr.G
            B += clr.B
        Next
        R /= width
        G /= width
        B /= width
        Dim detectedColor = Color.FromArgb(R, G, B)
        For Each knowColor In fissColors
            If ColorThreshold(detectedColor, knowColor) Then
                uiColor = detectedColor
                If reset Then
                    bmp.Dispose()
                End If
                Exit Sub
            End If
        Next
        Main.addLog("Couldn't find matching ui color out of: " & detectedColor.ToString)
        If reset Then
            bmp.Dispose()
        End If
    End Sub

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
        dpiScaling = 1

        Using form As New Form()
            Using g As Graphics = form.CreateGraphics()
                If g.DpiX <> 96 Then
                    Main.addLog("FOUND DPI: g.DpiX")
                    dpiScaling = g.DpiX / 96
                    Return dpiScaling
                ElseIf g.DpiY <> 96 Then
                    Main.addLog("FOUND DPI: g.DpiY")
                    dpiScaling = g.DpiY / 96
                    Return dpiScaling
                End If
            End Using
        End Using

        Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            Dim desktop As IntPtr = g.GetHdc()
            dpiScaling = GetDeviceCaps(desktop, DeviceCap.DESKTOPVERTRES) / GetDeviceCaps(desktop, DeviceCap.VERTRES)
            If dpiScaling <> 1 Then
                Main.addLog("FOUND DPI: VERTRES")
            End If
        End Using

        totalScaling = dpiScaling * uiScaling
        Return dpiScaling
    End Function

    Public Overridable Function GetUIScaling() As Double
        uiScaling = My.Settings.Scaling / 100
        '     All values are based on 1920x1080
        If window.Width / window.Height > 16 / 9 Then
            uiScaling *= window.Height / 1080
        Else
            uiScaling *= window.Width / 1920
        End If
        totalScaling = dpiScaling * uiScaling
        Return uiScaling
    End Function

    Public Overridable Function GetWFProc() As Boolean
        For Each p As Process In Process.GetProcesses
            If p.ProcessName.Contains("Warframe") Then
                If WF_Proc Is Nothing OrElse p.Handle <> WF_Proc.Handle Then
                    WF_Proc = p
                    UpdateCenter()
                End If
                Return True
            End If
        Next
        WF_Proc = Nothing
        If Debug And window = Nothing Then
            FakeUpdateCenter()
        End If
        Return False
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

        ' Get DPI Scaling
        GetDPIScaling()

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

        window = New Rectangle(tempRect.Left, tempRect.Top, tempRect.Right - tempRect.Left, tempRect.Bottom - tempRect.Top)
        Main.addLog("WINDOW AREA: " & window.ToString())

        Dim GWL_STYLE As Int32 = -16
        Dim WS_THICKFRAME As Long = 262144
        Dim WS_MAXIMIZE As Long = 16777216
        Dim WS_POPUP As Long = 2147483648
        Dim styles As Long = GetWindowLong(WF_Proc.MainWindowHandle, GWL_STYLE)
        If (styles And WS_THICKFRAME) <> 0 Then
            window = New Rectangle(window.Left + 8, window.Top + 30, window.Width - 16, window.Height - 38)
            Main.addLog("WINDOWED ADJUSTMENT: " & window.ToString())
            currStyle = WindowStyle.WINDOWED
            DwmEnableComposition(True)
        ElseIf (styles And WS_POPUP) <> 0 Then
            currStyle = WindowStyle.BORDERLESS
            DwmEnableComposition(True)
        Else
            currStyle = WindowStyle.FULLSCREEN
            DwmEnableComposition(False)
        End If

        If window.Width <= 0 Or window.Height <= 0 Then
            WF_Proc = Nothing
            window = Nothing
        Else
            ' Get DPI Scaling
            GetDPIScaling()

            ' Get Window Points
            Dim horz_center As Integer = window.Left + (window.Width / 2)
            Dim vert_center As Integer = window.Top + (window.Height / 2)
            center = New Point(dpiScaling * horz_center, dpiScaling * vert_center)

            Main.addLog("UPDATED CENTER COORS: " & center.ToString())
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
        If Not IsWFActive() Then
            Return False
        End If

        GetDPIScaling()
        GetUIScaling()
        GetUiColor()

        Dim bnds As New Rectangle(window.Left + pixFissXDisp * totalScaling,
                                  window.Top + pixFissYDisp * totalScaling,
                                  pixFissWid * totalScaling,
                                  pixFissHei * totalScaling)

        Using bmp As New Bitmap(bnds.Width + 40, bnds.Height + 40)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(bnds.X, bnds.Y, 20, 20, New Size(bnds.Width, bnds.Height), CopyPixelOperation.SourceCopy)
            End Using

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

            If Debug Then
                bmp.Save(appData & "\WFInfo\debug\FISS_CHECK-" & My.Settings.FisschckCount.ToString() & ".png")
                My.Settings.FisschckCount += 1
            End If

            If Not found Then
                Return False
            End If

            Dim ret As String = DefaultParseText(bmp, 0).Replace(" ", "")
            ' Finds: YOID FILS S URE -> YOIDFILSSURE

            If LevDist(ret, "VOIDFISSURE") < 4 Then
                Return True
            End If
        End Using
        Return False
    End Function

    Public Overridable Function GetPlayers() As Integer
        Dim width As Integer = 4 * totalScaling
        width *= 2
        Dim quarter As Integer = pixRwrdWid * totalScaling / 4
        Dim bnds As New Rectangle(center.X - quarter * 2, center.Y - pixRareYDisp * totalScaling - 5,
                                  quarter * 4, 11)


        Dim count As Integer = -1
        Dim clr As Color = Nothing

        Using bmp As New Bitmap(bnds.Width, bnds.Height)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(bnds.X, bnds.Y, 0, 0, New Size(bnds.Width, bnds.Height), CopyPixelOperation.SourceCopy)
            End Using
            bmp.Save(appData & "\WFInfo\debug\RARE_BAR-" & My.Settings.RarebarCount.ToString() & ".png")
            My.Settings.RarebarCount += 1

            For n As Integer = 1 To 4
                Dim i As Integer = 5 - n
                Dim failed As Boolean = False
                Dim x As Integer = quarter / 2 * i
                x -= width / 2
                For j As Integer = 0 To Math.Max(0, width - 5)
                    clr = bmp.GetPixel(x + j, 5)
                    Console.WriteLine("(" & (x + j) & ", 5) - " & clr.ToString() & " Bright: " & clr.GetBrightness())
                    If clr.GetBrightness() > 0.5 Then
                        failed = True
                        Exit For
                    End If
                Next
                If failed Then
                    ' if 4 then odd number and at least 1
                    ' if 3 then even number and at least 2
                    ' if 2 then odd number and 3
                    ' if 1 then even number and 4

                    count = n
                    Console.WriteLine("CURRENT PLAYER COUNT: " & count)
                End If
                Console.WriteLine("-------------------------------------------")
            Next
        End Using
        Console.WriteLine(count)


        Console.WriteLine("FOUND " & count & " PLAYERS")
        Return count
    End Function

    Public Overridable Function GetPlayerImage(plyr As Integer, count As Integer) As Bitmap
        Dim padding = 6
        Dim width As Integer = pixRwrdWid / 4 * totalScaling - padding
        Dim lineHeight As Integer = pixRwrdLineHei * totalScaling

        Dim left As Integer = center.X - (width + padding) * (count / 2 - plyr) + 3
        Dim top As Integer = center.Y - pixRwrdYDisp * totalScaling + pixRwrdHei * totalScaling - lineHeight

        Dim ret As New Bitmap(width, lineHeight + 10)
        Using graph As Graphics = Graphics.FromImage(ret)
            graph.CopyFromScreen(left, top, 5, 5, New Size(width, lineHeight), CopyPixelOperation.SourceCopy)
        End Using
        If Debug Then
            foundRec(plyr) = New Rectangle(New Point(left, top), New Size(width, lineHeight))
        End If
        Return CleanImage(ret, Math.Max(uiColor.R, Math.Max(uiColor.G, uiColor.B)) / 7 + 30)
    End Function

    Public ParsePlayer_timer() As Long = {0, 0, 0, 0}
    Public Overridable Sub ParsePlayer(plyr As Integer)
        ParsePlayer_timer(plyr) = clock.Elapsed.TotalMilliseconds
        Dim text As Bitmap = GetPlayerImage(plyr, plyr_count)

        text.Save(appData & "\WFInfo\debug\SCAN" & My.Settings.EtcCount.ToString() & "-PLYR-" & plyr & ".png")

        Dim result = DefaultParseText(text, plyr + 1)

        foundText(plyr) = db.GetPartName(result)

        ParsePlayer_timer(plyr) -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("PLAYER(" & plyr & ") PARSE-" & ParsePlayer_timer(plyr) & "ms")
    End Sub

    Public ParseScreen_timer As Long = 0
    Public Overridable Sub ParseScreen()
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        If Not IsWFActive() Then
            Return
        End If

        GetDPIScaling()
        GetUIScaling()
        GetUiColor()

        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("GET COLOR & SCALING-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        plyr_count = GetPlayers()

        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("GET PLAYER COUNT-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds


        For i As Integer = 0 To plyr_count - 1
            Dim plyr As Integer = i
            running.Add(Task.Factory.StartNew(Sub() ParsePlayer(plyr)))
        Next

        Task.WaitAll(running.ToArray())
        running.Clear()

        My.Settings.EtcCount += 1
        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("GET ALL PARTS-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        If Debug Then
            Dim ss_area As New Rectangle(center.X - pixRwrdWid * totalScaling / 2,
                                         center.Y - pixRwrdYDisp * totalScaling,
                                         pixRwrdWid * totalScaling,
                                         pixRwrdHei * totalScaling)


            Dim vf_area As New Rectangle(window.Left + pixFissXDisp * totalScaling,
                                         window.Top + pixFissYDisp * totalScaling,
                                         pixFissWid * totalScaling,
                                         pixFissHei * totalScaling)

            Dim debugRet As New Bitmap(CInt(Screen.PrimaryScreen.Bounds.Width * dpiScaling), CInt(Screen.PrimaryScreen.Bounds.Height * dpiScaling))
            Using graph As Graphics = Graphics.FromImage(debugRet)
                Dim screenSize As New Size(Screen.PrimaryScreen.Bounds.Width * dpiScaling, Screen.PrimaryScreen.Bounds.Height * dpiScaling)
                graph.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, screenSize, CopyPixelOperation.SourceCopy)
                Dim print As String =
                        "Tried looking at " & ss_area.ToString & vbCrLf &
                        "Screen resolution: " & Screen.PrimaryScreen.Bounds.Size.ToString & vbCrLf &
                        "Screen center: " & center.ToString & vbCrLf &
                        "Screen bounds: " & window.ToString & vbCrLf &
                        "UI scaling: " & uiScaling & vbTab & vbTab & " Windows scaling: " & dpiScaling
                Dim font As New Font("Tahoma", (Screen.PrimaryScreen.Bounds.Height / 120.0))
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
                    Dim printBoundsRelic As SizeF = graph.MeasureString(foundText(i), font, graph.MeasureString(foundText(i), font).Width)
                    Dim rewardBox = New Rectangle(elem.Left + 3, elem.Bottom + 3, printBoundsRelic.Width, printBoundsRelic.Height)
                    graph.FillRectangle(Brushes.Black, rewardBox)                   'Black background for reward box
                    graph.DrawString(foundText(i), font, Brushes.HotPink, rewardBox) 'Debug text ontop of screenshot
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
            Console.WriteLine("PRINT DEBUG-" & ParseScreen_timer & "ms")
            ParseScreen_timer = clock.Elapsed.TotalMilliseconds
        End If

        ' Display window true = seperate window
        ' Display window false = overlay
        If DisplayWindow Then
            'run window
            Main.Instance.Invoke(Sub() RewardWindow.Display(foundText))
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY WINDOW-" & ParseScreen_timer & "ms")
        Else
            'run overlay
            ' Move over if you don't have all 4



            Dim pad As Integer = pixRwrdHei * 0.05 * totalScaling 'padding to prevent it from looking off.
            Dim top = center.Y - pixRwrdYDisp * totalScaling + pad 'from center to the top it's 248px
            Dim left = center.X - (pixRwrdWid / 2 * totalScaling) - pad 'Going from the center you substract half of the width times the ui scale.
            Dim offset = pixRwrdWid / 4 * totalScaling
            For i = 0 To plyr_count - 1
                left += offset
                Dim j As Integer = i
                Main.Instance.Invoke(Sub() rwrdPanels(j).ShowLoading(left / dpiScaling, top / dpiScaling))
            Next

            For i = 0 To foundText.Count - 1
                Dim plat As Double = db.market_data(foundText(i))("plat")
                Dim ducat As Double = db.market_data(foundText(i))("ducats").ToString()
                Dim vaulted As Boolean = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
                Dim j As Integer = i
                rwrdPanels(j).Invoke(Sub() rwrdPanels(j).LoadText(plat.ToString("N1"), ducat, vaulted))
            Next
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY OVERLAYS-" & ParseScreen_timer & "ms")
        End If

    End Sub

End Class
