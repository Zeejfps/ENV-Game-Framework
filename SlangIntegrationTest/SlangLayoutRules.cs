namespace SlangIntegrationTest;

// SlangLayoutRules from slang.h — used by spReflection_GetTypeLayout.
// Distinct from the slang::LayoutRules enum (in Slang.cs) used by ISession.GetTypeLayout.
public enum SlangLayoutRules : uint
{
    Default = 0,
    MetalArgumentBufferTier2 = 1,
    DefaultStructuredBuffer = 2,
    DefaultConstantBuffer = 3,
}
