namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

/// <summary>
/// Coordination handle that lets the grain fire off sandbox provisioning early
/// and lets SandboxTools await the result when the LLM first calls sandbox_exec.
/// </summary>
public sealed class SandboxProvisioningHandle
{
    private TaskCompletionSource<string> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Await this to get the sandbox pod name once provisioning completes.
    /// </summary>
    public Task<string> Task => _tcs.Task;

    /// <summary>
    /// True if provisioning failed and a retry is possible.
    /// </summary>
    public bool Failed { get; private set; }

    /// <summary>
    /// Called by the grain when the sandbox pod is ready.
    /// </summary>
    public void SetResult(string podName) => _tcs.TrySetResult(podName);

    /// <summary>
    /// Called by the grain when provisioning fails.
    /// </summary>
    public void SetFailed(Exception ex)
    {
        Failed = true;
        _tcs.TrySetException(ex);
    }

    /// <summary>
    /// Resets the handle so a subsequent turn can retry provisioning.
    /// </summary>
    public void PrepareRetry()
    {
        Failed = false;
        _tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
