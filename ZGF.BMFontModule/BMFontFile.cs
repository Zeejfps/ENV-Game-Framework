﻿// ---- AngelCode BmFont XML serializer ----------------------
// ---- By DeadlyDan @ deadlydan@gmail.com -------------------
// ---- There's no license restrictions, use as you will. ----
// ---- Credits to http://www.angelcode.com/ -----------------

using System.Numerics;
using System.Xml.Serialization;

namespace ZGF.BMFontModule
{
	[Serializable]
	[XmlRoot ( "font" )]
	public sealed class BMFontFile
	{
		[XmlElement ( "info" )]
		public FontInfo Info
		{
			get;
			set;
		}

		[XmlElement ( "common" )]
		public FontCommon Common
		{
			get;
			set;
		}

		[XmlArray ( "pages" )]
		[XmlArrayItem ( "page" )]
		public List<FontPage> Pages
		{
			get;
			set;
		}

		[XmlArray ( "chars" )]
		[XmlArrayItem ( "char" )]
		public List<FontChar> Chars
		{
			get;
			set;
		}

		[XmlArray ( "kernings" )]
		[XmlArrayItem ( "kerning" )]
		public List<FontKerning> Kernings
		{
			get;
			set;
		}

		private readonly Dictionary<int, FontChar> m_IdToCharTable = new();

		public void Update()
		{
			foreach (var c in Chars)
				m_IdToCharTable.Add(c.ID, c);
		}

		public bool TryGetFontChar(int codePoint, out FontChar c)
		{
			return m_IdToCharTable.TryGetValue(codePoint, out c);
		}
	}

	[Serializable]
	public class FontInfo
	{
		[XmlAttribute ( "face" )]
		public String Face
		{
			get;
			set;
		}

		[XmlAttribute ( "size" )]
		public Int32 Size
		{
			get;
			set;
		}

		[XmlAttribute ( "bold" )]
		public Int32 Bold
		{
			get;
			set;
		}

		[XmlAttribute ( "italic" )]
		public Int32 Italic
		{
			get;
			set;
		}

		[XmlAttribute ( "charset" )]
		public String CharSet
		{
			get;
			set;
		}

		[XmlAttribute ( "unicode" )]
		public Int32 Unicode
		{
			get;
			set;
		}

		[XmlAttribute ( "stretchH" )]
		public Int32 StretchHeight
		{
			get;
			set;
		}

		[XmlAttribute ( "smooth" )]
		public Int32 Smooth
		{
			get;
			set;
		}

		[XmlAttribute ( "aa" )]
		public Int32 SuperSampling
		{
			get;
			set;
		}

		[XmlAttribute ( "padding" )]
		public String Padding
		{
			get;
			set;
		}

		private Vector2 _Spacing;
		[XmlAttribute ( "spacing" )]
		public String Spacing
		{
			get
			{
				return _Spacing.X + "," + _Spacing.Y;
			}
			set
			{
				String[] spacing = value.Split ( ',' );
				_Spacing = new Vector2 ( Convert.ToInt32 ( spacing[0] ), Convert.ToInt32 ( spacing[1] ) );
			}
		}

		[XmlAttribute ( "outline" )]
		public Int32 OutLine
		{
			get;
			set;
		}
	}

	[Serializable]
	public class FontCommon
	{
		[XmlAttribute ( "lineHeight" )]
		public Int32 LineHeight
		{
			get;
			set;
		}

		[XmlAttribute ( "base" )]
		public Int32 Base
		{
			get;
			set;
		}

		[XmlAttribute ( "scaleW" )]
		public Int32 ScaleW
		{
			get;
			set;
		}

		[XmlAttribute ( "scaleH" )]
		public Int32 ScaleH
		{
			get;
			set;
		}

		[XmlAttribute ( "pages" )]
		public Int32 Pages
		{
			get;
			set;
		}

		[XmlAttribute ( "packed" )]
		public Int32 Packed
		{
			get;
			set;
		}

		[XmlAttribute ( "alphaChnl" )]
		public Int32 AlphaChannel
		{
			get;
			set;
		}

		[XmlAttribute ( "redChnl" )]
		public Int32 RedChannel
		{
			get;
			set;
		}

		[XmlAttribute ( "greenChnl" )]
		public Int32 GreenChannel
		{
			get;
			set;
		}

		[XmlAttribute ( "blueChnl" )]
		public Int32 BlueChannel
		{
			get;
			set;
		}
	}

	[Serializable]
	public class FontPage
	{
		[XmlAttribute ( "id" )]
		public Int32 ID
		{
			get;
			set;
		}

		[XmlAttribute ( "file" )]
		public String File
		{
			get;
			set;
		}
	}

	[Serializable]
	public class FontChar
	{
		[XmlAttribute ( "id" )]
		public Int32 ID
		{
			get;
			set;
		}

		[XmlAttribute ( "x" )]
		public Int32 X
		{
			get;
			set;
		}

		[XmlAttribute ( "y" )]
		public Int32 Y
		{
			get;
			set;
		}

		[XmlAttribute ( "width" )]
		public Int32 Width
		{
			get;
			set;
		}

		[XmlAttribute ( "height" )]
		public Int32 Height
		{
			get;
			set;
		}

		[XmlAttribute ( "xoffset" )]
		public Int32 XOffset
		{
			get;
			set;
		}

		[XmlAttribute ( "yoffset" )]
		public Int32 YOffset
		{
			get;
			set;
		}

		[XmlAttribute ( "xadvance" )]
		public Int32 XAdvance
		{
			get;
			set;
		}

		[XmlAttribute ( "page" )]
		public Int32 Page
		{
			get;
			set;
		}

		[XmlAttribute ( "chnl" )]
		public Int32 Channel
		{
			get;
			set;
		}
	}

	[Serializable]
	public class FontKerning
	{
		[XmlAttribute ( "first" )]
		public Int32 First
		{
			get;
			set;
		}

		[XmlAttribute ( "second" )]
		public Int32 Second
		{
			get;
			set;
		}

		[XmlAttribute ( "amount" )]
		public Int32 Amount
		{
			get;
			set;
		}
	}
}