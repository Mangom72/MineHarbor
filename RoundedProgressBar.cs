using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal sealed class RoundedProgressBar : Control
{
	private int maximum = 100;
	private int value = 0;
	private bool isIndeterminate = true;
	private float spinnerAngle = 0f;
	private readonly Timer animTimer;
	private GraphicsPath cachedLinearPath = new GraphicsPath();
	private int lastFillWidth;
	private int lastHeight;

	public int Maximum
	{
		get { return maximum; }
		set { maximum = value; Invalidate(); }
	}

	public int Value
	{
		get { return this.value; }
		set 
		{ 
			this.value = Math.Max(0, Math.Min(value, maximum)); 
			isIndeterminate = false;
			Invalidate(); 
		}
	}

	public bool IsIndeterminate
	{
		get { return isIndeterminate; }
		set { isIndeterminate = value; Invalidate(); }
	}

	public RoundedProgressBar()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		BackColor = Color.Transparent;
		ForeColor = Color.DodgerBlue; // default accent
		
		animTimer = new Timer { Interval = 16 };
		animTimer.Tick += delegate
		{
			if (isIndeterminate)
			{
				spinnerAngle += 8f;
				if (spinnerAngle >= 360f) spinnerAngle -= 360f;
				Invalidate();
			}
		};
		animTimer.Start();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			animTimer.Stop();
			animTimer.Dispose();
			if (cachedLinearPath != null)
			{
				cachedLinearPath.Dispose();
				cachedLinearPath = null;
			}
		}
		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

		if (isIndeterminate)
		{
			// Draw circular spinner centered
			int size = Math.Min(Width, Height) - 6;
			if (size <= 0) return;
			
			Rectangle rect = new Rectangle((Width - size) / 2, (Height - size) / 2, size, size);
			using (Pen pen = new Pen(Color.FromArgb(40, ForeColor), 3f))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Round;
				e.Graphics.DrawArc(pen, rect, 0, 360);
			}
			using (Pen pen = new Pen(ForeColor, 3f))
			{
				pen.StartCap = LineCap.Round;
				pen.EndCap = LineCap.Round;
				e.Graphics.DrawArc(pen, rect, spinnerAngle, 90);
			}
		}
		else
		{
			// Draw linear bar
			float progress = maximum > 0 ? (float)value / maximum : 0;
			if (progress <= 0) return;

			int fillWidth = (int)(Width * progress);
			int height = Height;
			int radius = height / 2;
			
			if (fillWidth < height) fillWidth = height; // minimum width to draw pill

			if (lastFillWidth != fillWidth || lastHeight != height)
			{
				cachedLinearPath.Reset();
				cachedLinearPath.AddArc(0, 0, height, height, 90, 180);
				cachedLinearPath.AddArc(fillWidth - height, 0, height, height, 270, 180);
				cachedLinearPath.CloseFigure();
				lastFillWidth = fillWidth;
				lastHeight = height;
			}
			
			using (SolidBrush brush = new SolidBrush(ForeColor))
			{
				e.Graphics.FillPath(brush, cachedLinearPath);
			}
		}
	}
}
