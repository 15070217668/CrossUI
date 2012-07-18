﻿using SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Factory = SharpDX.DirectWrite.Factory;

namespace CrossUI.SharpDX.Drawing
{
	partial class DrawingTarget
	{
		Factory _writeFactory_;

		string _fontName = "Arial";
		double _textSize = 10;
		Brush _textBrush;
		DW.TextAlignment _textAlign;
		DW.ParagraphAlignment _paragraphAlign;
		DW.WordWrapping _wrapping;
		DW.FontWeight _fontWeight = DW.FontWeight.Normal;
		DW.FontStyle _fontStyle = DW.FontStyle.Normal;

		public void Font(string name, FontWeight? weight, FontStyle? style)
		{
			if (name != null)
				_fontName = name;

			if (weight != null)
				_fontWeight = weight.Value.import();

			if (style != null)
				_fontStyle = style.Value.import();
		}

		public void Text(
			double? size, 
			Color? color, 
			TextAlignment? alignment, 
			ParagraphAlignment? paragraphAlignment,
			WordWrapping? wordWrapping)
		{

			if (size != null)
				_textSize = size.Value;

			if (alignment != null)
				_textAlign = alignment.Value.import();

			if (color != null)
			{
				_textBrush.Dispose();
				_textBrush = new SolidColorBrush(_target, color.Value.import());
			}

			if (paragraphAlignment != null)
				_paragraphAlign = paragraphAlignment.Value.import();

			if (wordWrapping != null)
				_wrapping = wordWrapping.Value.import();
		}

		public void Text(string text, double x, double y, double width, double height)
		{
			using (var format = new DW.TextFormat(
				requireWriteFactory(), 
				_fontName, 
				null, _fontWeight, _fontStyle, DW.FontStretch.Normal, _textSize.import()))
			{
				using (var layout = new DW.TextLayout(requireWriteFactory(), text, format, width.import(), height.import()))
				{
					layout.TextAlignment = _textAlign;
					layout.ParagraphAlignment = _paragraphAlign;
					layout.WordWrapping = _wrapping;

					_target.DrawTextLayout(importPoint(x, y), layout, _textBrush);
				}
			}
		}

		public TextSize MeasureText(string text, double layoutWidth, double layoutHeight)
		{
			using (var format = new DW.TextFormat(
				requireWriteFactory(),
				_fontName,
				null, _fontWeight, _fontStyle, DW.FontStretch.Normal, _textSize.import()))
			{
				using (var layout = new DW.TextLayout(requireWriteFactory(), text, format, layoutWidth.import(), layoutHeight.import()))
				{
					layout.TextAlignment = _textAlign;
					layout.ParagraphAlignment = _paragraphAlign;
					layout.WordWrapping = _wrapping;

					return new TextSize(layout.Metrics.Width, layout.Metrics.Height);
				}
			}
		}

		Factory requireWriteFactory()
		{
			return _writeFactory_ ?? (_writeFactory_ = new Factory());
		}
	}

	static class TextConverters
	{
		public static DW.TextAlignment import(this TextAlignment alignment)
		{
			switch (alignment)
			{
				case TextAlignment.Leading: return DW.TextAlignment.Leading;
				case TextAlignment.Center: return DW.TextAlignment.Center;
				case TextAlignment.Trailing: return DW.TextAlignment.Trailing;
			}

			return default(DW.TextAlignment);
		}

		public static DW.ParagraphAlignment import(this ParagraphAlignment alignment)
		{
			switch (alignment)
			{
				case ParagraphAlignment.Near: return DW.ParagraphAlignment.Near;
				case ParagraphAlignment.Far: return DW.ParagraphAlignment.Far;
				case ParagraphAlignment.Center: return DW.ParagraphAlignment.Center;
			}

			return default(DW.ParagraphAlignment);
		}

		public static DW.WordWrapping import(this WordWrapping wrapping)
		{
			switch (wrapping)
			{
				case WordWrapping.Wrap: return DW.WordWrapping.Wrap;
				case WordWrapping.NoWrap: return DW.WordWrapping.NoWrap;
			}

			return default(DW.WordWrapping);
		}

		public static DW.FontWeight import (this FontWeight weight)
		{
			switch (weight)
			{
				case FontWeight.Normal: return DW.FontWeight.Normal;
				case FontWeight.Bold: return DW.FontWeight.Bold;
			}

			return DW.FontWeight.Normal;
		}

		public static DW.FontStyle import(this FontStyle style)
		{
			switch (style)
			{
				case FontStyle.Normal: return DW.FontStyle.Normal;
				case FontStyle.Italic: return DW.FontStyle.Italic;
			}

			return DW.FontStyle.Normal;
		}
	}
}