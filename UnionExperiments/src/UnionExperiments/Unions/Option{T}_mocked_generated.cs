using DiscriminatedUnions;

namespace UnionExperiments.Unions;


public readonly record struct None();

public readonly record struct Some<T>(T Value);

public readonly partial struct Option<T>
{

    private enum Cases { NoneCase, SomeCase }

    private readonly None _caseNone;
    private readonly Some<T> _caseSome;
    private readonly Cases _validCase;

    private readonly Type<None> _typeNone;
    private readonly Type<Some<T>> _typeSome;
    private readonly bool _initialised;

    private Option(None caseNone, Some<T> caseSome, Cases validCase)
    {
        _caseNone = caseNone;
        _caseSome = caseSome;

        _validCase = validCase;

        _typeNone = Type<None>.Value;
        _typeSome = Type<Some<T>>.Value;
        _initialised = true;
    }

    public static partial Option<T> AsNone() => new(new None(), default, Cases.NoneCase);
    public static partial Option<T> AsSome(T value) => new(default, new Some<T>(value), Cases.SomeCase);

    public object Case => _validCase switch {
        Cases.NoneCase => _typeNone,
        Cases.SomeCase or _ => _typeSome
    };

    public None None => _caseNone;

    public Some<T> Some => _caseSome;

    public void Deconstruct(out object type, out object? value)
    {
        if (!_initialised) throw null!;

        switch (_validCase)
        {
            case Cases.NoneCase:
                type = _typeNone;
                value = _caseNone;
                break;
            case Cases.SomeCase: 
            default:
                type = _typeSome;
                value = _caseSome.Value;
                break;
        }
    }
}