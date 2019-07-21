Imports System.Drawing
Imports System.Text
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports WFInfo

<TestClass()> Public Class OCRtest
    Inherits OCR2

    Public isWFActive_Skip As Boolean = False
    Public Overrides Function isWFActive() As Boolean
        If isWFActive_Skip Then
            Return True
        End If
        Return MyBase.isWFActive()
    End Function

    Public GetWFProc_Skip As Boolean = False
    Public Overrides Function GetWFProc() As Process
        If GetWFProc_Skip Then
            Return Nothing
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
    Public Overrides Function GetDPIScaling() As Double
        If GetScalingFactor_Skip Then
            Return -1.0
        End If
        Return MyBase.GetDPIScaling()
    End Function

    Public Screenshot_Override As Boolean = False
    Public Overrides Function GetPlayerImage(plyr As Integer, count As Integer) As Bitmap
        If Screenshot_Override Then
            ' Do something
            If plyr < 0 Or count < 0 Then
                Return Nothing
            End If
        End If
        Return MyBase.GetPlayerImage(plyr, count)
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
        Assert.AreEqual(GetWFProc(), Nothing)
        GetWFProc_Skip = False

        uiScaling = -1.0
        GetUIScaling_Skip = True
        Assert.AreEqual(-1.0, GetUIScaling())
        GetUIScaling_Skip = False

        GetScalingFactor_Skip = True
        Assert.AreEqual(-1.0, GetDPIScaling())
        GetScalingFactor_Skip = False

        Screenshot_Override = True
        Assert.IsNull(GetPlayerImage(-1, -1))
        Screenshot_Override = False
    End Sub

    <TestMethod()> Public Sub TestTesseract()



    End Sub
End Class