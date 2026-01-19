using System.Runtime.InteropServices;
using System.Text;

namespace ProjectAFS.Core.Utility.Native;

public static partial class NativeMessageBox
{
	/// <summary>
	/// Provides a simple MessageBox implementation for Linux. (Requires GTK3)
	/// </summary>
	private static class Linux
	{
		private const string GtkLib = "libgtk-3.so.0";

		private enum GtkMessageType
		{
			Info = 0,
			Warning = 1,
			Question = 2,
			Error = 3
		}

		private enum GtkButtonsType
		{
			None = 0,
			Ok = 1,
			Close = 2,
			Cancel = 3,
			OkCancel = 4,
			YesNo = 5
		}

		private enum GtkResponseType
		{
			None = -1,
			Ok = -5,
			Cancel = -6,
			Yes = -8,
			No = -9
		}
		
		[DllImport(GtkLib, EntryPoint = "gtk_init_check")]
		private static extern bool GtkInit(IntPtr argc, IntPtr argv);
		
		[DllImport(GtkLib, EntryPoint = "gtk_dialog_run")]
		private static extern int GtkDialogRun(IntPtr dialog);
		
		[DllImport(GtkLib, EntryPoint = "gtk_widget_destroy")]
		private static extern void GtkWidgetDestroy(IntPtr widget);

		[DllImport(GtkLib, EntryPoint = "gtk_message_dialog_new")]
		private static extern IntPtr GtkNewUTF8MsgDialog(IntPtr parent, int flags, GtkMessageType type, GtkButtonsType buttons, byte[] messageFormat);
		
		[DllImport(GtkLib, EntryPoint = "gtk_window_set_title")]
		private static extern void GtkSetUTF8WindowTitle(IntPtr window, byte[] title);
		
		[DllImport(GtkLib, EntryPoint = "gtk_events_pending")]
		private static extern bool GtkPendingEvents();
		
		[DllImport(GtkLib, EntryPoint = "gtk_main_iteration")]
		private static extern void GtkMainIteration();

		private static byte[] StringToUtf8(string str)
		{
			return Encoding.UTF8.GetBytes(str + '\0');
		}

		// ReSharper disable once MemberHidesStaticFromOuterClass
		public static MessageBoxResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, IntPtr owner)
		{
			if (!GtkInit(IntPtr.Zero, IntPtr.Zero))
			{
				Console.Error.WriteLine("GTK# initialization failed.");
				// GTK initialization failed
				Console.WriteLine($"MessageBox ({caption}): {text}");
				return MessageBoxResult.OK;
			}

			var msgType = icon switch
			{
				MessageBoxIcon.Information => GtkMessageType.Info,
				MessageBoxIcon.Question => GtkMessageType.Question,
				MessageBoxIcon.Warning => GtkMessageType.Warning,
				MessageBoxIcon.Error => GtkMessageType.Error,
				_ => GtkMessageType.Info
			};
			
			var buttonsType = buttons switch
			{
				MessageBoxButtons.OK => GtkButtonsType.Ok,
				MessageBoxButtons.OKCancel => GtkButtonsType.OkCancel,
				MessageBoxButtons.YesNo => GtkButtonsType.YesNo,
				MessageBoxButtons.YesNoCancel => GtkButtonsType.YesNo, // GTK does not have YesNoCancel, fallback to YesNo
				_ => GtkButtonsType.Ok
			};
			
			byte[] textUtf8 = StringToUtf8(text);
			byte[] captionUtf8 = StringToUtf8(caption);
			
			nint dialog = GtkNewUTF8MsgDialog(owner, 0, msgType, buttonsType, textUtf8);
			GtkSetUTF8WindowTitle(dialog, captionUtf8);
			
			int result = GtkDialogRun(dialog);
			GtkWidgetDestroy(dialog);

			while (GtkPendingEvents())
			{
				GtkMainIteration();
			}

			return (GtkResponseType) result switch
			{
				GtkResponseType.Ok => MessageBoxResult.OK,
				GtkResponseType.Cancel => MessageBoxResult.Cancel,
				GtkResponseType.Yes => MessageBoxResult.Yes,
				GtkResponseType.No => MessageBoxResult.No,
				_ => MessageBoxResult.Cancel // click close button or other cases
			};
		}
	}
}