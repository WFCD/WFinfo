Imports System.Drawing.Drawing2D

Public Class Picker
    Dim PrevColor As Color
    Dim SliderPos As Integer
    Dim LastSelected As PictureBox
    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer
    Private Sub Picker_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateColors(Me)
        Me.MaximizeBox = False

        pbTitleBar.BackColor = My.Settings.cTitleBar
        pbBackground.BackColor = My.Settings.cBackground
        pbSideBar.BackColor = My.Settings.cSideBar
        pbText.BackColor = My.Settings.cText
        pbTray.BackColor = My.Settings.cTray

        SliderPos = pbSliderBG.Width / 2
        UpdateSlider()

        LastSelected = pbTitleBar
        pbPreview.BackColor = LastSelected.BackColor

        Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
        Me.Location = Settings.Location()
    End Sub


    Private Sub pbColorWheel_MouseDown(sender As Object, e As MouseEventArgs) Handles pbColorWheel.MouseDown
        tGetColor.Start()
    End Sub
    Private Sub pbSliderFG_MouseDown(sender As Object, e As EventArgs) Handles pbSliderFG.MouseDown
        tGetShade.Start()
    End Sub
    Private Sub tGetColor_Tick(sender As Object, e As EventArgs) Handles tGetColor.Tick
        Try
            Dim myBitmap As Bitmap
            myBitmap = CType(pbColorWheel.Image, Bitmap)
            PrevColor = myBitmap.GetPixel(pbColorWheel.PointToClient(Cursor.Position).X, pbColorWheel.PointToClient(Cursor.Position).Y)
            UpdateSlider()

        Catch ex As Exception
        End Try
        If Not (MouseButtons = System.Windows.Forms.MouseButtons.Left) Then
            tGetColor.Enabled = False
            tGetColor.Stop()
        End If
    End Sub

    Private Sub tGetShade_Tick(sender As Object, e As EventArgs) Handles tGetShade.Tick
        Try
            Dim darkBrush As New LinearGradientBrush(
            New PointF(0, 0),
            New PointF(pbSliderBG.Width / 2, 0),
            Color.Black, PrevColor)

            Dim lightBrush As New LinearGradientBrush(
            New PointF(0, 0),
            New PointF(pbSliderBG.Width / 2, 0),
            PrevColor, Color.White)

            Dim blackPen As New Pen(Color.FromArgb(255, 24, 24, 24), 2)
            Dim img1 = New Bitmap(pbSliderBG.Width, pbSliderBG.Height)
            Dim img2 = New Bitmap(pbSliderBG.Width, pbSliderBG.Height)
            Dim halfWidth As Integer = pbSliderBG.Width / 2
            Dim G1 As Graphics = Graphics.FromImage(img1)
            Dim G2 As Graphics = Graphics.FromImage(img2)
            G1.FillRectangle(darkBrush, 0, 0, halfWidth, pbSliderBG.Height)
            G2.FillRectangle(darkBrush, 0, 0, halfWidth, pbSliderBG.Height)
            G1.FillRectangle(lightBrush, halfWidth, 0, halfWidth, pbSliderBG.Height)
            G2.FillRectangle(lightBrush, halfWidth, 0, halfWidth, pbSliderBG.Height)
            G2.DrawLine(blackPen, New Point(SliderPos, -5), New Point(SliderPos, pbSliderBG.Height + 5))
            pbSliderBG.Image = img1
            pbSliderFG.Image = img2
            darkBrush.Dispose()
            lightBrush.Dispose()
            blackPen.Dispose()
            SliderPos = pbSliderBG.PointToClient(Cursor.Position).X
            pbSliderBG.Refresh()
            Dim myBitmap As Bitmap
            myBitmap = CType(pbSliderBG.Image, Bitmap)
            pbPreview.BackColor = myBitmap.GetPixel(SliderPos + 1, pbSliderBG.Height / 2)
        Catch ex As Exception
        End Try
        If Not (MouseButtons = System.Windows.Forms.MouseButtons.Left) Then
            tGetShade.Enabled = False
            tGetShade.Stop()
        End If
    End Sub

    Private Sub pbColorWheel_Paint(sender As Object, e As PaintEventArgs) Handles pbColorWheel.Paint
        Dim gp As New GraphicsPath
        'e.Graphics.DrawEllipse(Pens.White, New Rectangle(3, 3, pbColorWheel.Width - 8, pbColorWheel.Height - 8))
        gp.AddEllipse(New Rectangle(3, 3, pbColorWheel.Width - 8, pbColorWheel.Height - 8))
        pbColorWheel.Region = New Region(gp)
    End Sub

    Private Sub UpdateSlider()
        Try
            Dim darkBrush As New LinearGradientBrush(
            New PointF(0, 0),
            New PointF(pbSliderBG.Width / 2, 0),
            Color.Black, PrevColor)

            Dim lightBrush As New LinearGradientBrush(
            New PointF(0, 0),
            New PointF(pbSliderBG.Width / 2, 0),
            PrevColor, Color.White)

            Dim blackPen As New Pen(Color.FromArgb(255, 24, 24, 24), 2)
            Dim img1 = New Bitmap(pbSliderBG.Width, pbSliderBG.Height)
            Dim img2 = New Bitmap(pbSliderBG.Width, pbSliderBG.Height)
            Dim halfWidth As Integer = pbSliderBG.Width / 2
            Dim G1 As Graphics = Graphics.FromImage(img1)
            Dim G2 As Graphics = Graphics.FromImage(img2)
            G1.FillRectangle(darkBrush, 0, 0, halfWidth, pbSliderBG.Height)
            G2.FillRectangle(darkBrush, 0, 0, halfWidth, pbSliderBG.Height)
            G1.FillRectangle(lightBrush, halfWidth, 0, halfWidth, pbSliderBG.Height)
            G2.FillRectangle(lightBrush, halfWidth, 0, halfWidth, pbSliderBG.Height)
            G2.DrawLine(blackPen, New Point(SliderPos, -5), New Point(SliderPos, pbSliderBG.Height + 5))
            pbSliderBG.Image = img1
            pbSliderFG.Image = img2
            darkBrush.Dispose()
            lightBrush.Dispose()
            blackPen.Dispose()
            pbSliderBG.Refresh()
            Dim myBitmap As Bitmap
            myBitmap = CType(pbSliderBG.Image, Bitmap)
            pbPreview.BackColor = myBitmap.GetPixel(SliderPos + 1, pbSliderBG.Height / 2)
            pbPreview.Refresh()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub pbClick(sender As Object, e As EventArgs) Handles pbTitleBar.Click, pbBackground.Click, pbSideBar.Click, pbText.Click, pbTray.Click
        LastSelected.BorderStyle = BorderStyle.FixedSingle
        LastSelected = sender
        LastSelected.BorderStyle = BorderStyle.Fixed3D
        pbPreview.BackColor = LastSelected.BackColor
        PrevColor = LastSelected.BackColor
        SliderPos = pbSliderBG.Width / 2
        UpdateSlider()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        LastSelected.BackColor = pbPreview.BackColor
        'Me.BackColor = My.Settings.BG
        Panel1.BackColor = pbBackground.BackColor
        'Panel2.BackColor = pbFG.BackColor
        'tbRefresh.BackColor = pbHeader.BackColor
        For Each lb As Control In Panel1.Controls
            If TypeName(lb) = "Label" Then
                lb.ForeColor = pbTray.BackColor
            End If
        Next
        For Each btn As Control In Me.Controls
            If TypeName(btn) = "Button" Then
                btn.ForeColor = pbTray.BackColor
                btn.BackColor = pbSideBar.BackColor
            End If
        Next
        'Label11.ForeColor = pbText.BackColor
        'Main.UpdateColors()
    End Sub


    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        My.Settings.cTitleBar = pbTitleBar.BackColor
        My.Settings.cBackground = pbBackground.BackColor
        My.Settings.cSideBar = pbSideBar.BackColor
        My.Settings.cText = pbText.BackColor
        My.Settings.cTray = pbTray.BackColor
        My.Settings.Save()
        UpdateColors(Main)
        UpdateColors(Settings)
        Me.Close()
    End Sub

    Private Sub pbSliderFG_MouseDown(sender As Object, e As MouseEventArgs) Handles pbSliderFG.MouseDown

    End Sub

    Private Sub btnDefault_Click_1(sender As Object, e As EventArgs) Handles btnDefault.Click
        pbTitleBar.BackColor = Color.FromArgb(15, 15, 15)
        pbBackground.BackColor = Color.FromArgb(27, 27, 27)
        pbSideBar.BackColor = Color.FromArgb(23, 23, 23)
        pbText.BackColor = Color.FromArgb(177, 208, 217)
        pbTray.BackColor = Color.FromArgb(1, 1, 1)
        pbPreview.BackColor = LastSelected.BackColor
        btnSet.PerformClick()
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

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub
    Private Sub pb_Click(sender As Object, e As EventArgs) Handles pbTitleBar.Click, pbBackground.Click, pbSideBar.Click, pbText.Click, pbTray.Click
        LastSelected.BorderStyle = BorderStyle.FixedSingle
        LastSelected = sender
        LastSelected.BorderStyle = BorderStyle.Fixed3D
        pbPreview.BackColor = LastSelected.BackColor
        PrevColor = LastSelected.BackColor
        SliderPos = pbSliderBG.Width / 2
        UpdateSlider()
    End Sub

    Private Sub btnSet_Click(sender As Object, e As EventArgs) Handles btnSet.Click
        LastSelected.BackColor = pbPreview.BackColor
        For Each c As Control In Me.Controls
            If c.Name = "pTitle" Then
                c.BackColor = pbTitleBar.BackColor
                For Each c2 As Control In c.Controls
                    If TypeOf c2 Is Label Then c2.ForeColor = pbText.BackColor
                    If TypeOf c2 Is Button Then c2.BackColor = pbTitleBar.BackColor
                    If TypeOf c2 Is Button Then c2.ForeColor = pbText.BackColor
                    If c2.Name = "lbStatus" Then c2.ForeColor = Color.Lime
                Next
            Else
                If TypeOf c Is Label Then c.ForeColor = pbText.BackColor
                If TypeOf c Is Panel Then c.BackColor = pbBackground.BackColor
                If TypeOf c Is Label Then c.ForeColor = pbText.BackColor
                If TypeOf c Is Button Then c.ForeColor = pbText.BackColor
                If TypeOf c Is Button Then c.BackColor = pbTitleBar.BackColor
                For Each c2 As Control In c.Controls
                    If TypeOf c2 Is Panel Then c2.BackColor = pbBackground.BackColor
                    If TypeOf c2 Is Label Then c2.ForeColor = pbText.BackColor
                    If TypeOf c2 Is Button Then c2.ForeColor = pbText.BackColor
                    If TypeOf c2 Is Button Then c2.BackColor = pbTitleBar.BackColor
                    For Each c3 As Control In c2.Controls
                        If TypeOf c3 Is Panel Then c3.BackColor = pbBackground.BackColor
                        If TypeOf c3 Is Label Then c3.ForeColor = pbText.BackColor
                        If TypeOf c3 Is Button Then c3.ForeColor = pbText.BackColor
                        If TypeOf c3 Is Button Then c3.BackColor = pbTitleBar.BackColor
                        For Each c4 As Control In c3.Controls
                            If TypeOf c4 Is Panel Then c4.BackColor = pbBackground.BackColor
                            If TypeOf c4 Is Label Then c4.ForeColor = pbText.BackColor
                            If TypeOf c4 Is Button Then c4.ForeColor = pbText.BackColor
                            If TypeOf c4 Is Button Then c4.BackColor = pbTitleBar.BackColor
                        Next
                    Next
                Next
            End If
        Next
        Me.Refresh()
    End Sub
End Class