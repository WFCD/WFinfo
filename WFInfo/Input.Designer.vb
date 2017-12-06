<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Input
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.btnAccept = New System.Windows.Forms.Button()
        Me.tbCommand = New System.Windows.Forms.TextBox()
        Me.bgMain = New System.ComponentModel.BackgroundWorker()
        Me.tActivate = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'btnAccept
        '
        Me.btnAccept.Location = New System.Drawing.Point(2, 2)
        Me.btnAccept.Name = "btnAccept"
        Me.btnAccept.Size = New System.Drawing.Size(14, 12)
        Me.btnAccept.TabIndex = 2
        Me.btnAccept.UseVisualStyleBackColor = True
        '
        'tbCommand
        '
        Me.tbCommand.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer))
        Me.tbCommand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.tbCommand.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.tbCommand.Location = New System.Drawing.Point(12, 20)
        Me.tbCommand.Name = "tbCommand"
        Me.tbCommand.Size = New System.Drawing.Size(456, 20)
        Me.tbCommand.TabIndex = 3
        Me.tbCommand.Visible = False
        '
        'tActivate
        '
        Me.tActivate.Enabled = True
        '
        'Input
        '
        Me.AcceptButton = Me.btnAccept
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(480, 60)
        Me.Controls.Add(Me.tbCommand)
        Me.Controls.Add(Me.btnAccept)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Input"
        Me.ShowInTaskbar = False
        Me.Text = "Input"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnAccept As Button
    Friend WithEvents tbCommand As TextBox
    Friend WithEvents bgMain As System.ComponentModel.BackgroundWorker
    Friend WithEvents tActivate As Timer
End Class
