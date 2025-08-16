// ---- AngelCode BmFont XML serializer ----------------------
// ---- By DeadlyDan @ deadlydan@gmail.com -------------------
// ---- There's no license restrictions, use as you will. ----
// ---- Credits to http://www.angelcode.com/ -----------------

using System.Xml.Serialization;

namespace ZGF.BMFontModule
{
	public sealed class BMFontFile
	{
		public FontInfo Info
		{
			get;
			set;
		}

		public FontCommon Common
		{
			get;
			set;
		}

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
}