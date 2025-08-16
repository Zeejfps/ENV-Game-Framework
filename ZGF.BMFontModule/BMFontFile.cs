namespace ZGF.BMFontModule
{
	public sealed class BMFontFile
	{
		public FontInfo Info { get; set; }
		public FontCommon Common { get; set; }
		public List<FontPage> Pages { get; set; }
		public List<FontChar> Chars { get; set; }
		public List<FontKerning> Kernings { get; set; }

		private readonly Dictionary<int, FontChar> m_IdToCharTable = new();

		public void Update()
		{
			foreach (var c in Chars)
				m_IdToCharTable.Add(c.Id, c);
		}

		public bool TryGetFontChar(int codePoint, out FontChar c)
		{
			return m_IdToCharTable.TryGetValue(codePoint, out c);
		}
	}
}