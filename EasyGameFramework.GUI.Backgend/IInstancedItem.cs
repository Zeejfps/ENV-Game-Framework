namespace OpenGLSandbox;

public interface IInstancedItem<TInstancedData> where TInstancedData : unmanaged
{
    event Action<IInstancedItem<TInstancedData>> BecameDirty;

    void UpdateInstanceData(ref TInstancedData instancedData);
}