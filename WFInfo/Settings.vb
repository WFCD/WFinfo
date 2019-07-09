Imports System.IO
Imports System.Text.RegularExpressions

Public Class Settings
    Dim chTemp As String
    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer

    'mouse hacks
    Private storedCursor As Cursor

    Private Sub btnHkey1_Click(sender As Object, e As EventArgs) Handles btnHkey1.Click
        '_________________________________________________________________________
        'Toggle for setting hotkey 1
        '_________________________________________________________________________
        If Not key1Tog Then
            btnHkey1.Text = "..."
            key1Tog = True
        Else
            btnHkey1.Text = My.Settings.HKey1Text
        End If
    End Sub
	
	Private Sub btnHkey1_MouseUp(sender as Object, e as MouseEventArgs) Handles btnHkey1.MouseUp
        If key1Tog And (e.Button = MouseButtons.XButton1 Or e.Button = MouseButtons.XButton2) Then
            key1Tog = False
            HKey1 = e.Button
            Select Case e.Button
                Case MouseButtons.XButton1
                    btnHkey1.Text = "MOUSE 4"
                Case MouseButtons.XButton2
                    btnHkey1.Text = "MOUSE 5"
            End Select
        End If
    End Sub

    Private Sub btnHkey1_KeyUp(sender As Object, e As KeyEventArgs) Handles btnHkey1.KeyUp
        '_________________________________________________________________________
        'Sets the key for hotkey 1
        '_________________________________________________________________________
        If key1Tog Then
            key1Tog = False
            HKey1 = e.KeyCode
            e.SuppressKeyPress = True
            If e.KeyCode = Keys.PrintScreen Then
                btnHkey1.Text = "Print Screen"
            Else
                Select Case e.KeyCode

                    Case 112 To 123
                        btnHkey1.Text = "F" & e.KeyCode - 111
                    Case 32
                        btnHkey1.Text = "SPACE"
                    Case 8
                        btnHkey1.Text = "BACKSPACE"
                    Case 16
                        btnHkey1.Text = "SHIFT"
                    Case 17
                        btnHkey1.Text = "CTRL"
                    Case 18
                        btnHkey1.Text = "ALT"
                    Case 9
                        btnHkey1.Text = "TAB"
                    Case 20
                        btnHkey1.Text = "CAPSLOCK"
                    Case 45
                        btnHkey1.Text = "INS"
                    Case 46
                        btnHkey1.Text = "DELETE"
                    Case 36
                        btnHkey1.Text = "HOME"
                    Case 35
                        btnHkey1.Text = "END"
                    Case 33
                        btnHkey1.Text = "PG UP"
                    Case 34
                        btnHkey1.Text = "PG DOWN"
                    Case Else
                        btnHkey1.Text = chTemp
                End Select
            End If
        End If
    End Sub

    Private Sub Settings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        '_________________________________________________________________________
        'Visual stuff and loading settings
        '_________________________________________________________________________
        If My.Settings.SettingsWinLoc.X = 0 And My.Settings.SettingsWinLoc.Y = 0 Then
            Location = New Point(Main.Location.X + Main.Width + 25, Main.Location.Y)
        Else
            Location = My.Settings.SettingsWinLoc
        End If
        If My.Settings.ResultWindow Then
            TrackBar1.Value = 2
        ElseIf My.Settings.NewOverlay Then
            TrackBar1.Value = 0
        End If
        btnHkey1.Text = My.Settings.HKey1Text
        cbAutomation.Checked = Automate
        cbDebug.Checked = Debug
        ScaleBar.Value = My.Settings.Scaling
        TextBox1.Text = My.Settings.Scaling & "%"
        ScaleOption.SelectedIndex = My.Settings.ScaleType
    End Sub

    Private Sub Settings_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible And Not IsWindowMoveable(Me) Then
            Dim scr As Screen = GetMainScreen()
            Me.Location = New Point(scr.WorkingArea.X + 200, scr.WorkingArea.Y + 200)
        End If
    End Sub

    Private Sub Settings_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress, btnHkey1.KeyPress
        '_________________________________________________________________________
        'Stores the next keypress char code to chTemp when ready to set a hotkey
        '_________________________________________________________________________
        If key1Tog Then
            chTemp = Chr(AscW(e.KeyChar)).ToString.ToUpper()
        End If
    End Sub

    Private Sub Settings_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        '_________________________________________________________________________
        'Further retrieving the code for hotkeys
        '_________________________________________________________________________
        If key1Tog Then
            key1Tog = False
            HKey1 = e.KeyCode
            e.SuppressKeyPress = True
            If e.KeyCode = Keys.PrintScreen Then
                btnHkey1.Text = "Print Screen"
            Else
                Select Case e.KeyCode

                    Case 112 To 123
                        btnHkey1.Text = "F" & e.KeyCode - 111
                    Case 32
                        btnHkey1.Text = "SPACE"
                    Case 8
                        btnHkey1.Text = "BACKSPACE"
                    Case 16
                        btnHkey1.Text = "SHIFT"
                    Case 17
                        btnHkey1.Text = "CTRL"
                    Case 18
                        btnHkey1.Text = "ALT"
                    Case 9
                        btnHkey1.Text = "TAB"
                    Case 20
                        btnHkey1.Text = "CAPSLOCK"
                    Case 45
                        btnHkey1.Text = "INS"
                    Case 46
                        btnHkey1.Text = "DELETE"
                    Case 36
                        btnHkey1.Text = "HOME"
                    Case 35
                        btnHkey1.Text = "END"
                    Case 33
                        btnHkey1.Text = "PG UP"
                    Case 34
                        btnHkey1.Text = "PG DOWN"
                    Case Else
                        btnHkey1.Text = chTemp
                End Select
            End If
        End If
    End Sub

    Private Sub Settings_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        '_________________________________________________________________________
        'Stores the settings when closing
        '_________________________________________________________________________
        My.Settings.SettingsWinLoc = Location
        saveSettings()
    End Sub

    Private Sub cbAutomation_Click(sender As Object, e As EventArgs) Handles cbAutomation.Click
        Automate = cbAutomation.Checked
        Main.tAutomate.Enabled = Automate
        saveSettings()
    End Sub

    Private Sub cbDebug_Click(sender As Object, e As EventArgs) Handles cbDebug.Click
        My.Settings.Debug = cbDebug.Checked
        Debug = cbDebug.Checked
        saveSettings()
    End Sub

    Private Sub startDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown, lbTitle.MouseDown, pbIcon.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub DRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove, lbTitle.MouseMove, pbIcon.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub stopDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp, lbTitle.MouseUp, pbIcon.MouseUp
        drag = False
        My.Settings.SettingsWinLoc = Location
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub saveSettings()
        ''Saves settings
        My.Settings.Automate = cbAutomation.Checked
        My.Settings.HKey1 = HKey1
        My.Settings.HKey1Text = btnHkey1.Text
        My.Settings.Debug = cbDebug.Checked
        My.Settings.Scaling = ScaleBar.Value
        My.Settings.ScaleType = ScaleOption.SelectedIndex
        My.Settings.Save()
    End Sub

    Private Sub ScaleBar_Scroll(sender As Object, e As EventArgs) Handles ScaleBar.Scroll
        TextBox1.Text = ScaleBar.Value & "%"
        parser2.UpdateCenter()
        saveSettings()
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ScaleOption.SelectedIndexChanged
        ScaleBar.Enabled = ScaleOption.SelectedIndex = 2
        TextBox1.Enabled = ScaleBar.Enabled
        If ScaleOption.SelectedIndex = 0 Then
            ScaleBar.Value = 100
        ElseIf ScaleOption.SelectedIndex = 1 Then
            ScaleBar.Value = 62
        End If
        ScaleBar_Scroll(sender, e)
        'Kek: To remove the blue highlight... because I don't like the look of it
        ActiveControl = Panel1
        parser2.UpdateCenter()
        saveSettings()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)
        Dim folderBrowserDialog1 As New FolderBrowserDialog()
        folderBrowserDialog1.ShowNewFolderButton = False
        Dim result As DialogResult = folderBrowserDialog1.ShowDialog()
        If result = DialogResult.OK Then
            My.Settings.LocStorage = folderBrowserDialog1.SelectedPath
            'SSdirSelection.Text = My.Settings.LocStorage
        End If
    End Sub

    Private Sub TrackBar1_Scroll_1(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        My.Settings.NewOverlay = (TrackBar1.Value = 0)
        My.Settings.ResultWindow = (TrackBar1.Value = 2)
    End Sub

    Private Sub TextBox1_Enter(sender As Object, e As EventArgs) Handles TextBox1.Enter
        TextBox1.Text = TextBox1.Text.Replace("%", "")
    End Sub

    Private Sub TextBox1_TextPress(ByVal sender As Object, ByVal e As KeyPressEventArgs) Handles TextBox1.KeyPress
        If Cursor.Current IsNot Nothing Then
            storedCursor = Cursor.Current
            Cursor.Current = Nothing
        End If
        If e.KeyChar = ChrW(Keys.Enter) Then
            ' Kek: Cause I like the perc sign disappearing on edits
            ActiveControl = Panel1
            If Cursor.Current Is Nothing Then
                Cursor.Current = storedCursor
            End If
            e.Handled = True
        End If
        If Not Char.IsNumber(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then e.KeyChar = ""
    End Sub

    Private Sub TextBox1_Leave(sender As Object, e As EventArgs) Handles TextBox1.Leave
        saveScaling(TextBox1.Text)
    End Sub

    Private Sub saveScaling(text As String)
        text = text.Replace("%", "")
        If text Is "" Then text = "50"
        Dim value = Convert.ToInt32(text)
        If value < 50 Then
            value = 50
        ElseIf value > 100 Then
            value = 100
        End If
        ScaleBar.Value = value
        My.Settings.Scaling = value
        TextBox1.Text = value & "%"
    End Sub
End Class