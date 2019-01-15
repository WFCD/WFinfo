Imports Newtonsoft.Json.Linq
Imports System.Text.RegularExpressions
Imports Tesseract

Module OCR
    Private engine As New TesseractEngine("", "eng") With {
        .DefaultPageSegMode = Tesseract.PageSegMode.SingleLine
    }

    Dim img_count As Integer = 1
    Dim rarity As New List(Of Color) From {Color.FromArgb(171, 159, 117), Color.FromArgb(175, 175, 175), Color.FromArgb(134, 98, 50)}


    Public Sub ParseScreen()
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
        Console.WriteLine(players)
        Console.WriteLine("PLAYER COUNT: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        ' Get Part Text from Image
        ' Only retrieve capitals
        engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        For i = 0 To players - 1
            ' Crops out each players relic window
            ' Finds text in those images, then adds to list
            Dim text As String = GetPartText(Crop(screen, players, i))
            foundText.Add(text)
            Console.WriteLine(text)
        Next
        Console.WriteLine("GET OCR TEXT: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        foundText = GetPartNames(foundText, screen, players)

        Console.WriteLine("GET PART NAMES: " + (clock.Elapsed.Ticks - prev_time).ToString())
        prev_time = clock.Elapsed.Ticks

        Dim plat As String = ""
        Dim ducat As String = ""
        Dim vaulted As Boolean
        Dim y As Integer = My.Settings.StartPoint.Y + (My.Settings.StartPoint.Y * 0.05)
        Dim x As Integer
        For i = 0 To foundText.Count - 1
            plat = db.market_data(foundText(i))("plat").ToString()
            ducat = db.market_data(foundText(i))("ducats").ToString()
            vaulted = foundText(i).Equals("Forma Blueprint") OrElse db.IsPartVaulted(foundText(i))
            Console.WriteLine(foundText(i) & ": " & plat & ", " & ducat & ", " & vaulted)
            x = My.Settings.StartPoint.X + (screen.Width * (i / 4 + (5 - players) / 8))
            Dim panel As New Overlay
            panel.Display(x, y, plat, ducat, vaulted)
        Next
        Console.WriteLine("DISPLAY OVERLAYS: " + (clock.Elapsed.Ticks - prev_time).ToString())
        clock.Stop()
        Console.WriteLine("Total: " + clock.Elapsed.Ticks.ToString())
    End Sub

    Private Function GetPartNames(foundText As List(Of String), screen As Bitmap, plyr_count As Integer) As List(Of String)
        Dim finalList As New List(Of String)()
        For i = 0 To foundText.Count - 1

            'Blueprint means that it is a multi-line part name
            Console.WriteLine(foundText(i) + ": " + LevDist(foundText(i), "Blueprint").ToString())
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

    Private Function GetDebugShot() As Bitmap
        Return New System.Drawing.Bitmap("C:\Users\Kekasi\AppData\Roaming\WFInfo\tests\Original Test.jpg")
    End Function

    Private Function GetScreenShot() As Bitmap
        Dim ret As Bitmap
        If Fullscreen Then
            ret = New System.Drawing.Bitmap(My.Settings.LastFile)
        Else
            ret = New System.Drawing.Bitmap(My.Settings.RecSize.X, My.Settings.RecSize.Y, System.Drawing.Imaging.PixelFormat.Format32bppRgb)
            ret.Save(appData & "\WFInfo\tests\Screen_Shot.jpg")
            Dim graph As Graphics = Graphics.FromImage(ret)
            graph.CopyFromScreen(My.Settings.StartPoint.X, My.Settings.StartPoint.Y, 0, 0, My.Settings.RecSize, CopyPixelOperation.SourceCopy)
        End If
        Return ret
    End Function

    Private Sub Prev_Code()


        ''_________________________________________________________________________
        ''Retrieves the platinum and ducat prices using warframe.market 
        ''And the ducat list we pulled from the wiki when the application launched
        ''_________________________________________________________________________
        ''
        ''Stores them in p() and d()
        ''
        ''This also stores the text to display in the tray (if used) in qItems
        ''_________________________________________________________________________
        'Dim HighestPlat As Integer = 0
        'Dim p As New List(Of String)()
        'Dim d As New List(Of String)()
        'Dim v As New List(Of Boolean)()
        'Dim n As New List(Of String)()
        'For i = 0 To finalList.Count - 1
        '    Dim guess As String = finalList(i)
        '    If Not finalList(i) = "Forma Blueprint" Then
        '        Dim plat As String = ""
        '        Dim ducat As String = ""
        '        Dim job As JObject = Nothing
        '        If db.market_data.TryGetValue(guess, job) Then
        '            plat = job("plat").ToString()
        '            p.Add(plat)
        '            ducat = job("ducats").ToString()
        '            d.Add(ducat)
        '        Else
        '            Dim plat_int As Integer = db.GetPlat(KClean(guess))
        '            plat = plat_int
        '            If plat_int > HighestPlat Then
        '                HighestPlat = plat_int
        '            End If
        '            If plat_int < 0 Then
        '                plat = "X"
        '            End If
        '            p.Add(plat)

        '            If plat = "X" OrElse plat = 0 Then
        '                ducat = plat
        '            Else
        '                ducat = db.market_data(check(guess))("ducats").ToString()
        '            End If
        '            d.Add(ducat)
        '        End If
        '        guess = KClean(guess)

        '        If guess.Length > 27 Then
        '            n.Add(guess.Substring(0, 27) & "...")
        '        Else
        '            n.Add(guess)
        '        End If

        '        ' TODO: Add in "vaulted" to ducat_plat database
        '        '       And probably should rename it to prime_parts or something
        '        '       Then have check like this:
        '        ' v.Add(job("vaulted").ToObject(Of Boolean))
        '        If db.market_data.TryGetValue(guess, Nothing) Then
        '            v.Add(db.market_data(guess)("vaulted").ToObject(Of Boolean))
        '        Else
        '            v.Add(False)
        '        End If

        '        If guess.Length > 27 Then
        '            qItems.Add(guess.Substring(0, 27) & "..." & vbNewLine & "    Ducks: " & ducat & "   Plat: " & plat & vbNewLine)
        '        Else
        '            qItems.Add(guess & vbNewLine & "    Ducks: " & ducat & "   Plat: " & plat & vbNewLine)
        '        End If
        '    Else
        '        n.Add(vbNewLine & KClean(guess))
        '        p.Add(0)
        '        d.Add(0)
        '        v.Add(False)
        '    End If
        'Next

        'Console.WriteLine("GET PLAT/DUCAT: " + (clock.Elapsed.Ticks - prev_time).ToString())
        'prev_time = clock.Elapsed.Ticks

        ''_________________________________________________________________________
        ''Displays the information using either newstyle(overlay) or old(tray)
        ''_________________________________________________________________________
        'If Not Fullscreen And Not NewStyle Then
        '    Tray.Clear()
        '    Tray.Display()
        'Else
        '    Tray.Clear()
        '    qItems.Clear()

        '    'Each part has it's own panel/overlay
        '    Dim panel1 As New Overlay
        '    Dim panel2 As New Overlay
        '    Dim panel3 As New Overlay
        '    Dim panel4 As New Overlay
        '    For i = 0 To players - 1
        '        Dim width As Integer = (CliptoImage.Width / 4)
        '        Dim y As Integer = My.Settings.StartPoint.Y + (My.Settings.StartPoint.Y * 0.05)
        '        Select Case players
        '            Case 4
        '                Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 0.25))
        '                Select Case i
        '                    Case 0
        '                        panel1.Display(x, y, p(i), d(i), v(i))
        '                    Case 1
        '                        panel2.Display(x, y, p(i), d(i), v(i))
        '                    Case 2
        '                        panel3.Display(x, y, p(i), d(i), v(i))
        '                    Case 3
        '                        panel4.Display(x, y, p(i), d(i), v(i))
        '                End Select
        '            Case 3
        '                Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 0.75))
        '                Select Case i
        '                    Case 0
        '                        panel1.Display(x, y, p(i), d(i), v(i))
        '                    Case 1
        '                        panel2.Display(x, y, p(i), d(i), v(i))
        '                    Case 2
        '                        panel3.Display(x, y, p(i), d(i), v(i))
        '                    Case 3
        '                        panel4.Display(x, y, p(i), d(i), v(i))
        '                End Select
        '            Case 2
        '                Dim x As Integer = ((CliptoImage.Width / 4) * 0.8) + (width * (i + 1.25))
        '                Select Case i
        '                    Case 0
        '                        panel1.Display(x, y, p(i), d(i), v(i))
        '                    Case 1
        '                        panel2.Display(x, y, p(i), d(i), v(i))
        '                    Case 2
        '                        panel3.Display(x, y, p(i), d(i), v(i))
        '                    Case 3
        '                        panel4.Display(x, y, p(i), d(i), v(i))
        '                End Select
        '        End Select
        '    Next

        '    If DisplayNames Then
        '        'Each plaque has it's own panel/overlay
        '        Dim plaque1 As New NamePlaque
        '        Dim plaque2 As New NamePlaque
        '        Dim plaque3 As New NamePlaque
        '        Dim plaque4 As New NamePlaque
        '        For i = 0 To players - 1
        '            Dim width As Integer = (CliptoImage.Width / 4)
        '            Dim y As Integer = My.Settings.StartPoint.Y + (My.Settings.RecSize.Y * 0.93)
        '            Dim w As Integer = (((CliptoImage.Width / 4) * 0.8) - My.Settings.StartPoint.X) + 125
        '            Dim x As Integer = 0
        '            Select Case players
        '                Case 4
        '                    x = (width * i) + (width * 0.25) + (i * width * 0.005)
        '                Case 3
        '                    x = (width * i) + (width * 0.75) + (i * width * 0.005)
        '                Case 2
        '                    x = (width * i) + (width * 1.25) + (i * width * 0.005)
        '            End Select
        '            Select Case i
        '                Case 0
        '                    plaque1.Display(x, y, w, n(i))
        '                Case 1
        '                    plaque2.Display(x, y, w, n(i))
        '                Case 2
        '                    plaque3.Display(x, y, w, n(i))
        '                Case 3
        '                    plaque4.Display(x, y, w, n(i))
        '            End Select
        '        Next
        '    End If
        'End If

        'Console.WriteLine("DISPLAY OVERLAYS: " + (clock.Elapsed.Ticks - prev_time).ToString())
        'prev_time = clock.Elapsed.Ticks

        ''_________________________________________________________________________
        ''Readies the program for the next run and updates the session information
        ''_________________________________________________________________________
        'count += 1
        'Sess += 1
        'PPM += HighestPlat
        'lbStatus.ForeColor = Color.Lime
        'lbChecks.Text = "Checks this Session:              " & Sess
        'lbPPM.Text = "Platinum this Session:          " & PPM
        'clock.Stop()
        'Console.WriteLine("Total: " + clock.Elapsed.Ticks.ToString())
        ''tPPrice.Start()
    End Sub

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

        bmp.Save(appData & "\WFInfo\tests\FunTest_" & img_count & ".jpg")
        img_count += 1

        Dim result As String = ""
        Using bmp
            Using page As Page = engine.Process(bmp)
                result = Regex.Replace(page.GetText(), "[^A-Z&]", "")
            End Using
        End Using
        Return result
    End Function

    Private Function GetText(bmp As Bitmap) As String
        '_________________________________________________________________________
        'Retrieves the text information (location, type, etc) with OCR of an image
        '_________________________________________________________________________


        Dim result As String = ""
        Using bmp
            bmp = prepare(bmp)
            engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-.")
            Using page As Page = engine.Process(bmp)
                result = page.GetHOCRText(1)
            End Using
        End Using
        Return result
    End Function

    Private Function prepare(img As Image) As Image
        Using img
            Dim bmp As New Bitmap(img)
            Dim clr As Color = Nothing
            For i As Integer = 0 To bmp.Width - 1
                For j As Integer = 0 To bmp.Height - 1
                    clr = bmp.GetPixel(i, j)
                    If clr.R < 100 AndAlso clr.G < 100 AndAlso clr.B < 100 Then
                        clr = Color.White
                    Else
                        clr = Color.Black
                    End If
                    bmp.SetPixel(i, j, clr)
                Next
            Next
            Return bmp
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

    Private Function RemoveNoise(ByVal bmap As Bitmap) As Bitmap
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

    Private Function GetPlayers(screen As Image) As Integer
        '_________________________________________________________________________
        'Gets the number of seperate strings(players) in an image
        '_________________________________________________________________________
        Dim img As Bitmap = Crop(screen)

        Dim count As Integer = 1
        Using img

            Dim temp As String = GetText(img)
            Console.WriteLine(temp)

            Dim span_start As Integer = temp.IndexOf("<span class='ocrx_word'")
            Dim span_end As Integer = temp.IndexOf("</span>", span_start)
            Dim prevDist As Integer = Integer.Parse(temp.Substring(temp.IndexOf("bbox", span_start) + 4, 4))
            span_start = temp.IndexOf("<span class='ocrx_word'", span_end)
            While span_start <> -1
                span_end = temp.IndexOf("</span>", span_start)
                Dim tempDist As Integer = Integer.Parse(temp.Substring(temp.IndexOf("bbox", span_start) + 4, 4))
                Dim text_end As Integer = temp.IndexOf("</", span_start)
                If Not temp.Chars(text_end - 1).Equals(">"c) AndAlso Not temp.Chars(text_end - 2).Equals(">"c) AndAlso Not temp.Chars(text_end - 3).Equals(">"c) Then
                    count += 1
                    prevDist = tempDist
                    If count = 4 Then
                        Return 4
                    End If
                End If
                span_start = temp.IndexOf("<span class='ocrx_word'", span_end)
            End While
        End Using
        Return count
    End Function

    Private Function ResizeImage(ByVal img As Image, multi As Double) As Image
        '_________________________________________________________________________
        'Used to improve OCR accuracy by blowing the image up
        '_________________________________________________________________________
        Return New Bitmap(img, New Size(img.Width * multi, img.Height * multi))
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
        'If Debug Then
        '    img.Save(appData & "\WFInfo\tests\" & "Original Test" & ".jpg", Imaging.ImageFormat.Jpeg)
        'End If
        Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
        Using grp = Graphics.FromImage(CropImage)
            grp.DrawImage(OriginalImage, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
        End Using
        Return CropImage
    End Function
End Module
