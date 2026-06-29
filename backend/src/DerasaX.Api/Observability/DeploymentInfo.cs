using System.Reflection;

namespace DerasaX.Api.Observability
{
    /// <summary>
    /// Phase 19 — deployment identity surfaced on /health and operational-status so an
    /// operator can tell WHICH build produced an error. No secrets; version only.
    /// </summary>
    public static class DeploymentInfo
    {
        public const string ServiceName = "derasax-backend";

        public static string Version { get; } =
            typeof(DeploymentInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(DeploymentInfo).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
