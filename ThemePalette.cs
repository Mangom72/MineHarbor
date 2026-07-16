using System;
using System.Drawing;
using System.Windows.Forms;

internal static partial class Launcher
{
	/// <summary>Centralized font names used throughout the launcher UI.</summary>
	internal static class ThemeFonts
	{
		private static string ResolveFontFamily(params string[] candidates)
		{
			foreach (string candidate in candidates)
			{
				try
				{
					using (Font font = new Font(candidate, 10F, FontStyle.Regular, GraphicsUnit.Point))
					{
						if (string.Equals(font.Name, candidate, StringComparison.OrdinalIgnoreCase))
						{
							return candidate;
						}
					}
				}
				catch { }
			}
			return SystemFonts.DefaultFont.Name;
		}

		public static readonly string Body = ResolveFontFamily("Pretendard", "Malgun Gothic", "Segoe UI");
		public static readonly string BodySemibold = Body;
		public static readonly string Display = Body;
		public static readonly string Monospace = ResolveFontFamily("Consolas", "Courier New");
		public static readonly string Fallback = Body;

		public const float TitleSize = 22F;
		public const float HeadingSize = 17F;
		public const float BodySize = 10.5F;
		public const float SmallSize = 9.5F;
		public const float SmallSemiboldSize = 9F;
		public const float SubtitleSize = 10F;
		public const float AddressSize = 11F;
		public const float ConsoleSize = 9F;
		public const float StatusSize = 10.5F;
	}

	internal sealed class ThemePalette
	{
		public Color Window;
		public Color Card;
		public Color CardSecondary;
		public Color Text;
		public Color Muted;
		public Color Border;
		public Color Accent;
		public Color AccentHover;
		public Color AccentSoft;
		public Color Danger;
		public Color DangerSoft;
		public Color Success;
		public Color Warning;
		public Color Console;

		public static ThemePalette Create(bool dark)
		{
			ThemePalette palette = new ThemePalette();
			if (SystemInformation.HighContrast)
			{
				palette.Window = SystemColors.Window;
				palette.Card = SystemColors.Control;
				palette.CardSecondary = SystemColors.Window;
				palette.Text = SystemColors.WindowText;
				palette.Muted = SystemColors.GrayText;
				palette.Border = SystemColors.WindowText;
				palette.Accent = SystemColors.Highlight;
				palette.AccentHover = SystemColors.HotTrack;
				palette.AccentSoft = SystemColors.Control;
				palette.Danger = SystemColors.HotTrack;
				palette.DangerSoft = SystemColors.Control;
				palette.Success = SystemColors.Highlight;
				palette.Warning = SystemColors.HotTrack;
				palette.Console = SystemColors.Window;
				return palette;
			}
			if (dark)
			{
				palette.Window = Color.FromArgb(23, 23, 28);
				palette.Card = Color.FromArgb(32, 32, 38);
				palette.CardSecondary = Color.FromArgb(42, 43, 50);
				palette.Text = Color.FromArgb(242, 244, 246);
				palette.Muted = Color.FromArgb(174, 180, 188);
				palette.Border = Color.FromArgb(58, 59, 68);
				palette.Console = Color.FromArgb(15, 16, 20);
			}
			else
			{
				palette.Window = Color.FromArgb(246, 247, 249);
				palette.Card = Color.White;
				palette.CardSecondary = Color.FromArgb(242, 244, 246);
				palette.Text = Color.FromArgb(25, 31, 40);
				palette.Muted = Color.FromArgb(82, 94, 108);
				palette.Border = Color.FromArgb(220, 226, 232);
				palette.Console = Color.FromArgb(25, 28, 34);
			}
			palette.Accent = Color.FromArgb(0, 100, 255);
			palette.AccentHover = Color.FromArgb(0, 78, 214);
			palette.AccentSoft = dark ? Color.FromArgb(38, 61, 96) : Color.FromArgb(232, 240, 254);
			palette.Danger = dark ? Color.FromArgb(255, 113, 128) : Color.FromArgb(197, 40, 61);
			palette.DangerSoft = dark ? Color.FromArgb(78, 40, 46) : Color.FromArgb(255, 235, 238);
			palette.Success = dark ? Color.FromArgb(49, 214, 155) : Color.FromArgb(8, 122, 85);
			palette.Warning = dark ? Color.FromArgb(255, 176, 103) : Color.FromArgb(165, 78, 0);
			return palette;
		}
	}
}
