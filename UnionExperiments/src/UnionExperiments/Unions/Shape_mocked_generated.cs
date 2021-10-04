using UnionExperiments.Glue;

namespace UnionExperiments.Unions;


public readonly record struct Point();

public readonly record struct Rectangle(double Width, double Length);

public readonly record struct Circle(double Radius);

public readonly struct Shape
{

    private enum Cases { PointCase, RectangleCase, CircleCase }

    private readonly Point _casePoint;
    private readonly Rectangle _caseRectangle;
    private readonly Circle _caseCircle;

    private readonly Cases _validCase;

    private readonly Type<Point> _typePoint;
    private readonly Type<Rectangle> _typeRectangle;
    private readonly Type<Circle> _typeCircle;

    private readonly bool _initialised;

    private Shape(Point casePoint, Rectangle caseRectangle, Circle caseCircle, Cases validCase)
    {
        _casePoint = casePoint;
        _caseRectangle = caseRectangle;
        _caseCircle = caseCircle;

        _validCase = validCase;

        _typePoint = Type<Point>.Instance;
        _typeRectangle = Type<Rectangle>.Instance;
        _typeCircle = Type<Circle>.Instance;

        _initialised = true;
    }

    public static Shape AsPoint() => new(new Point(), default, default, Cases.PointCase);

    public static Shape AsRectangle(double width, double length) =>
        new(default, new Rectangle(width, length), default, Cases.RectangleCase);

    public static Shape AsCircle(double radius) => new(default, default, new Circle(radius), Cases.CircleCase);

    public object Case => _validCase switch {
        Cases.PointCase => _typePoint,
        Cases.RectangleCase => _typeRectangle,
        Cases.CircleCase or _ => _typeCircle
    };

    public Point Point => _casePoint;

    public Rectangle Rectangle => _caseRectangle;

    public Circle Circle => _caseCircle;


    public void Deconstruct(out object type, out object? value)
    {
        if (!_initialised) throw null!;

        switch (_validCase)
        {
            case Cases.PointCase:
                type = _typePoint;
                value = _casePoint;
                break;
            case Cases.RectangleCase:
                type = _typeRectangle;
                value = (_caseRectangle.Width, _caseRectangle.Length);
                break;
            case Cases.CircleCase:
            default:
                type = _typeCircle;
                value = _caseCircle.Radius;
                break;
        }
    }
}