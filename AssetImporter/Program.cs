// See https://aka.ms/new-console-template for more information

using AssetImporter;

Console.WriteLine("Welcome to the Asset Importer");

var importMeshOption = new ImportMaterialOption();

var shouldContinue = true;

while (shouldContinue)
{
    Console.WriteLine("What would you like to do?");
    Console.WriteLine("1 - Import Mesh");
    Console.WriteLine("2 - Import Texture");
    Console.WriteLine("3 - Import Material");
    Console.WriteLine("0 - Exit");
    Console.Write("Option: ");

    var option = Console.ReadLine();
    switch (option)
    {
        case "1":
        case "2":
            Console.WriteLine("Under construction...");
            break;
        case "3":
            importMeshOption.Run();
            break;
        case "4":
            shouldContinue = false;
            break;
        default:
            Console.WriteLine("Invalid option selected");
            break;
    }
    Console.WriteLine();
}
