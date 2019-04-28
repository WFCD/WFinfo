Imports System.IO

Public Class Settings
    Dim chTemp As String
    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer
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
        UpdateColors(Me)
        Me.Location = New Point(Main.Location.X + Main.Width + 25, Main.Location.Y)
        btnHkey1.Text = My.Settings.HKey1Text
        cbAutomation.Checked = Automate
        cbFullscreen.Checked = Fullscreen
        cbDebug.Checked = Debug
        ScaleBar.Value = My.Settings.Scaling
        Label9.Text = My.Settings.Scaling.ToString() + "%"
        ScaleOption.SelectedIndex = My.Settings.ScaleType
        If My.Settings.LocStorage.Length <= 1 Then
            My.Settings.LocStorage = Environment.ExpandEnvironmentVariables("%Userprofile%\Pictures\Warframe")
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
        saveSettings()
    End Sub

    Private Sub cbFullscreen_Click(sender As Object, e As EventArgs) Handles cbFullscreen.Click
        '_________________________________________________________________________
        'This is a new implementation of fullscreens support that does not rely on steam screenshots. 
        'Instead it allows the user to select any directory they wish.
        '_________________________________________________________________________
        Fullscreen = cbFullscreen.Checked
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

    Private Sub btnCustomize_Click(sender As Object, e As EventArgs) Handles btnCustomize.Click
        Picker.Show()
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
        Label9.Text = ScaleBar.Value.ToString() + "%"
        saveSettings()
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ScaleOption.SelectedIndexChanged
        ScaleBar.Enabled = ScaleOption.SelectedIndex = 2
        If ScaleOption.SelectedIndex = 0 Then
            ScaleBar.Value = 100
        ElseIf ScaleOption.SelectedIndex = 1 Then
            ScaleBar.Value = 95
        End If
        ScaleBar_Scroll(sender, e)
        'Kek: To remove the blue highlight... because I don't like the look of it
        ScaleBar.Select()
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

    Private Sub lbKey_Click(sender As Object, e As EventArgs) Handles lbKey.Click

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click

    End Sub

    Private Sub Label9_Click(sender As Object, e As EventArgs) Handles Label9.Click

    End Sub

    Private Sub Label8_Click(sender As Object, e As EventArgs) Handles Label8.Click

    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub Label12_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs)
    End Sub

    Private Sub Label10_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs)

    End Sub
End Class