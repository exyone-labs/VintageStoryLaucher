using VSL.Domain;

namespace VSL.UI.ViewModels;

public sealed class WorldRuleItemViewModel : ObservableObject
{
    private string? _value;

    public required WorldRuleDefinition Definition { get; init; }

    public string Key => Definition.Key;

    public string Label => Definition.LabelZh;

    public IReadOnlyList<string> Choices => Definition.Choices;

    public string? Value
    {
        get => _value;
        set
        {
            if (SetProperty(ref _value, value))
            {
                OnPropertyChanged(nameof(BoolValue));
            }
        }
    }

    public bool BoolValue
    {
        get => bool.TryParse(_value, out var parsed) && parsed;
        set => Value = value ? "true" : "false";
    }

    public bool IsBoolean => Definition.Type == WorldRuleType.Boolean;

    public bool IsChoice => Definition.Type == WorldRuleType.Choice;

    public bool IsTextLike => Definition.Type is WorldRuleType.Text or WorldRuleType.Number;

    public WorldRuleValue ToDomainValue()
    {
        return new WorldRuleValue
        {
            Definition = Definition,
            Value = Value
        };
    }
}
