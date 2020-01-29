using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WFInfoCS {
	public delegate void KeyboardAction();

	class LowLevelListener {
		private const int WH_MOUSE_LL = 14;
		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private static readonly LowLevelKeyboardProc _procKeyboard = HookCallbackKB;
		private static IntPtr _hookIDKeyboard = IntPtr.Zero;
		private static IntPtr _hookIDMouse = IntPtr.Zero;
		private static readonly LowLevelMouseProc _procMouse = HookCallbackM;

		private enum mouseMessages {
			WM_LBUTTONDOWN = 0x0201,
			WM_LBUTTONUP = 0x0202,
			WM_MOUSEMOVE = 0x0200,
			WM_MOUSEWHEEL = 0x020A,
			WM_RBUTTONDOWN = 0x0204,
			WM_RBUTTONUP = 0x0205,
			WM_XBUTTONDOWN = 0x020B
		}
		[StructLayout(LayoutKind.Sequential)]

		private struct point {
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct msslHookStructure {
			public point pt;
			public uint mouseData;
			public uint flags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		public LowLevelListener() {
		}

		public void Hook() {
			_hookIDKeyboard = SetHookKB(_procKeyboard);
			_hookIDMouse = SetHookM(_procMouse);
		}

		public void UnHook() {
			UnhookWindowsHookEx(_hookIDKeyboard);
			UnhookWindowsHookEx(_hookIDMouse);
		}

		private static IntPtr SetHookKB(LowLevelKeyboardProc proc) {
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule) {
				return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private static IntPtr SetHookM(LowLevelMouseProc proc) {
			using (Process curProcess = Process.GetCurrentProcess())
			using (ProcessModule curModule = curProcess.MainModule) {
				return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		public delegate void keyActionHandler(Keys key);
		public static event keyActionHandler KeyAction;
		private static IntPtr HookCallbackKB(int nCode, IntPtr wParam, IntPtr lParam) //handels keyboard input
		{
			if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
				int vkCode = Marshal.ReadInt32(lParam);
				//Console.WriteLine((Keys)vkCode);
				OnKeyAction((Keys)vkCode);
			}
			return CallNextHookEx(_hookIDKeyboard, nCode, wParam, lParam);
		}

		protected static void OnKeyAction(Keys key) {
			KeyAction?.Invoke(key);
		}

		private static IntPtr HookCallbackM(int nCode, IntPtr wParam, IntPtr lParam) //handels mouse input
		{
			if (nCode >= 0) {
				msslHookStructure hookStruct = (msslHookStructure)Marshal.PtrToStructure(lParam, typeof(msslHookStructure));
				//Console.WriteLine((MouseMessages)wParam);
				switch ((mouseMessages)wParam) {
					case mouseMessages.WM_MOUSEMOVE:
					break;
					case mouseMessages.WM_LBUTTONDOWN:
					OnKeyAction(Keys.LButton);
					break;
					case mouseMessages.WM_RBUTTONDOWN:
					OnKeyAction(Keys.RButton);
					break;
					case mouseMessages.WM_MOUSEWHEEL:
					//Should this stay implemented?
					break;
					case mouseMessages.WM_XBUTTONDOWN: //https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-xbuttondown
					if (hookStruct.pt.y == 1)
						OnKeyAction(Keys.XButton1);
					else
						OnKeyAction(Keys.XButton2);
					break;
					default:
					break;
				}
				//Console.WriteLine(hookStruct.pt.x + ", " + hookStruct.pt.y);

			}
			return CallNextHookEx(_hookIDMouse, nCode, wParam, lParam);
		}

		private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);



		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
	}

}