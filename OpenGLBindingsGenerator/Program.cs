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

var glFeatures = root.GetElementsByTagName("feature").Cast<XmlElement>()
    .Where(featureNode => featureNode.GetAttribute("api") == "gl");

var removed = root.GetElementsByTagName("remove").Cast<XmlElement>()
    .ToImmutableArray();

var removedCommands = removed
    .SelectMany(removedElement => removedElement.GetElementsByTagName("command").Cast<XmlElement>())
    .Select(commandElement => commandElement.GetAttribute("name"))
    .ToImmutableHashSet();

var removedEnums = removed
    .SelectMany(removeElement => removeElement.GetElementsByTagName("enum").Cast<XmlElement>())
    .Select(enumElement => enumElement.GetAttribute("name"))
    .ToImmutableHashSet();

var requiredEnums = glFeatures
    .SelectMany(featureNode => featureNode.GetElementsByTagName("enum").Cast<XmlElement>())
    .Select(enumNode => enumNode.GetAttribute("name"))
    .Where(name => !removedEnums.Contains(name))
    .ToImmutableHashSet();

var enumNameToValueMap = root.GetElementsByTagName("enums").Cast<XmlElement>()
    .SelectMany(group => group.GetElementsByTagName("enum").Cast<XmlElement>())
    .Where(enumElement => requiredEnums.Contains(enumElement.GetAttribute("name")))
    .ToImmutableArray();

using (var writer = new StreamWriter(outPath))
{
    writer.WriteLine("public static unsafe class Test");
    writer.WriteLine("{");

    foreach (var element in enumNameToValueMap)
    {
        var name = element.GetAttribute("name");
        var value = element.GetAttribute("value");
        writer.WriteLine($"\tpublic const int {name} = {value};");
    }
    
    writer.WriteLine("{");
}