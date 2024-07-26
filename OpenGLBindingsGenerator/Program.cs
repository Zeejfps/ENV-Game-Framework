using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Xml;

var pathToXml = args[0];
var outPath = args[1];
Console.WriteLine($"Parsing file: {pathToXml} to {outPath}");

var xmlDoc = new XmlDocument();
xmlDoc.PreserveWhitespace = true;
xmlDoc.Load(pathToXml);
    
var root = xmlDoc.DocumentElement;
if (root == null)
{
    Console.WriteLine("Failed to parse document. Root is null");
    return;
}

// var glFeatures = root.GetElementsByTagName("feature").Cast<XmlElement>()
//     .Where(featureNode => featureNode.GetAttribute("api") == "gl");

var glFeatures = root
    .SelectNodes("feature[@api='gl']")!
    .Cast<XmlElement>()
    .ToImmutableArray();

var removed = root.GetElementsByTagName("remove").Cast<XmlElement>()
    .ToImmutableArray();

var removedCommands = removed
    .SelectMany(removedElement => removedElement.GetElementsByTagName("command").Cast<XmlElement>())
    .Select(commandElement => commandElement.GetAttribute("name"))
    .ToImmutableHashSet();

Console.WriteLine($"Removed Commands: {removedCommands.Count}");

var removedEnums = removed
    .SelectMany(removeElement => removeElement.GetElementsByTagName("enum").Cast<XmlElement>())
    .Select(enumElement => enumElement.GetAttribute("name"))
    .ToImmutableHashSet();

var requiredEnums = glFeatures
    .SelectMany(featureNode => featureNode.GetElementsByTagName("enum").Cast<XmlElement>())
    .Select(enumNode => enumNode.GetAttribute("name"))
    .Where(name => !removedEnums.Contains(name))
    .ToImmutableHashSet();

var requiredCommands = glFeatures
    .SelectMany(featureElement => featureElement.GetElementsByTagName("command").Cast<XmlElement>())
    .Select(commandElement => commandElement.GetAttribute("name"))
    .Where(name => !removedCommands.Contains(name))
    .ToImmutableHashSet();

Console.WriteLine($"Required Commands: {requiredCommands.Count}");

var enumsToProcess = root.GetElementsByTagName("enums").Cast<XmlElement>()
    .SelectMany(group => group.GetElementsByTagName("enum").Cast<XmlElement>())
    .Where(enumElement => requiredEnums.Contains(enumElement.GetAttribute("name")))
    .ToImmutableArray();

var commandsToProcess = root.GetElementsByTagName("commands").Cast<XmlElement>()
    .SelectMany(group => group.GetElementsByTagName("command").Cast<XmlElement>())
    .Select(Command.FromXmlElement)
    .Where(command => requiredCommands.Contains(command.Name))
    .ToImmutableArray();

Console.WriteLine($"Commands to process: {commandsToProcess.Length}");
// Console.WriteLine(commandsToProcess[0]);

using (var writer = new StreamWriter(outPath))
{
    //writer.WriteLine("using System.Runtime.InteropServices;");
    writer.WriteLine("using System.Runtime.CompilerServices;");
    writer.WriteLine("using System.Security;");
    writer.WriteLine(@"// ReSharper disable InconsistentNaming");
    writer.WriteLine();
    writer.WriteLine("[System.Diagnostics.CodeAnalysis.SuppressMessage(\"ReSharper\", \"IdentifierTypo\")]");
    writer.WriteLine("[SuppressUnmanagedCodeSecurity]");
    writer.WriteLine("public static unsafe class GL46");
    writer.WriteLine("{");
    
    writer.WriteLine("\tpublic delegate IntPtr GetProcAddressDelegate(string funcName);");
    writer.WriteLine();
    
    foreach (var element in enumsToProcess)
    {
        var name = element.GetAttribute("name");
        var value = element.GetAttribute("value");
        writer.WriteLine($"\tpublic const uint {name} = {value};");
    }

    writer.WriteLine();

    var i = 0;
    foreach (var command in commandsToProcess)
    {
        //writer.WriteLine("\t[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
        //var delegateName = $"{command.Name}Delegate";
        //writer.Write($"\tprivate delegate {command.Prototype}Delegate(");
        var paramTypes = "";
        var paramNames = "";
        var paramTypeAndNames = "";

        foreach (var param in command.Params)
        {
            paramTypes += $"{param.Type}, ";
            paramNames += param.Name + ", ";
            paramTypeAndNames += param.Type + " " + param.Name + ", ";
        }

        if (!string.IsNullOrEmpty(paramTypes))
            paramTypes = paramTypes.Substring(0, paramTypes.Length - 2);
 
        if (paramNames.Length > 2)
            paramNames = paramNames.Substring(0, paramNames.Length - 2);
        
        if (paramTypeAndNames.Length > 2)
            paramTypeAndNames = paramTypeAndNames.Substring(0, paramTypeAndNames.Length - 2);
        
        writer.WriteLine($"\tprivate static void* s_{command.Name};");
        // writer.WriteLine($"\tpublic static {command.Prototype}({paramsString}) => s_{command.Name}({paramNames});");
        writer.WriteLine("\t[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
        writer.Write($"\tpublic static {command.Prototype}({paramTypeAndNames}) => ");
        writer.Write($"((delegate* unmanaged[Cdecl]<{paramTypes}");
        if (!string.IsNullOrEmpty(paramTypes))
            writer.Write(", ");
        writer.WriteLine($"{command.ReturnType}>)s_{command.Name})({paramNames});");
        
        writer.WriteLine();
        i++;

        // if (i > 10)
        //     break;
    }
    
    writer.WriteLine("\tpublic static void Import(GetProcAddressDelegate getProcAddress)");
    writer.WriteLine("\t{");

    foreach (var command in commandsToProcess)
    {
        writer.WriteLine($"\t\ts_{command.Name} = (void*)getProcAddress(\"{command.Name}\");");
    }
    
    writer.WriteLine("\t}");
    
    writer.WriteLine("}");
}

static class Utils
{
    public static Dictionary<string, string> glTypeToDotNetTypeTable = new()
    {
        {"GLenum", "uint"},
        {"GLbitfield", "uint"},
        {"GLint", "int"},
        {"GLsizei", "int"},
        {"GLuint", "uint"},
        {"GLuint64", "ulong"},
        {"GLbyte", "sbyte"},
        {"GLubyte", "byte"},
        {"GLboolean", "bool"},
        {"GLfloat", "float"},
        {"GLdouble", "double"},
        {"GLintptr", "IntPtr"},
        {"GLsizeiptr", "IntPtr"},
        {"GLDEBUGPROC", "IntPtr"},
        {"GLsync", "IntPtr"},
        {"GLchar", "byte"},
        {"GLshort", "short"},
        {"GLushort", "ushort"},
        {"GLint64", "long"},
    };
}

struct Param
{
    public string Type;
    public string Name;
}

struct Command
{
    public string ReturnType;
    public string Prototype;
    public string Name;
    public Param[] Params;
    
    public static Command FromXmlElement(XmlElement element)
    {
        var command = new Command();
        var protoNode = element.SelectSingleNode("proto");
        if (protoNode != null)
        {
            var ptypeNode = protoNode.SelectSingleNode("ptype");
            if (ptypeNode != null)
            {
                var returnType = ptypeNode.InnerText;
                if (Utils.glTypeToDotNetTypeTable.TryGetValue(returnType, out var convertedType))
                {
                    returnType = convertedType;
                    command.ReturnType = returnType;
                }
                
                ptypeNode.InnerText = returnType;
            }
            
            if (protoNode.HasChildNodes)
            {
                foreach (var childNode in protoNode.ChildNodes)
                {
                    if (childNode is XmlText text)
                    {  
                        var value = text.Value;
                        if (value != null)
                            command.ReturnType += value.Replace("const", "").Trim();
                    }
                }
            }

            command.Prototype = protoNode.InnerText.Replace("const", "")
                .Replace("params ", "args")
                .Replace("string ", "str")
                .Replace("ref ", "reference");

            var nameNode = protoNode.SelectSingleNode("name");
            if (nameNode != null)
            {
                command.Name = nameNode.InnerText;
            }
        }

        var paramNodes = element.SelectNodes("param");
        if (paramNodes != null)
        {
            command.Params = new Param[paramNodes.Count];
            var i = 0;
            foreach (var paramNode in paramNodes.Cast<XmlElement>())
            {
                var param = new Param();
                
                var ptypeNode = paramNode.SelectSingleNode("ptype");
                if (ptypeNode != null)
                {
                    var type = ptypeNode.InnerText;
                    if (Utils.glTypeToDotNetTypeTable.TryGetValue(type, out var convertedType))
                    {
                        type = convertedType;
                        param.Type = type;
                        if (ptypeNode.NextSibling is XmlText textNode)
                            param.Type += textNode.InnerText.Replace("const*", "").Trim();
                    }

                    ptypeNode.InnerText = type;
                }
                
                if (paramNode.FirstChild is XmlText text)
                {
                    var value = text.Value;
                    if (value != null)
                        param.Type += value.Replace("const", "").Replace(" ", "");
                }

                var nameNode = paramNode.SelectSingleNode("name");
                if (nameNode != null)
                {
                    param.Name = nameNode.InnerText.Replace("const", "")
                        .Replace("params", "args")
                        .Replace("string", "str")
                        .Replace("ref", "reference")
                        .Trim();
                }
                
                command.Params[i] = param;
                i++;
            }
        }
        else
        {
            command.Params = Array.Empty<Param>();
        }

        return command;
    }
    
}