Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class NameTray
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.tHide = New System.Windows.Forms.Timer(Me.components)
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.lbPlat = New System.Windows.Forms.Label()
        Me.lbPDropShadow = New System.Windows.Forms.Label()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tHide
        '
        Me.tHide.Interval = 10000
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.PictureBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.PictureBox1.Image = Global.WFInfo.My.Resources.Resources.NameTray
        Me.PictureBox1.Location = New System.Drawing.Point(0, 0)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(250, 57)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 0
        Me.PictureBox1.TabStop = False
        '
        'lbPlat
        '
        Me.lbPlat.BackColor = System.Drawing.Color.Transparent
        Me.lbPlat.Font = New System.Drawing.Font("Calibri", 18.0!, System.Drawing.FontStyle.Bold)
        Me.lbPlat.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbPlat.Location = New System.Drawing.Point(12, 9)
        Me.lbPlat.Name = "lbPlat"
        Me.lbPlat.Size = New System.Drawing.Size(226, 39)
        Me.lbPlat.TabIndex = 2
        Me.lbPlat.Text = "120"
        Me.lbPlat.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'lbPDropShadow
        '
        Me.lbPDropShadow.BackColor = System.Drawing.Color.Transparent
        Me.lbPDropShadow.Font = New System.Drawing.Font("Calibri", 21.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbPDropShadow.ForeColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(20, Byte), Integer), CType(CType(24, Byte), Integer))
        Me.lbPDropShadow.Location = New System.Drawing.Point(12, 9)
        Me.lbPDropShadow.Name = "lbPDropShadow"
        Me.lbPDropShadow.Size = New System.Drawing.Size(226, 39)
        Me.lbPDropShadow.TabIndex = 3
        Me.lbPDropShadow.Text = "120"
        Me.lbPDropShadow.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'NameTray
        '
        Me.BackColor = System.Drawing.Color.Black
        Me.ClientSize = New System.Drawing.Size(250, 57)
        Me.Controls.Add(Me.lbPDropShadow)
        Me.Controls.Add(Me.lbPlat)
        Me.Controls.Add(Me.PictureBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "NameTray"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.TransparencyKey = System.Drawing.Color.Black
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents tHide As Timer
    Friend WithEvents lbPlat As Label
    Friend WithEvents lbPDropShadow As Label
End Class
