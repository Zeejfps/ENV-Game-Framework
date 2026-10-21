namespace ZGF.ILSpy.Core;

internal static class ILLexer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "aggressiveinlining", "aggressiveoptimization", "algorithm", "alignment", "ansi", "any", "arglist", "array",
        "as", "assembly", "assert", "at", "auto", "autochar", "beforefieldinit", "bestfit", "blob", "blob_object", "bool",
        "brnull", "brnull.s", "brzero", "brzero.s", "bstr", "bytearray", "byvalstr", "callmostderived", "carray", "catch",
        "cdecl", "cf", "char", "cil", "class", "clsid", "const", "currency", "custom", "date", "decimal", "default", "demand",
        "deny", "endmac", "enum", "error", "explicit", "extends", "extern", "false", "famandassem", "family", "famorassem",
        "fastcall", "fault", "field", "filetime", "filter", "final", "finally", "fixed", "float", "float32", "float64",
        "forwardref", "fromunmanaged", "handler", "hidebysig", "hresult", "idispatch", "il", "illegal", "implements",
        "implicitcom", "implicitres", "import", "in", "inheritcheck", "init", "initonly", "instance", "int", "int16", "int32",
        "int64", "int8", "interface", "internalcall", "iunknown", "lasterr", "lcid", "linkcheck", "literal", "localloc",
        "lpstr", "lpstruct", "lptstr", "lpvoid", "lpwstr", "managed", "marshal", "method", "modopt", "modreq", "native",
        "nested", "newslot", "noappdomain", "noinlining", "nomachine", "nomangle", "nometadata", "noncasdemand",
        "noncasinheritance", "noncaslinkdemand", "nooptimization", "noprocess", "not", "not_in_gc_heap", "notremotable",
        "notserialized", "null", "nullref", "object", "objectref", "opt", "optil", "out", "permitonly", "pinned",
        "pinvokeimpl", "prefix1", "prefix2", "prefix3", "prefix4", "prefix5", "prefix6", "prefix7", "prefixref", "prejitdeny",
        "prejitgrant", "preservesig", "private", "privatescope", "protected", "public", "record", "request", "reqmin",
        "reqopt", "reqrefuse", "reqsecobj", "request", "retval", "rtspecialname", "runtime", "safearray", "sealed",
        "sequential", "serializable", "special", "specialname", "static", "stdcall", "storage", "stored_object", "stream",
        "streamed_object", "string", "struct", "synchronized", "syschar", "sysstring", "tbstr", "thiscall", "tls", "to",
        "true", "typedref", "unicode", "unmanaged", "unmanagedexp", "unsigned", "unused", "userdefined", "valuetype",
        "vararg", "variant", "vector", "virtual", "void", "wchar", "winapi", "with", "wrapper", "uint", "uint8", "uint16",
        "uint32", "uint64", "native", "flags", "nullable", "readonly", "nested", "auto", "rtspecialname",
    };

    public static void Classify(string text, int start, int end, ref bool inComment, List<DocumentSpan> output)
    {
        var i = start;
        while (i < end)
        {
            if (inComment)
            {
                Add(output, i, end, SpanKind.Comment);
                return;
            }

            var c = text[i];
            var tokenStart = i;
            SpanKind kind;
            if (c == '/' && i + 1 < end && text[i + 1] == '/')
            {
                inComment = true;
                continue;
            }
            if (c is '"' or '\'')
            {
                i++;
                while (i < end && text[i] != c)
                    i += text[i] == '\\' && i + 1 < end ? 2 : 1;
                i = Math.Min(i + 1, end);
                kind = c == '"' ? SpanKind.String : SpanKind.Plain;
            }
            else if (char.IsDigit(c))
            {
                while (i < end && (char.IsAsciiHexDigit(text[i]) || text[i] is 'x' or '.'))
                    i++;
                kind = SpanKind.Number;
            }
            else if (char.IsLetter(c) || c is '_' or '.')
            {
                i++;
                while (i < end && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '.' or '`' or '$' or '@'))
                    i++;
                var word = text.AsSpan(tokenStart, i - tokenStart);
                if (word is ".ctor" or ".cctor")
                    kind = SpanKind.Method;
                else if (word.Length > 1 && word[0] == '.' && char.IsLetter(word[1]))
                    kind = SpanKind.Keyword;
                else if (word.StartsWith("IL_"))
                    kind = SpanKind.ILLabel;
                else
                    kind = Keywords.GetAlternateLookup<ReadOnlySpan<char>>().Contains(word) ? SpanKind.Keyword : SpanKind.Plain;
            }
            else
            {
                i++;
                kind = SpanKind.Plain;
            }
            Add(output, tokenStart, i, kind);
        }
    }

    private static void Add(List<DocumentSpan> output, int start, int end, SpanKind kind)
    {
        if (end <= start) return;
        if (output.Count > 0)
        {
            var last = output[^1];
            if (last.Kind == kind && last.Reference is null && !last.IsDefinition && last.End == start)
            {
                output[^1] = last with { Length = end - last.Start };
                return;
            }
        }
        output.Add(new DocumentSpan(start, end - start, kind, null, false));
    }
}
