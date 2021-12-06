//using System.Runtime.InteropServices;
//using DiscriminatedUnions;

//namespace UnionExperiments.Unions;


//public readonly record struct Point();

//public readonly record struct Rectangle(double Width, double Length);

//public readonly record struct Circle(double Radius);

//[StructLayout(LayoutKind.Explicit)]
//public readonly struct Shape
//{

//    private enum Cases : byte { NotYetInitilaised, PointCase, RectangleCase, CircleCase }

//    [FieldOffset(0)] private readonly Cases _validCase;

//    [FieldOffset(1)] private readonly Type<Point> _typePoint = Type<Point>.Value;
//    [FieldOffset(2)] private readonly Type<Rectangle> _typeRectangle = Type<Rectangle>.Value;
//    [FieldOffset(3)] private readonly Type<Circle> _typeCircle = Type<Circle>.Value;

//    [FieldOffset(8)] private readonly Point _casePoint;
//    [FieldOffset(8)] private readonly Rectangle _caseRectangle;
//    [FieldOffset(8)] private readonly Circle _caseCircle;


//    private Shape(Point casePoint)
//    {
//        _caseCircle = default;
//        _caseRectangle = default;
//        _casePoint = casePoint;

//        _validCase = Cases.PointCase;
//    }

//    private Shape(Rectangle caseRectangle)
//    {
//        _casePoint = default;
//        _caseCircle = default;
//        _caseRectangle = caseRectangle;

//        _validCase = Cases.RectangleCase;
//    }

//    private Shape(Circle caseCircle)
//    {
//        _casePoint = default;
//        _caseRectangle = default;
//        _caseCircle = caseCircle;

//        _validCase = Cases.CircleCase;
//    }

//    public static Shape AsPoint() => new(new Point());

//    public static Shape AsRectangle(double width, double length) => new(new Rectangle(width, length));

//    public static Shape AsCircle(double radius) => new(new Circle(radius));

//    public object Case => _validCase switch {
//        Cases.PointCase => _typePoint,
//        Cases.RectangleCase => _typeRectangle,
//        Cases.CircleCase => _typeCircle,
//        Cases.NotYetInitilaised or _ => 
//            throw new InvalidOperationException("Incorrectly initialised Shape with no valid case")
//    };

//    public Point Point => _validCase is Cases.PointCase ? _casePoint : default;

//    public Rectangle Rectangle => _validCase is Cases.RectangleCase ? _caseRectangle : default;

//    public Circle Circle => _validCase is Cases.CircleCase ? _caseCircle : default;


//    public void Deconstruct(out object type, out object? value)
//    {
//        switch (_validCase)
//        {
//            case Cases.PointCase:
//                type = _typePoint;
//                value = _casePoint;
//                break;
//            case Cases.RectangleCase:
//                type = _typeRectangle;
//                value = (_caseRectangle.Width, _caseRectangle.Length);
//                break;
//            case Cases.CircleCase:
//                type = _typeCircle;
//                value = _caseCircle.Radius;
//                break;
//            default:
//                throw new InvalidOperationException("Incorrectly initialised Shape with no valid case");
//        }
//    }
//}