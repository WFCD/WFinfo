Public Class TargetSelector
    Dim mode As Integer = 0
    Dim startPoint As Point = New Point(5, 5)
    Dim recSize As Point = New Point(25, 25)
    Private Sub TargetSelector_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Me.TransparencyKey = Color.Black
        Me.BackColor = Color.Black
        Me.Size = New Point(25, 25)
    End Sub

    Private Sub tControl_Tick(sender As Object, e As EventArgs)
        Select Case mode
            Case 0
                Me.Location = MousePosition - New Point(12, 12)
            Case 1
                Me.Location = startPoint
                recSize = MousePosition - startPoint - New Point(7, 7)
                If recSize.X <= 4 Then
                    recSize.X = 5
                End If
                If recSize.Y <= 4 Then
                    recSize.Y = 5
                End If
                Me.Size = recSize + New Point(8, 8)

        End Select
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click
        Select Case mode
            Case 0
                mode = 1
                PictureBox1.Image = Nothing
                startPoint = MousePosition - New Point(1, 1)
            Case 1
                My.Settings.StartPoint = startPoint
                recSize = MousePosition - startPoint
                recSize.Y = recSize.Y * 1.1
                My.Settings.RecSize = recSize
                My.Settings.TargetAreaSet = True
                My.Settings.Save()
                Me.Close()
        End Select
    End Sub

    Private Sub PictureBox1_Paint(sender As Object, e As PaintEventArgs) Handles PictureBox1.Paint
        Select Case mode
            Case 0
            Case 1
                ' Create pen. 
                Dim grayPen As New Pen(Color.Gray, 3)

                ' Create rectangle. 
                Dim rect As New Rectangle(0, 0, Me.Size.Width - 1, Me.Size.Height - 1)

                ' Draw rectangle to screen.
                e.Graphics.DrawRectangle(grayPen, rect)
        End Select
    End Sub
End Class