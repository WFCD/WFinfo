' Class Based on answers from
' https://stackoverflow.com/questions/97459/making-a-winforms-textbox-behave-like-your-browsers-address-bar
' Modified from original C#

Public Class CustomTextBox
    Inherits TextBox

    Private _focused As Boolean = False

    Protected Overrides Sub OnEnter(e As EventArgs)
        MyBase.OnEnter(e)
        If (MouseButtons = MouseButtons.None) Then
            SelectAll()
            _focused = True
        End If
    End Sub

    Protected Overrides Sub OnLeave(e As EventArgs)
        MyBase.OnLeave(e)
        _focused = False
    End Sub

    Protected Overrides Sub OnMouseUp(mevent As MouseEventArgs)
        MyBase.OnMouseUp(mevent)
        If Not _focused Then
            If SelectionLength = 0 Then
                SelectAll()
            End If
            _focused = True
        End If
    End Sub
End Class
