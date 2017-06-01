<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Main
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Main))
        Me.tPB = New System.Windows.Forms.Timer(Me.components)
        Me.BGWorker = New System.ComponentModel.BackgroundWorker()
        Me.lbSession = New System.Windows.Forms.Label()
        Me.lbStatus = New System.Windows.Forms.Label()
        Me.tPPrice = New System.Windows.Forms.Timer(Me.components)
        Me.bgPPrice = New System.ComponentModel.BackgroundWorker()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.pbDebug = New System.Windows.Forms.PictureBox()
        Me.lbVersion = New System.Windows.Forms.Label()
        Me.lbTitle = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.pbHome = New System.Windows.Forms.PictureBox()
        Me.pbDonate = New System.Windows.Forms.PictureBox()
        Me.pbSettings = New System.Windows.Forms.PictureBox()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.PictureBox3 = New System.Windows.Forms.PictureBox()
        Me.pTitle = New System.Windows.Forms.Panel()
        Me.Panel1.SuspendLayout()
        CType(Me.pbDebug, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbHome, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbDonate, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.pbSettings, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.pTitle.SuspendLayout()
        Me.SuspendLayout()
        '
        'tPB
        '
        Me.tPB.Enabled = True
        Me.tPB.Interval = 1
        '
        'lbSession
        '
        Me.lbSession.AutoSize = True
        Me.lbSession.BackColor = System.Drawing.Color.Transparent
        Me.lbSession.Font = New System.Drawing.Font("Source Sans Pro", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbSession.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbSession.Location = New System.Drawing.Point(29, 53)
        Me.lbSession.Name = "lbSession"
        Me.lbSession.Size = New System.Drawing.Size(165, 20)
        Me.lbSession.TabIndex = 4
        Me.lbSession.Text = "Checks this Session:   0"
        '
        'lbStatus
        '
        Me.lbStatus.AutoSize = True
        Me.lbStatus.BackColor = System.Drawing.Color.Transparent
        Me.lbStatus.Font = New System.Drawing.Font("Source Sans Pro", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbStatus.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbStatus.Location = New System.Drawing.Point(29, 24)
        Me.lbStatus.Name = "lbStatus"
        Me.lbStatus.Size = New System.Drawing.Size(114, 20)
        Me.lbStatus.TabIndex = 5
        Me.lbStatus.Text = "Status:    Ready"
        '
        'tPPrice
        '
        Me.tPPrice.Interval = 1000
        '
        'bgPPrice
        '
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.pbDebug)
        Me.Panel1.Controls.Add(Me.lbStatus)
        Me.Panel1.Controls.Add(Me.lbSession)
        Me.Panel1.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Panel1.Location = New System.Drawing.Point(0, 27)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(334, 99)
        Me.Panel1.TabIndex = 19
        '
        'pbDebug
        '
        Me.pbDebug.BackColor = System.Drawing.Color.Silver
        Me.pbDebug.Location = New System.Drawing.Point(11, 73)
        Me.pbDebug.Name = "pbDebug"
        Me.pbDebug.Size = New System.Drawing.Size(10, 11)
        Me.pbDebug.TabIndex = 6
        Me.pbDebug.TabStop = False
        Me.pbDebug.Visible = False
        '
        'lbVersion
        '
        Me.lbVersion.AutoSize = True
        Me.lbVersion.BackColor = System.Drawing.Color.Transparent
        Me.lbVersion.Font = New System.Drawing.Font("Cambria", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbVersion.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbVersion.Location = New System.Drawing.Point(88, 7)
        Me.lbVersion.Name = "lbVersion"
        Me.lbVersion.Size = New System.Drawing.Size(38, 12)
        Me.lbVersion.TabIndex = 7
        Me.lbVersion.Text = "v3.0.2"
        '
        'lbTitle
        '
        Me.lbTitle.AutoSize = True
        Me.lbTitle.BackColor = System.Drawing.Color.Transparent
        Me.lbTitle.Font = New System.Drawing.Font("Cambria", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbTitle.Location = New System.Drawing.Point(31, 3)
        Me.lbTitle.Name = "lbTitle"
        Me.lbTitle.Size = New System.Drawing.Size(58, 17)
        Me.lbTitle.TabIndex = 6
        Me.lbTitle.Text = "WFInfo"
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnClose.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold)
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(304, -1)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(30, 32)
        Me.btnClose.TabIndex = 22
        Me.btnClose.Text = "x"
        Me.btnClose.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.btnClose.UseVisualStyleBackColor = False
        '
        'pbHome
        '
        Me.pbHome.BackColor = System.Drawing.Color.Transparent
        Me.pbHome.Image = Global.WFInfo.My.Resources.Resources.home
        Me.pbHome.Location = New System.Drawing.Point(307, 37)
        Me.pbHome.Name = "pbHome"
        Me.pbHome.Size = New System.Drawing.Size(25, 21)
        Me.pbHome.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.pbHome.TabIndex = 17
        Me.pbHome.TabStop = False
        '
        'pbDonate
        '
        Me.pbDonate.BackColor = System.Drawing.Color.Transparent
        Me.pbDonate.Image = Global.WFInfo.My.Resources.Resources.Donate
        Me.pbDonate.Location = New System.Drawing.Point(307, 64)
        Me.pbDonate.Name = "pbDonate"
        Me.pbDonate.Size = New System.Drawing.Size(25, 21)
        Me.pbDonate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pbDonate.TabIndex = 16
        Me.pbDonate.TabStop = False
        '
        'pbSettings
        '
        Me.pbSettings.BackColor = System.Drawing.Color.Transparent
        Me.pbSettings.Image = Global.WFInfo.My.Resources.Resources.Settings
        Me.pbSettings.Location = New System.Drawing.Point(307, 91)
        Me.pbSettings.Name = "pbSettings"
        Me.pbSettings.Size = New System.Drawing.Size(25, 21)
        Me.pbSettings.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize
        Me.pbSettings.TabIndex = 15
        Me.pbSettings.TabStop = False
        '
        'PictureBox1
        '
        Me.PictureBox1.BackColor = System.Drawing.Color.FromArgb(CType(CType(23, Byte), Integer), CType(CType(23, Byte), Integer), CType(CType(23, Byte), Integer))
        Me.PictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PictureBox1.Location = New System.Drawing.Point(306, 27)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(28, 99)
        Me.PictureBox1.TabIndex = 18
        Me.PictureBox1.TabStop = False
        '
        'PictureBox3
        '
        Me.PictureBox3.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox3.Image = Global.WFInfo.My.Resources.Resources.WFLogo
        Me.PictureBox3.Location = New System.Drawing.Point(0, -1)
        Me.PictureBox3.Name = "PictureBox3"
        Me.PictureBox3.Size = New System.Drawing.Size(25, 25)
        Me.PictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox3.TabIndex = 23
        Me.PictureBox3.TabStop = False
        '
        'pTitle
        '
        Me.pTitle.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.pTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pTitle.Controls.Add(Me.lbVersion)
        Me.pTitle.Controls.Add(Me.btnClose)
        Me.pTitle.Controls.Add(Me.PictureBox3)
        Me.pTitle.Controls.Add(Me.lbTitle)
        Me.pTitle.Location = New System.Drawing.Point(0, 0)
        Me.pTitle.Name = "pTitle"
        Me.pTitle.Size = New System.Drawing.Size(334, 27)
        Me.pTitle.TabIndex = 6
        '
        'Main
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(334, 127)
        Me.Controls.Add(Me.pbHome)
        Me.Controls.Add(Me.pbDonate)
        Me.Controls.Add(Me.pbSettings)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.pTitle)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Main"
        Me.Text = "WFInfo"
        Me.Panel1.ResumeLayout(False)
        Me.Panel1.PerformLayout()
        CType(Me.pbDebug, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbHome, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbDonate, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.pbSettings, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.pTitle.ResumeLayout(False)
        Me.pTitle.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents tPB As Timer
    Friend WithEvents BGWorker As System.ComponentModel.BackgroundWorker
    Friend WithEvents lbSession As Label
    Friend WithEvents lbStatus As Label
    Friend WithEvents tPPrice As Timer
    Friend WithEvents bgPPrice As System.ComponentModel.BackgroundWorker
    Friend WithEvents pbSettings As PictureBox
    Friend WithEvents pbDonate As PictureBox
    Friend WithEvents pbHome As PictureBox
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents lbTitle As Label
    Friend WithEvents btnClose As Button
    Friend WithEvents PictureBox3 As PictureBox
    Friend WithEvents pTitle As Panel
    Friend WithEvents pbDebug As PictureBox
    Friend WithEvents lbVersion As Label
End Class
