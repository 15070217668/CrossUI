﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct2D1;

namespace CrossUI.SharpDX.Drawing
{
	sealed partial class DrawingTarget : IDrawingTarget, IDisposable
	{
		readonly RenderTarget _target;
		readonly List<string> _reports = new List<string>();

		public DrawingTarget(RenderTarget target, int width, int height)
		{
			_target = target;
			_target.AntialiasMode = AntialiasMode.PerPrimitive;

			Width = width;
			Height = height;
			
			// note: careful, if brushes are not being disposed they start leaking and refering back to
			// all resources created in the render target.... We need to check SharpDX if this is 
			// by design or a bug!

			_strokeBrush = new SolidColorBrush(_target, new Color4(0, 0, 0, 1));
			_fillBrush = new SolidColorBrush(_target, new Color4(0, 0, 0, 1));
			_textBrush = new SolidColorBrush(_target, new Color4(0, 0, 0, 1));

			_strokeWeight = 1;
		}

		public void Dispose()
		{
			_textBrush.Dispose();
			_fillBrush.Dispose();
			_strokeBrush.Dispose();
		}

		Brush _strokeBrush;
		float _strokeWeight;
		StrokeAlignment _strokeAlignment;
		Brush _fillBrush;
		bool _fill;

		public int Width { get; private set; }
		public int Height { get; private set; }

		public void Report(string text)
		{
			_reports.Add(text);
		}

		public IEnumerable<string> Reports
		{
			get { return _reports; }
		}

		public void Fill(Color? color)
		{
			if (color != null)
			{
				_fillBrush.Dispose();
				_fillBrush = new SolidColorBrush(_target, color.Value.import());
			}

			_fill = true;
		}

		public void NoFill()
		{
			_fill = false;
		}

		public void Stroke(Color? color, double? weight, StrokeAlignment? alignment)
		{
			if (color != null)
			{
				_strokeBrush.Dispose();
				_strokeBrush = new SolidColorBrush(_target, color.Value.import());
			}

			if (weight != null)
				_strokeWeight = weight.Value.import();

			if (alignment != null)
				_strokeAlignment = alignment.Value;
		}

		public void NoStroke()
		{
			_strokeWeight = 0;
		}

		bool Filling
		{
			get { return _fill; }
		}

		bool Stroking
		{
			get { return _strokeWeight != 0f; }
		}

		public void Line(double x1, double y1, double x2, double y2)
		{
			if (Stroking)
				_target.DrawLine(importPoint(x1, y1), importPoint(x2, y2), _strokeBrush, _strokeWeight);
		}

		public void Rect(double x, double y, double width, double height)
		{
			if (Stroking)
			{
				var r = strokeAlignedRect(x, y, width, height);
				_target.DrawRectangle(r, _strokeBrush, _strokeWeight);
			}

			if (Filling)
			{
				var r = fillRect(x, y, width, height);
				_target.FillRectangle(r, _fillBrush);
			}

		}

		public void RoundedRect(double x, double y, double width, double height, double cornerRadius)
		{
			if (Filling)
			{
				var roundedRect = new RoundedRect
				{
					Rect = fillRect(x, y, width, height),
					RadiusX = import(cornerRadius),
					RadiusY = import(cornerRadius)
				};

				_target.FillRoundedRectangle(roundedRect, _fillBrush);
			}

			if (Stroking)
			{
				var roundedRect = new RoundedRect
				{
					Rect = strokeAlignedRect(x, y, width, height),
					RadiusX = import(cornerRadius),
					RadiusY = import(cornerRadius)
				};

				_target.DrawRoundedRectangle(roundedRect, _strokeBrush, _strokeWeight);
			}
		}

		public void Polygon(double[] pairs)
		{
			if ((pairs.Length & 1) == 1)
				throw new Exception("Number of polygon pairs need to be even.");

			if (pairs.Length < 4)
				return;

			if (pairs.Length == 4)
			{
				Line(pairs[0], pairs[1], pairs[2], pairs[3]);
				return;
			}

			var startPoint = importPoint(pairs[0], pairs[1]);

			if (Filling)
			{
				fillPath(startPoint,
					sink =>
						{
							for (int i = 2; i != pairs.Length; i += 2)
								sink.AddLine(importPoint(pairs[i], pairs[i + 1]));
						});
			}

			if (Stroking)
			{
				drawClosedPath(startPoint,
					sink =>
					{
						for (int i = 2; i != pairs.Length; i += 2)
							sink.AddLine(importPoint(pairs[i], pairs[i + 1]));
					});
			}
		}

		public void Ellipse(double x, double y, double width, double height)
		{
			if (Filling)
			{
				var r = fillRect(x, y, width, height);
				var ellipse = new Ellipse(importPoint(r.Left + r.Width / 2, r.Top + r.Height / 2), r.Width / 2, r.Height / 2);
				_target.FillEllipse(ellipse, _fillBrush);
			}

			if (Stroking)
			{
				var r = strokeAlignedRect(x, y, width, height);
				var ellipse = new Ellipse(importPoint(r.Left + r.Width / 2, r.Top + r.Height / 2), r.Width / 2, r.Height / 2);
				_target.DrawEllipse(ellipse, _strokeBrush, _strokeWeight);
			}
		}

		public void Arc(double x, double y, double width, double height, double start, double stop)
		{
			if (Filling)
			{
				var r = fillRect(x, y, width, height);

				var centerPoint = new DrawingPointF(r.X + r.Width / 2, r.Y + r.Height / 2);

				fillPath(centerPoint, sink =>
					{
						var startPoint = pointOnArc(r, start);
						sink.AddLine(startPoint);
						addArc(r, start, stop, sink);
					});
			}

			if (Stroking)
			{
				var r = strokeAlignedRect(x, y, width, height);
				var currentPoint = pointOnArc(r, start);
				drawOpenPath(currentPoint, sink => addArc(r, start, stop, sink));
			}
		}

		DrawingPointF pointOnArc(RectangleF r, double angle)
		{
			var rx = r.Width / 2;
			var ry = r.Height / 2;
			var cx = r.X + rx;
			var cy = r.Y + ry;

			var dx = Math.Cos(angle) * rx;
			var dy = Math.Sin(angle) * ry;

			return new DrawingPointF((cx + dx).import(), (cy + dy).import());
		}

		void addArc(RectangleF r, double start, double stop, GeometrySink sink)
		{
			var rx = r.Width / 2;
			var ry = r.Height / 2;
			var angle = start;

			// the quality of Direct2D arcs are lousy, so we render them in 16 segments per circle

			const int MaxSegments = 16;
			const double SegmentAngle = Math.PI * 2 / MaxSegments;

			for (var segment = 0; angle < stop && segment != MaxSegments; ++segment)
			{
				var angleLeft = stop - angle;
				var angleNow = Math.Min(SegmentAngle, angleLeft);
				var nextAngle = angle + angleNow;
				var nextPoint = pointOnArc(r, nextAngle);

				sink.AddArc(new ArcSegment
				{
					ArcSize = ArcSize.Small,
					Size = new DrawingSizeF(rx, ry),
					Point = nextPoint,
					RotationAngle = (stop - start).import(),
					SweepDirection = SweepDirection.Clockwise
				});

				angle = nextAngle;
			}
		}

		public void Bezier(double x, double y, double s1x, double s1y, double s2x, double s2y, double ex, double ey)
		{
			if (Filling)
			{
				fillPath(importPoint(x, y), sink =>
				{
					var bezierSegment = new BezierSegment()
					{
						Point1 = importPoint(s1x, s1y),
						Point2 = importPoint(s2x, s2y),
						Point3 = importPoint(ex, ey)
					};

					sink.AddBezier(bezierSegment);
				});
			}

			if (Stroking)
			{
				drawOpenPath(importPoint(x, y), sink =>
					{
						var bezierSegment = new BezierSegment()
						{
							Point1 = importPoint(s1x, s1y),
							Point2 = importPoint(s2x, s2y),
							Point3 = importPoint(ex, ey)
						};

						sink.AddBezier(bezierSegment);
					});
			}
		}

		void drawOpenPath(DrawingPointF begin, Action<GeometrySink> figureBuilder)
		{
			using (var geometry = createPath(false, begin, figureBuilder))
			{
				_target.DrawGeometry(geometry, _strokeBrush, _strokeWeight);
			}
		}

		void drawClosedPath(DrawingPointF begin, Action<GeometrySink> figureBuilder)
		{
			using (var geometry = createPath(true, begin, figureBuilder))
			{
				_target.DrawGeometry(geometry, _strokeBrush, _strokeWeight);
			}
		}

		void fillPath(DrawingPointF begin, Action<GeometrySink> figureBuilder)
		{
			using (var geometry = createPath(true, begin, figureBuilder))
			{
				_target.FillGeometry(geometry, _fillBrush);
			}
		}

		public Geometry createPath(bool filled, DrawingPointF begin, Action<GeometrySink> figureBuilder)
		{
			var pg = new PathGeometry(_target.Factory);

			using (var sink = pg.Open())
			{
				sink.BeginFigure(begin, filled ? FigureBegin.Filled : FigureBegin.Hollow);
				figureBuilder(sink);
				sink.EndFigure(filled ? FigureEnd.Closed : FigureEnd.Open);
				sink.Close();
			}

			return pg;
		}

		RectangleF fillRect(double x, double y, double width, double height)
		{
			if (!Stroking)
			{
				return new RectangleF(
					import(x),
					import(y),
					import(x + width),
					import(y + height));
			}

			var fs = strokeFillShift();

			return new RectangleF(
				import(x+fs),
				import(y+fs),
				import(x + width - fs),
				import(y + height - fs));
		}


		RectangleF strokeAlignedRect(double x, double y, double width, double height)
		{
			var strokeShift = strokeAlignShift();

			var rect = new RectangleF(
				import(x + strokeShift),
				import(y + strokeShift),
				import(x + width - strokeShift),
				import(y + height - strokeShift));

			return rect;
		}

		double strokeAlignShift()
		{
			var width = _strokeWeight;

			switch (_strokeAlignment)
			{
				case StrokeAlignment.Center:
					return 0;
				case StrokeAlignment.Inside:
					return width/2;
				case StrokeAlignment.Outside:
					return -width/2;
			}

			Debug.Assert(false);
			return 0;
		}

		double strokeFillShift()
		{
			var width = _strokeWeight;

			switch (_strokeAlignment)
			{
				case StrokeAlignment.Center:
					return width/2;
				case StrokeAlignment.Inside:
					return width;
				case StrokeAlignment.Outside:
					return 0;
			}

			Debug.Assert(false);
			return 0;
		}

		static float import(double d)
		{
			return d.import();
		}

		static DrawingPointF importPoint(double x, double y)
		{
			return new DrawingPointF(x.import(), y.import());
		}

		static RectangleF importRect(double x, double y, double width, double height)
		{
			return new RectangleF(x.import(), y.import(), (x + width).import(), (y + height).import());
		}
	}

	static class Conversions
	{
		public static float import(this double d)
		{
			return (float) d;
		}

		public static Color4 import(this Color color)
		{
			return new Color4(color.Red.import(), color.Green.import(), color.Blue.import(), color.Alpha.import());
		}
	}
}