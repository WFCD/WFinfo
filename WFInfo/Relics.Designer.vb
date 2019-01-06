<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Relics
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
        Dim TreeNode1 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Lith")
        Dim TreeNode2 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Meso")
        Dim TreeNode3 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Neo")
        Dim TreeNode4 As System.Windows.Forms.TreeNode = New System.Windows.Forms.TreeNode("Axi")
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Relics))
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.RelicTree2 = New System.Windows.Forms.TreeView()
        Me.Panel2 = New System.Windows.Forms.Panel()
        Me.SortSelection = New System.Windows.Forms.ComboBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.RelicTree = New System.Windows.Forms.TreeView()
        Me.HideMenu = New System.Windows.Forms.ContextMenuStrip(Me.components)
        Me.HideOpt = New System.Windows.Forms.ToolStripMenuItem()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.pTitle = New System.Windows.Forms.Panel()
        Me.PictureBox3 = New System.Windows.Forms.PictureBox()
        Me.lbTitle = New System.Windows.Forms.Label()
        Me.Panel1.SuspendLayout()
        Me.Panel2.SuspendLayout()
        Me.pTitle.SuspendLayout()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Controls.Add(Me.RelicTree2)
        Me.Panel1.Controls.Add(Me.Panel2)
        Me.Panel1.Controls.Add(Me.RelicTree)
        Me.Panel1.Location = New System.Drawing.Point(2, 29)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(456, 557)
        Me.Panel1.TabIndex = 24
        '
        'RelicTree2
        '
        Me.RelicTree2.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.RelicTree2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.RelicTree2.CausesValidation = False
        Me.RelicTree2.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll
        Me.RelicTree2.Font = New System.Drawing.Font("Tahoma", 9.0!, System.Drawing.FontStyle.Bold)
        Me.RelicTree2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.RelicTree2.FullRowSelect = True
        Me.RelicTree2.Location = New System.Drawing.Point(2, 33)
        Me.RelicTree2.Name = "RelicTree2"
        Me.RelicTree2.Size = New System.Drawing.Size(450, 520)
        Me.RelicTree2.TabIndex = 20
        '
        'Panel2
        '
        Me.Panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel2.Controls.Add(Me.SortSelection)
        Me.Panel2.Controls.Add(Me.Label1)
        Me.Panel2.Controls.Add(Me.Label2)
        Me.Panel2.Location = New System.Drawing.Point(2, 2)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(450, 32)
        Me.Panel2.TabIndex = 19
        '
        'SortSelection
        '
        Me.SortSelection.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.SortSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.SortSelection.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.SortSelection.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold)
        Me.SortSelection.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.SortSelection.FormattingEnabled = True
        Me.SortSelection.Items.AddRange(New Object() {"Name", "Intact Plat", "Rad Bonus"})
        Me.SortSelection.Location = New System.Drawing.Point(280, 2)
        Me.SortSelection.Name = "SortSelection"
        Me.SortSelection.Size = New System.Drawing.Size(126, 26)
        Me.SortSelection.TabIndex = 23
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(210, 6)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(68, 18)
        Me.Label1.TabIndex = 22
        Me.Label1.Text = "Sort By:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Font = New System.Drawing.Font("Tahoma", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(48, 6)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(78, 18)
        Me.Label2.TabIndex = 21
        Me.Label2.Text = "All Relics"
        '
        'RelicTree
        '
        Me.RelicTree.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.RelicTree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.RelicTree.CausesValidation = False
        Me.RelicTree.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll
        Me.RelicTree.Font = New System.Drawing.Font("Tahoma", 9.0!, System.Drawing.FontStyle.Bold)
        Me.RelicTree.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.RelicTree.FullRowSelect = True
        Me.RelicTree.Location = New System.Drawing.Point(2, 33)
        Me.RelicTree.Name = "RelicTree"
        TreeNode1.Name = "lith"
        TreeNode1.Text = "Lith"
        TreeNode2.Name = "meso"
        TreeNode2.Text = "Meso"
        TreeNode3.Name = "neo"
        TreeNode3.Text = "Neo"
        TreeNode4.Name = "axi"
        TreeNode4.Text = "Axi"
        Me.RelicTree.Nodes.AddRange(New System.Windows.Forms.TreeNode() {TreeNode1, TreeNode2, TreeNode3, TreeNode4})
        Me.RelicTree.Size = New System.Drawing.Size(450, 520)
        Me.RelicTree.TabIndex = 18
        '
        'HideMenu
        '
        Me.HideMenu.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.HideMenu.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None
        Me.HideMenu.Font = New System.Drawing.Font("Tahoma", 10.0!, System.Drawing.FontStyle.Bold)
        Me.HideMenu.Name = "HideMenu"
        Me.HideMenu.ShowImageMargin = False
        Me.HideMenu.ShowItemToolTips = False
        Me.HideMenu.Size = New System.Drawing.Size(36, 4)
        '
        'HideOpt
        '
        Me.HideOpt.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.HideOpt.Name = "HideOpt"
        Me.HideOpt.ShowShortcutKeys = False
        Me.HideOpt.Size = New System.Drawing.Size(155, 22)
        Me.HideOpt.Text = "Hide"
        Me.HideOpt.TextAlign = System.Drawing.ContentAlignment.TopLeft
        '
        'btnClose
        '
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Popup
        Me.btnClose.Font = New System.Drawing.Font("Tahoma", 8.25!, System.Drawing.FontStyle.Bold)
        Me.btnClose.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.btnClose.Location = New System.Drawing.Point(428, 2)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(30, 32)
        Me.btnClose.TabIndex = 17
        Me.btnClose.Text = "x"
        Me.btnClose.TextAlign = System.Drawing.ContentAlignment.TopCenter
        Me.btnClose.UseVisualStyleBackColor = False
        '
        'pTitle
        '
        Me.pTitle.BackColor = System.Drawing.Color.FromArgb(CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer), CType(CType(15, Byte), Integer))
        Me.pTitle.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.pTitle.Controls.Add(Me.PictureBox3)
        Me.pTitle.Controls.Add(Me.lbTitle)
        Me.pTitle.Controls.Add(Me.btnClose)
        Me.pTitle.Location = New System.Drawing.Point(2, 2)
        Me.pTitle.Name = "pTitle"
        Me.pTitle.Size = New System.Drawing.Size(456, 27)
        Me.pTitle.TabIndex = 25
        '
        'PictureBox3
        '
        Me.PictureBox3.BackColor = System.Drawing.Color.Transparent
        Me.PictureBox3.Image = Global.WFInfo.My.Resources.Resources.WFLogo
        Me.PictureBox3.Location = New System.Drawing.Point(0, 1)
        Me.PictureBox3.Name = "PictureBox3"
        Me.PictureBox3.Size = New System.Drawing.Size(25, 25)
        Me.PictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox3.TabIndex = 22
        Me.PictureBox3.TabStop = False
        '
        'lbTitle
        '
        Me.lbTitle.AutoSize = True
        Me.lbTitle.BackColor = System.Drawing.Color.Transparent
        Me.lbTitle.Font = New System.Drawing.Font("Cambria", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.lbTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(177, Byte), Integer), CType(CType(208, Byte), Integer), CType(CType(217, Byte), Integer))
        Me.lbTitle.Location = New System.Drawing.Point(32, 6)
        Me.lbTitle.Name = "lbTitle"
        Me.lbTitle.Size = New System.Drawing.Size(50, 17)
        Me.lbTitle.TabIndex = 17
        Me.lbTitle.Text = "Relics"
        '
        'Relics
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer), CType(CType(27, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(460, 588)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.pTitle)
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Name = "Relics"
        Me.Text = "Relics"
        Me.Panel1.ResumeLayout(False)
        Me.Panel2.ResumeLayout(False)
        Me.Panel2.PerformLayout()
        Me.pTitle.ResumeLayout(False)
        Me.pTitle.PerformLayout()
        CType(Me.PictureBox3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel1 As Panel
    Friend WithEvents PictureBox3 As PictureBox
    Friend WithEvents btnClose As Button
    Friend WithEvents pTitle As Panel
    Friend WithEvents lbTitle As Label
    Friend WithEvents RelicTree As TreeView
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Label2 As Label
    Friend WithEvents HideMenu As ContextMenuStrip
    Friend WithEvents HideOpt As ToolStripMenuItem
    Friend WithEvents Label1 As Label
    Friend WithEvents RelicTree2 As TreeView
    Friend WithEvents SortSelection As ComboBox
End Class
