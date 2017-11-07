Imports System.ComponentModel

Public Class UpdateWindow
    Dim drag As Boolean
    Dim mouseX As Integer
    Dim mouseY As Integer
    Private Sub btnYes_Click(sender As Object, e As EventArgs) Handles btnYes.Click
        Process.Start("https://sites.google.com/site/wfinfoapp/changes")
        If cbShowAgain.Checked = True Then
            My.Settings.CheckUpdates = False
        Else
            My.Settings.CheckUpdates = True
        End If
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub Update_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If cbShowAgain.Checked = True Then
            My.Settings.CheckUpdates = False
        Else
            My.Settings.CheckUpdates = True
        End If
    End Sub

    Private Sub Update_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        If cbShowAgain.Checked = True Then
            My.Settings.CheckUpdates = False
        Else
            My.Settings.CheckUpdates = True
        End If
    End Sub

    Private Sub btnNo_Click(sender As Object, e As EventArgs) Handles btnNo.Click
        If cbShowAgain.Checked = True Then
            My.Settings.CheckUpdates = False
        Else
            My.Settings.CheckUpdates = True
        End If
        My.Settings.Save()
        Me.Close()
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
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

    Private Sub UpdateWindow_Load(sender As Object, e As EventArgs) Handles Me.Load
        lbVersion.Text = "v" + My.Settings.Version
        Me.BringToFront()
        Me.Refresh()
    End Sub

    Private Sub UpdateWindow_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.BringToFront()
        Me.Refresh()
    End Sub

    Private Sub pTitle_Paint(sender As Object, e As PaintEventArgs) Handles pTitle.Paint

    End Sub
End Class