Module Win32

    Public Const WH_KEYBOARD_LL As Integer = 13
    Public Const WH_MOUSE_LL As Integer = 14
    Public Const HC_ACTION As Integer = 0
    Public Const WM_KEYDOWN = &H100
    Public Const WM_KEYUP = &H101
    Public Const WM_SYSKEYDOWN = &H104
    Public Const WM_SYSKEYUP = &H105
    Public Const WM_LBUTTONDOWN = &H201
    Public Const WM_LBUTTONUP = &H202
    Public Const WM_RBUTTONDOWN = &H204
    Public Const WM_RBUTTONUP = &H205
    Public Const WM_MOUSEWHEEL = &H20A
    Public Const WM_MOUSEHWHEEL = &H20E

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

    <Flags()>
    Public Enum KBDLLHOOKSTRUCTFlags As UInt32
        LLKHF_EXTENDED = &H1
        LLKHF_INJECTED = &H10
        LLKHF_ALTDOWN = &H20
        LLKHF_UP = &H80
    End Enum

    Public Enum DeviceCap
        VERTRES = 10
        DESKTOPVERTRES = 117
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Public Structure KBDLLHOOKSTRUCT
        Public vkCode As UInt32
        Public scanCode As UInt32
        Public flags As KBDLLHOOKSTRUCTFlags
        Public time As UInt32
        Public dwExtraInfo As UIntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure BLENDFUNCTION
        Dim BlendOp As Byte
        Dim BlendFlags As Byte
        Dim SourceConstantAlpha As Byte
        Dim AlphaFormat As Byte
    End Structure

    Public Delegate Function HookProc(ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer

    <DllImport("User32.dll")>
    Public Function SendMessage(hWnd As IntPtr, msg As Integer, wp As IntPtr, lp As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Function GetScrollPos(hWnd As IntPtr, nBar As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Public Function SetScrollPos(hWnd As IntPtr, nBar As Integer, nPos As Integer, bRedraw As Boolean) As Integer
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Public Function SetWindowsHookEx(ByVal idHook As Integer, ByVal HookProc As HookProc, ByVal hInstance As IntPtr, ByVal wParam As Integer) As Integer
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Public Function CallNextHookEx(ByVal idHook As Integer, ByVal nCode As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As Integer
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.StdCall)>
    Public Function UnhookWindowsHookEx(ByVal idHook As Integer) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function GetKeyState(ByVal nVirtKey As Integer) As Short
    End Function

    <DllImport("user32.dll")>
    Public Function GetWindowRect(ByVal hWnd As HandleRef, ByRef lpRect As RECT) As Boolean
    End Function

    <DllImport("User32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Public Function GetWindowLong(hWnd As IntPtr, nIndex As Int16) As Int32
    End Function

    <DllImport("dwmapi.dll", PreserveSig:=False)>
    Public Sub DwmEnableComposition(bEnable As Boolean)
    End Sub

    <DllImport("gdi32.dll")>
    Public Function GetDeviceCaps(hdc As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", ExactSpelling:=True, SetLastError:=True)>
    Public Function UpdateLayeredWindow(hwnd As IntPtr, hdcdst As IntPtr, ByRef pptDst As Point, ByRef psize As Size, hdcSrc As IntPtr, ByRef pprSrc As Point, crKey As Int32, ByRef pblend As BLENDFUNCTION, dwFlags As Int32) As Boolean
    End Function

    <DllImport("user32.dll", ExactSpelling:=True, SetLastError:=True)>
    Public Function GetDC(hwnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", ExactSpelling:=True)>
    Public Function ReleaseDC(hwnd As IntPtr, hdc As IntPtr) As Integer
    End Function

    <DllImport("gdi32.dll", ExactSpelling:=True, SetLastError:=True)>
    Public Function CreateCompatibleDC(hdc As IntPtr) As IntPtr
    End Function

    <DllImport("gdi32.dll", ExactSpelling:=True, SetLastError:=True)>
    Public Function DeleteDC(hdc As IntPtr) As Boolean
    End Function

    <DllImport("gdi32.dll", ExactSpelling:=True)>
    Public Function SelectObject(hdc As IntPtr, hObject As IntPtr) As IntPtr
    End Function

    <DllImport("gdi32.dll", ExactSpelling:=True, SetLastError:=True)>
    Public Function DeleteObject(hObject As IntPtr) As Boolean
    End Function
End Module
