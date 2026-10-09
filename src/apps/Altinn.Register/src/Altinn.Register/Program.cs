namespace Altinn.Register;

/// <summary>
/// Placeholder for the Register service. The real Register still lives in the altinn-register
/// repository and moves into this one with https://github.com/Altinn/altinn-auth/issues/4056,
/// which replaces this project entirely. Until then the project exists only so that the vertical
/// builds and its infrastructure under the vertical's infra folder can be deployed. Do not build
/// on it or wire anything to it.
/// </summary>
public static class Program
{
    /// <summary>
    /// Starts an empty web host.
    /// </summary>
    /// <param name="args">Command line arguments, passed on to the host builder.</param>
    public static void Main(string[] args)
    {
        WebApplication.CreateBuilder(args).Build().Run();
    }
}
