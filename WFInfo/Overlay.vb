Imports System.Runtime.InteropServices
Imports System.Drawing.Text
Imports System.Text

Public Class Overlay
    Dim loc As Point

    Public Sub Position(x As Integer, y As Integer)
        UpdateColors(Me)
        loc = New Point(x, y)
        Me.Size = New Point(125, 70)
        PictureBox1.Image = Tint(PictureBox1.Image, My.Settings.cTray, 0.25)
        Me.BackColor = Color.Black
        Me.TopMost = True

        Dim fontSize As Integer = 0.26 * Me.Size.Height
        Dim allFont As New Font(lbPDropShadow.Font.FontFamily, fontSize, FontStyle.Bold)

        lbPDropShadow.Location = New Point((Me.Size.Width / 2.58) + 2, (Me.Size.Height / 27))
        lbPDropShadow.Font = allFont
        lbPDropShadow.Parent = PictureBox1

        lbPlat.Location = New Point(-2, 0)
        lbPlat.Font = allFont
        lbPlat.Parent = lbPDropShadow

        lbDDropShadow.Location = New Point((Me.Size.Width / 2.58) + 2, (Me.Size.Height / 2.15) + (Me.Size.Height / 27))
        lbDDropShadow.Font = allFont
        lbDDropShadow.Parent = PictureBox1

        lbDucats.Location = New Point(-1, 0)
        lbDucats.Font = allFont
        lbDucats.Parent = lbDDropShadow
    End Sub

    Public Sub LoadText(plat As String, ducat As Integer, Optional vaulted As Boolean = False)
        If vaulted Then
            PictureBox1.Image = My.Resources.Panel_V
        Else
            PictureBox1.Image = My.Resources.Panel_UV
        End If
        PictureBox1.Image = Tint(PictureBox1.Image, My.Settings.cTray, 0.25)

        lbPlat.Text = plat
        lbPDropShadow.Text = lbPlat.Text

        lbDucats.Text = ducat.ToString()
        lbDDropShadow.Text = lbDucats.Text
    End Sub

    Public Sub ShowOverlay()
        Me.Show()
        Me.Location = loc
        tHide.Start()
    End Sub

    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        Me.Hide()
        tHide.Stop()
    End Sub
End Class
