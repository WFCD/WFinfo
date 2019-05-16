Imports System.Text.RegularExpressions
Imports Tesseract

Public Class OCR
    Public engine As New TesseractEngine("", "eng") With {
        .DefaultPageSegMode = PageSegMode.SingleLine
    }
    Public WF_Proc As Process = Nothing

    Public window As Rectangle = Nothing

    Public dpiScaling As Double = -1.0
    Public uiScaling As Double = -1.0
    Public center As Point = Nothing
    Public currStyle As WindowStyle
    Public Enum WindowStyle
        FULLSCREEN
        BORDERLESS
        WINDOWED
    End Enum

    Public pixRwrdWid As Integer = 1732 '1516
    Public pixRwrdHei As Integer = 349 '305
    Public pixRwrdPos As Integer = 363 '318
    Public pixSlctWid As Integer = 198 '172
    Public pixSlctHei As Integer = 25 '22
    Public pixSlctPos As Integer = 49 '42

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

    <System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig:=False)>
    Public Shared Sub DwmEnableComposition(bEnable As Boolean)
    End Sub

    <DllImport("gdi32.dll")>
    Public Shared Function GetDeviceCaps(hdc As IntPtr, nIndex As Integer) As Integer
    End Function

    Public Enum DeviceCap
        VERTRES = 10
        DESKTOPVERTRES = 117
    End Enum

    Public Sub New()
        engine.SetVariable("load_system_dawg", False)
        engine.SetVariable("user_words_suffix", "prime-words")
    End Sub

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
        Return False
    End Function

    Public Overridable Function isWFActive() As Boolean
        If WF_Proc Is Nothing Then
            GetWFProc()
        ElseIf WF_Proc.HasExited Then
            WF_Proc = Nothing
        End If
        Return WF_Proc IsNot Nothing
    End Function

    Public Overridable Function GetScalingFactor() As Double
        Using form As New Form()
            Using g As Graphics = form.CreateGraphics()
                If g.DpiX <> 96 Then
                    Main.addLog("FOUND DPI: g.DpiX")
                    Return g.DpiX / 96
                ElseIf g.DpiY <> 96 Then
                    Main.addLog("FOUND DPI: g.DpiY")
                    Return g.DpiY / 96
                End If
            End Using
        End Using

        Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            Dim desktop As IntPtr = g.GetHdc()
            Dim temp As Double = GetDeviceCaps(desktop, DeviceCap.DESKTOPVERTRES)
            temp /= GetDeviceCaps(desktop, DeviceCap.VERTRES)
            If temp <> 1 Then
                Main.addLog("FOUND DPI: VERTRES")
                Return temp
            End If
        End Using

        Return 1
    End Function

    Public Overridable Function GetUIScaling() As Double
        uiScaling = My.Settings.Scaling / 100
        '     All values are based on 1920x1080
        If window.Width / window.Height > 16 / 9 Then
            uiScaling *= window.Height / 1080
        Else
            uiScaling *= window.Width / 1920
        End If
        uiScaling *= dpiScaling
        Return uiScaling
    End Function

    Public Overridable Sub ForceUpdateCenter()
        ' May updated center twice
        '   It will occur if WF_Proc is nothing when isWFActive is called
        '   Given that isWFActive is called every minute
        '     This means that WF was only active for less than a minute, and therefore nothing will be happening
        '     So extra computation will not negatively affect user
        If isWFActive() Then
            UpdateCenter()
        End If
    End Sub

    Public Overridable Sub UpdateCenter()
        Dim hr As New HandleRef(WF_Proc, WF_Proc.MainWindowHandle)
        Dim tempRect As New RECT
        GetWindowRect(hr, tempRect)
        window = New Rectangle(tempRect.Left, tempRect.Top, tempRect.Right - tempRect.Left, tempRect.Bottom - tempRect.Top)

        Main.addLog("WINDOW AREA: " & window.ToString())

        Dim GWL_STYLE As Int32 = -16
        Dim WS_THICKFRAME As Long = 262144
        Dim WS_MAXIMIZE As Long = 16777216
        Dim WS_POPUP As Long = 2147483648
        'Dim GWL_EXSTYLE As Int32 = -20
        'Console.WriteLine("GWL_STYLE: " & Hex(GetWindowLong(WF_Proc.MainWindowHandle, GWL_STYLE)))
        'Console.WriteLine("GWL_EXSTYLE: " & Hex(GetWindowLong(WF_Proc.MainWindowHandle, GWL_EXSTYLE)))
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
            dpiScaling = GetScalingFactor()

            ' Start at left of window
            Dim horz_center As Integer = window.Left
            ' Padding from left and right are equal, so can just use window.Width to get to center
            ' Move to center
            horz_center += window.Width / 2

            ' Start at top of the window
            Dim vert_center As Integer = window.Top

            ' move to center
            vert_center += window.Height / 2

            ' Get Center points
            center = New Point(dpiScaling * horz_center, dpiScaling * vert_center)

            Main.addLog("UPDATED CENTER COORS: " & center.ToString())

            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        End If
    End Sub

    Public Overridable Function DefaultParseText(bmp As Bitmap) As String
        Using bmp
            Using page As Page = engine.Process(bmp)
                Return page.GetText().Trim
            End Using
        End Using
    End Function

    Public Overridable Function IsRelicWindow() As Boolean
        If Not isWFActive() Then
            Return False
        End If

        ' UI Scaling is not in UpdateCenter because it will change more frequently than the rest
        '     Also UI Scaling is 3 computations (5 if you include writing) (who knows how many with reads)
        GetUIScaling()

        Dim wid As Integer = (uiScaling * pixSlctWid)
        Dim left As Integer = center.X - Math.Ceiling(wid / 2)

        Dim hei As Integer = (uiScaling * pixSlctHei)
        Dim top As Integer = center.Y + (uiScaling * pixSlctPos)

        Main.addLog("CHECKING FOR REWARDS: {X=" & left & ",Y=" & top & ",Width=" & wid & ",Height=" & hei & "}")

        Using bmp As New Bitmap(wid, hei)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(left, top, 0, 0, New Size(wid, hei), CopyPixelOperation.SourceCopy)
            End Using

            ' Pre processing
            ' "white" is > 100 on all values
            ' looking for "white" lines in the middle of the image
            Dim mid As Integer = bmp.Height / 2
            Dim tot_white As Integer = 0
            Dim gap As Integer = 0
            Dim black As Boolean = True
            Dim clr As Color = Nothing
            For i As Integer = 0 To bmp.Width - 1
                clr = bmp.GetPixel(i, mid)
                If clr.R > 100 AndAlso clr.B > 100 AndAlso clr.G > 100 Then
                    tot_white += 1
                    If black Then
                        black = False
                        gap = 0
                    End If
                Else
                    If Not black Then
                        black = True
                        gap = 0
                    End If
                End If
                gap += 1
                ' "white" lines must be <14px
                ' gaps must be <14px
                If gap > 16 * uiScaling Then
                    Return False
                End If
            Next
            ' PERCENTAGE NEEDS TO BE BETWEEN 50% and 70%
            If tot_white < bmp.Width * 0.4 OrElse tot_white > bmp.Width * 0.6 Then
                Return False
            End If

            If LevDist(DefaultParseText(bmp), "CHOSEN REWARD") < 4 Then
                Return True
            End If
        End Using
        Return False
    End Function

    '_____________________________________________________
    '
    '    BEGIN RELIC REWARDS STUFF
    '_____________________________________________________

    Public Overridable Function Screenshot(width As Integer, height As Integer, heightPosition As Integer) As Bitmap
        Dim ss_area As New Rectangle(center.X - width / 2,
                                     center.Y - heightPosition,
                                     width,
                                     height)
        Main.addLog("TAKING SCREENSHOT:" & vbCrLf & "DPI SCALE: " & dpiScaling & vbCrLf & "SS REGION: " & ss_area.ToString())
        Dim ret As New Bitmap(width, height)
        If Debug Then 'screenshot the whole screen
            Dim debugRet As New Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height)
            Using graph As Graphics = Graphics.FromImage(debugRet)
                graph.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy)
                Dim print As String =
                        "Tried looking at " & ss_area.ToString & vbCrLf &
                        "Screen resolution: " & Screen.PrimaryScreen.Bounds.Size.ToString & vbCrLf &
                        "Screen center: " & center.ToString & vbCrLf &
                        "Screen bounds: " & window.ToString & vbCrLf &
                        "UI scaling: " & uiScaling & vbTab & vbTab & " Windows scaling: " & dpiScaling
                Dim font As New Font("Tahoma", (Screen.PrimaryScreen.Bounds.Height / 120.0))
                Dim printBounds As SizeF = graph.MeasureString(print, font, graph.MeasureString(print, font).Width)

                Dim textbox = New Rectangle(center.X, center.Y, printBounds.Width, printBounds.Height)

                graph.FillEllipse(Brushes.Red, center.X - 3, center.Y - 3, 3, 3)    'Dot centered at where it thinks the center of warframe is
                graph.DrawRectangle(New Pen(Brushes.Red), ss_area)                  'The area that it tried to read from
                graph.FillRectangle(Brushes.Black, textbox)                         'Black background for text box
                graph.DrawString(print, font, Brushes.Red, textbox)                 'Debug text ontop of screenshot
                debugRet.Save(appData & "\WFInfo\debug\SSFULL-" & My.Settings.SSCount.ToString() & ".png")
                Main.addLog("SAVING SCREENSHOT: " & appData & "\WFInfo\debug\SSFULL-" & My.Settings.SSCount.ToString() & ".png")
                My.Settings.SSCount += 1
            End Using
        End If

        Using graph As Graphics = Graphics.FromImage(ret)
            graph.CopyFromScreen(ss_area.X, ss_area.Y, 0, 0, New Size(ss_area.Width, ss_area.Height), CopyPixelOperation.SourceCopy)
        End Using
        Return ret
    End Function

    Public Overridable Function GetRelicWindow() As Bitmap
        GetUIScaling()

        Dim wid As Integer = pixRwrdWid * uiScaling + 2
        Dim hei As Integer = pixRwrdHei * uiScaling + 2
        Dim top As Integer = pixRwrdPos * uiScaling + 2

        Dim ret As Bitmap = Screenshot(wid, hei, top)

        Return ret
    End Function

    Public Overridable Function GetPlayers(screen As Image) As Integer
        Dim count As Integer = 0

        Dim CropRect As New Rectangle(0, screen.Height * 0.95, screen.Width, 4)
        Using CropImage As New Bitmap(CropRect.Width, CropRect.Height)
            Using grp = Graphics.FromImage(CropImage)
                grp.DrawImage(screen, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            End Using

            ' ITERATING FROM LEFT TO RIGHT
            '   LOOKING FOR bright spots
            '   WHEN ONE IS FOUND, MARK IT, THEN MOVE RIGHT
            Dim clr As Color = Nothing
            Dim last As Integer = -200
            Dim gap As Integer = screen.Width / 9
            For i As Integer = 0 To CropImage.Width - 1
                For j As Integer = 0 To CropImage.Height - 1
                    clr = CropImage.GetPixel(i, j)
                    If clr.R > 80 OrElse clr.G > 80 OrElse clr.B > 80 Then
                        If last + gap < i Then
                            count += 1
                        End If
                        last = i
                        Exit For
                    End If
                Next
            Next
        End Using
        Return count
    End Function

    Public Overridable Function ColortoRarity(clr As Color, Optional diff As Integer = 50) As Integer
        Dim ret As Integer = 3
        Dim temp As Integer
        For Each rare As Color In rarity
            temp = clr.R
            temp -= rare.R
            If temp > -diff AndAlso temp < diff Then
                temp = clr.G
                temp -= rare.G
                If temp > -diff AndAlso temp < diff Then
                    temp = clr.B
                    temp -= rare.B
                    If temp > -diff AndAlso temp < diff Then
                        Return ret
                    End If
                End If
            End If
            ret -= 1
        Next
        Return ret
    End Function

    Public GetPartText_timer As Long = 0
    Public Overridable Function GetPartText(screen As Bitmap, plyr_count As Integer, plyr As Integer, Optional multi As Boolean = False) As String
        GetPartText_timer = clock.Elapsed.TotalMilliseconds
        ' This will not only check the bottom line of text
        '   But also will check one line up if the bottom line is ONLY "BLUEPRINT"
        Dim height As Integer = screen.Height * 0.1    ' Line Height
        Dim width As Integer = (screen.Width / 4)      ' Window Width
        Dim startX As Integer = (screen.Width / 4) * (plyr + (4 - plyr_count) / 2)
        Dim startY As Integer = screen.Height * 0.8
        If multi Then
            startY = screen.Height * 0.7
        End If

        Using bmp As New Bitmap(width, height)
            Dim clr As Color = Nothing
            For i As Integer = 0 To bmp.Width - 1
                For j As Integer = 0 To bmp.Height - 1
                    clr = screen.GetPixel(startX + i, startY + j)
                    Select Case (ColortoRarity(clr))
                        Case 3, 2, 1
                            bmp.SetPixel(i, j, Color.Black)
                        Case 0
                            bmp.SetPixel(i, j, Color.White)
                    End Select
                Next
            Next
            GetPartText_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("IMAGE FILTER-" & GetPartText_timer & "ms")
            GetPartText_timer = clock.Elapsed.TotalMilliseconds
            ' ONLY one page can be used at a time
            '   So we have to get the text and dispose of the page quickly
            Dim ret As String = ""
            Using page As Page = engine.Process(bmp)
                ret = Regex.Replace(page.GetText(), "[^A-Z& ]", "").Trim
            End Using

            GetPartText_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("LINE-" & GetPartText_timer & "ms")

            ' IF A LONGER NAME IS ADDED
            '   THIS WILL NEED TO BE IMPROVED ON
            If ret.Length < 14 AndAlso LevDist(ret, "Blueprint") < 4 Then
                Return GetPartText(screen, plyr_count, plyr, True) & " " & ret
            End If
            Return ret
        End Using
    End Function

    Public ParseScreen_timer As Long = 0
    Public Overridable Sub ParseScreen()
        If Not isWFActive() Then
            Return
        End If

        Dim screen As Bitmap
        Dim players As Integer = 0
        Dim foundText As New List(Of String)()
        ' Start timer
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds
        UpdateCenter()
        screen = GetRelicWindow()

        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("SCREENSHOT-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        ' Get Player Count from Image
        players = GetPlayers(screen)
        Dim top As Integer = 0
        Dim right As Integer = 0

        If Not DisplayWindow Then
            Dim pad As Integer = screen.Height * 0.05
            top = center.Y - pixRwrdPos * uiScaling + pad
            right = center.X - screen.Width / 2 - pad
            ' Adjust for <4 players
            right -= (players - 4) * screen.Width / 8
            For i = 0 To players - 1
                right += screen.Width / 4
                Dim j As Integer = i
                Main.Instance.Invoke(Sub() rwrdPanels(j).ShowLoading(right, top))
            Next
        End If

        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("PLAYER COUNT(" + players.ToString() + ")-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        ' Get Part Text from Image
        ' Only retrieve capitals
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        For i = 0 To players - 1
            ' Crops out each players relic window
            ' Finds text in those images, then adds to list
            Dim text As String = GetPartText(screen, players, i)
            text = db.GetPartName(text)
            foundText.Add(text)
        Next
        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("GET ALL PARTS-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        If DisplayWindow Then
            Main.Instance.Invoke(Sub() RewardWindow.Display(foundText))
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY WINDOW-" & ParseScreen_timer & "ms")
        Else
            ' Move over if you don't have all 4
            For i = 0 To foundText.Count - 1

                Main.addLog("DISPLAY OVERLAY " & (i + 1) & ":" & vbCrLf & "Right, Top: " & right & ", " & top)
                Dim plat As Double = db.market_data(foundText(i))("plat")
                Dim ducat As Double = db.market_data(foundText(i))("ducats").ToString()
                Dim vaulted As Boolean = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
                Console.WriteLine(foundText(i) & "--" & plat & "---" & ducat)
                Dim j As Integer = i
                rwrdPanels(j).Invoke(Sub() rwrdPanels(j).LoadText(plat.ToString("N1"), ducat, vaulted))

            Next
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY OVERLAYS-" & ParseScreen_timer & "ms")
        End If

    End Sub

    '_____________________________________________________
    '
    '    BEGIN RELIC Refinement STUFF
    '_____________________________________________________

    Public scrollLoc As Integer = -100
    Public eraLoc As Integer = -1

    Public RefineORSelection As Boolean = False
    Public FoundRefineWin As Boolean = False
    Public RefineOverlayShown As Boolean = False

    Public Overridable Function CheckEraSelection() As Boolean
        Console.WriteLine("CheckEraSelection")

        GetUIScaling()

        Dim eraX As Integer = center.X - 259 * uiScaling
        Dim eraY As Integer = center.Y - 340 * uiScaling
        Dim eraW As Integer = 507 * uiScaling
        Using bmp As New Bitmap(eraW, 1)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(eraX, eraY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy)
            End Using
            Dim currLoc As Integer = 0
            For i As Integer = 0 To eraW - 1
                If bmp.GetPixel(i, 0).R > 100 Then
                    currLoc = i
                    Exit For
                End If
            Next
            currLoc = currLoc / (eraW / 4)
            If currLoc = eraLoc Then
                Return False
            End If
            eraLoc = currLoc
        End Using
        Console.WriteLine("ERA CHANGED")
        Return True
    End Function

    Public Overridable Function CheckScrollBar() As Boolean
        Console.WriteLine("CheckScrollBar")
        ' Look for changes in scroll bar on right side of window
        ' if changes found, hide overlays and start ~200ms timer
        GetUIScaling()

        ' Scroll bar coordinates from center
        Dim scrollX As Integer = 310 * uiScaling
        Dim scrollY As Integer = -300 * uiScaling
        Dim scrollH As Integer = 650 * uiScaling
        Using bmp As New Bitmap(1, scrollH)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(center.X + scrollX, center.Y + scrollY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy)
            End Using
            Dim currLoc As Integer = 0
            For i As Integer = 0 To scrollH - 1
                If bmp.GetPixel(0, i).R > 200 Then
                    currLoc = i
                    Exit For
                End If
            Next
            If Math.Abs(currLoc - scrollLoc) < 5 Then
                Return False
            End If
            scrollLoc = currLoc
        End Using
        Console.WriteLine("SCROLLBAR CHANGED")
        Return True
    End Function

    Public Overridable Sub UpdateRefineOverlay()

        ' Need to split up 







        Console.WriteLine("UpdateRefinementOverlay")
        CheckEraSelection()
        CheckScrollBar()

        Dim relicH As Integer = 221 * uiScaling
        Dim relicW As Integer = 315 * uiScaling
        Dim x As Integer = center.X - 645 * uiScaling
        Dim y As Integer = center.Y - 306 * uiScaling
        Dim bmpSize As New Size(300 * uiScaling, 22 * uiScaling)

        Dim left As Integer = x
        Dim top As Integer = y
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789[]")

        Dim more As Boolean = True
        For i As Integer = 0 To 2
            For j As Integer = 0 To 2
                left = x + relicW * j
                top = y + relicH * i
                Dim pad As Integer = relicW * 0.03
                Dim index As Integer = i * 3 + j
                relicPanels(index).ShowLoading(left + relicW - pad, top + pad)

                left += 8 * uiScaling
                top += 162 * uiScaling

                Dim name As String
                Dim clr As Color
                ' Get half of text (bottom half)
                Using bmp As New Bitmap(bmpSize.Width, bmpSize.Height)
                    ' Copy bottom half to bmp
                    Using graph As Graphics = Graphics.FromImage(bmp)
                        graph.CopyFromScreen(left, top + bmpSize.Height, 0, 0, bmpSize, CopyPixelOperation.SourceCopy)

                        If Debug Then
                            bmp.Save(appData & "\WFInfo\debug\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")
                            Main.addLog("SAVING SCREENSHOT: " & appData & "\WFInfo\debug\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")

                            My.Settings.DebugCount += 1
                        End If

                        For l As Integer = 0 To bmpSize.Width - 1
                            For k As Integer = 0 To bmpSize.Height - 1
                                clr = bmp.GetPixel(l, k)
                                If l = 0 Then
                                    If clr.R < 180 OrElse clr.G < 180 OrElse clr.B < 80 OrElse (clr.B < 180 And clr.B > 100) Then
                                        Console.WriteLine("FOUND LAST-" & k)
                                        relicPanels(index).Hide()
                                        more = False
                                        Exit For
                                    End If
                                End If
                            Next
                        Next
                        If Debug Then
                            bmp.Save(appData & "\WFInfo\debug\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")
                            Main.addLog("SAVING SCREENSHOT: " & appData & "\WFInfo\debug\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")

                        End If
                        If Not more Then
                            Exit For
                        End If

                        name = DefaultParseText(bmp)
                    End Using
                End Using

                Using bmp As New Bitmap(bmpSize.Width, bmpSize.Height)
                    ' Copy bottom half to bmp
                    Using graph As Graphics = Graphics.FromImage(bmp)
                        graph.CopyFromScreen(left, top, 0, 0, bmpSize, CopyPixelOperation.SourceCopy)
                        If Debug Then
                            bmp.Save(appData & "\WFInfo\debug\RR" & i & j & "2-" & My.Settings.DebugCount & ".png")
                            Main.addLog("SAVING SCREENSHOT: " & appData & "\WFInfo\debug\RR" & i & j & "2-" & My.Settings.DebugCount & ".png")

                        End If
                        Dim multi As Boolean = True
                        For k As Integer = 0 To bmpSize.Height - 1
                            clr = bmp.GetPixel(bmpSize.Width - 1, k)
                            If clr.R < 180 OrElse clr.G < 180 OrElse clr.B < 80 OrElse (clr.B < 180 And clr.B > 100) Then
                                multi = False
                                Exit For
                            End If
                        Next
                        If multi Then
                            name = DefaultParseText(bmp) & " " & name
                        End If
                    End Using
                End Using
                Dim temp_str As String = "????"
                If RefineORSelection AndAlso eraLoc > -1 AndAlso eraLoc < 4 Then
                    temp_str = {"LITH", "MESO", "NEO", "AXI"}(eraLoc)
                Else
                    Dim temp As Integer = name.IndexOf(" ")
                    If temp < 3 Then
                        temp = name.IndexOf(" ", temp + 1)
                    End If
                    temp_str = name.Substring(0, temp).Replace(" ", "")
                End If
                name = name.Replace(" ", "")
                Dim diff As Integer = LevDist2(name, temp_str + "??RELIC")
                Console.WriteLine("COMPARE: " & name & " TO " & temp_str + "??RELIC" & " = " & diff)
                Dim found_name As String = db.GetRelicName(name)
                Console.WriteLine("FOUND: " & found_name & " FROM " & name)
                Dim found_split As String() = found_name.Split(" ")
                relicPanels(index).LoadText(found_split(0), found_split(1), True)
            Next
            If Not more Then
                Exit For
            End If
        Next

        ' Once timer ends, parse screen locations for text
        ' Show relic info one by one starting at top left and cycling through until a missing one is found
        ' Relic info will use the standard overlays (cause graphics work is hard):
        '    Plat: 5.9 (+0.9) --- 5.9 currently with +0.9 when radiant
        '    Ducat: 23 (+5.6) --- average of 23 ducats (rounded) with +5.6 when radiant

        ' Screen Locations At 100%:
        '   Relic 3x3 - 945 x 663 at (195, 219)
        '     horz pad of 11px
        '     vert pad of 10px
        '     indv relic - 315 x 221 (1st at 195, 219))
        '       text - 300 x 36 at (+8, +178) (203, 397)
        '      multi - 300 x 60 at (+8, +154) (203, 373)
        '     How to get size + lines:
        '       Start at (500,430) > go up until not "white" / "yellow"
        '   Scroll bar - 11 x 651 at (1145, 225)
        '     Only need 1 pixel     1 x 650 at (1150 , 225)

    End Sub

    Public Overridable Sub ShowRelicOverlay()
        GetUIScaling()
        UpdateRefineOverlay()
    End Sub

    Public Overridable Function IsRefinementWindow() As Boolean
        If Not isWFActive() Then
            FoundRefineWin = False
            Return False
        End If

        Console.WriteLine("IsRefinementWindow")
        GetUIScaling()

        ' 520 x 47 @ (580, 91)

        Dim wid As Integer = Math.Ceiling(uiScaling * 260) * 2
        Dim hei As Integer = (uiScaling * 47)
        Dim left As Integer = center.X - (wid / 2)
        Dim top As Integer = center.Y - (uiScaling * 434)

        Using bmp As New Bitmap(wid, hei)
            Using graph As Graphics = Graphics.FromImage(bmp)
                graph.CopyFromScreen(left, top, 0, 0, New Size(wid, hei), CopyPixelOperation.SourceCopy)
            End Using

            ' Pre processing
            ' "white" is > 100 on all values
            ' looking for "white" lines in the middle of the image
            Dim mid As Integer = bmp.Height / 2
            Dim tot_white As Integer = 0
            Dim gap As Integer = 0
            Dim black As Boolean = True
            Dim clr As Color = Nothing
            For i As Integer = 0 To bmp.Width - 1
                clr = bmp.GetPixel(i, mid)
                If clr.R > 100 AndAlso clr.B > 100 AndAlso clr.G > 100 Then
                    tot_white += 1
                    If black Then
                        black = False
                        gap = 0
                    End If
                Else
                    If Not black Then
                        black = True
                        gap = 0
                    End If
                End If
                gap += 1
                ' "white" lines must be <42px
                ' gaps must be <42px
                If gap > 42 * uiScaling Then
                    FoundRefineWin = False
                    Return False
                End If
            Next
            ' PERCENTAGE NEEDS TO BE BETWEEN 50% and 70%

            If tot_white < bmp.Width * 0.2 OrElse tot_white > bmp.Width * 0.4 Then
                Return False
            End If
            Dim parsed As String = DefaultParseText(bmp)
            If LevDist(parsed, "VOID RELIC REFINEMENT") < 4 Then
                RefineORSelection = True
                FoundRefineWin = True
                Return True
            End If
            If LevDist(parsed, "VOID RELIC SELECTION") < 4 Then
                RefineORSelection = False
                FoundRefineWin = True
                Return True
            End If
        End Using
        FoundRefineWin = False
        Return False
    End Function
End Class

'Notes for inventory parsing:
'All these are taken at 16:9 Examples are taken at the highest scaling factor
'@ 99-100% scaling = 6  items per row, example: https://i.imgur.com/5xexNtw.png
'@ 98-90%  scaling = 7  items per row, example: https://i.imgur.com/t1p9Gzp.png
'@ 89-84%  scaling = 8  items per row, example: https://i.imgur.com/5i9ztVj.png
'@ 83-77%  scaling = 9  items per row, example: https://i.imgur.com/oJDLyME.png
'@ 76-72%  scaling = 10 items per row, example: https://i.imgur.com/RGBGBeM.png
'@ 71-68%  scaling = 11 items per row, example: https://i.imgur.com/U2poYYG.png
'@ 67-64%  scaling = 12 items per row, example: https://i.imgur.com/18hKGeT.png
'@ 63-60%  scaling = 13 items per row, example: https://i.imgur.com/2yvtA4g.png
'@ 59-57%  scaling = 14 items per row, example: https://i.imgur.com/ucd71Pp.png
'@ 56-54%  scaling = 15 items per row, example: https://i.imgur.com/0a5FAKg.png
'@ 53-51%  scaling = 16 items per row, example: https://i.imgur.com/mBemIRN.png
'@ 50-50%  scaling = 17 items per row, example: https://i.imgur.com/9hcgnt5.png