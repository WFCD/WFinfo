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
    Dim relic_data As JObject
    Dim relic_file_path As String = Path.Combine(Environment.CurrentDirectory, "relic_data.json")
    Dim hidden_nodes As JObject
    Dim hidden_file_path As String = Path.Combine(Environment.CurrentDirectory, "hidden.json")
    Dim textBrush As Brush = New SolidBrush(Color.FromArgb(177, 208, 217))
    Dim stealthBrush As Brush = New SolidBrush(Color.FromArgb(118, 139, 145))
    Dim commonColor As Color = Color.FromArgb(205, 127, 50)
    Dim commonBrush As Brush = New SolidBrush(commonColor)
    Dim uncommonColor As Color = Color.FromArgb(192, 192, 192)
    Dim uncommonBrush As Brush = New SolidBrush(uncommonColor)
    Dim rareColor As Color = Color.FromArgb(255, 215, 0)
    Dim rareBrush As Brush = New SolidBrush(rareColor)
    Dim RelicToHide As TreeNode = Nothing
    ' Common - #cd7f32
    ' Uncommon - #c0c0c0
    ' Rare - #ffd700

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
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Hide()
    End Sub

    Public Sub Load_Relic_Data()
        Dim request As WebRequest = Nothing
        If File.Exists(relic_file_path) Then
            request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
            request.Method = "HEAD"
            ' Move last_mod back one hour, so that it doesn't equal timestamp
            Dim last_mod As Date = DateTime.Parse(request.GetResponse().Headers.Get("Last-Modified")).AddHours(-1)
            Console.WriteLine(last_mod)
            relic_data = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(relic_file_path))
            Dim ignore As JToken = Nothing
            If relic_data.TryGetValue("timestamp", ignore) Then
                Dim timestamp As Date = DateTime.Parse(relic_data("timestamp"))
                If last_mod < timestamp Then
                    Console.WriteLine("FILE IS GOOD")
                    Load_Relic_Tree()
                    Return
                End If
                Console.WriteLine("RELOAD FILE")
            End If
        End If
        relic_data = New JObject()
        request = WebRequest.Create("https://n8k6e2y6.ssl.hwcdn.net/repos/hnfvc0o3jnfvc873njb03enrf56.html")
        Dim response As WebResponse = request.GetResponse()
        relic_data("timestamp") = DateTime.Parse(response.Headers.Get("Last-Modified"))
        Dim drop_data As String = Nothing
        Using reader As New StreamReader(response.GetResponseStream(), System.Text.ASCIIEncoding.ASCII)
            drop_data = reader.ReadToEnd()
        End Using

        Dim first As Integer = drop_data.IndexOf("id=""relicRewards""")
        first = drop_data.IndexOf("<table>", first)
        Dim last As Integer = drop_data.IndexOf("</table>", first)
        Dim index As Integer = drop_data.IndexOf("<tr>", first)
        Dim tr_stop As Integer = 0
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            If sub_str.Contains("Relic") AndAlso sub_str.Contains("Intact") Then
                sub_str = Regex.Replace(sub_str, "<[^>]+>|\([^\)]+\)", "")
                Dim split As String() = sub_str.Split(" ")
                Dim era As String = split(0)
                Dim relic As String = split(1)
                Dim ignore As JObject = Nothing
                If Not relic_data.TryGetValue(era, ignore) Then
                    relic_data(era) = New JObject()
                End If
                relic_data(era)(relic) = New JObject()
                relic_data(era)(relic)("vaulted") = True
                Dim cmnNum As Integer = 1
                Dim uncNum As Integer = 1
                index = drop_data.IndexOf("<tr", tr_stop)
                tr_stop = drop_data.IndexOf("</tr>", index)
                sub_str = drop_data.Substring(index, tr_stop - index)
                While Not sub_str.Contains("blank-row")
                    sub_str = sub_str.Replace("<tr><td>", "").Replace("</td>", "").Replace("td>", "")
                    split = sub_str.Split("<")
                    If split(1).Contains("2.") Then
                        relic_data(era)(relic)("rare1") = split(0)
                    ElseIf split(1).Contains("11") Then
                        relic_data(era)(relic)("uncommon" + uncNum.ToString()) = split(0)
                        uncNum += 1
                    Else
                        relic_data(era)(relic)("common" + cmnNum.ToString()) = split(0)
                        cmnNum += 1
                    End If

                    index = drop_data.IndexOf("<tr", tr_stop)
                    tr_stop = drop_data.IndexOf("</tr>", index)
                    sub_str = drop_data.Substring(index, tr_stop - index)
                End While

            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Missions
        last = drop_data.IndexOf("id=""relicRewards""")
        index = drop_data.IndexOf("<tr>")
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Dim split As String() = sub_str.Split(" ")
                relic_data(split(0))(split(1))("vaulted") = False
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While

        ' Find NOT Vauled Relics in Special Rewards
        last = drop_data.IndexOf("id=""modByAvatar""")
        index = drop_data.IndexOf("id=""keyRewards""")
        index = drop_data.IndexOf("<tr>", index)
        While index < last AndAlso index <> -1
            tr_stop = drop_data.IndexOf("</tr>", index)
            Dim sub_str As String = drop_data.Substring(index, tr_stop - index)
            index = sub_str.IndexOf("Relic")
            If index <> -1 Then
                sub_str = sub_str.Substring(0, index - 1)
                index = sub_str.LastIndexOf(">") + 1
                sub_str = sub_str.Substring(index)
                Console.WriteLine("--" + sub_str + "--")
                Dim split As String() = sub_str.Split(" ")
                Dim ignore As JToken = Nothing
                If relic_data.TryGetValue(split(0), ignore) Then
                    relic_data(split(0))(split(1))("vaulted") = False
                End If
            End If
            index = drop_data.IndexOf("<tr>", tr_stop)
        End While



        File.WriteAllText(relic_file_path, JsonConvert.SerializeObject(relic_data, Newtonsoft.Json.Formatting.Indented))

        Load_Relic_Tree()
    End Sub

    Private Sub Load_Relic_Tree()

        For Each node As TreeNode In RelicTree.Nodes
            For Each relic As JProperty In relic_data(node.Text)
                Dim kid As New TreeNode(relic.Name)
                kid.Name = relic.Name
                node.Nodes.Add(kid)

                kid.Nodes.Add(relic.Value("rare1").ToString()).ForeColor = rareColor
                kid.Nodes.Add(relic.Value("uncommon1").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("uncommon2").ToString()).ForeColor = uncommonColor
                kid.Nodes.Add(relic.Value("common1").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common2").ToString()).ForeColor = commonColor
                kid.Nodes.Add(relic.Value("common3").ToString()).ForeColor = commonColor
                For Each temp As TreeNode In kid.Nodes
                    If temp.Parent.FullPath = kid.FullPath Then
                        If Not Glob.ducat_plat.TryGetValue(temp.Text, Nothing) And temp.Text <> "Forma Blueprint" Then
                            If temp.Text.Contains("Kavasa") Then
                                If temp.Text.Contains("Kubrow") Then
                                    temp.Text = temp.Text.Replace("Kubrow ", "")
                                Else
                                    temp.Text = temp.Text.Replace("Prime", "Prime Collar")
                                End If
                            Else
                                temp.Text = temp.Text.Replace(" Blueprint", "")
                            End If
                            If Not Glob.ducat_plat.TryGetValue(temp.Text, Nothing) Then
                                Console.WriteLine(temp.FullPath + " -- " + temp.Text)
                            End If
                        End If
                    End If
                Next
            Next
            node.Nodes.Add("Hidden").Name = "Hidden"
        Next

        Load_Hidden_Nodes()

        RelicTree.TreeViewNodeSorter = New NodeSorter()
        RelicTree.Sort()
    End Sub

    Private Sub Load_Hidden_Nodes()
        If File.Exists(hidden_file_path) Then
            hidden_nodes = JsonConvert.DeserializeObject(Of JObject)(File.ReadAllText(hidden_file_path))

            For Each node As TreeNode In RelicTree.Nodes
                Console.WriteLine(node.FullPath)
                For Each hide As JValue In hidden_nodes(node.Text)
                    Dim move As TreeNode = node.Nodes.Find(hide.Value, False)(0)
                    node.Nodes.Remove(move)
                    node.Nodes.Find("Hidden", False)(0).Nodes.Add(move)
                Next
            Next
        Else
            hidden_nodes = New JObject()
            hidden_nodes("Lith") = New JArray()
            hidden_nodes("Meso") = New JArray()
            hidden_nodes("Neo") = New JArray()
            hidden_nodes("Axi") = New JArray()
            File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
        End If
    End Sub

    Private Sub Relics_Opened(sender As Object, e As EventArgs) Handles Me.Shown
        '_________________________________________________________________________
        ' Loads Nodes for TreeView
        '_________________________________________________________________________


    End Sub

    Private Sub TreeView1_AfterSelect(sender As Object, e As TreeViewEventArgs)

    End Sub

    Private Sub RelicTree_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawTreeNodeEventArgs) Handles RelicTree.DrawNode

        e.DrawDefault = True
        If e.Bounds.Width = 0 Then
            Return
        End If
        Dim fullPath As String = e.Node.FullPath.Replace("Hidden\", "")
        Dim split As String() = fullPath.Split("\")

        ' Write Plat + Ducat values

        Dim sf As New StringFormat With {
            .Alignment = StringAlignment.Far
        }
        If split.Count = 2 Then
            Dim find As JObject = Nothing
            If relic_data.TryGetValue(split(0), find) Then
                If find.TryGetValue(split(1), find) Then
                    If find("vaulted") Then
                        If e.Node.FullPath.Contains("Hidden") Then
                            e.Graphics.DrawString("Vaulted", RelicTree.Font, stealthBrush, 135, e.Bounds.Top + 1, sf)
                        Else
                            e.Graphics.DrawString("Vaulted", RelicTree.Font, stealthBrush, 115, e.Bounds.Top + 1, sf)
                        End If
                    End If

                    'Double.Parse(Glob.ducat_plat(name)("plat"))
                    Dim rtot As Double = 0
                    Dim itot As Double = 0
                    Dim rperc As Double = 0.1
                    Dim iperc As Double = 0.02
                    Dim count As Integer = 0
                    For Each node As TreeNode In e.Node.Nodes
                        If Glob.ducat_plat.TryGetValue(node.Text, Nothing) Then
                            If count > 2 Then
                                rperc = 0.5 / 3.0
                                iperc = 0.76 / 3.0
                            ElseIf count > 0 Then
                                rperc = 0.2
                                iperc = 0.11
                            End If
                            Dim plat As Double = Double.Parse(ducat_plat(node.Text)("plat"))
                            rtot += plat * rperc
                            itot += plat * iperc
                            count += 1
                        End If
                    Next
                    e.Graphics.DrawString("RAD:", RelicTree.Font, stealthBrush, 265, e.Bounds.Top, sf)
                    e.Graphics.DrawString(rtot.ToString("N1"), RelicTree.Font, stealthBrush, 300, e.Bounds.Top, sf)
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
                e.Graphics.DrawString(Double.Parse(Glob.ducat_plat(name)("plat")).ToString("N1"), RelicTree.Font, brush, 300, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.plat, 300, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
                e.Graphics.DrawString(Glob.ducat_plat(name)("ducats"), RelicTree.Font, brush, 370, e.Bounds.Top + 1, sf)
                e.Graphics.DrawImage(My.Resources.ducat_w, 370, e.Bounds.Top + 2, e.Bounds.Height - 4, e.Bounds.Height - 4)
            End If
        End If
    End Sub

    Private Sub EraSelect_DrawItem(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DrawItemEventArgs)
        Dim index As Integer = e.Index
        If index < 0 Then
            index = 0
        End If

        Dim brush As Brush = New SolidBrush(Color.FromArgb(177, 208, 217))
        If e.State.HasFlag(DrawItemState.Selected) AndAlso Not e.State.HasFlag(DrawItemState.ComboBoxEdit) Then
            e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(60, 60, 60)), e.Bounds)
        Else
            e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(40, 40, 40)), e.Bounds)
        End If
        'e.Graphics.DrawString(EraSelect.Items(index).ToString(), e.Font, brush, e.Bounds, StringFormat.GenericDefault)
    End Sub

    Private Sub HideMenu_Click(sender As Object, e As ToolStripItemClickedEventArgs) Handles HideMenu.ItemClicked
        Dim split As String() = RelicToHide.FullPath.Replace("Hidden\", "").Split("\")
        Console.WriteLine(split(0) + "--" + split(1))
        Dim arr As JArray = hidden_nodes(split(0))

        If RelicToHide.FullPath.Contains("Hidden") Then
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Parent.Nodes.Add(RelicToHide)
            Dim tok As JToken = arr.SelectToken("$[?(@ == '" + split(1) + "')]")
            arr.Remove(tok)
        Else
            Dim parent As TreeNode = RelicToHide.Parent
            parent.Nodes.Remove(RelicToHide)
            parent.Nodes.Find("Hidden", False)(0).Nodes.Add(RelicToHide)
            arr.Add(split(1))
        End If
        File.WriteAllText(hidden_file_path, JsonConvert.SerializeObject(hidden_nodes, Newtonsoft.Json.Formatting.Indented))
    End Sub

    Private Sub RelicTree_Click(sender As Object, e As TreeNodeMouseClickEventArgs) Handles RelicTree.NodeMouseClick
        If e.Button <> MouseButtons.Right Then
            Console.WriteLine(e.Button)
            Return
        End If
        RelicToHide = e.Node

        HideMenu.Items.Clear()
        If RelicToHide IsNot Nothing AndAlso RelicToHide.Name.Length = 2 Then
            If RelicToHide.FullPath.Contains("Hidden") Then
                Console.WriteLine("SHOW " + RelicToHide.FullPath)
                HideMenu.Items.Add("Show").ForeColor = Color.FromArgb(177, 208, 217)
                HideMenu.Show(RelicTree, e.Location)

            Else
                Console.WriteLine("HIDE " + RelicToHide.FullPath)
                HideMenu.Items.Add("Hide").ForeColor = Color.FromArgb(177, 208, 217)
                HideMenu.Show(RelicTree, e.Location)

            End If
        End If
    End Sub
End Class

Public Class NodeSorter
    Implements IComparer

    Private Function IComparer_Compare(x As Object, y As Object) As Integer Implements IComparer.Compare
        Dim tx As TreeNode = x
        Dim ty As TreeNode = y

        If tx.Text.Length = 4 OrElse tx.Text.Length = 3 Then
            ' Lith < Meso < Neo < Axi
            Dim relics As String() = {"Lith", "Meso", "Neo", "Axi"}
            Return Array.IndexOf(relics, tx.Text) - Array.IndexOf(relics, ty.Text)
        ElseIf tx.Text.Contains(" ") Then
            Return tx.Index - ty.Index
        Else
            If tx.Text.Length <> ty.Text.Length Then
                Return tx.Text.Length - ty.Text.Length
            End If
            Return String.Compare(tx.Text, ty.Text)
        End If
    End Function
End Class