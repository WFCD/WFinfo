<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Overlay
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
        Me.lbPlat = New System.Windows.Forms.Label()
        Me.lbPDropShadow = New System.Windows.Forms.Label()
        Me.lbDucats = New System.Windows.Forms.Label()
        Me.lbDDropShadow = New System.Windows.Forms.Label()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lbPlat
        '
        Me.lbPlat.AutoSize = True
        Me.lbPlat.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbPlat.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbPlat.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbPlat.Location = New System.Drawing.Point(70, 19)
        Me.lbPlat.Name = "lbPlat"
        Me.lbPlat.Size = New System.Drawing.Size(60, 36)
        Me.lbPlat.TabIndex = 1
        Me.lbPlat.Text = "120"
        '
        'lbPDropShadow
        '
        Me.lbPDropShadow.AutoSize = True
        Me.lbPDropShadow.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbPDropShadow.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbPDropShadow.ForeColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.lbPDropShadow.Location = New System.Drawing.Point(163, 19)
        Me.lbPDropShadow.Name = "lbPDropShadow"
        Me.lbPDropShadow.Size = New System.Drawing.Size(60, 36)
        Me.lbPDropShadow.TabIndex = 2
        Me.lbPDropShadow.Text = "120"
        '
        'lbDucats
        '
        Me.lbDucats.AutoSize = True
        Me.lbDucats.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbDucats.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbDucats.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbDucats.Location = New System.Drawing.Point(70, 84)
        Me.lbDucats.Name = "lbDucats"
        Me.lbDucats.Size = New System.Drawing.Size(60, 36)
        Me.lbDucats.TabIndex = 3
        Me.lbDucats.Text = "120"
        '
        'lbDDropShadow
        '
        Me.lbDDropShadow.AutoSize = True
        Me.lbDDropShadow.BackColor = System.Drawing.SystemColors.ActiveCaptionText
        Me.lbDDropShadow.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbDDropShadow.ForeColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.lbDDropShadow.Location = New System.Drawing.Point(163, 84)
        Me.lbDDropShadow.Name = "lbDDropShadow"
        Me.lbDDropShadow.Size = New System.Drawing.Size(60, 36)
        Me.lbDDropShadow.TabIndex = 4
        Me.lbDDropShadow.Text = "120"
        '
        'tHide
        '
        Me.tHide.Enabled = True
        Me.tHide.Interval = 10000
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.Image = Global.WFInfo.My.Resources.Resources.DnPPanel
        Me.PictureBox1.Location = New System.Drawing.Point(0, 0)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(250, 140)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 0
        Me.PictureBox1.TabStop = False
        '
        'Overlay
        '
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(250, 140)
        Me.Controls.Add(Me.lbDDropShadow)
        Me.Controls.Add(Me.lbDucats)
        Me.Controls.Add(Me.lbPDropShadow)
        Me.Controls.Add(Me.lbPlat)
        Me.Controls.Add(Me.PictureBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "Overlay"
        Me.ShowInTaskbar = False
        Me.TransparencyKey = System.Drawing.Color.Black
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents lbPlat As Label
    Friend WithEvents lbPDropShadow As Label
    Friend WithEvents lbDucats As Label
    Friend WithEvents lbDDropShadow As Label
    Friend WithEvents tHide As Timer
End Class
