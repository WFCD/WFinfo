Imports System.Runtime.InteropServices
Imports System.Text
Public Class Input
    Declare Function SetForegroundWindow Lib "user32.dll" (ByVal hwnd As Integer) As Integer
    Private InitialStyle As Integer
    Dim PercentVisible As Decimal
    Dim myWindowID As Long
    Dim busy As Boolean = False
    Dim screenWidth As Integer = Screen.PrimaryScreen.Bounds.Width
    Dim screenHeight As Integer = Screen.PrimaryScreen.Bounds.Height
    Dim GetWarframe As Boolean = False
    Dim WFhWnd As String = ""
    Private Sub Input_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        UpdateColors(Me)
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
            Dim command As String = tbCommand.Text.ToLower()
            Me.Hide()
            Tray.Clear()
            Try
                If GetWarframe Then
                    While Not ActiveWindowName() = "WARFRAME"
                        AppActivate("WARFRAME")
                    End While

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

            If command.Contains("duck") Or command.Contains("quack") Then
                Tray.quack()
                Me.Close()
                tbCommand.Text = ""
                Exit Try
            End If
            If command.Split(" ")(0) = "mod" Or command.Split(" ")(0) = "m" Then
                Dim modStr As String = StrConv(nth(command, 0), VbStrConv.ProperCase)
                qItems.Add(vbNewLine & modStr & vbNewLine & "    Plat: " & GetPlat(modStr, True, True) & vbNewLine)
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""
            ElseIf command.Split(" ")(0) = "where" Or command.Split(" ")(0) = "w" Then
                If command.Split(" ")(0) = "w" Then
                    tbCommand.Text = "where" & command.Remove(0, 1)
                End If
                Dim cmdRes As String = command.Replace("where ", "")
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
                Tray.Display()
                Me.Close()

            ElseIf command.Split(" ")(0) = "e" Then
                Dim found As Boolean = False
                Dim foundItem As String = ""
                Dim checkItem As String = nth(command, 0)
                If checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "p" Or checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "prime" Then
                    checkItem = checkItem.Substring(0, checkItem.LastIndexOf(" ")) & " prime"
                End If

                For Each item As String In Equipment.Split(",")
                    If item = checkItem Then
                        found = True
                        foundItem = StrConv(item, VbStrConv.ProperCase)
                    End If
                Next
                If found Then
                    qItems.Add(vbNewLine & foundItem & vbNewLine & "Already Leveled")
                Else
                    qItems.Add(vbNewLine & "Not Leveled")
                End If
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

            ElseIf command.Split(" ")(0) = "ea" Then
                Dim item As String = nth(command, 0)
                If item.Substring(item.LastIndexOf(" ") + 1) = "p" Then
                    item = item.Substring(0, item.LastIndexOf(" ")) & " prime"
                End If
                Equipment = Equipment & item & ","
                Me.Close()
                tbCommand.Text = ""

            ElseIf command.Split(" ")(0) = "er" Then
                Dim found As Boolean = False
                Dim checkItem As String = nth(command, 0)
                If checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "p" Or checkItem.Substring(checkItem.LastIndexOf(" ") + 1) = "prime" Then
                    checkItem = checkItem.Substring(0, checkItem.LastIndexOf(" ")) & " prime"
                End If
                For Each item As String In Equipment.Split(",")
                    If item = checkItem Then
                        Equipment = Equipment.Replace(item & ",", "")
                        found = True
                    End If
                Next
                If Not found Then
                    qItems.Add(vbNewLine & "Item Not Found")
                    Tray.Display()
                End If
                Me.Close()
                tbCommand.Text = ""

            ElseIf command.Split(" ")(0) = "el" Then
                Dim clipString As String = ""
                For Each item As String In Equipment.Split(",")
                    clipString &= item & vbNewLine
                Next
                Clipboard.SetText(clipString)
                qItems.Add(vbNewLine & "Equipment Coppied" & vbNewLine & "To Clipboard")
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

            ElseIf command.Split(" ")(0) = "ec" Then
                Equipment = ""
                qItems.Add(vbNewLine & "Equipment Cleared")
                Tray.Display()
                Me.Close()
                tbCommand.Text = ""

            Else
                command = command.Replace(" p ", " prime ")
                command = command.Replace("bp", "blueprint")
                tbCommand.Text = command
                If command = "clear" Then
                    Tray.Clear()
                    tbCommand.Text = ""
                Else
                    Dim found As Boolean = False
                    Dim guess As String = ""
                    If command.Contains("set") Then
                        guess = checkSet(tbCommand.Text)
                    Else
                        For i = 0 To Names.Count - 1
                            If Names(i).ToLower.Contains(command) Then
                                found = True
                                guess = Names(i)
                            End If
                        Next
                        If Not found Then
                            guess = Names(check(tbCommand.Text))
                        End If
                    End If
                    If Not guess = "Forma Blueprint" Then
                        Dim plat As String = GetPlat(Main.KClean(guess), True)
                        Dim duck As String
                        If command.Contains("set") Then
                            duck = ""
                        Else
                            duck = "    Ducks: " & Ducks(check(guess)) & vbNewLine
                        End If
                        If Main.KClean(guess).Length > 27 Then
                            qItems.Add(Main.KClean(guess).Substring(0, 27) & "..." & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        Else
                            qItems.Add(Main.KClean(guess) & vbNewLine & duck & "    Plat: " & plat & vbNewLine)
                        End If
                    Else
                        qItems.Add(vbNewLine & guess & vbNewLine)
                    End If
                    Tray.Display()
                End If
                Me.Close()
                tbCommand.Text = ""
            End If
        Catch ex As Exception
            Me.Close()
            tbCommand.Text = ""
        End Try
    End Sub
    Public Function nth(s As String, n As Integer) As String
        For i = 0 To n
            s = s.Substring(s.IndexOf(" ") + 1)
        Next
        Return s
    End Function
    Public Sub Display()

        'Checks for WF as active, sets boolean and hWnd
        If ActiveWindowName() = "WARFRAME" Then
            GetWarframe = True
            WFhWnd = GetForegroundWindow()
        End If

        Me.Refresh()
        Me.TransparencyKey = Color.LightBlue
        Me.BackColor = Color.LightBlue
        Me.TopMost = True
        Me.WindowState = FormWindowState.Maximized
        Me.Show()
        Me.Refresh()
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
            If GetWarframe Then
                While Not ActiveWindowName() = "WARFRAME"
                    AppActivate("WARFRAME")
                End While

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
            Me.Close()
        End If
    End Sub

    Private Sub tActivate_Tick(sender As Object, e As EventArgs) Handles tActivate.Tick
        If Me.Visible Then
            If Not GetForegroundWindow() = Me.Handle.ToString Then
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