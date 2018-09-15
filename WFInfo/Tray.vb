Imports System.Runtime.InteropServices
Imports System.Text
Public Class Tray

    Private InitialStyle As Integer
    Dim PercentVisible As Decimal
    Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
    Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
    Dim closing As Boolean = False
    Dim goalY As Integer = screenHeight * 0.63
    Dim GetWarframe As Boolean = False
    Dim WFhWnd As String = ""
    Protected Overrides ReadOnly Property CreateParams As System.Windows.Forms.CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H80
            Return cp
        End Get
    End Property

    '_________________________________________________________________________
    'Visual and location stuff, also allows it to be clicked through
    '_________________________________________________________________________
    Private Sub Form_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        UpdateColors(Me)
        pbBG.Image = Tint(pbBG.Image, My.Settings.cTray, 0.125)
        Me.Location = New Point(screenWidth * 3, screenHeight * 3)
        Me.Location = New Point(screenWidth * 0.82, screenHeight)
        Dim fontSize As Integer = (screenHeight) / 75
        Dim x As Integer = 0.08 * Me.Width
        Dim y As Integer = 0.02 * Me.Height
        lbDisplay.Font = New Font(lbDDropShadow.Font.FontFamily, fontSize, FontStyle.Bold)
        lbDDropShadow.Font = New Font(lbDDropShadow.Font.FontFamily, fontSize, FontStyle.Bold)
        lbDDropShadow.Parent = pbBG
        lbDisplay.Parent = lbDDropShadow
        lbDDropShadow.Location = New Point(x, y)
        lbDisplay.Location = New Point(-1, 0)
        lbDDropShadow.BringToFront()
        pbBG.Dock = DockStyle.Fill
        pbBG.SendToBack()
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

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetWindowText(hWnd As IntPtr, text As StringBuilder, count As Integer) As Integer
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function GetWindowTextLength(hWnd As IntPtr) As Integer
    End Function

    <DllImport("user32.dll", EntryPoint:="GetWindowRect")>
    Private Shared Function GetWindowRect(ByVal hWnd As IntPtr, ByRef lpRect As Rectangle) As <MarshalAs(UnmanagedType.Bool)> Boolean
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

    Private Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer,
      ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer,
      ByVal dwExtraInfo As Integer)

    Public Sub Display()
        closing = False
        'Checks for WF as active, sets boolean and hWnd
        If ActiveWindowName() = "WARFRAME" Then
            GetWarframe = True
            WFhWnd = GetForegroundWindow()
        End If

        If Me.Visible Then
            UpdateLb()
        End If
        Me.Show()
        tHide.Stop()
        tHide.Start()
    End Sub
    Public Sub Clear()
        lbDisplay.Text = ""
        lbDDropShadow.Text = ""
    End Sub
    Private Sub UpdateLb()
        For i = 0 To qItems.Count - 1
            lbDisplay.Text += qItems(i) + vbNewLine
            lbDDropShadow.Text += qItems(i) + vbNewLine
        Next
        qItems.Clear()
    End Sub
    Private Sub tHide_Tick(sender As Object, e As EventArgs) Handles tHide.Tick
        closing = True
        lbDisplay.Text = ""
        lbDDropShadow.Text = ""
        tHide.Stop()
    End Sub

    Private Sub Overlay_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Refresh()

        If GetWarframe Then
            AppActivate("WARFRAME")

            'Get WAREFRAME screen position
            Dim wr As Rectangle
            GetWindowRect(WFhWnd, wr)

            'Give WARFRAME mouse control
            Dim prevPos As Point = Cursor.Position
            Cursor.Position = New Point(wr.Left, wr.Top)
            mouse_event(&H2, 0, 0, 0, 0)
            mouse_event(&H4, 0, 0, 0, 0)
            Cursor.Position = prevPos
        End If

        '_________________________________________________________________________
        'Special condition for certain widescreen monitors
        '_________________________________________________________________________
        Me.Width = screenWidth * 0.18
        Me.Height = screenHeight * 0.37
        If screenWidth > 2000 Then
            Me.Height = screenHeight * 0.5
            goalY = screenHeight * 0.5
        End If
        InitialStyle = GetWindowLong(Me.Handle, GWL.ExStyle)
        PercentVisible = 0.97

        SetWindowLong(Me.Handle, GWL.ExStyle, InitialStyle Or WS_EX.Layered Or WS_EX.Transparent)
        SetLayeredWindowAttributes(Me.Handle, 0, 255 * PercentVisible, LWA.Alpha)
        Me.BackColor = Color.Black
        Me.TopMost = True

        tAnimate.Start()
        tHide.Stop()
        tHide.Start()
    End Sub

    Private Sub tAnimate_Tick(sender As Object, e As EventArgs) Handles tAnimate.Tick
        '_________________________________________________________________________
        'Slide up and slide down animation control
        '_________________________________________________________________________
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
                            If GetWarframe Then
                                AppActivate("WARFRAME")

                                'Get WAREFRAME screen position
                                Dim wr As Rectangle
                                GetWindowRect(WFhWnd, wr)

                                'Give WARFRAME mouse control
                                Dim prevPos As Point = Cursor.Position
                                Cursor.Position = New Point(wr.Left, wr.Top)
                                mouse_event(&H2, 0, 0, 0, 0)
                                mouse_event(&H4, 0, 0, 0, 0)
                                Cursor.Position = prevPos
                            End If
                        Catch ex As Exception
                        End Try
                        Me.Close()
                    End If
                End If
            Else
                If Not Input.Visible = True Then

                    Try
                        If GetWarframe Then
                            AppActivate("WARFRAME")

                            'Get WAREFRAME screen position
                            Dim wr As Rectangle
                            GetWindowRect(WFhWnd, wr)

                            'Give WARFRAME mouse control
                            Dim prevPos As Point = Cursor.Position
                            Cursor.Position = New Point(wr.Left, wr.Top)
                            mouse_event(&H2, 0, 0, 0, 0)
                            mouse_event(&H4, 0, 0, 0, 0)
                            Cursor.Position = prevPos
                        End If
                    Catch ex As Exception
                    End Try
                    Me.Close()
                    Me.Dispose()
                End If
            End If
        End If
    End Sub
    Public Sub quack()
        '_________________________________________________________________________
        'Easter egg text to display
        '_________________________________________________________________________
        qItems.Clear()
        lbDisplay.Text = ""
        lbDDropShadow.Text = ""
        qItems.Add("  _________")
        qItems.Add("/             \            _")
        qItems.Add("| Quack    >    <(o )___")
        qItems.Add("\_________/         ( ._> /")
        Me.Display()
    End Sub

    Private Function ActiveWindowName() As String
        Dim strTitle As String = String.Empty
        Dim handle As IntPtr = GetForegroundWindow()
        ' Obtain the length of the text   
        Dim intLength As Integer = GetWindowTextLength(handle) + 1
        Dim stringBuilder As New StringBuilder(intLength)
        If GetWindowText(handle, stringBuilder, intLength) > 0 Then
            strTitle = stringBuilder.ToString()
        End If
        Return strTitle
    End Function
End Class