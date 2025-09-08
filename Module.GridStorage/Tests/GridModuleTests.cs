namespace Module.GridStorage.Tests;

public class GridModuleTests
{
    private sealed class TestItem
    {
        
    }
    
    public void GridStorageTests()
    {
        var gridStorage = new GridStorage<TestItem>(10, 10);

        var testItem = new TestItem();
        if (gridStorage.TryGetSlot(0, 0, out var slot))
        {
            
        }
        
        gridStorage.TryAdd(testItem, 2, 3, out var slot1);

        gridStorage.TryInsert(testItem, 0, 0, 2, 3);
        
        gridStorage.TryGetSlot(testItem, out var slot2);
        
        gridStorage.Remove(testItem);
    }
}