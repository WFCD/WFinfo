Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class Relics
    Private drag As Boolean = False
    Private resizing As Boolean = False
    Private mouseX As Integer
    Private mouseY As Integer
    Private RelicToHide As TreeNode = Nothing
    Private Relic2ToHide As TreeNode = Nothing
    Public Tree1Sorter As New NodeSorter(0)
    Public Tree2Sorter As New NodeSorter(1)
    Public Tree3Sorter As New NodeSorter(0)
    Private hidden_nodes As JObject                                      ' Contains list of nodes to hide                {"Lith": ["A1","A2",...], "Meso": [...], "Neo": [...], "Axi": [...]}
    Private hidden_file_path As String = Path.Combine(appData, "WFInfo\hidden.json")
    Public eras As New List(Of String) From {"Lith", "Meso", "Neo", "Axi"}

    ' Drag n Drop code

    Private Sub startDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseDown, lbTitle.MouseDown, pbIcon.MouseDown
        drag = True
        mouseX = Cursor.Position.X - Left
        mouseY = Cursor.Position.Y - Top
    End Sub

    Private Sub DRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseMove, lbTitle.MouseMove, pbIcon.MouseMove
        If drag Then
            Top = Cursor.Position.Y - mouseY
            Left = Cursor.Position.X - mouseX
        End If
    End Sub

    Private Sub stopDRAGnDROP(sender As Object, e As MouseEventArgs) Handles pTitle.MouseUp, lbTitle.MouseUp, pbIcon.MouseUp
        drag = False
        My.Settings.RelicWinLoc = Location
    End Sub

    ' Resizing code

    Private Sub startResize(sender As Object, e As EventArgs) Handles BottomResize.MouseDown
        resizing = True
        mouseY = Cursor.Position.Y - Size.Height
    End Sub

    Private Sub contResize(sender As Object, e As EventArgs) Handles BottomResize.MouseMove
        If resizing Then
            Dim newSize As Integer = Cursor.Position.Y - mouseY
            If newSize < 100 Then
                newSize = 100
            End If
            Size = New Size(Size.Width, newSize)
            BottomResize.Location = New Point(BottomResize.Location.X, newSize - 6)
            Panel1.Size = New Size(Panel1.Size.Width, newSize - 26)
            RelicTree1.Size = New Size(RelicTree1.Size.Width, newSize - 57)
            RelicTree2.Size = New Size(RelicTree2.Size.Width, newSize - 57)
            RelicTree3.Size = New Size(RelicTree3.Size.Width, newSize - 57)
        End If

    End Sub

    Private Sub stopResize(sender As Object, e As EventArgs) Handles BottomResize.MouseUp
        resizing = False
    End Sub

    ' Buttons/Checkbox code

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Hide()
    End Sub

    Private Sub btnSwap_Click(sender As Object, e As EventArgs) Handles btnSwap.Click
        RelicTree2.Visible = My.Settings.TreeOne
        My.Settings.TreeOne = Not My.Settings.TreeOne
        RelicTree1.Visible = My.Settings.TreeOne
        lbTitle.Select()
        If My.Settings.TreeOne Then
            Tree3Sorter.relic = 0
            btnSwap.Text = "Relic Eras"
        Else
            Tree3Sorter.relic = 1
            btnSwap.Text = "All Relics"
        End If
        If RelicTree3.Visible Then
            UpdateRelicTree3(current_filters)
        End If
    End Sub

    Private Sub btnCollapse_Click(sender As Object, e As EventArgs) Handles btnCollapse.Click
        lbTitle.Select()
        If RelicTree3.Visible Then
            RelicTree3.Visible = False
            RelicTree3.CollapseAll()
            RelicTree3.Visible = True
            RelicTree3.Nodes(0).EnsureVisible()
        ElseIf RelicTree2.Visible Then
            RelicTree2.Visible = False
            RelicTree2.CollapseAll()
            RelicTree2.Visible = True
            RelicTree2.Nodes(0).EnsureVisible()
        Else
            RelicTree1.Visible = False
            RelicTree1.CollapseAll()
            RelicTree1.Visible = True
            RelicTree1.Nodes(0).EnsureVisible()
        End If
    End Sub

    Private Sub btnExpand_Click(sender As Object, e As EventArgs) Handles btnExpand.Click
        If RelicTree3.Visible Then
            RelicTree3.Visible = False
            RelicTree3.ExpandAll()
            RelicTree3.Visible = True
            RelicTree3.Nodes(0).EnsureVisible()
            RelicTree3.Select()
        ElseIf RelicTree2.Visible Then
            RelicTree2.Visible = False
            RelicTree2.ExpandAll()
            RelicTree2.Visible = True
            RelicTree2.Nodes(0).EnsureVisible()
            RelicTree2.Select()
        Else
            RelicTree1.Visible = False
            RelicTree1.ExpandAll()
            RelicTree1.Visible = True
            RelicTree1.Nodes(0).EnsureVisible()
            RelicTree1.Select()
        End If
    End Sub

    Private Sub VaultCheck_CheckedChanged(sender As Object, e As EventArgs) Handles VaultCheck.CheckedChanged
        If Me.Visible Then
            If VaultCheck.Checked Then
                Load_Relic_Tree()
            Else
                Remove_Vaulted()
            End If

            If RelicTree3.Visible Then
                UpdateRelicTree3(current_filters)
            End If
        End If
    End Sub

    ' Hide/Show code

    Private Sub HideMenu_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles HideMenu.ItemClicked
        Dim split As String() = RelicToHide.FullPath.Replace("Hidden\", "").Split("\")
        Dim arr As JArray = hidden_nodes(split(0))

        If RelicToHide.FullPath.Contains("Hidden") Then
            Dim hiddenNode As TreeNode = RelicToHide.Parent
            hiddenNode.Nodes.Remove(RelicToHide)
            hiddenNode.Parent.Nodes.Add(RelicToHide)

            If hiddenNode.Nodes.Count = 0 Then
                hiddenNode.Parent.Nodes.Remove(hiddenNode)
            End If

            hiddenNode = Relic2ToHide.Parent
            hiddenNode.Nodes.Remove(Relic2ToHide)
            RelicTree2.Nodes.Add(Relic2ToHide)
            If hiddenNode.Nodes.Count = 0 Then
                hiddenNode.Parent.Nodes.Remove(hiddenNode)
            End If
            Dim tok As JToken = arr.SelectToken("$[?(@ == '" + split(1) + "')]")
            arr.Remove(tok)
        Else
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            GetHiddenNode(parent.Nodes).Nodes.Add(RelicToHide)

            RelicTree2.Nodes.Remove(Relic2ToHide)
            GetHiddenNode(RelicTree2.Nodes).Nodes.Add(Relic2ToHide)
            arr.Add(split(1))
        End If
        File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Formatting.Indented))
    End Sub

    Private Function GetHiddenNode(nodes As TreeNodeCollection) As TreeNode
        Dim foundNodes As TreeNode() = nodes.Find("Hidden", False)
        If foundNodes.Length > 0 Then
            Return foundNodes(0)
        End If

        Dim hiddenNode As TreeNode = nodes.Add("Hidden")
        hiddenNode.Name = "Hidden"
        CheckIfExpand(hiddenNode)
        Return hiddenNode
    End Function

    Private Sub RelicTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles RelicTree1.NodeMouseClick, RelicTree2.NodeMouseClick
        If e.Button <> MouseButtons.Right Then
            RelicToHide = Nothing
            Relic2ToHide = Nothing
            sender.SelectedNode = Nothing
            Return
        End If
        e.Node.TreeView.SelectedNode = e.Node

        Dim fullPath As String = e.Node.FullPath.Replace("Hidden\", "")
        If sender.Equals(RelicTree2) Then
            fullPath = ReplaceFirst(fullPath, " ", "|")

        End If
        Dim split As String() = fullPath.Split(New Char() {CChar("\"), CChar("|")})

        ' Write Plat + Ducat values
        Dim sf As New StringFormat With {.Alignment = StringAlignment.Far}
        If split.Count = 2 Then
            Dim find As JObject = Nothing
            If db.relic_data.TryGetValue(split(0), find) Then
                If find.TryGetValue(split(1), find) Then
                    If Not find("vaulted").ToObject(Of Boolean) Then
                        Return
                    End If
                End If
            End If
        End If

        If RelicTree1.Visible Then
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
            For Each node As TreeNode In RelicTree1.Nodes.Find(e.Node.Name, True)
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
                HideMenu.Show(RelicTree1, e.Location)
            Else
                HideMenu.Items.Add("Hide").ForeColor = textColor
                HideMenu.Show(RelicTree1, e.Location)
            End If
        End If
    End Sub

    ' Startup code

    Private Sub Relics_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Location = My.Settings.RelicWinLoc
        If Location.X = 0 And Location.Y = 0 Then
            Location = New Point(Main.Location.X + Main.Width + 25, Main.Location.Y)
        End If
        If My.Settings.TreeOne Then
            Tree3Sorter.relic = 0
            RelicTree2.Visible = False
            btnSwap.Text = "Relic Eras"
            RelicTree2.Select()
        Else
            Tree3Sorter.relic = 1
            RelicTree1.Visible = False
            btnSwap.Text = "All Relics"
            RelicTree1.Select()
        End If
        SortSelection.SelectedIndex = My.Settings.SortType
    End Sub

    Private Sub Relics_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Visible And Not IsWindowMoveable(Me) Then
            Dim scr As Screen = GetMainScreen()
            Location = New Point(scr.WorkingArea.X + 200, scr.WorkingArea.Y + 200)
        End If

        If FilterText.Text <> "Filter Terms..." Then
            FilterText.Select()
        End If
        VaultCheck.Checked = True
    End Sub

    ' TreeView Edit/Modification code

    Public Sub Load_Relic_Tree()
        Dim hide As TreeNode = Nothing
        For Each node As TreeNode In RelicTree1.Nodes
            CheckIfExpand(node)

            For Each relic As JProperty In db.relic_data(node.Text)
                Dim kids As TreeNode() = node.Nodes.Find(relic.Name, True)
                If kids.Length = 0 Then
                    Dim kid As New TreeNode(relic.Name)
                    kid.Name = relic.Name
                    node.Nodes.Add(kid)

                    kid.Nodes.Add(relic.Value("rare1").ToString()).ForeColor = rareColor
                    kid.Nodes.Add(relic.Value("uncommon1").ToString()).ForeColor = uncommonColor
                    kid.Nodes.Add(relic.Value("uncommon2").ToString()).ForeColor = uncommonColor
                    kid.Nodes.Add(relic.Value("common1").ToString()).ForeColor = commonColor
                    kid.Nodes.Add(relic.Value("common2").ToString()).ForeColor = commonColor
                    kid.Nodes.Add(relic.Value("common3").ToString()).ForeColor = commonColor
                    Dim rtot As Double = 0
                    Dim itot As Double = 0
                    Dim rperc As Double = 0.1
                    Dim iperc As Double = 0.02
                    Dim count As Integer = 0
                    For Each temp As TreeNode In kid.Nodes
                        If temp.Parent.FullPath = kid.FullPath Then
                            Try
                                If count > 2 Then
                                    rperc = 0.5 / 3.0
                                    iperc = 0.76 / 3.0
                                ElseIf count > 0 Then
                                    rperc = 0.2
                                    iperc = 0.11
                                End If
                                Dim plat As Double = Double.Parse(db.market_data(temp.Text)("plat"), culture)
                                rtot += (plat * rperc)
                                itot += (plat * iperc)
                                count += 1
                            Catch ex As Exception
                                If db.market_data.TryGetValue(temp.Text, Nothing) Then
                                    Main.addLog("UNKNOWN ERROR: " + temp.Text + " -- " + db.market_data(temp.Text)("plat").ToString() + "\n")
                                    Main.addLog(ex.ToString())
                                Else
                                    Main.addLog("MISSING RELIC PLAT VALUES: " + temp.FullPath + " -- " + temp.Text)
                                End If
                            End Try
                        End If
                    Next
                    db.relic_data(node.Text)(relic.Name)("rad") = rtot
                    db.relic_data(node.Text)(relic.Name)("int") = itot
                    db.relic_data(node.Text)(relic.Name)("diff") = rtot - itot
                    CheckIfExpand(kid)

                    kid = kid.Clone()
                    kid.Text = node.Text + " " + relic.Name
                    RelicTree2.Nodes.Add(kid)
                    CheckIfExpand(kid)
                End If
            Next
        Next

        Load_Hidden_Nodes()

        RelicTree1.TreeViewNodeSorter = Tree1Sorter
        RelicTree2.TreeViewNodeSorter = Tree2Sorter
        RelicTree3.TreeViewNodeSorter = Tree3Sorter
        RelicTree1.Sort()
        RelicTree2.Sort()
        RelicTree3.Sort()
    End Sub

    Private Sub CheckIfExpand(node As TreeNode)
        Dim temp As String = "|" + node.FullPath.Replace("\", " ") + "|"
        If node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If My.Settings.ExpandedRelics.Contains(temp) Then
            node.Expand()
        End If
    End Sub

    Private Sub Load_Hidden_Nodes()
        If Not File.Exists(hidden_file_path) Then
            hidden_nodes = New JObject()
            For Each era As String In eras
                Dim jar As New JArray()
                For Each kvp As KeyValuePair(Of String, JToken) In db.relic_data(era).ToObject(Of JObject)
                    If kvp.Value.Item("vaulted").ToObject(Of Boolean) Then
                        jar.Add(kvp.Key)
                    End If
                Next
                hidden_nodes(era) = jar
            Next
            File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Formatting.Indented))
        Else
            hidden_nodes = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(hidden_file_path))
        End If

        For Each node As TreeNode In RelicTree1.Nodes
            For Each hide As JValue In hidden_nodes(node.Text)
                Dim foundNodes As TreeNode() = node.Nodes.Find(hide.Value, False)
                If foundNodes.Length > 0 Then
                    Dim move As TreeNode = foundNodes(0)
                    Dim job As JObject = Nothing
                    If db.relic_data.TryGetValue(move.Parent.Text, job) Then
                        If job.TryGetValue(move.Text, job) Then
                            If Not job("vaulted").ToObject(Of Boolean) Then
                                Continue For
                            End If
                        End If
                    End If

                    node.Nodes.Remove(move)
                    GetHiddenNode(node.Nodes).Nodes.Add(move)

                    For Each found As TreeNode In RelicTree2.Nodes.Find(hide.Value, False)
                        If found.Text.Equals(node.Text + " " + hide.Value) Then
                            RelicTree2.Nodes.Remove(found)
                            GetHiddenNode(RelicTree2.Nodes).Nodes.Add(found)
                        End If
                    Next
                End If
            Next
        Next
    End Sub

    Public Sub Reload_Data()
        ' Find any missing Relics
        Load_Relic_Tree()

        ' Update all rad/int values
        For Each era As KeyValuePair(Of String, JToken) In db.relic_data
            If Not era.Key.Contains("timestamp") Then
                For Each relic As KeyValuePair(Of String, JToken) In era.Value.ToObject(Of JObject)
                    Dim rtot As Double = 0
                    Dim itot As Double = 0
                    Dim rperc As Double = 0.1
                    Dim iperc As Double = 0.02
                    Dim count As Integer = 0
                    For Each part As KeyValuePair(Of String, JToken) In relic.Value.ToObject(Of JObject)
                        If Not part.Key.Contains("vaulted") AndAlso part.Key.Length > 4 Then
                            Dim job As JObject = Nothing
                            If db.market_data.TryGetValue(part.Value, job) Then
                                Dim plat As Double = Double.Parse(job("plat"), culture)
                                rtot += (plat * rperc)
                                itot += (plat * iperc)
                            Else
                                Main.addLog("MISSING RELIC PLAT VALUES: " + era.Key & " " & relic.Key & " - " & part.Key & ": " & part.Value.ToString())
                            End If
                        End If
                    Next
                    db.relic_data(era.Key)(relic.Key)("rad") = rtot
                    db.relic_data(era.Key)(relic.Key)("int") = itot
                    db.relic_data(era.Key)(relic.Key)("diff") = rtot - itot
                Next
            End If
        Next
    End Sub

    Public Sub Remove_Vaulted()
        For Each era As KeyValuePair(Of String, JToken) In db.relic_data
            If Not era.Key.Contains("timestamp") Then
                For Each relic As KeyValuePair(Of String, JToken) In era.Value.ToObject(Of JObject)
                    If relic.Value("vaulted") Then
                        For Each node As TreeNode In RelicTree1.Nodes.Find(relic.Key, True)
                            If node.FullPath.Contains(era.Key) Then
                                node.Remove()
                                Exit For
                            End If
                        Next
                        For Each node As TreeNode In RelicTree2.Nodes.Find(relic.Key, True)
                            If node.FullPath.Contains(era.Key) Then
                                node.Remove()
                                Exit For
                            End If
                        Next
                    End If
                Next
            End If
        Next

        For Each hide As TreeNode In RelicTree1.Nodes.Find("Hidden", True)
            If hide.Nodes.Count > 0 Then
                For Each err As TreeNode In hide.Nodes
                    Main.addLog("EXTRA NODE WHILE REMOVING VAULTED: " & err.FullPath)
                Next
            End If
            hide.Remove()
        Next
        For Each hide As TreeNode In RelicTree2.Nodes.Find("Hidden", True)
            If hide.Nodes.Count > 0 Then
                For Each err As TreeNode In hide.Nodes
                    Main.addLog("EXTRA NODE WHILE REMOVING VAULTED: " & err.FullPath)
                Next
            End If
            hide.Remove()
        Next
    End Sub

    ' Treeview Event code

    Private Sub RelicTree_Collapse(sender As Object, e As TreeViewEventArgs) Handles RelicTree1.AfterCollapse, RelicTree2.AfterCollapse, RelicTree3.AfterCollapse
        Dim temp As String = "|" + e.Node.FullPath.Replace("\", " ") + "|"
        If e.Node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics = My.Settings.ExpandedRelics.Replace(temp, "")
        End If
        temp = temp.Replace("|", "")
        Dim era_name As String() = temp.Split(" ")
        If era_name.Count = 2 AndAlso era_name(1).Length = 2 Then
            If sender.Equals(RelicTree3) OrElse sender.Equals(RelicTree2) Then
                For Each node As TreeNode In RelicTree1.Nodes.Find(era_name(1), True)
                    If node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                        node.Collapse()
                    End If
                Next
            End If
            If sender.Equals(RelicTree3) OrElse sender.Equals(RelicTree1) Then
                For Each node As TreeNode In RelicTree2.Nodes.Find(era_name(1), True)
                    If node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                        node.Collapse()
                    End If
                Next
            End If
        End If
    End Sub

    Private Sub RelicTree_Expand(sender As Object, e As TreeViewEventArgs) Handles RelicTree1.AfterExpand, RelicTree2.AfterExpand, RelicTree3.AfterExpand
        Dim temp As String = "|" + e.Node.FullPath.Replace("\", " ") + "|"
        If e.Node.Text <> "Hidden" Then
            temp = temp.Replace("Hidden ", "")
        End If
        If Not My.Settings.ExpandedRelics.Contains(temp) Then
            My.Settings.ExpandedRelics += temp
        End If
        temp = temp.Replace("|", "")
        Dim era_name As String() = temp.Split(" ")
        If era_name.Count = 2 AndAlso era_name(1).Length = 2 Then
            If sender.Equals(RelicTree3) OrElse sender.Equals(RelicTree2) Then
                For Each node As TreeNode In RelicTree1.Nodes.Find(era_name(1), True)
                    If Not node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                        node.Expand()
                    End If
                Next
            End If
            If sender.Equals(RelicTree3) OrElse sender.Equals(RelicTree1) Then
                For Each node As TreeNode In RelicTree2.Nodes.Find(era_name(1), True)
                    If Not node.IsExpanded AndAlso node.FullPath.Contains(era_name(0)) Then
                        node.Expand()
                    End If
                Next
            End If
        End If
    End Sub

    Private Sub RelicTree_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawTreeNodeEventArgs) Handles RelicTree1.DrawNode, RelicTree2.DrawNode, RelicTree3.DrawNode
        e.DrawDefault = True
        If e.Bounds.IsEmpty OrElse e.Bounds.Left = -1 Then
            Return
        End If
        e.Graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic



        Dim fullPath As String = e.Node.FullPath.Replace("Hidden\", "")
        If RelicTree2.Visible Then
            fullPath = ReplaceFirst(fullPath, " ", "|")
        End If
        Dim split As String() = fullPath.Split(New Char() {CChar("\"), CChar("|")})

        ' Write Plat + Ducat values
        Dim sf As New StringFormat With {.Alignment = StringAlignment.Far}
        If split.Count = 2 Then
            Dim find As JObject = Nothing
            If db.relic_data.TryGetValue(split(0), find) Then
                If find.TryGetValue(split(1), find) Then
                    If find("vaulted") Then
                        e.Graphics.DrawString("Vaulted", tahoma9_bold, stealthBrush, New PointF(e.Bounds.Right, e.Bounds.Y))
                    End If

                    Dim itot As String = CDbl(find("int")).ToString("N1")
                    Dim rtot As String = CDbl(find("rad")).ToString("N1")
                    Dim diff_v As Double = find("diff")
                    Dim diff As String = diff_v.ToString("N1")
                    If diff_v > 0 Then
                        diff = "+" & diff
                    Else
                        diff = ChrW(&H2014) & diff.Substring(1)
                    End If
                    diff = "(" & diff & ")"

                    Using br = New SolidBrush(textColor)
                        e.Graphics.FillRectangle(bgBrush, 200, e.Bounds.Top, 180, e.Bounds.Height)
                        e.Graphics.DrawString("INT", tahoma9_bold, br, 216, e.Bounds.Top, sf)
                        e.Graphics.DrawImage(My.Resources.plat, 217, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                        e.Graphics.DrawString(":", tahoma9_bold, br, 235, e.Bounds.Top, sf)
                        e.Graphics.DrawString(itot, tahoma9_bold, br, 270, e.Bounds.Top, sf)

                        e.Graphics.DrawString("RAD", tahoma9_bold, br, 321, e.Bounds.Top, sf)
                        e.Graphics.DrawImage(My.Resources.plat, 322, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                        e.Graphics.DrawString(":", tahoma9_bold, br, 340, e.Bounds.Top, sf)
                        e.Graphics.DrawString(rtot, tahoma9_bold, br, 375, e.Bounds.Top, sf)
                        e.Graphics.DrawString(diff, tahoma9_bold, br, 375, e.Bounds.Top)
                    End Using
                End If
            End If

        ElseIf split.Count = 3 Then
            ' Common - #cd7f32
            ' Uncommon - #c0c0c0
            ' Rare - #ffd700
            Dim clr As Color = e.Node.ForeColor
            Dim brush As Brush = rareBrush
            If clr = commonColor Then
                brush = commonBrush
            ElseIf clr = uncommonColor Then
                brush = uncommonBrush
            End If
            Dim name As String = split(2)
            If name <> "Forma Blueprint" Then
                Dim vals As JObject = db.market_data(name)
                Using br = New SolidBrush(bgColor)
                    e.Graphics.FillRectangle(br, 260, e.Bounds.Top, 130, e.Bounds.Height)
                End Using
                e.Graphics.DrawString(Double.Parse(vals("plat"), culture).ToString("N1"), tahoma9_bold, brush, 300, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                e.Graphics.DrawString(vals("ducats"), tahoma9_bold, brush, 370, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.ducat_w, 370, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
            End If
            If RelicTree3.Visible AndAlso Not CheckNode(e.Node.Parent, current_filters) AndAlso CheckNode(e.Node, current_filters) Then
                e.DrawDefault = False
                Dim highlights As List(Of Integer()) = CombineSegments(GetMatchedText(e.Node, current_filters))
                Dim loc As Integer = 43
                If RelicTree2.Visible Then
                    loc = 24
                End If
                Dim boundRect As Rectangle = e.Bounds
                Dim textToDraw As String = ""
                If highlights(0)(0) <> 0 Then
                    textToDraw = e.Node.Text.Substring(0, highlights(0)(0))
                    TextRenderer.DrawText(e.Graphics, textToDraw, tahoma9_bold, boundRect, e.Node.ForeColor, e.Node.BackColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
                    boundRect.X += e.Graphics.MeasureString(textToDraw, tahoma9_bold, 9999, StringFormat.GenericTypographic).Width + 2
                    If textToDraw.Trim.Length <> textToDraw.Length Then
                        boundRect.X += 4
                    End If
                End If
                For i As Integer = 0 To highlights.Count - 1
                    Dim seg As Integer() = highlights(i)
                    textToDraw = e.Node.Text.Substring(seg(0), seg(1) - seg(0))
                    TextRenderer.DrawText(e.Graphics, textToDraw, tahoma9_bold, boundRect, e.Node.BackColor, e.Node.ForeColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
                    boundRect.X += e.Graphics.MeasureString(textToDraw, tahoma9_bold, 9999, StringFormat.GenericTypographic).Width + 2

                    textToDraw = e.Node.Text.Substring(seg(1))
                    If i < highlights.Count - 1 Then
                        textToDraw = e.Node.Text.Substring(seg(1), highlights(i + 1)(0) - seg(1))
                    End If
                    TextRenderer.DrawText(e.Graphics, textToDraw, tahoma9_bold, boundRect, e.Node.ForeColor, e.Node.BackColor, TextFormatFlags.Left Or TextFormatFlags.VerticalCenter)
                    boundRect.X += e.Graphics.MeasureString(textToDraw, tahoma9_bold, 9999, StringFormat.GenericTypographic).Width + 1
                    If textToDraw.Trim.Length <> textToDraw.Length Then
                        boundRect.X += 4
                    End If
                Next
            End If
        End If
        Dim left As Integer = 20
        If e.Node.Parent Is Nothing Then
            left = 40
        ElseIf e.Node.Parent.Parent Is Nothing Then
            left = 60
        Else
            left = 80
        End If
        e.Graphics.DrawLine(New Pen(bgColor), left, e.Bounds.Top, 450, e.Bounds.Top)
        e.Graphics.DrawLine(New Pen(bgColor), left, e.Bounds.Bottom, 450, e.Bounds.Bottom)
    End Sub

    ' Treeview Sort code

    Private Sub SortSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortSelection.SelectedIndexChanged
        My.Settings.SortType = SortSelection.SelectedIndex
        Tree1Sorter.type = My.Settings.SortType
        Tree2Sorter.type = My.Settings.SortType
        Tree3Sorter.type = My.Settings.SortType
        RelicTree1.Sort()
        RelicTree2.Sort()
        RelicTree3.Sort()
        lbTitle.Select()

    End Sub

    ' Treeview Filter code

    Private current_filters As String() = Nothing
    Private showAll As Boolean = True

    Private Sub FilterText_Enter(sender As Object, e As EventArgs) Handles FilterText.Enter
        If FilterText.Text = "Filter Terms..." Then
            FilterText.Text = ""
            FilterText.ForeColor = textColor
        End If
    End Sub

    Private Sub RelicTree_MouseEnter(sender As Object, e As EventArgs) Handles RelicTree1.MouseEnter, RelicTree2.MouseEnter, RelicTree3.MouseEnter
        Dim treeCast As TreeView = sender
        treeCast.Select()
        treeCast.SelectedNode = Nothing
    End Sub

    Private Sub FilterText_Exit(sender As Object, e As EventArgs) Handles FilterText.Leave
        If FilterText.Text = "" Then
            FilterText.Text = "Filter Terms..."
            FilterText.ForeColor = stealthColor
        End If
    End Sub

    Private Sub FilterText_TextChanged(sender As Object, e As EventArgs) Handles FilterText.TextChanged
        RelicTree3.Visible = False
        If FilterText.Text.Length <> 0 AndAlso FilterText.Text <> "Filter Terms..." Then
            Dim textTemp As String = FilterText.Text.Trim
            If FilterText.Text.Chars(0) = "!"c Then
                textTemp = textTemp.Substring(1)
                showAll = False
            Else
                showAll = True
            End If
            If textTemp.Length > 0 Then
                current_filters = textTemp.ToLower().Split(" ")
                UpdateRelicTree3(current_filters)
            Else
                RelicTree3.Nodes.Clear()
            End If
        End If
    End Sub

    Private Sub RecursiveGetExpandedNodes(nodes As TreeNodeCollection, ByRef arr As List(Of String))
        For Each kid As TreeNode In nodes
            If kid.IsExpanded Then
                arr.Add(kid.FullPath.Replace("\Hidden", ""))
            End If
            If kid.Nodes.Count > 0 Then
                RecursiveGetExpandedNodes(kid.Nodes, arr)
            End If
        Next
    End Sub

    Private Function GetExpandedNodes(tree As TreeView) As List(Of String)
        Dim ret As New List(Of String)
        RecursiveGetExpandedNodes(tree.Nodes, ret)
        Return ret
    End Function

    Private Sub RecursiveExpandNodes(nodes As TreeNodeCollection, arr As List(Of String))
        For Each kid As TreeNode In nodes
            If arr.Contains(kid.FullPath) Then
                kid.Expand()
            End If
            If kid.Nodes.Count > 0 Then
                RecursiveExpandNodes(kid.Nodes, arr)
            End If
        Next
    End Sub

    Private Sub UpdateRelicTree3(filters As String())
        RelicTree3.Visible = False
        Dim nodes As List(Of String) = Nothing
        If showAll Then
            If RelicTree1.Visible Then
                nodes = GetExpandedNodes(RelicTree1)
            Else
                nodes = GetExpandedNodes(RelicTree2)
            End If
        End If

        RelicTree3.Nodes.Clear()

        If RelicTree1.Visible Then
            Tree3Sorter.relic = 0
            'RelicTree1
            For Each era As TreeNode In RelicTree1.Nodes
                Dim eraNode As TreeNode = Nothing
                For Each node As TreeNode In era.Nodes
                    If node.Text = "Hidden" Then
                        'Parse the hidden list
                        For Each hide As TreeNode In node.Nodes
                            If showAll Then
                                If CheckNodes(hide.Nodes, filters) Then
                                    If eraNode Is Nothing Then
                                        eraNode = New TreeNode(era.Text)
                                        RelicTree3.Nodes.Add(eraNode)
                                    End If
                                    eraNode.Nodes.Add(hide.Clone())
                                End If
                            Else
                                Dim tempNode As TreeNode = Nothing
                                For Each part As TreeNode In hide.Nodes
                                    If CheckNode(part, filters) Then
                                        If eraNode Is Nothing Then
                                            eraNode = New TreeNode(era.Text)
                                            RelicTree3.Nodes.Add(eraNode)
                                        End If
                                        If tempNode Is Nothing Then
                                            tempNode = New TreeNode(hide.Text)
                                            eraNode.Nodes.Add(tempNode)
                                        End If
                                        tempNode.Nodes.Add(part.Text).ForeColor = part.ForeColor
                                    End If
                                Next
                            End If
                        Next

                    Else
                        If showAll Then
                            If CheckNodes(node.Nodes, filters) Then
                                If eraNode Is Nothing Then
                                    eraNode = New TreeNode(era.Text)
                                    RelicTree3.Nodes.Add(eraNode)
                                End If
                                eraNode.Nodes.Add(node.Clone())
                            End If
                        Else
                            Dim tempNode As TreeNode = Nothing
                            For Each part As TreeNode In node.Nodes
                                If CheckNode(part, filters) Then
                                    If eraNode Is Nothing Then
                                        eraNode = New TreeNode(era.Text)
                                        RelicTree3.Nodes.Add(eraNode)
                                    End If
                                    If tempNode Is Nothing Then
                                        tempNode = New TreeNode(node.Text)
                                        eraNode.Nodes.Add(tempNode)
                                    End If
                                    tempNode.Nodes.Add(part.Text).ForeColor = part.ForeColor
                                End If
                            Next
                        End If
                    End If
                Next
            Next
        Else
            Tree3Sorter.relic = 1
            'RelicTree2
            ' {Relics, Hidden:{Relics}}
            For Each node As TreeNode In RelicTree2.Nodes
                If node.Text = "Hidden" Then
                    'Parse the hidden list
                    For Each hide As TreeNode In node.Nodes
                        If showAll Then
                            If CheckNodes(hide.Nodes, filters) Then
                                RelicTree3.Nodes.Add(hide.Clone())
                            End If
                        Else
                            Dim tempNode As TreeNode = Nothing
                            For Each part As TreeNode In hide.Nodes
                                If CheckNode(part, filters) Then
                                    If tempNode Is Nothing Then
                                        tempNode = New TreeNode(hide.Text)
                                        RelicTree3.Nodes.Add(tempNode)
                                    End If
                                    tempNode.Nodes.Add(part.Text).ForeColor = part.ForeColor
                                End If
                            Next
                        End If
                    Next
                Else
                    If showAll Then
                        If CheckNodes(node.Nodes, filters) Then
                            RelicTree3.Nodes.Add(node.Clone())
                        End If
                    Else
                        Dim tempNode As TreeNode = Nothing
                        For Each part As TreeNode In node.Nodes
                            If CheckNode(part, filters) Then
                                If tempNode Is Nothing Then
                                    tempNode = New TreeNode(node.Text)
                                    RelicTree3.Nodes.Add(tempNode)
                                End If
                                tempNode.Nodes.Add(part.Text).ForeColor = part.ForeColor
                            End If
                        Next
                    End If
                End If
            Next
        End If

        RelicTree3.Sort()

        If RelicTree3.Nodes.Count > 0 Then
            If showAll Then
                RecursiveExpandNodes(RelicTree3.Nodes, nodes)
            Else
                RelicTree3.ExpandAll()
            End If
            RelicTree3.Nodes(0).EnsureVisible()
        End If
        RelicTree3.Visible = True
    End Sub

    Private Function CheckNodes(nodes As TreeNodeCollection, filters() As String) As Boolean
        Dim matchesAll As Boolean = True
        For Each node As TreeNode In nodes
            If CheckNode(node, filters) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function CheckNode(node As TreeNode, filters() As String) As Boolean
        Dim checkStr As String = node.FullPath.ToLower()
        For Each filt As String In filters
            If Not checkStr.Contains(filt) Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Function GetMatchedText(node As TreeNode, filters As String()) As List(Of Integer())
        Dim ret As New List(Of Integer())
        Dim checkStr As String = node.Text.ToLower()
        For Each filt As String In filters
            If checkStr.Contains(filt) Then
                Dim first As Integer = checkStr.IndexOf(filt)
                Do
                    Dim last As Integer = first + filt.Length
                    ret.Add({first, last})
                    first = checkStr.IndexOf(filt, last)
                Loop While first <> -1
            End If
        Next
        ret.Sort(Function(x, y) x(0).CompareTo(y(0)))
        Return ret
    End Function

    Private Function CombineSegments(arr As List(Of Integer())) As List(Of Integer())
        Dim ret As New List(Of Integer())
        Dim i As Integer = 0
        While i < arr.Count
            Dim hold As Integer() = arr(i)
            i += 1
            While i < arr.Count AndAlso arr(i)(0) <= hold(1)
                If arr(i)(1) > hold(1) Then
                    hold(1) = arr(i)(1)
                End If
                i += 1
            End While
            ret.Add(hold)
        End While

        Return ret
    End Function

    Private Sub FilterText_EnterKey(sender As Object, e As KeyPressEventArgs) Handles FilterText.KeyPress
        If e.KeyChar = Chr(Keys.Enter) Then
            RelicTree3.Select()
            e.Handled = True
        End If
    End Sub
End Class

Public Class NodeSorter
    Implements IComparer
    Public relic As Integer = 0
    Public type As Integer = 0 ' 0: Name, 1: Intact Plat, 2: Rad Bonus

    Public Sub New(x As Integer)
        relic = x
    End Sub

    Private Function GetFullPath(node As TreeNode) As String
        Dim tempStr As String = node.Text
        Dim tempNode As TreeNode = node.Parent
        While tempNode IsNot Nothing
            tempStr = tempNode.Text & "|" & tempStr
            tempNode = tempNode.Parent
        End While
        Return tempStr
    End Function

    Private Function IComparer_Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim tx As TreeNode = x
        Dim ty As TreeNode = y
        Dim strx As String = tx.Text
        Dim stry As String = ty.Text
        Dim erax As String = ""
        Dim eray As String = ""
        If strx.Contains("Prime") OrElse strx.Contains("Forma") OrElse stry.Contains("Prime") OrElse stry.Contains("Forma") Then
            If tx.ForeColor.G = ty.ForeColor.G Then
                If Not db.market_data.TryGetValue(strx, Nothing) OrElse Not db.market_data.TryGetValue(stry, Nothing) Then
                    Return -1
                End If
                Return db.market_data(stry)("ducats").ToObject(Of Integer) - db.market_data(strx)("ducats").ToObject(Of Integer)
            End If
            Return CInt(ty.ForeColor.G) - CInt(tx.ForeColor.G)
        End If

        If relic = 0 Then
            Dim relic_list As String() = {"Lith", "Meso", "Neo", "Axi"}
            Dim indx = Array.IndexOf(relic_list, strx)
            Dim indy = Array.IndexOf(relic_list, stry)
            If indx <> -1 OrElse indy <> -1 Then
                ' Lith < Meso < Neo < Axi
                Return indx - indy
            ElseIf strx = "Hidden" Then
                Return 100
            ElseIf stry = "Hidden" Then
                Return -100
            End If

            ' ISSUE WITH ADDING NODES DURING SORT
            '   ONLY WITH FILTER SCRIPT
            '   workaround by using whichever parent is not dead as primary
            If tx.Parent Is Nothing OrElse ty.Parent Is Nothing Then
                Return 0
            End If



            erax = tx.Parent.Text
            If erax = "Hidden" Then
                erax = tx.Parent.Parent.Text
            End If

            eray = ty.Parent.Text
            If eray = "Hidden" Then
                eray = ty.Parent.Parent.Text
            End If
        ElseIf relic = 1 Then
            Dim splitx As String() = strx.Split(" ")
            Dim splity As String() = stry.Split(" ")
            If splitx.Length = 1 Or splity.Length = 1 Then
                ' If one is "Hidden"
                Return splity.Length - splitx.Length
            ElseIf type = 0 AndAlso splitx(0) <> splity(0) Then
                ' If the Eras are different 
                '    AND the sort type is name
                Return Relics.eras.IndexOf(splitx(0)) - Relics.eras.IndexOf(splity(0))
            End If
            strx = splitx(1)
            stry = splity(1)
            erax = splitx(0)
            eray = splity(0)
        End If

        Dim jobx As JObject = db.relic_data(erax)(strx)
        Dim joby As JObject = db.relic_data(eray)(stry)
        If type = 1 Then
            Return (Double.Parse(joby("int"), culture) - Double.Parse(jobx("int"), culture)) * 100
        ElseIf type = 2 Then
            Dim resu As Integer = (Double.Parse(joby("rad"), culture) - Double.Parse(jobx("rad"), culture)) * 100
            'Console.WriteLine("Compare " & erax & " " & strx & " to " & eray & " " & stry & ": " & resu)
            Return resu
        ElseIf type = 3 Then
            Return (Double.Parse(joby("diff"), culture) - Double.Parse(jobx("diff"), culture)) * 100
        End If

        ' Fixes S10 & S11 being sorted above S2 through S9
        If strx.Chars(0) = stry.Chars(0) Then
            Dim xNum As Integer = strx.Substring(1)
            Dim yNum As Integer = stry.Substring(1)
            Return xNum - yNum
        End If

        Return Asc(strx.Chars(0)) - Asc(stry.Chars(0))
    End Function
End Class