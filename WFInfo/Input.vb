Imports System.Runtime.InteropServices
Public Class Input
    Declare Function SetForegroundWindow Lib "user32.dll" (ByVal hwnd As Integer) As Integer
    Private InitialStyle As Integer
    Dim PercentVisible As Decimal
    Dim myWindowID As Long
    Dim busy As Boolean = False
    Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
    Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
    Private Sub Input_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Location = New Point(screenWidth * 3, screenHeight * 3)
    End Sub

    Public Enum GWL As Integer
        ExStyle = -20
    End Enum

    Public Enum WS_EX As Integer
        Transparent = &H20
        Layered = &H80000
    End Enum

    Public Enum LWA As Integer
        ColorKey = &H1
        Alpha = &H2
    End Enum

    <DllImport("user32.dll", EntryPoint:="GetWindowLong")>
    Public Shared Function GetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL
            ) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="SetWindowLong")>
    Public Shared Function SetWindowLong(
        ByVal hWnd As IntPtr,
        ByVal nIndex As GWL,
        ByVal dwNewLong As WS_EX
            ) As Integer
    End Function

    Private Declare Function GetForegroundWindow Lib "user32" () As Long

    <DllImport("user32.dll",
      EntryPoint:="SetLayeredWindowAttributes")>
    Public Shared Function SetLayeredWindowAttributes(
        ByVal hWnd As IntPtr,
        ByVal crKey As Integer,
        ByVal alpha As Byte,
        ByVal dwFlags As LWA
            ) As Boolean
    End Function

    Private Sub Input_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Refresh()
        Me.Location = New Point(screenWidth * 0.7, screenHeight * 0.94)
        Me.Width = screenWidth * 0.3
        Me.Height = screenHeight * 0.06
        btnAccept.Location = New Point(Me.Width * 2, Me.Height * 2)
        Dim size As Integer = screenHeight * 0.011
        Dim xLoc As Integer = Me.Height * 0.9
        Dim yLoc As Integer = Me.Width * 0.33
        tbCommand.Location = New Point(yLoc, xLoc)
        tbCommand.Width = Me.Width * 0.33
        tbCommand.Font = New Font(tbCommand.Font.FontFamily, size, FontStyle.Bold)
        tbCommand.Visible = True
        InitialStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        PercentVisible = 0.9
        SetWindowLong(Me.Handle, GWL.ExStyle, InitialStyle Or WS_EX.Layered Or WS_EX.Transparent)
        SetLayeredWindowAttributes(Me.Handle, 0, 255 * PercentVisible, LWA.Alpha)
        Me.TransparencyKey = Color.LightBlue
        Me.BackColor = Color.LightBlue
        Me.TopMost = True
        tbCommand.Focus()
    End Sub

    Private Sub btnAccept_Click(sender As Object, e As EventArgs) Handles btnAccept.Click
        busy = True
        Try
            Try
                AppActivate("WARFRAME")
            Catch ex As Exception
            End Try
            If tbCommand.Text.ToLower.Contains("duck") Or tbCommand.Text.ToLower.Contains("quack") Then
                Overlay.quack()
                Me.Close()
                tbCommand.Text = ""
                Exit Try
            End If
            If tbCommand.Text.ToLower.Split(" ")(0) = "mod" Or tbCommand.Text.ToLower.Split(" ")(0) = "m" Then
                If tbCommand.Text.ToLower.Split(" ")(0) = "m" Then
                    tbCommand.Text = "mod" & tbCommand.Text.ToLower.Remove(0, 1)
                End If
                Dim modStr As String = StrConv(tbCommand.Text.ToLower.Replace("mod ", ""), VbStrConv.ProperCase)
                qItems.Add(vbNewLine & modStr & vbNewLine & "    Plat: " & GetPlat(modStr, True, True) & vbNewLine)
                Overlay.Display()
                Me.Close()
                tbCommand.Text = ""
            ElseIf tbCommand.Text.ToLower.Split(" ")(0) = "where" Or tbCommand.Text.ToLower.Split(" ")(0) = "w" Then
                If tbCommand.Text.ToLower.Split(" ")(0) = "w" Then
                    tbCommand.Text = "where" & tbCommand.Text.ToLower.Remove(0, 1)
                End If
                Dim cmdRes As String = tbCommand.Text.ToLower.Replace("where ", "")
                Dim ResInd As Integer = -1
                Dim Found As Boolean = False
                For Each str As String In My.Settings.Resources.Split(vbNewLine)
                    If Not Found Then
                        ResInd += 1
                        For Each substr As String In str.Split(",")(0).Split(" ")
                            If LevDist(substr, cmdRes) <= 1 Then
                                Found = True
                            End If
                        Next
                    End If
                Next
                Dim lowestLev As Integer = 9999
                If Not Found Then
                    ResInd = 0
                    Dim levCount As Integer = -1
                    For Each str As String In My.Settings.Resources.Split(vbNewLine)
                        levCount += 1
                        Dim dist As Integer = LevDist(str.Split(",")(0), cmdRes)
                        If dist < lowestLev Then
                            ResInd = levCount
                            lowestLev = dist
                        End If
                    Next
                End If
                qItems.Add(My.Settings.Resources.Split(vbNewLine)(ResInd).Split(",")(0) & vbNewLine & My.Settings.Resources.Split(vbNewLine)(ResInd).Split(",")(1) & vbNewLine)
                Overlay.Display()
                Me.Close()
            Else
                tbCommand.Text = tbCommand.Text.ToLower.Replace(" p ", " prime ")
                tbCommand.Text = tbCommand.Text.ToLower.Replace("bp", "blueprint")
                If tbCommand.Text.ToLower = "clear" Then
                    Overlay.Clear()
                    tbCommand.Text = ""
                Else
                    Dim found As Boolean = False
                    Dim guess As String = ""
                    If tbCommand.Text.ToLower.Contains("set") Then
                        guess = checkSet(tbCommand.Text)
                    Else
                        For i = 0 To Names.Count - 1
                            If Names(i).ToLower.Contains(tbCommand.Text.ToLower) Then
                                found = True
                                guess = Names(i)
                            End If
                        Next
                        If Not found Then
                            guess = Names(check(tbCommand.Text))
                        End If
                    End If
                    If Not guess = "Forma Blueprint" Then
                        Dim plat As String = GetPlat(guess, True)
                        Dim duck As String
                        If tbCommand.Text.ToLower.Contains("set") Then
                            duck = ""
                        Else
                            duck = "    Ducks: " & Ducks(check(guess)) & vbNewLine
                        End If
                        If guess.Length > 27 Then
                            qItems.Add(guess.Substring(0, 27) & "..." & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        Else
                            qItems.Add(guess & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        End If
                    Else
                        qItems.Add(vbNewLine & guess & vbNewLine)
                    End If
                    Overlay.Display()
                End If
                Me.Close()
                tbCommand.Text = ""
            End If
        Catch ex As Exception
        End Try
    End Sub
    Public Sub Display()
        Me.Refresh()
        Me.TransparencyKey = Color.LightBlue
        Me.BackColor = Color.LightBlue
        Me.TopMost = True
        Me.WindowState = FormWindowState.Maximized
        Me.Show()
        Me.Select()
    End Sub

    Private Sub tbCommand_LostFocus(sender As Object, e As EventArgs) Handles tbCommand.LostFocus
        If busy Then
            Me.Hide()
        Else
            Me.Close()
        End If
    End Sub

    Private Sub tbCommand_KeyDown(sender As Object, e As KeyEventArgs) Handles tbCommand.KeyDown
        If e.KeyCode = Keys.Escape Then
            e.Handled = True
            Me.Close()
        End If
    End Sub

    Private Sub tActivate_Tick(sender As Object, e As EventArgs) Handles tActivate.Tick
        If Me.Visible Then
            If Not GetForegroundWindow() = Me.Handle.ToString Then
                SendKeys.Send(Keys.Alt)
                Input.SetForegroundWindow(Me.Handle)
            End If
            If Not tbCommand.Focused Then
                Show()
                tbCommand.Focus()
            End If
        End If
    End Sub

    Private Sub Input_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        tbCommand.Select()
    End Sub
End Class