Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports Tesseract

Module OCR
    Private engine As New TesseractEngine("", "eng") With {
        .DefaultPageSegMode = Tesseract.PageSegMode.SingleLine
    }

    Private window As Rect = Nothing
    Private win_area As Rect = Nothing
    Private relic_area As Rect = Nothing
    Private relic_pt As Point = Nothing
    Dim img_count As Integer = 1
    Dim rarity As New List(Of Color) From {Color.FromArgb(171, 159, 117), Color.FromArgb(175, 175, 175), Color.FromArgb(134, 98, 50)}

    <DllImport("user32.dll")>
    Private Function GetWindowRect(ByVal hWnd As HandleRef, ByRef lpRect As Rect) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function GetClientRect(ByVal hWnd As HandleRef, ByRef lpRect As Rect) As Boolean
    End Function

    Public Function GetCoors() As Boolean
        If window <> Nothing Then
            Return UpdateCoors()
        End If
        Dim wf As Process = Nothing
        For Each p As Process In System.Diagnostics.Process.GetProcesses
            If p.ProcessName.Contains("Warframe") Then
                wf = p
                Exit For
            End If
        Next
        If wf Is Nothing Then
            Return False
        End If

        Dim hr As New HandleRef(wf, wf.MainWindowHandle)
        GetWindowRect(hr, window)
        GetClientRect(hr, win_area)


        Return UpdateCoors()
    End Function

    Public Function UpdateCoors() As Boolean
        If window = Nothing Then
            Return GetCoors()
        End If
        ' Works for ONLY 1680/1050
        Dim scale As Double = My.Settings.Scaling
        scale /= 100
        scale *= win_area.Width
        scale /= 1680


        'FROM (0,0)
        Dim top As Integer = (win_area.Height / 2) - (314 * scale)
        Dim left As Integer = (win_area.Width / 2) - (749 * scale)
        relic_pt = New Point(left, top)

        'Adjust to actual "top-left"
        left += window.X1
        top += window.Y1
        If (win_area.Width <> window.Width Or win_area.Height <> window.Height) Then
            Dim padding As Integer = (window.Width - win_area.Width) / 2
            left += padding
            top += window.Height - win_area.Height - padding
        End If

        relic_area = New Rect(left - 1, top - 1, 1500 * scale + 2, 300 * scale + 2)
        Return True
    End Function

    Public Sub ParseScreen()
        If Not Fullscreen AndAlso window = Nothing AndAlso Not GetCoors() Then
            Return
        End If
        Dim screen As Bitmap
        Dim players As Integer = 0
        Dim foundText As New List(Of String)()

        ' Start timer
        prev_time = 0
        clock.Restart()

        ' Get Relic Image
        screen = GetScreenShot()
        'screen = GetDebugShot()
        Console.WriteLine("SCREENSHOT: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        ' Get Player Count from Image
        players = GetPlayers(screen)
        Console.WriteLine("PLAYER COUNT(" + players.ToString() + "): " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        ' Get Part Text from Image
        ' Only retrieve capitals
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        For i = 0 To players - 1
            ' Crops out each players relic window
            ' Finds text in those images, then adds to list
            Dim text As String = GetPartText(Crop(screen, players, i))
            foundText.Add(text)
            Console.WriteLine("FOUND PART: " + text)
        Next
        Console.WriteLine("GET OCR TEXT: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        foundText = GetPartNames(foundText, screen, players)

        Console.WriteLine("GET PART NAMES: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        Dim plat As String = ""
        Dim ducat As String = ""
        Dim vaulted As Boolean
        Dim y As Integer = relic_area.Y1 * 1.05
        Dim x As Integer
        For i = 0 To foundText.Count - 1
            x = relic_area.X1 + (screen.Width * (i / 4 + (5 - players) / 8))
            plat = db.market_data(foundText(i))("plat").ToString()
            ducat = db.market_data(foundText(i))("ducats").ToString()
            vaulted = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
            db.panels(i).Position(x, y)
            db.panels(i).LoadText(plat, ducat, vaulted)
            db.panels(i).ShowOverlay()
        Next
        Console.WriteLine("DISPLAY OVERLAYS: " + (clock.Elapsed.Ticks - prev_time).ToString())
        clock.Stop()
        Console.WriteLine("Total: " + clock.Elapsed.Ticks.ToString())
    End Sub

    Private Function GetPartNames(foundText As List(Of String), screen As Bitmap, plyr_count As Integer) As List(Of String)
        Dim finalList As New List(Of String)()
        For i = 0 To foundText.Count - 1

            'Blueprint means that it is a multi-line part name
            If LevDist(foundText(i), "Blueprint") < 4 Then

                ' BY SCHWAXX
                '  Since it's multi-line you need to use mode 1 of the crop function 
                '  This gets one line higher than the usual
                Using img As Image = Crop(screen, 1, i, plyr_count)
                    finalList.Add(db.GetPartName(GetPartText(img) + " Blueprint"))
                End Using
            Else
                finalList.Add(db.GetPartName(foundText(i)))
            End If
        Next
        Return finalList
    End Function

    Private Function GetScreenShot() As Bitmap
        Dim ret As Bitmap
        If Fullscreen Then
            ret = New System.Drawing.Bitmap(My.Settings.LastFile)
            Dim CropRect As New Rectangle(relic_pt.X, relic_pt.Y, relic_area.Width, relic_area.Height)
            Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
            Using grp = Graphics.FromImage(CropImage)
                grp.DrawImage(ret, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            End Using
            ret = CropImage
        Else
            ret = New System.Drawing.Bitmap(relic_area.Width, relic_area.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb)
            Dim graph As Graphics = Graphics.FromImage(ret)
            graph.CopyFromScreen(relic_area.X1, relic_area.Y1, 0, 0, New Size(relic_area.Width, relic_area.Height), CopyPixelOperation.SourceCopy)
        End If
        If Debug Then
            ret.Save(appData & "\WFInfo\tests\SS-" + My.Settings.SSCount + ".jpg")
            My.Settings.SSCount += 1
        End If
        Return ret
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

    Private Function GetPartText(bmp As Bitmap) As String
        '_________________________________________________________________________
        'Retrives the text from a cropped image
        '_________________________________________________________________________

        ' Change image to black + white
        ' Mainly for contrast
        Dim clr As Color = Nothing
        For i As Integer = 0 To bmp.Width - 1
            For j As Integer = 0 To bmp.Height - 1
                clr = bmp.GetPixel(i, j)
                Select Case (ColortoRarity(clr))
                    Case 3, 2, 1
                        bmp.SetPixel(i, j, Color.Black)
                    Case 0
                        bmp.SetPixel(i, j, Color.White)
                End Select
            Next
        Next

        Dim result As String = ""
        Using bmp
            If Debug Then
                bmp.Save(appData & "\WFInfo\tests\Text-" + My.Settings.EtcCount + ".jpg")
                My.Settings.EtcCount += 1
            End If
            Using page As Page = engine.Process(bmp)
                result = Regex.Replace(page.GetText(), "[^A-Z&]", "")
            End Using
        End Using
        Return result
    End Function

    Private Function GetPlayers(screen As Image) As Integer
        Dim count As Integer = 0
        Using bmp As Bitmap = Crop(screen)

            ' ITERATING FROM LEFT TO RIGHT
            '   LOOKING FOR bright spots
            '   WHEN ONE IS FOUND, MARK IT, THEN MOVE RIGHT
            Dim clr As Color = Nothing
            Dim last As Integer = -200
            For i As Integer = 0 To bmp.Width - 1
                For j As Integer = bmp.Height / 2 To (bmp.Height / 2 + 4)
                    clr = bmp.GetPixel(i, j)
                    If clr.R > 80 OrElse clr.G > 80 OrElse clr.B > 80 Then
                        If last + 160 < i Then
                            count += 1
                        End If
                        last = i
                        Exit For
                    End If
                    bmp.SetPixel(i, j, Color.White)
                Next
            Next
        End Using
        Return count
    End Function

    Private Function Crop(img As Image, Optional mode As Integer = 0, Optional pos As Integer = 1, Optional players As Integer = 0) As Bitmap
        '_________________________________________________________________________
        'Function used to crop the part names and usernames for player count
        '_________________________________________________________________________
        Dim startX As Integer
        Dim startY As Integer
        Dim height As Integer = img.Height * 0.1    ' Line Height
        Dim width As Integer = (img.Width / 4)      ' Window Width
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
        Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
        Using grp = Graphics.FromImage(CropImage)
            grp.DrawImage(OriginalImage, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
        End Using
        Return CropImage
    End Function
End Module
