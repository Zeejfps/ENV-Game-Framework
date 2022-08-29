using System.Buffers;
using EasyGameFramework.API;
using static OpenGL.Gl;

namespace GlfwOpenGLBackend;

internal class ShaderStorageBuffer_GL : IBuffer
{
    public uint BufferId { get; }
    
    private readonly ShaderStorageBufferApi_GL m_Handle;

    public ShaderStorageBuffer_GL(uint index)
    {
        BufferId = glGenBuffer();
        glAssertNoError();
        glBindBufferBase(GL_SHADER_STORAGE_BUFFER, index, BufferId);
        glAssertNoError();

        m_Handle = new ShaderStorageBufferApi_GL(this);
    }
    
    public IBufferHandle Use()
    {
        glBindBuffer(GL_SHADER_STORAGE_BUFFER, BufferId);
        glAssertNoError();
        return m_Handle;
    }

    class ShaderStorageBufferApi_GL : IBufferHandle
    {
        private int m_Ptr;
        private bool m_NeedsResizing;
        private byte[] m_Data;
        private readonly ShaderStorageBuffer_GL m_Buffer;

        public ShaderStorageBufferApi_GL(ShaderStorageBuffer_GL buffer)
        {
            m_Buffer = buffer;
            m_Data = new byte[256];
            m_NeedsResizing = true;
        }

        public void Clear()
        {
            m_Ptr = 0;
        }

        public void Put<T>(T data) where T : unmanaged
        {
            unsafe
            {
                Write(new Span<byte>(&data, sizeof(T)));
            }
        }

        public void Put<T>(Span<T> data) where T : unmanaged
        {
            unsafe 
            {
                fixed (void* p = &data[0])
                    Write(new Span<byte>(p, sizeof(T) * data.Length));
            }
        }

        public void Apply()
        {
            unsafe
            {
                fixed (void* p = &m_Data[0])
                {
                    if (m_NeedsResizing)
                    {
                        glBufferData(GL_SHADER_STORAGE_BUFFER, m_Data.Length, p, GL_DYNAMIC_COPY);
                        m_NeedsResizing = false;
                    }
                    else
                    {
                        glBufferSubData(GL_SHADER_STORAGE_BUFFER, 0, m_Data.Length, p);
                    }
                    glAssertNoError();
                }
            }
        }

        private void Write(Span<byte> newData)
        {
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(newData.Length);
            try
            {
                newData.CopyTo(sharedBuffer);
                Write(sharedBuffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        private void Write(byte[] newData)
        {
            if (m_Ptr + newData.Length >= m_Data.Length)
            {
                var newLength = Math.Max(m_Data.Length * 2, m_Data.Length + newData.Length);
                var oldData = m_Data;
                m_Data = new byte[newLength];
                Buffer.BlockCopy(oldData, 0, m_Data, 0, oldData.Length);
                m_NeedsResizing = true;
            }
                
            Buffer.BlockCopy(newData, 0, m_Data, m_Ptr, newData.Length);
            m_Ptr += newData.Length;
        }
        
        public void Dispose()
        {
            glBindBuffer(GL_SHADER_STORAGE_BUFFER, 0);
        }
    }
}