
Public Class Overlay

    Private vault_img As Image
    Private unvault_img As Image
    Private loading_img As Image

    Public Sub New()
        InitializeComponent()
        'Me.CreateControl()
        loading_img = Tint(PictureBox1.Image, bgColor, 0.25)
        vault_img = Tint(My.Resources.Panel_V, bgColor, 0.25)
        unvault_img = Tint(My.Resources.Panel_UV, bgColor, 0.25)
        PictureBox1.Image = loading_img

        Me.BackColor = Color.Black
        Me.TopMost = True

        Dim fontSize As Integer = 18
        Dim allFont As New Font(lbPDropShadow.Font.FontFamily, fontSize, FontStyle.Bold)

        lbPDropShadow.Location = New Point(35, 3)
        lbPDropShadow.Font = allFont
        lbPDropShadow.Parent = PictureBox1

        lbPlat.Location = New Point(-2, 0)
        lbPlat.Font = allFont
        lbPlat.Parent = lbPDropShadow

        lbDDropShadow.Location = New Point(35, 36)
        lbDDropShadow.Font = allFont
        lbDDropShadow.Parent = PictureBox1

        lbDucats.Location = New Point(-1, 0)
        lbDucats.Font = allFont
        lbDucats.Parent = lbDDropShadow
    End Sub

    Public Sub LoadText(plat As String, ducat As String, Optional vaulted As Boolean = False)

        If vaulted Then
            PictureBox1.Image = vault_img
        Else
            PictureBox1.Image = unvault_img
        End If

        lbPlat.Text = plat
        lbPDropShadow.Text = lbPlat.Text

        lbDucats.Text = ducat
        lbDDropShadow.Text = lbDucats.Text

        If Not My.Settings.Automate Then
            tHide.Start()
        End If
    End Sub

    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        Me.Hide()
        tHide.Stop()
    End Sub

    Public Sub ShowLoading(right As Integer, top As Integer)
        lbPlat.Text = ""
        lbPDropShadow.Text = ""
        lbDucats.Text = ""
        lbDDropShadow.Text = ""
        PictureBox1.Image = loading_img
        Me.Show()
        Me.Size = New Size(125, 70)
        Me.Location = New Point(right - Me.Size.Width, top)
        Me.Refresh()
    End Sub
End Class
