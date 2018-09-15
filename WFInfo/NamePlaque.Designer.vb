<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class NamePlaque
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
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.lbNDropShadow = New System.Windows.Forms.Label()
        Me.lbName = New System.Windows.Forms.Label()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'PictureBox1
        '
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.Image = Global.WFInfo.My.Resources.Resources.PlaqueTray
        Me.PictureBox1.Location = New System.Drawing.Point(0, 0)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(250, 140)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 0
        Me.PictureBox1.TabStop = False
        '
        'lbNDropShadow
        '
        Me.lbNDropShadow.AutoSize = True
        Me.lbNDropShadow.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbNDropShadow.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbNDropShadow.ForeColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.lbNDropShadow.Location = New System.Drawing.Point(142, 52)
        Me.lbNDropShadow.Name = "lbNDropShadow"
        Me.lbNDropShadow.Size = New System.Drawing.Size(60, 36)
        Me.lbNDropShadow.TabIndex = 4
        Me.lbNDropShadow.Text = "120"
        '
        'lbName
        '
        Me.lbName.AutoSize = True
        Me.lbName.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbName.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbName.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbName.Location = New System.Drawing.Point(49, 52)
        Me.lbName.Name = "lbName"
        Me.lbName.Size = New System.Drawing.Size(60, 36)
        Me.lbName.TabIndex = 3
        Me.lbName.Text = "120"
        '
        'tHide
        '
        Me.tHide.Enabled = True
        Me.tHide.Interval = 10000
        '
        'NamePlaque
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(250, 140)
        Me.Controls.Add(Me.lbNDropShadow)
        Me.Controls.Add(Me.lbName)
        Me.Controls.Add(Me.PictureBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "NamePlaque"
        Me.ShowInTaskbar = False
        Me.Text = "NamePlaque"
        Me.TransparencyKey = System.Drawing.Color.Black
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents lbNDropShadow As Label
    Friend WithEvents lbName As Label
    Friend WithEvents tHide As Timer
End Class
