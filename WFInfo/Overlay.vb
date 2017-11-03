Imports System.Runtime.InteropServices
Imports System.Math

Public Class Overlay

    Private InitialStyle As Integer
    Dim PercentVisible As Decimal
    Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
    Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
    Dim closing As Boolean = False
    Dim goalY As Integer = screenHeight * 0.63

    Private Sub Form_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
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

    <DllImport("user32.dll",
      EntryPoint:="SetLayeredWindowAttributes")>
    Public Shared Function SetLayeredWindowAttributes(
        ByVal hWnd As IntPtr,
        ByVal crKey As Integer,
        ByVal alpha As Byte,
        ByVal dwFlags As LWA
            ) As Boolean
    End Function
    Public Sub Display()
        If Me.Visible Then
            UpdateLb()
        End If
        Me.Show()
        tHide.Stop()
        tHide.Start()
    End Sub
    Public Sub Clear()
        lbDisplay.Text = ""
    End Sub
    Private Sub UpdateLb()
        For i = 0 To qItems.Count - 1
            lbDisplay.Text += qItems(i) + vbNewLine
        Next
        qItems.Clear()
    End Sub
    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        closing = True
        lbDisplay.Text = ""
        tHide.Stop()
    End Sub

    Private Sub Overlay_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Refresh()
        Me.Location = New Point(screenWidth * 0.82, screenHeight)
        Dim size As Integer = (screenWidth + screenHeight) / 200
        lbDisplay.Font = New Font(lbDisplay.Font.FontFamily, size, FontStyle.Bold)
        Me.Width = screenWidth * 0.18
        Me.Height = screenHeight * 0.37
        InitialStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        PercentVisible = 0.95

        SetWindowLong(Me.Handle, GWL.ExStyle, InitialStyle Or WS_EX.Layered Or WS_EX.Transparent)
        SetLayeredWindowAttributes(Me.Handle, 0, 255 * PercentVisible, LWA.Alpha)
        Me.BackColor = Color.Black
        Me.TopMost = True
        tAnimate.Start()
    End Sub

    Private Sub tAnimate_Tick(sender As Object, e As EventArgs) Handles tAnimate.Tick
        If Not closing Then
            If Animate Then
                If Not Me.Location.Y - 22 <= goalY Then
                    Me.Location = New Point(Me.Location.X, Me.Location.Y - 22)
                ElseIf Me.Location.Y <> goalY Then
                    Me.Location = New Point(Me.Location.X, goalY)
                    UpdateLb()
                End If
            ElseIf Me.Location.Y <> goalY Then
                Me.Location = New Point(Me.Location.X, goalY)
                UpdateLb()
            End If
        Else
            If Animate Then
                If Not Me.Location.Y > screenHeight Then
                    Me.Location = New Point(Me.Location.X, Me.Location.Y + 20)
                Else
                    If Not Input.Visible = True Then
                        Try
                            AppActivate("WARFRAME")
                        Catch ex As Exception
                        End Try
                        Me.Close()
                    End If
                End If
            Else
                If Not Input.Visible = True Then

                    Try
                        AppActivate("WARFRAME")
                    Catch ex As Exception
                    End Try
                    Me.Close()
                End If
            End If
            End If
    End Sub
    Public Sub quack()
        qItems.Clear()
        lbDisplay.Text = ""
        qItems.Add("  _________")
        qItems.Add("/             \            _")
        qItems.Add("| Quack    >    <(o )___")
        qItems.Add("\_________/         ( ._> /")
        Me.Display()
    End Sub
End Class