// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using System.Xml;

var pathToXml = args[0];
var outPath = args[1];
var xmlDoc = new XmlDocument();
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
    writer.WriteLine("public static unsafe class Test");
    writer.WriteLine("{");

    foreach (var element in enumsToProcess)
    {
        var name = element.GetAttribute("name");
        var value = element.GetAttribute("value");
        writer.WriteLine($"\tpublic const int {name} = {value};");
    }

    writer.WriteLine();

    foreach (var command in commandsToProcess)
    {
        writer.WriteLine("\t[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
        writer.Write($"\tprivate delegate {command.ReturnType} {command.Name}Delegate(");
        var paramsString = "";
        foreach (var param in command.Params)
        {
            paramsString += $"{param.Type} {param.Name}, ";
        }

        if (!string.IsNullOrEmpty(paramsString))
            paramsString = paramsString.Substring(0, paramsString.Length - 2);
        
        writer.Write(paramsString);
        writer.WriteLine(");");
        
        writer.WriteLine();
    }
    
    writer.WriteLine("}");
}


static class Utils
{
    public static Dictionary<string, string> glTypeToDotNetTypeTable = new()
    {
        {"GLenum", "int"},
        {"GLbitfield", "int"},
        {"GLint", "int"},
        {"GLuint", "uint"},
        {"GLsizei", "uint"},
        {"GLuint64", "ulong"},
        {"GLubyte", "byte"},
        {"GLboolean", "bool"},
        {"GLfloat", "float"},
        {"GLdouble", "double"},
        {"GLintptr", "IntPtr"},
    };
}

struct Param
{
    public string Type;
    public string Name;
}

struct Command
{
    public string Name;
    public string ReturnType;
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
                    returnType = convertedType;
                command.ReturnType = returnType;
            }
            else
            {
                command.ReturnType = "void";
            }

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
                        type = convertedType;
                    param.Type = type;
                    
                    var nameNode = paramNode.SelectSingleNode("name");
                    if (nameNode != null)
                    {
                        param.Name = nameNode.InnerText;
                    }
                }
                else
                {
                    param.Type = paramNode.InnerText.Replace("const", "").Trim();
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