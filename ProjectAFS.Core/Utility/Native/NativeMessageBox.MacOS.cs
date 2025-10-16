using System.Runtime.InteropServices;

namespace ProjectAFS.Core.Utility.Native;

public static partial class NativeMessageBox
{
	/// <summary>
	/// Provides a simple MessageBox implementation for macOS.
	/// </summary>
	private static partial class MacOS
	{
		private const string DylibName = "libNativeMessageBox.dylib";
		
		[LibraryImport(DylibName, StringMarshalling = StringMarshalling.Utf8)]
		private static partial int ShowMessageBox(string title, string text, int buttons, int icon);
		
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, IntPtr owner)
		{
			int buttonStyle = buttons switch
			{
				MessageBoxButtons.OK => 0,
				MessageBoxButtons.OKCancel => 1,
				MessageBoxButtons.YesNo => 2,
				MessageBoxButtons.YesNoCancel => 3,
				_ => 0
			};
			
			int iconStyle = icon switch
			{
				MessageBoxIcon.Information => 1,
				MessageBoxIcon.Question => 2,
				MessageBoxIcon.Warning => 3,
				MessageBoxIcon.Error => 4,
				_ => 0
			};
			
			// Return value in Objective-C is corresponding to MessageBoxResult enum
			int result = ShowMessageBox(caption, text, buttonStyle, iconStyle);
			return (MessageBoxResult)result;
		}
	}
}