Public Class GlobalHook

    Public Shared Event KeyDown(ByVal Key As Keys)
    Public Shared Event KeyUp(ByVal Key As Keys)
    Public Shared Event MouseScroll(ByVal Scroll As Int32)

    Private KBDLLHookProcDelegate As HookProc = New HookProc(AddressOf KeyboardProc)
    Private KBDHHookID As IntPtr = IntPtr.Zero

    Private Function KeyboardProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
        If (nCode = HC_ACTION) Then
            Dim struct As Win32.KBDLLHOOKSTRUCT
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
                Case WM_XBUTTONDOWN
                    If (CType(Marshal.PtrToStructure(lParam, struct.GetType()), MSLLHOOKSTRUCT).mouseData >> 16) = 1 Then
                        RaiseEvent KeyDown(Keys.XButton1)
                    Else
                        RaiseEvent KeyDown(Keys.XButton2)
                    End If
                Case WM_LBUTTONUP
                    RaiseEvent KeyUp(Keys.LButton)
                Case WM_RBUTTONUP
                    RaiseEvent KeyUp(Keys.RButton)
                Case WM_XBUTTONUP
                    If (CType(Marshal.PtrToStructure(lParam, struct.GetType()), MSLLHOOKSTRUCT).mouseData >> 16) = 1 Then
                        RaiseEvent KeyDown(MouseButtons.XButton1)
                    Else
                        RaiseEvent KeyDown(MouseButtons.XButton2)
                    End If
                Case WM_MOUSEWHEEL
                    RaiseEvent MouseScroll(CType(Marshal.PtrToStructure(lParam, struct.GetType()), MSLLHOOKSTRUCT).mouseData >> 16)
            End Select
        End If
        Return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam)
    End Function

    Public Sub New()

        Dim pointer As IntPtr = Marshal.GetHINSTANCE(Reflection.Assembly.GetExecutingAssembly.GetModules()(0))

        MSHHookID = SetWindowsHookEx(WH_MOUSE_LL, MSLLHookProcDelegate, pointer, 0)
        KBDHHookID = SetWindowsHookEx(WH_KEYBOARD_LL, KBDLLHookProcDelegate, pointer, 0)

        Dim msg As Message
        While GetMessage(msg, Nothing, 0, 0)
            DispatchMessage(msg)
        End While
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