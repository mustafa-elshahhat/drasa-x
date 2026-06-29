using Microsoft.AspNetCore.Builder;

namespace DerasaX.Api.Helper
{
    /// <summary>
    /// Isolates the <c>UseSwaggerUI()</c> call in its own method so the (unsigned) Swashbuckle
    /// SwaggerUI assembly is only resolved/loaded when the interactive UI is actually enabled.
    /// The JIT resolves every assembly referenced by a method when it compiles that method, so a
    /// plain <c>if</c> guard around the call in <c>Program</c> would still force the load. Keeping
    /// the reference here means the in-process integration test host (which sets
    /// <c>Swagger:DisableUi</c>) never triggers the load — relevant under Smart App Control, which
    /// blocks freshly-built unsigned DLLs on this machine.
    /// </summary>
    internal static class SwaggerUiActivation
    {
        internal static void Enable(WebApplication app) => app.UseSwaggerUI();
    }
}
