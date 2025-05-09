[Obsolete]
public struct FixedPoint
{
    private const int FractionalBits = 16;
    private const int FractionalMultiplier = 1 << FractionalBits;
    
    private int value;
    
    public FixedPoint(int value)
    {
        this.value = value;
    }
    
    public static FixedPoint operator +(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint(a.value + b.value);
    }
    
    public static FixedPoint operator -(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint(a.value - b.value);
    }
    
    public static FixedPoint operator *(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint((a.value * b.value) >> FractionalBits);
    }
    
    public static FixedPoint operator /(FixedPoint a, FixedPoint b)
    {
        return new FixedPoint((a.value << FractionalBits) / b.value);
    }
    
    public static implicit operator FixedPoint(int value)
    {
        return new FixedPoint(value << FractionalBits);
    }
    
    public static implicit operator FixedPoint(float value)
    {
        return new FixedPoint((int)(value * FractionalMultiplier));
    }
    
    public static implicit operator int(FixedPoint value)
    {
        return value.value >> FractionalBits;
    }
    
    public static implicit operator float(FixedPoint value)
    {
        return (float)value.value / FractionalMultiplier;
    }
}