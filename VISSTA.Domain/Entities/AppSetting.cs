namespace VISSTA.Domain.Entities;

public sealed class AppSetting
{
    public int Id { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    private AppSetting()
    {
    }

    public AppSetting(string key, string value)
    {
        Key = key.Trim();
        Value = value.Trim();
    }

    public void UpdateValue(string value)
    {
        Value = value.Trim();
    }
}
