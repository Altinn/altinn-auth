using Altinn.AccessMgmt.FFB.Services.Contracts;

namespace Altinn.AccessMgmt.FFB.Services;

public class EnvironmentState
{
    private string _current;

    public event Action? OnChange;

    public EnvironmentState(IEnvironmentDbContextFactory factory)
    {
        _current = factory.Environments.FirstOrDefault(factory.IsConfigured) ?? string.Empty;
    }

    public string Current
    {
        get => _current;
        set
        {
            if (_current == value)
            {
                return;
            }

            _current = value;
            OnChange?.Invoke();
        }
    }
}
