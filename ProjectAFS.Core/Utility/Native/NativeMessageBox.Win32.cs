using System.Runtime.InteropServices;

namespace ProjectAFS.Core.Utility.Native;

public static partial class NativeMessageBox
{
	/// <summary>
	/// Provides a simple MessageBox implementation for Windows.
	/// </summary>
	private static partial class Win32
	{
		[LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
		private static partial int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

		private static class Constants
		{
			// ReSharper disable IdentifierTypo
			public const uint MB_OK = 0x00000000;
			public const uint MB_OKCANCEL = 0x00000001;
			public const uint MB_YESNOCANCEL = 0x00000003;
			public const uint MB_YESNO = 0x00000004;
			
			public const uint MB_ICONEXCLAMATION = 0x00000030; // Warning
			public const uint MB_ICONWARNING = MB_ICONEXCLAMATION;
			public const uint MB_ICONINFORMATION = 0x00000040; // Information
			public const uint MB_ICONASTERISK = MB_ICONINFORMATION;
			public const uint MB_ICONQUESTION = 0x00000020; // Question
			public const uint MB_ICONSTOP = 0x00000010; // Error
			public const uint MB_ICONERROR = MB_ICONSTOP;
			public const uint MB_ICONHAND = MB_ICONSTOP;

			public const int IDOK = 1;
			public const int IDCANCEL = 2;
			public const int IDYES = 6;
			public const int IDNO = 7;
			// ReSharper restore IdentifierTypo
		}
		
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, IntPtr owner)
		{
			uint type = 0;

			switch (buttons)
			{
				case MessageBoxButtons.OK: type |= Constants.MB_OK; break;
				case MessageBoxButtons.OKCancel: type |= Constants.MB_OKCANCEL; break;
				case MessageBoxButtons.YesNo: type |= Constants.MB_YESNO; break;
				case MessageBoxButtons.YesNoCancel: type |= Constants.MB_YESNOCANCEL; break;
			}

			switch (icon)
			{
				case MessageBoxIcon.Information: type |= Constants.MB_ICONINFORMATION; break;
				case MessageBoxIcon.Question: type |= Constants.MB_ICONQUESTION; break;
				case MessageBoxIcon.Warning: type |= Constants.MB_ICONWARNING; break;
				case MessageBoxIcon.Error: type |= Constants.MB_ICONERROR; break;
			}
				
			int result = MessageBoxW(owner, text, caption, type);

			return result switch
			{
				Constants.IDOK => MessageBoxResult.OK,
				Constants.IDCANCEL => MessageBoxResult.Cancel,
				Constants.IDYES => MessageBoxResult.Yes,
				Constants.IDNO => MessageBoxResult.No,
				_ => MessageBoxResult.OK
			};
		}
	}
}