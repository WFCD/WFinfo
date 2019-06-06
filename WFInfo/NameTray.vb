Public Class NameTray

    Public Sub New()
        InitializeComponent()
        'Me.CreateControl()

        Me.BackColor = Color.Black
        Me.TopMost = True

        Dim fontSize As Integer = 12
        Dim allFont As New Font(lbPDropShadow.Font.FontFamily, fontSize, FontStyle.Bold)

        lbPDropShadow.Location = New Point(1, 1)
        lbPDropShadow.Font = allFont
        lbPDropShadow.Parent = PictureBox1

        lbPlat.Location = New Point(-1, -1)
        lbPlat.Font = allFont
        lbPlat.Parent = lbPDropShadow
    End Sub


    Public Sub LoadText(name As String)

        lbPlat.Text = name
        lbPDropShadow.Text = lbPlat.Text

        If Not My.Settings.Automate Then
            tHide.Start()
        End If
    End Sub

    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        Me.Hide()
        tHide.Stop()
    End Sub

    Public Sub ShowLoading(right As Integer, top As Integer, width As Integer)
        lbPlat.Text = ""
        lbPDropShadow.Text = ""
        Me.Show()
        Me.Size = New Size(width, width / 4)
        lbPlat.Size = Me.Size
        lbPDropShadow.Size = Me.Size
        Me.Location = New Point(right - Me.Size.Width, top)
        Me.Refresh()
    End Sub
End Class