using System.Linq;
using System.Runtime.InteropServices;

namespace Aardvark.Base
{
    /// <summary>
    /// A two-dimensional ray with an origin and a direction.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Ray2d : IValidity, IBoundingBox2d
    {
        public V2d Origin;
        public V2d Direction;

        #region Constructors

        /// <summary>
        /// Creates Ray from origin point and directional vector
        /// </summary>
        public Ray2d(V2d origin, V2d direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public static Ray2d FromEndPoints(V2d origin, V2d target)
        {
            return new Ray2d(origin, target - origin);
        }

        #endregion

        #region Constants

        /// <summary>
        /// An invalid ray has a zero direction.
        /// </summary>
        public static readonly Ray2d Invalid = new Ray2d(V2d.NaN, V2d.Zero);

        #endregion

        #region Properties

        public bool IsValid { get { return Direction != V2d.Zero; } }
        public bool IsInvalid { get { return Direction == V2d.Zero; } }

        public Line2d Line2d
        {
            get { return new Line2d(Origin, Origin + Direction); }
        }

        public Plane2d Plane2d
        {
            get 
            {
                V2d n = new V2d(-Direction.Y, Direction.X);
                return new Plane2d(n, Origin);
            }
        }

        public Ray2d Reversed
        {
            get { return new Ray2d(Origin, -Direction); }
        }

        #endregion

        #region Ray Arithmetics

        /// <summary>
        /// Gets the point on the ray that is t * direction from origin.
        /// </summary>
        public V2d GetPointOnRay(double t)
        {
            return (Origin + Direction * t);
        }

        /// <summary>
        /// Gets segment on the ray starting at range.Min * direction from origin
        /// and ending at range.Max * direction from origin.
        /// </summary>
        public Line2d GetLine2dOnRay(Range1d range)
        {
            return new Line2d(Origin + Direction * range.Min, Origin + Direction * range.Max);
        }

        /// <summary>
        /// Gets segment on the ray starting at tMin * direction from origin
        /// and ending at tMax * direction from origin.
        /// </summary>
        public Line2d GetLine2dOnRay(double tMin, double tMax)
        {
            return new Line2d(Origin + Direction * tMin, Origin + Direction * tMax);
        }

        /// <summary>
        /// Gets the t for a point p on this ray. 
        /// </summary>
        public double GetT(V2d p)
        {
            var v = p - Origin;
            return (Direction.X.Abs() > Direction.Y.Abs())
                ? (v.X / Direction.X)
                : (v.Y / Direction.Y);
        }

        /// <summary>
        /// Gets the point on the ray that is closest to the given point.
        /// Ray direction must be normalized (length 1).
        /// </summary>
        public V2d GetClosestPointOnRay(V2d p)
        {
            return Origin + Direction * Direction.Dot(p - Origin);
        }

        public double GetDistanceToRay(V2d p)
        {
            var f = GetClosestPointOnRay(p);
            return (f - p).Length;
        }

        public V2d Intersect(Ray2d r)
        {
            V2d a = r.Origin - Origin;
            if (a.Abs.AllSmaller(Constant<double>.PositiveTinyValue))
                return Origin; // Early exit when rays have same origin

            double cross = Direction.Dot270(r.Direction);
            if (!Fun.IsTiny(cross)) // Rays not parallel
                return Origin + Direction * r.Direction.Dot90(a) / cross;
            else // Rays are parallel
                return V2d.NaN;
        }

        public V2d Intersect(V2d dirVector)
        {
            if (Origin.Abs.AllSmaller(Constant<double>.PositiveTinyValue))
                return Origin; // Early exit when rays have same origin

            double cross = Direction.Dot270(dirVector);
            if (!Fun.IsTiny(cross)) // Rays not parallel
                return Origin + Direction * dirVector.Dot270(Origin) / cross;
            else // Rays are parallel
                return V2d.NaN;
        }

        /// <summary>
        /// Returns a signed value where left is negative and right positive.
        /// The magnitude is equal to the double size of the triangle the ray + direction and p.
        /// </summary>
        public double GetPointSide(V2d p)
        {
            return Direction.Dot90(p - Origin);
        }

        #endregion

        #region Comparison Operators

        public static bool operator ==(Ray2d a, Ray2d b)
        {
            return (a.Origin == b.Origin) && (a.Direction == b.Direction);
        }

        public static bool operator !=(Ray2d a, Ray2d b)
        {
            return !((a.Origin == b.Origin) && (a.Direction == b.Direction));
        }

        public int LexicalCompare(Ray2d other)
        {
            var cmp = Origin.LexicalCompare(other.Origin);
            if (cmp != 0) return cmp;
            return Direction.LexicalCompare(other.Direction);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Calculates Hash-code of the given ray.
        /// </summary>
        /// <returns>Hash-code.</returns>
        public override int GetHashCode()
        {
            return HashCode.GetCombined(Origin, Direction);
        }

        /// <summary>
        /// Checks if 2 objects are equal.
        /// </summary>
        /// <returns>Result of comparison.</returns>
        public override bool Equals(object other)
        {
            if (other is Ray2d)
            {
                var value = (Ray2d)other;
                return (Origin == value.Origin) && (Direction == value.Direction);
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format(Localization.FormatEnUS, "[{0}, {1}]", Origin, Direction);
        }

        public static Ray2d Parse(string s)
        {
            var x = s.NestedBracketSplitLevelOne().ToArray();
            return new Ray2d(V2d.Parse(x[0]), V2d.Parse(x[1]));
        }

        #endregion

        #region IBoundingBox2d

        public Box2d BoundingBox2d
        {
            get
            {
                return Box2d.FromPoints(Origin, Direction + Origin);
            }
        }

        #endregion
    }

    /// <summary>
    /// A fast ray contains a ray and a number of precomputed flags and
    /// fields for fast intersection computation with bounding boxes and
    /// other axis-aligned sturctures such as kd-Trees.
    /// </summary>
    public struct FastRay2d
    {
        public readonly Ray2d Ray;
        public readonly Vec.DirFlags DirFlags;
        public readonly V2d InvDir;

        #region Constructors

        public FastRay2d(Ray2d ray)
        {
            Ray = ray;
            DirFlags = ray.Direction.DirFlags();
            InvDir = 1.0 / ray.Direction;
        }

        public FastRay2d(V2d origin, V2d direction)
            : this(new Ray2d(origin, direction))
        { }

        #endregion

        #region Ray Arithmetics

        public bool Intersects(
            Box2d box,
            ref double tmin,
            ref double tmax
        )
        {
            var dirFlags = DirFlags;

            if ((dirFlags & Vec.DirFlags.PositiveX) != 0)
            {
                {
                    double t = (box.Max.X - Ray.Origin.X) * InvDir.X;
                    if (t <= tmin) return false;
                    if (t < tmax) tmax = t;
                }
                {
                    double t = (box.Min.X - Ray.Origin.X) * InvDir.X;
                    if (t >= tmax) return false;
                    if (t > tmin) tmin = t;
                }
            }
            else if ((dirFlags & Vec.DirFlags.NegativeX) != 0)
            {
                {
                    double t = (box.Min.X - Ray.Origin.X) * InvDir.X;
                    if (t <= tmin) return false;
                    if (t < tmax) tmax = t;
                }
                {
                    double t = (box.Max.X - Ray.Origin.X) * InvDir.X;
                    if (t >= tmax) return false;
                    if (t > tmin) tmin = t;
                }
            }
            else    // ray parallel to X-plane
            {
                if (Ray.Origin.X < box.Min.X || Ray.Origin.X > box.Max.X)
                    return false;
            }

            if ((dirFlags & Vec.DirFlags.PositiveY) != 0)
            {
                {
                    double t = (box.Max.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t <= tmin) return false;
                    if (t < tmax) tmax = t;
                }
                {
                    double t = (box.Min.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t >= tmax) return false;
                    if (t > tmin) tmin = t;
                }
            }
            else if ((dirFlags & Vec.DirFlags.NegativeY) != 0)
            {
                {
                    double t = (box.Min.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t <= tmin) return false;
                    if (t < tmax) tmax = t;
                }
                {
                    double t = (box.Max.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t >= tmax) return false;
                    if (t > tmin) tmin = t;
                }
            }
            else    // ray parallel to Y-plane
            {
                if (Ray.Origin.Y < box.Min.Y || Ray.Origin.Y > box.Max.Y)
                    return false;
            }

            if (tmin > tmax) return false;

            return true;
        }

        /// <summary>
        /// This variant of the intersection method returns the affected
        /// planes of the box if the box was hit.
        /// </summary>
        public bool Intersects(
            Box2d box,
            ref double tmin,
            ref double tmax,
            out Box.Flags tminFlags,
            out Box.Flags tmaxFlags
        )
        {
            var dirFlags = DirFlags;
            tminFlags = Box.Flags.None;
            tmaxFlags = Box.Flags.None;

            if ((dirFlags & Vec.DirFlags.PositiveX) != 0)
            {
                {
                    double t = (box.Max.X - Ray.Origin.X) * InvDir.X;
                    if (t <= tmin) return false;
                    if (t < tmax) { tmax = t; tmaxFlags = Box.Flags.MaxX; }
                }
                {
                    double t = (box.Min.X - Ray.Origin.X) * InvDir.X;
                    if (t >= tmax) return false;
                    if (t > tmin) { tmin = t; tminFlags = Box.Flags.MinX; }
                }
            }
            else if ((dirFlags & Vec.DirFlags.NegativeX) != 0)
            {
                {
                    double t = (box.Min.X - Ray.Origin.X) * InvDir.X;
                    if (t <= tmin) return false;
                    if (t < tmax) { tmax = t; tmaxFlags = Box.Flags.MinX; }
                }
                {
                    double t = (box.Max.X - Ray.Origin.X) * InvDir.X;
                    if (t >= tmax) return false;
                    if (t > tmin) { tmin = t; tminFlags = Box.Flags.MaxX; }
                }
            }
            else    // ray parallel to X-plane
            {
                if (Ray.Origin.X < box.Min.X || Ray.Origin.X > box.Max.X)
                    return false;
            }

            if ((dirFlags & Vec.DirFlags.PositiveY) != 0)
            {
                {
                    double t = (box.Max.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t <= tmin) return false;
                    if (t < tmax) { tmax = t; tmaxFlags = Box.Flags.MaxY; }
                }
                {
                    double t = (box.Min.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t >= tmax) return false;
                    if (t > tmin) { tmin = t; tminFlags = Box.Flags.MinY; }
                }
            }
            else if ((dirFlags & Vec.DirFlags.NegativeY) != 0)
            {
                {
                    double t = (box.Min.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t <= tmin) return false;
                    if (t < tmax) { tmax = t; tmaxFlags = Box.Flags.MinY; }
                }
                {
                    double t = (box.Max.Y - Ray.Origin.Y) * InvDir.Y;
                    if (t >= tmax) return false;
                    if (t > tmin) { tmin = t; tminFlags = Box.Flags.MaxY; }
                }
            }
            else    // ray parallel to Y-plane
            {
                if (Ray.Origin.Y < box.Min.Y || Ray.Origin.Y > box.Max.Y)
                    return false;
            }

            if (tmin > tmax) return false;

            return true;
        }

        #endregion
    }


}
