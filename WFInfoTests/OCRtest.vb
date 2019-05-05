Imports System.Drawing
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports WFInfo

<TestClass()> Public Class OCRtest
    Inherits OCR

    Public isWFActive_Skip As Boolean = False
    Public Overrides Function isWFActive() As Boolean
        If isWFActive_Skip Then
            Return True
        End If
        Return MyBase.isWFActive()
    End Function

    Public GetWFProc_Skip As Boolean = False
    Public Overrides Function GetWFProc() As Boolean
        If GetWFProc_Skip Then
            Return True
        End If
        Return MyBase.GetWFProc()
    End Function

    Public GetUIScaling_Skip As Boolean = False
    Public Overrides Function GetUIScaling() As Double
        If GetUIScaling_Skip Then
            Return uiScaling
        End If
        Return MyBase.GetUIScaling()
    End Function

    Public GetScalingFactor_Skip As Boolean = False
    Public Overrides Function GetScalingFactor() As Double
        If GetScalingFactor_Skip Then
            Return -1.0
        End If
        Return MyBase.GetScalingFactor()
    End Function

    Public Screenshot_Override As Boolean = False
    Public Overrides Function Screenshot(wid As Integer, hei As Integer, top As Integer) As Bitmap
        If Screenshot_Override Then
            ' Do something
            If wid < 0 Or hei < 0 Then
                Return Nothing
            End If
        End If
        Return MyBase.Screenshot(wid, hei, top)
    End Function

    Public UpdateCenter_Skip As Boolean = False
    Public Overrides Sub UpdateCenter()
        If Not UpdateCenter_Skip Then
            MyBase.UpdateCenter()
        End If
    End Sub

    <TestMethod()> Public Sub TestOverrides()
        isWFActive_Skip = True
        Assert.IsTrue(isWFActive())
        isWFActive_Skip = False

        GetWFProc_Skip = True
        Assert.IsTrue(GetWFProc())
        GetWFProc_Skip = False

        uiScaling = -1.0
        GetUIScaling_Skip = True
        Assert.AreEqual(-1.0, GetUIScaling())
        GetUIScaling_Skip = False

        GetScalingFactor_Skip = True
        Assert.AreEqual(-1.0, GetScalingFactor())
        GetScalingFactor_Skip = False

        Screenshot_Override = True
        Assert.IsNull(Screenshot(-1, -1, -1))
        Screenshot_Override = False
    End Sub

    <TestMethod()> Public Sub TestTesseract()



    End Sub
End Class