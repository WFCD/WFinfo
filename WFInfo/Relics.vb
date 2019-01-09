Imports System.IO
Imports System.Net
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Management
Imports System.Security.Cryptography
Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging
Imports System.Data.SQLite
Imports Tesseract


Public Class Relics
    Dim drag As Boolean = False
    Dim mouseX As Integer
    Dim mouseY As Integer
    Dim RelicToHide As TreeNode = Nothing
    Dim Relic2ToHide As TreeNode = Nothing
    Public Tree1Sorter As New NodeSorter(0)
    Public Tree2Sorter As New NodeSorter(1)

    Private Sub pTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub pTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub pTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp
        drag = False
        My.Settings.RelicWinLoc = Me.Location
    End Sub

    Private Sub lbTitle_MouseDown(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Me.Left
        mouseY = Cursor.Position.Y - Me.Top
    End Sub

    Private Sub lbTitle_MouseMove(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseMove
        If drag Then
            Me.Top = Cursor.Position.Y - mouseY
            Me.Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub lbTitle_MouseUp(sender As Object, e As MouseEventArgs) Handles lbTitle.MouseUp
        drag = False
        My.Settings.RelicWinLoc = Me.Location
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Private Sub RelicTree_Collapse(sender As Object, e As TreeViewEventArgs) Handles RelicTree.AfterCollapse, RelicTree2.AfterCollapse
        Dim temp As String = "|" + e.Node.Name
        If e.Node.Name = "Hidden" Then
            If e.Node.Parent IsNot Nothing Then
                temp += e.Node.Parent.Name
            Else
                temp += "|"
            End If
        End If
        If My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics = My.Settings.ExpandedRelics.Replace(temp, "")
        End If
    End Sub

    Private Sub RelicTree_Expand(sender As Object, e As TreeViewEventArgs) Handles RelicTree.AfterExpand, RelicTree2.AfterExpand
        Dim temp As String = "|" + e.Node.Name
        If e.Node.Name = "Hidden" Then
            If e.Node.Parent IsNot Nothing Then
                temp += e.Node.Parent.Name
            Else
                temp += "|"
            End If
        End If
        If Not My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics += temp
        End If
    End Sub

    Private Sub Relics_Opening(sender As Object, e As EventArgs) Handles Me.Shown
        Me.Location = My.Settings.RelicWinLoc
        If My.Settings.TreeOne Then
            RelicTree2.Visible = False
            RelicTree.Select()
            Label2.Text = "Relic Eras"
        Else
            RelicTree.Visible = False
            RelicTree2.Select()
            Label2.Text = "All Relics"
        End If
        SortSelection.SelectedIndex = My.Settings.SortType
    End Sub

    Private Sub RelicTree_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawTreeNodeEventArgs) Handles RelicTree.DrawNode, RelicTree2.DrawNode

        e.DrawDefault = True
        If e.Bounds.Width = 0 Then
            Return
        End If
        Dim fullPath As String = e.Node.FullPath.Replace("Hidden\", "")
        If sender.Equals(RelicTree2) Then
            fullPath = ReplaceFirst(fullPath, " ", "|")

        End If
        Dim split As String() = fullPath.Split(New Char() {CChar("\"), CChar("|")})

        ' Write Plat + Ducat values

        Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Far
        }
        If split.Count = 2 Then
            Dim find As JObject = Nothing
            If relic_data.TryGetValue(split(0), find) Then
                If find.TryGetValue(split(1), find) Then
                    If find("vaulted") Then
                        Dim left As Integer = 115
                        If fullPath.Contains("|") Then
                            If e.Node.FullPath.Contains("Meso") Then
                                left = 131
                            ElseIf e.Node.FullPath.Contains("Axi") Then
                                left = 118
                            ElseIf e.Node.FullPath.Contains("Lith") Then
                                left = 123
                            Else
                                left = 122
                            End If
                            If e.Node.FullPath.Contains("Hidden") Then
                                left += 20
                            End If

                        ElseIf e.Node.FullPath.Contains("Hidden") Then
                            left = 135
                        End If
                        e.Graphics.DrawString("Vaulted", RelicTree.Font, stealthBrush, left, e.Bounds.Top + 1, sf)
                    End If

                    'Double.Parse(ducat_plat(name)("plat"))
                    Dim itot As Double = find("int")
                    Dim rtot As Double = find("rad")

                    Dim rtot_str As String = rtot.ToString("N1")
                    If rtot > 0 Then
                        rtot_str = "+" + rtot_str
                    End If
                    e.Graphics.DrawString("RAD:", RelicTree.Font, stealthBrush, 255, e.Bounds.Top, sf)
                    e.Graphics.DrawString(rtot_str, RelicTree.Font, stealthBrush, 300, e.Bounds.Top, sf)
                    e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                    e.Graphics.DrawString("INT:", RelicTree.Font, stealthBrush, 350, e.Bounds.Top, sf)
                    e.Graphics.DrawString(itot.ToString("N1"), RelicTree.Font, stealthBrush, 385, e.Bounds.Top, sf)
                    e.Graphics.DrawImage(My.Resources.plat, 385, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                End If
            End If

        ElseIf split.Count = 3 Then
            ' Common - #cd7f32
            ' Uncommon - #c0c0c0
            ' Rare - #ffd700
            Dim brush As Brush = rareBrush
            If e.Node.Index > 2 Then
                brush = commonBrush
            ElseIf e.Node.Index > 0 Then
                brush = uncommonBrush
            End If
            Dim name As String = split(2)
            If name <> "Forma Blueprint" Then
                Dim vals As JObject = GetMarketData(name)
                e.Graphics.DrawString(Double.Parse(vals("plat")).ToString("N1"), RelicTree.Font, brush, 300, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                e.Graphics.DrawString(vals("ducats"), RelicTree.Font, brush, 370, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.ducat_w, 370, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
            End If
        End If
    End Sub

    Private Sub HideMenu_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles HideMenu.ItemClicked
        Dim split As String() = RelicToHide.FullPath.Replace("Hidden\", "").Split("\")
        Dim arr As JArray = hidden_nodes(split(0))

        If RelicToHide.FullPath.Contains("Hidden") Then
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Parent.Nodes.Add(RelicToHide)

            Relic2ToHide.Parent.Nodes.Remove(Relic2ToHide)
            RelicTree2.Nodes.Add(Relic2ToHide)
            Dim tok As JToken = arr.SelectToken("$[?(@ == '" + split(1) + "')]")
            arr.Remove(tok)
        Else
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Nodes.Find("Hidden", False)(0).Nodes.Add(RelicToHide)

            RelicTree2.Nodes.Remove(Relic2ToHide)
            RelicTree2.Nodes.Find("Hidden", False)(0).Nodes.Add(Relic2ToHide)
            arr.Add(split(1))
        End If
        File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Private Sub RelicTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles RelicTree.NodeMouseClick, RelicTree2.NodeMouseClick
        If e.Button <> MouseButtons.Right Then
            RelicToHide = Nothing
            Relic2ToHide = Nothing
            Return
        End If
        If sender.Equals(RelicTree) Then
            RelicToHide = e.Node
            Dim era As String = e.Node.FullPath.Split("\")(0)
            For Each node As TreeNode In RelicTree2.Nodes.Find(e.Node.Name, True)
                If node.FullPath.Contains(era) Then
                    Relic2ToHide = node
                    Exit For
                End If
            Next
        Else
            Relic2ToHide = e.Node
            Dim era As String = e.Node.Text.Split(" ")(0)
            For Each node As TreeNode In RelicTree.Nodes.Find(e.Node.Name, True)
                If node.FullPath.Contains(era) Then
                    RelicToHide = node
                    Exit For
                End If
            Next
        End If

        HideMenu.Items.Clear()
        If e.Node IsNot Nothing AndAlso e.Node.Name.Length = 2 Then
            If e.Node.FullPath.Contains("Hidden") Then
                HideMenu.Items.Add("Show").ForeColor = textColor
                HideMenu.Show(RelicTree, e.Location)

            Else
                HideMenu.Items.Add("Hide").ForeColor = textColor
                HideMenu.Show(RelicTree, e.Location)

            End If
        End If
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        RelicTree2.Visible = My.Settings.TreeOne
        My.Settings.TreeOne = Not My.Settings.TreeOne
        RelicTree.Visible = My.Settings.TreeOne
        If My.Settings.TreeOne Then
            RelicTree.Select()
            Label2.Text = "Relic Eras"
        Else
            RelicTree2.Select()
            Label2.Text = "All Relics"
        End If
    End Sub

    Private Sub SortSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortSelection.SelectedIndexChanged
        My.Settings.SortType = SortSelection.SelectedIndex
        Tree1Sorter.type = My.Settings.SortType
        Tree2Sorter.type = My.Settings.SortType
        RelicTree.Sort()
        RelicTree2.Sort()
        If RelicTree.Visible Then
            RelicTree.Select()
        Else
            RelicTree2.Select()
        End If

    End Sub
End Class

Public Class NodeSorter
    Implements IComparer
    Dim relic As Integer = 0
    Public type As Integer = 0 ' 0: Name, 1: Intact Plat, 2: Rad Bonus
    Dim eras As String() = {"Lith", "Meso", "Neo", "Axi"}

    Public Sub New(x As Integer)
        Me.relic = x
    End Sub

    Private Function IComparer_Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim tx As TreeNode = x
        Dim ty As TreeNode = y
        Dim strx As String = tx.Text
        Dim stry As String = ty.Text
        Dim erax As String = ""
        Dim eray As String = ""
        If strx.Contains("Prime") OrElse strx.Contains("Forma") OrElse stry.Contains("Prime") OrElse stry.Contains("Forma") Then
            Return tx.Index - ty.Index
        End If

        If Me.relic = 0 Then
            If strx.Length = 4 OrElse strx.Length = 3 Then
                ' Lith < Meso < Neo < Axi
                Dim relics As String() = {"Lith", "Meso", "Neo", "Axi"}
                Return Array.IndexOf(relics, strx) - Array.IndexOf(relics, stry)
            ElseIf strx.Length <> stry.Length Then
                Return strx.Length - stry.Length
            End If
            If tx.Parent Is Nothing OrElse ty.Parent Is Nothing Then
                If Me.type > 0 Then
                    If tx.Parent IsNot Nothing Then
                        erax = tx.Parent.Text
                        If erax = "Hidden" Then
                            erax = tx.Parent.Parent.Text
                        End If
                        eray = erax
                    ElseIf ty.Parent IsNot Nothing Then
                        eray = ty.Parent.Text
                        If eray = "Hidden" Then
                            eray = ty.Parent.Parent.Text
                        End If
                        erax = eray
                    Else
                        Return 0
                    End If
                End If
            Else
                erax = tx.Parent.Text
                If erax = "Hidden" Then
                    erax = tx.Parent.Parent.Text
                End If
                eray = ty.Parent.Text
                If eray = "Hidden" Then
                    eray = ty.Parent.Parent.Text
                End If
            End If
        ElseIf Me.relic = 1 Then
            Dim splitx As String() = strx.Split(" ")
            Dim splity As String() = stry.Split(" ")
            If splitx.Length = 1 Or splity.Length = 1 Then
                ' If one is "Hidden"
                Return splity.Length - splitx.Length
            ElseIf Me.type = 0 AndAlso splitx(0) <> splity(0) Then
                ' If the Eras are different 
                '    AND the sort type is name
                Return Array.IndexOf(eras, splitx(0)) - Array.IndexOf(eras, splity(0))
            End If
            strx = splitx(1)
            stry = splity(1)
            erax = splitx(0)
            eray = splity(0)
        End If
        If Me.type <= 0 Then
            Return String.Compare(strx, stry)
        End If

        Dim jobx As JObject = relic_data(erax)(strx)
        Dim joby As JObject = relic_data(eray)(stry)
        If Me.type = 1 Then
            Return (Double.Parse(joby("int")) - Double.Parse(jobx("int"))) * 100
        End If
        Return (Double.Parse(joby("rad")) - Double.Parse(jobx("rad"))) * 100
    End Function
End Class