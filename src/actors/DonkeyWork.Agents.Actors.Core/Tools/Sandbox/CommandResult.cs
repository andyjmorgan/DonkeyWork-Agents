namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

public sealed record CommandResult(string Stdout, string Stderr, int ExitCode, bool TimedOut, int Pid);
