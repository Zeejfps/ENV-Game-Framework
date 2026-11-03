using System.Reflection;
using System.Runtime.CompilerServices;
using ZGF.ILSpy.Localization;

namespace ZGF.ILSpy.App.Tests;

public sealed class BackgroundTests
{
    [Fact]
    public void No_async_void_but_Background_Start()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var start = typeof(Background).GetMethod(nameof(Background.Start));
        var asyncVoid = typeof(Background).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(all))
            .Where(m => m.ReturnType == typeof(void) && m.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            .Where(m => m != start)
            .Select(m => $"{m.DeclaringType!.FullName}.{m.Name}")
            .ToList();
        Assert.Empty(asyncVoid);
    }

    [Fact]
    public void A_workflow_that_throws_after_awaiting_is_reported_as_unexpected()
    {
        var reported = new List<AppProblem>();
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Background.Start(Fail(resume.Task), problem =>
        {
            reported.Add(problem);
            done.SetResult();
        });

        resume.SetResult();
        done.Task.Wait(TimeSpan.FromSeconds(10));

        var problem = Assert.IsType<AppProblem.Unexpected>(Assert.Single(reported));
        Assert.IsType<InvalidOperationException>(problem.Error);
        Assert.Equal(Strings.En.ErrorUnexpected, ProblemTexts.Describe(Strings.En, problem).Title);
    }

    private static async Task Fail(Task resume)
    {
        await resume;
        throw new InvalidOperationException("after the await");
    }
}
