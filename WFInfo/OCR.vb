Imports System.Text.RegularExpressions
Imports Tesseract

Module OCR
    Private engine As New TesseractEngine("", "eng") With {
        .DefaultPageSegMode = PageSegMode.SingleLine
    }
    Private WF_Proc As Process = Nothing

    Private window As Rect = Nothing
    Private win_area As Rect = Nothing

    Private dpiScaling As Double = -1.0
    Private uiScaling As Double = -1.0
    Private center As Point = Nothing

    Dim rarity As New List(Of Color) From {Color.FromArgb(171, 159, 117), Color.FromArgb(175, 175, 175), Color.FromArgb(134, 98, 50)}

    <DllImport("user32.dll")>
    Private Function GetWindowRect(ByVal hWnd As HandleRef, ByRef lpRect As Rect) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function GetClientRect(ByVal hWnd As HandleRef, ByRef lpRect As Rect) As Boolean
    End Function

    <DllImport("gdi32.dll")>
    Public Function GetDeviceCaps(hdc As IntPtr, nIndex As Integer) As Integer
    End Function

    Public Enum DeviceCap
        VERTRES = 10
        DESKTOPVERTRES = 117
    End Enum

    Private Function GetWFProc() As Boolean
        For Each p As Process In Process.GetProcesses
            If p.ProcessName.Contains("Warframe") Then
                If WF_Proc Is Nothing OrElse p.Handle <> WF_Proc.Handle Then
                    WF_Proc = p
                    UpdateCenter()
                End If
                Return True
            End If
        Next
        Return Nothing
    End Function

    Public Function isWFActive() As Boolean
        If WF_Proc Is Nothing Then
            GetWFProc()
        ElseIf WF_Proc.HasExited Then
            WF_Proc = Nothing
        End If
        Return WF_Proc IsNot Nothing
    End Function

    Private Function GetScalingFactor() As Double
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

    Private Function GetUIScaling() As Double
        uiScaling = My.Settings.Scaling / 100
        '     All values are based on my PC 1680x1050
        uiScaling *= win_area.Width / 1680
        uiScaling *= dpiScaling
        Return uiScaling
    End Function

    Public Sub ForceUpdateCenter()
        ' May updated center twice
        '   It will occur if WF_Proc is nothing when isWFActive is called
        '   Given that isWFActive is called every minute
        '     This means that WF was only active for less than a minute, and therefore nothing will be happening
        '     So extra computation will not negatively affect user
        If isWFActive() Then
            UpdateCenter()
        End If
    End Sub

    Private Sub UpdateCenter()
        Dim hr As New HandleRef(WF_Proc, WF_Proc.MainWindowHandle)
        GetWindowRect(hr, window)
        GetClientRect(hr, win_area)
        If window.Width = 0 Or window.Height = 0 Or win_area.Width = 0 Or win_area.Height = 0 Then
            WF_Proc = Nothing
            window = Nothing
            win_area = Nothing
        Else
            ' Get DPI Scaling
            dpiScaling = GetScalingFactor()

            Dim padding As Integer = (window.Width - win_area.Width) / 2

            ' Start at left of window
            Dim horz_center As Integer = window.X1
            ' Padding from left and right are equal, so can just use window.Width to get to center
            ' Move to center
            horz_center += window.Width / 2

            ' Start at top of the window
            Dim vert_center As Integer = window.Y1
            ' Padding from top
            vert_center += window.Height - win_area.Height - padding
            ' move to center
            vert_center += win_area.Height / 2

            ' Get Center points
            center = New Point(dpiScaling * horz_center, dpiScaling * vert_center)

            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        End If
    End Sub

    Private Function DefaultParseText(bmp As Bitmap) As String
        Using bmp
            Using page As Page = engine.Process(bmp)
                Return page.GetText().Trim
            End Using
        End Using
    End Function

    Public Function IsRelicWindow() As Boolean
        If Not isWFActive() Then
            Return False
        End If

        ' UI Scaling is not in UpdateCenter because it will change more frequently than the rest
        '     Also UI Scaling is 3 computations (5 if you include writing) (who knows how many with reads)
        GetUIScaling()

        ' Vertically Centered (1680x1050 @ 100%)
        '    Width of 172
        Dim wid As Integer = (uiScaling * 172)
        Dim left As Integer = center.X - Math.Ceiling(wid / 2)

        ' Horizontally Offset (1680x1050 @ 100%)
        '    Displaced 42 below center
        '    Height of 22
        Dim hei As Integer = (uiScaling * 22)
        Dim top As Integer = center.Y + (uiScaling * 42)

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
                ' "white" lines must be <12px
                ' gaps must be <12px
                If gap > 14 * uiScaling Then
                    Return False
                End If
            Next
            ' PERCENTAGE NEEDS TO BE BETWEEN 50% and 70%
            If tot_white < bmp.Width * 0.5 OrElse tot_white > bmp.Width * 0.7 Then
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

    Private Function GetRelicWindow() As Bitmap
        ' NOT NEEDED
        '     Because this is a private function
        '     All public functions will confirm before processing

        'If Not isWFActive() Then
        '    Return False
        'End If

        GetUIScaling()

        ' Relic area is "centered" vertically and offset horizontally
        '   bot is up 13px relative to the center
        '   top is up 318px relative to the center
        Dim wid As Integer = 1516 * uiScaling + 2
        Dim hei As Integer = 305 * uiScaling + 2
        Dim top As Integer = 318 * uiScaling + 2

        Dim ss_area As New Rectangle(center.X - wid / 2, center.Y - top, wid, hei)
        If Debug Then
            Main.addLog("TAKING SCREENSHOT:" & vbCrLf & dpiScaling & vbCrLf & ss_area.ToString())
        End If
        Dim ret As New Bitmap(wid, hei)
        Using graph As Graphics = Graphics.FromImage(ret)
            graph.CopyFromScreen(ss_area.X, ss_area.Y, 0, 0, New Size(ss_area.Width, ss_area.Height), CopyPixelOperation.SourceCopy)
        End Using
        If Debug Then
            ret.Save(appData & "\WFInfo\tests\SS-" & My.Settings.SSCount.ToString() & ".png")
            My.Settings.SSCount += 1
        End If
        Return ret
    End Function

    Private Function GetPlayers(screen As Image) As Integer
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
                    If Debug Then
                        CropImage.SetPixel(i, j, Color.White)
                    End If
                Next
            Next
            If Debug Then
                CropImage.Save(appData & "\WFInfo\tests\Plyr-" & My.Settings.PlyrCount.ToString() & ".png")
                My.Settings.PlyrCount += 1
            End If
        End Using
        Return count
    End Function

    Private Function ColortoRarity(clr As Color, Optional diff As Integer = 50) As Integer
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

    Private Function GetPartText(screen As Bitmap, plyr_count As Integer, plyr As Integer, Optional multi As Boolean = False) As String
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

            If Debug Then
                bmp.Save(appData & "\WFInfo\tests\Text-" & My.Settings.EtcCount.ToString() & ".png")
                My.Settings.EtcCount += 1
            End If
            ' ONLY one page can be used at a time
            '   So we have to get the text and dispose of the page quickly
            Dim ret As String = ""
            Using page As Page = engine.Process(bmp)
                ret = Regex.Replace(page.GetText(), "[^A-Z& ]", "").Trim
            End Using

            ' IF A LONGER NAME IS ADDED
            '   THIS WILL NEED TO BE IMPROVED ON
            If ret.Length < 14 AndAlso LevDist(ret, "Blueprint") < 4 Then
                Return GetPartText(screen, plyr_count, plyr, True) & " " & ret
            End If
            Return ret
        End Using
    End Function

    ' THIS WILL BE IGNORING FULLSCREEN FOR NOW
    Private ParseScreen_timer As Long = 0
    Public Sub ParseScreen()
        If Not isWFActive() Then
            Return
        End If

        Dim screen As Bitmap
        Dim players As Integer = 0
        Dim foundText As New List(Of String)()
        ' Start timer
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        screen = GetRelicWindow()

        ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
        Console.WriteLine("SCREENSHOT-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        ' Get Player Count from Image
        players = GetPlayers(screen)

        Dim pad As Integer = screen.Height * 0.05
        Dim top As Integer = center.Y - 318 * uiScaling + pad
        Dim right As Integer = center.X - screen.Width / 2 - pad
        ' Adjust for <4 players
        right -= (players - 4) * screen.Width / 8
        For i = 0 To players - 1
            right += screen.Width / 4
            rwrdPanels(i).ShowLoading(right, top)
        Next

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
        Console.WriteLine("GET PART TEXT-" & ParseScreen_timer & "ms")
        ParseScreen_timer = clock.Elapsed.TotalMilliseconds

        If DisplayWindow Then
            RewardWindow.Display(foundText)
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY WINDOW-" & ParseScreen_timer & "ms")
        Else
            Dim plat As Double = 0
            Dim ducat As String = ""
            Dim vaulted As Boolean
            ' Move over if you don't have all 4
            For i = 0 To foundText.Count - 1

                If Debug Then
                    Main.addLog("DISPLAY OVERLAY " & (i + 1) & ":" & vbCrLf & "Right, Top: " & right & ", " & top)
                End If
                plat = db.market_data(foundText(i))("plat")
                ducat = db.market_data(foundText(i))("ducats").ToString()
                vaulted = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
                rwrdPanels(i).LoadText(plat.ToString("N1"), ducat, vaulted)
            Next
            ParseScreen_timer -= clock.Elapsed.TotalMilliseconds
            Console.WriteLine("DISPLAY OVERLAYS-" & ParseScreen_timer & "ms")
        End If

    End Sub

    '_____________________________________________________
    '
    '    BEGIN RELIC Refinement STUFF
    '_____________________________________________________

    Private scrollLoc As Integer = -100
    Private eraLoc As Integer = -1

    Private RefineORSelection As Boolean = False
    Public FoundRefineWin As Boolean = False
    Public RefineOverlayShown As Boolean = False

    Public Function CheckEraSelection() As Boolean
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

    Public Function CheckScrollBar() As Boolean
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

    Private Sub UpdateRefineOverlay()

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
        If Debug Then
            My.Settings.DebugCount += 1
        End If
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
                            bmp.Save(appData & "\WFInfo\tests\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")
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
                            bmp.Save(appData & "\WFInfo\tests\RR" & i & j & "1-" & My.Settings.DebugCount & ".png")
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
                            bmp.Save(appData & "\WFInfo\tests\RR" & i & j & "2-" & My.Settings.DebugCount & ".png")
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

    Public Sub ShowRelicOverlay()
        GetUIScaling()
        UpdateRefineOverlay()
    End Sub

    Public Function IsRefinementWindow() As Boolean
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

            Console.WriteLine(tot_white / bmp.Width)
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
End Module
