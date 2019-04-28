Public Class GlobalHook

    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function SetWindowsHookEx(ByVal idHook As Integer, ByVal HookProc As HookProc, ByVal hInstance As IntPtr, ByVal wParam As Integer) As Integer
    End Function
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function CallNextHookEx(ByVal idHook As Integer, ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    End Function
    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Private Overloads Shared Function UnhookWindowsHookEx(ByVal idHook As Integer) As Boolean
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Public Structure MSLLHOOKSTRUCT
        Public pt As Point
        Public mouseData As Int32
        Public flags As MSLLHOOKSTRUCTFlags
        Public time As Int32
        Public dwExtraInfo As UIntPtr
    End Structure

    <Flags()>
    Public Enum MSLLHOOKSTRUCTFlags As Int32
        LLMHF_INJECTED = 1
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Private Structure KBDLLHOOKSTRUCT
        Public vkCode As UInt32
        Public scanCode As UInt32
        Public flags As KBDLLHOOKSTRUCTFlags
        Public time As UInt32
        Public dwExtraInfo As UIntPtr
    End Structure

    <Flags()>
    Private Enum KBDLLHOOKSTRUCTFlags As UInt32
        LLKHF_EXTENDED = &H1
        LLKHF_INJECTED = &H10
        LLKHF_ALTDOWN = &H20
        LLKHF_UP = &H80
    End Enum

    Public Shared Event KeyDown(ByVal Key As Keys)
    Public Shared Event KeyUp(ByVal Key As Keys)
    Public Shared Event MouseScroll(ByVal Scroll As Int32)

    Private Const WH_KEYBOARD_LL As Integer = 13
    Private Const WH_MOUSE_LL As Integer = 14
    Private Const HC_ACTION As Integer = 0
    Private Const WM_KEYDOWN = &H100
    Private Const WM_KEYUP = &H101
    Private Const WM_SYSKEYDOWN = &H104
    Private Const WM_SYSKEYUP = &H105
    Private Const WM_LBUTTONDOWN = &H201
    Private Const WM_LBUTTONUP = &H202
    Private Const WM_RBUTTONDOWN = &H204
    Private Const WM_RBUTTONUP = &H205
    Private Const WM_MOUSEWHEEL = &H20A
    Private Const WM_MOUSEHWHEEL = &H20E

    Private Delegate Function HookProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer

    Private KBDLLHookProcDelegate As HookProc = New HookProc(AddressOf KeyboardProc)
    Private KBDHHookID As IntPtr = IntPtr.Zero

    Private Function KeyboardProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
        If (nCode = HC_ACTION) Then
            Dim struct As KBDLLHOOKSTRUCT
            Select Case wParam
                Case WM_KEYDOWN, WM_SYSKEYDOWN
                    RaiseEvent KeyDown(CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys))
                Case WM_KEYUP, WM_SYSKEYUP
                    RaiseEvent KeyUp(CType(CType(Marshal.PtrToStructure(lParam, struct.GetType()), KBDLLHOOKSTRUCT).vkCode, Keys))
            End Select
        End If
        Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
    End Function

    Private MSLLHookProcDelegate As HookProc = New HookProc(AddressOf MouseProc)
    Private MSHHookID As IntPtr = IntPtr.Zero

    Private Function MouseProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
        If (nCode = HC_ACTION) Then
            Dim struct As MSLLHOOKSTRUCT
            Select Case wParam
                Case WM_LBUTTONDOWN
                    RaiseEvent KeyDown(Keys.LButton)
                Case WM_RBUTTONDOWN
                    RaiseEvent KeyDown(Keys.RButton)
                Case WM_LBUTTONUP
                    RaiseEvent KeyUp(Keys.LButton)
                Case WM_RBUTTONUP
                    RaiseEvent KeyUp(Keys.RButton)
                Case WM_MOUSEWHEEL
                    RaiseEvent MouseScroll(CType(Marshal.PtrToStructure(lParam, struct.GetType()), MSLLHOOKSTRUCT).mouseData >> 16)
            End Select
        End If
        Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
    End Function

    Public Sub New()
        Try
            MSHHookID = SetWindowsHookEx(WH_MOUSE_LL, MSLLHookProcDelegate, IntPtr.Zero, 0)
            KBDHHookID = SetWindowsHookEx(WH_KEYBOARD_LL, KBDLLHookProcDelegate, IntPtr.Zero, 0)
        Catch ex As Exception
            MSHHookID = SetWindowsHookEx(WH_MOUSE_LL, MSLLHookProcDelegate, Marshal.GetHINSTANCE(Reflection.Assembly.GetExecutingAssembly.GetModules()(0)).ToInt32, 0)
            KBDHHookID = SetWindowsHookEx(WH_KEYBOARD_LL, KBDLLHookProcDelegate, Marshal.GetHINSTANCE(Reflection.Assembly.GetExecutingAssembly.GetModules()(0)).ToInt32, 0)
        End Try

        If MSHHookID = IntPtr.Zero Or KBDHHookID = IntPtr.Zero Then
            Throw New Exception("Could not set hook")
        End If
    End Sub

    Protected Overrides Sub Finalize()
        If Not MSHHookID = IntPtr.Zero Then
            UnhookWindowsHookEx(MSHHookID)
        End If
        If Not KBDHHookID = IntPtr.Zero Then
            UnhookWindowsHookEx(KBDHHookID)
        End If
        MyBase.Finalize()
    End Sub

End Class