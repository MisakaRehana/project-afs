using System.Runtime.InteropServices;

namespace ProjectAFS.Core.Utility.Native;

/// <summary>
/// Represents a cross-platform native message box utility.
/// </summary>
public static partial class NativeMessageBox
{
	public enum MessageBoxButtons
	{
		OK, OKCancel, YesNo, YesNoCancel
	}

	public enum MessageBoxIcon
	{
		None, Information, Question, Warning, Error,
		Asterisk = Information,
		Exclamation = Warning,
		Hand = Error,
		Stop = Error
	}

	public enum MessageBoxResult
	{
		OK, Cancel, Yes, No
	}

	/// <summary>
	/// Shows a native message box with the specified parameters.
	/// </summary>
	/// <param name="text">The message to display.</param>
	/// <param name="caption">The title of the message box window.</param>
	/// <param name="buttons">The buttons to include in the message box.</param>
	/// <param name="icon">The icon to display in the message box.</param>
	/// <param name="owner">The handle to the owner window (optional). If null, the message box will treat as having no owner and be non-modal (except on macOS, which is always modal).</param>
	/// <returns>A <see cref="MessageBoxResult"/> indicating which button was pressed.</returns>
	public static MessageBoxResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, IntPtr owner = 0)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Win32.Show(text, caption, buttons, icon, owner);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return MacOS.Show(text, caption, buttons, icon, owner);
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return Linux.Show(text, caption, buttons, icon, owner);
		}
		
		// Fallback for unsupported platforms or as a default
		Console.WriteLine($"MessageBox ({caption}): {text}");
		return MessageBoxResult.OK;
	}
}