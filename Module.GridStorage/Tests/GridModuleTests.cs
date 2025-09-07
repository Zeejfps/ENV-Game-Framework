namespace Module.GridStorage.Tests;

public class GridModuleTests
{
    sealed class TestItem
    {
        
    }
    
    public void GridStorageTests()
    {
        var gridStorage = new GridStorage<TestItem>(10, 10);

        var testItem = new TestItem();
        if (gridStorage.TryGet(0, 0, out var item, out var position))
        {
            
        }

        gridStorage.TryInsert(0, 0, 2, 3, testItem);

    }
}