using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Aardvark.Base
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rot2d
    {
        public double Angle;

        #region Constructors

        public Rot2d(double angleInRadians)
        {
            Angle = angleInRadians;
        }

        #endregion

        #region Constants

        public static readonly Rot2d Identity = new Rot2d(0);

        #endregion

        #region Rotation Arithmetics

        public void Invert()
        {
            Angle = -Angle;
        }

        public Rot2d Inverse
        {
            get
            {
                return new Rot2d(-Angle);
            }
        }

        /// <summary>
        /// Adds 2 rotations.
        /// </summary>
        public static Rot2d Add(Rot2d r0, Rot2d r1)
        {
            return new Rot2d(r0.Angle + r1.Angle);
        }

        /// <summary>
        /// Adds scalar to a rotation.
        /// </summary>
        public static Rot2d Add(Rot2d rot, double val)
        {
            return new Rot2d(rot.Angle + val);
        }

        /// <summary>
        /// Subtracts 2 rotations.
        /// </summary>
        public static Rot2d Subtract(Rot2d r0, Rot2d r1)
        {
            return new Rot2d(r0.Angle - r1.Angle);
        }

        /// <summary>
        /// Subtracts scalar from a rotation.
        /// </summary>
        public static Rot2d Subtract(Rot2d rot, double angle)
        {
            return new Rot2d(rot.Angle - angle);
        }

        /// <summary>
        /// Subtracts rotation from a scalar.
        /// </summary>
        public static Rot2d Subtract(double angle, Rot2d rot)
        {
            return new Rot2d(angle - rot.Angle);
        }

        /// <summary>
        /// Multiplies scalar with a rotation.
        /// </summary>
        public static Rot2d Multiply(Rot2d rot, double val)
        {
            return new Rot2d(rot.Angle * val);
        }

        public static V2d Multiply(Rot2d rot, V2d vec)
        {

            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new V2d( a * vec.X +
                            b * vec.Y,

                           -b * vec.X +
                            a * vec.Y);
        }

        public static V3d Multiply(Rot2d rot, V3d vec)
        {
            double ca = (double)System.Math.Cos(rot.Angle);
            double sa = (double)System.Math.Sin(rot.Angle);

            return new V3d( ca * vec.X + sa * vec.Y,
                           -sa * vec.X + ca * vec.Y,
                           vec.Z);
        }

        public static V4d Multiply(Rot2d rot, V4d vec)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new V4d( a * vec.X +
                            b * vec.Y,

                           -b * vec.X +
                            a * vec.Y,

                            vec.Z,

                            vec.W);
        }

        public static M22d Multiply(Rot2d rot, M22d mat)
        {
            return (M22d)rot * mat;
        }

        public static M33d Multiply(Rot2d rot, M33d mat)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new M33d( a * mat.M00 +
                             b * mat.M10,

                             a * mat.M01 +
                             b * mat.M11,

                             a * mat.M02 +
                             b * mat.M12,

                            -b * mat.M00 +
                             a * mat.M10,

                            -b * mat.M01 +
                             a * mat.M11,

                            -b * mat.M02 +
                             a * mat.M12,

                             mat.M20,

                             mat.M21,

                             mat.M22);
        }

        public static M34d Multiply(Rot2d rot, M34d mat)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new M34d( a * mat.M00 +
                             b * mat.M10,

                             a * mat.M01 +
                             b * mat.M11,

                             a * mat.M02 +
                             b * mat.M12,

                             a * mat.M03 +
                             b * mat.M13,

                            -b * mat.M00 +
                             a * mat.M10,

                            -b * mat.M01 +
                             a * mat.M11,

                            -b * mat.M02 +
                             a * mat.M12,

                            -b * mat.M03 +
                             a * mat.M13,

                             mat.M20,

                             mat.M21,

                             mat.M22,

                             mat.M23);
        }

        public static M44d Multiply(Rot2d rot, M44d mat)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new M44d( a * mat.M00 +
                             b * mat.M10,

                             a * mat.M01 +
                             b * mat.M11,

                             a * mat.M02 +
                             b * mat.M12,

                             a * mat.M03 +
                             b * mat.M13,

                            -b * mat.M00 +
                             a * mat.M10,

                            -b * mat.M01 +
                             a * mat.M11,

                            -b * mat.M02 +
                             a * mat.M12,

                            -b * mat.M03 +
                             a * mat.M13,

                             mat.M20,

                             mat.M21,

                             mat.M22,

                             mat.M23,

                             mat.M30,

                             mat.M31,

                             mat.M32,

                             mat.M33);
        }

        public static M33d Multiply(Rot2d rot2, Rot3d rot3)
        {
            return Rot2d.Multiply(rot2, (M33d)rot3);
        }

        /// <summary>
        /// Multiplies 2 rotations.
        /// </summary>
        public static Rot2d Multiply(Rot2d r0, Rot2d r2)
        {
            return new Rot2d(r0.Angle * r2.Angle);
        }

        public static M33d Multiply(Rot2d rot, Scale3d scale)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new M33d( a * scale.X,
                             b * scale.Y,
                             0,

                            -b * scale.X,
                             a * scale.Y,
                             0,

                             0,
                             0,
                             scale.Z);
        }

        public static M34d Multiply(Rot2d rot, Shift3d shift)
        {
            double a = (double)System.Math.Cos(rot.Angle);
            double b = (double)System.Math.Sin(rot.Angle);

            return new M34d( a, b, 0, a * shift.X + b * shift.Y,
                            -b, a, 0, -b * shift.X + a * shift.Y,
                             0, 0, 1, shift.Z);

        }

        /// <summary>
        /// Divides scalar by a rotation.
        /// </summary>
        public static Rot2d Divide(Rot2d rot, double val)
        {
            return new Rot2d(rot.Angle / val);
        }

        /// <summary>
        /// Negates rotation.
        /// </summary>
        public static Rot2d Negate(Rot2d rot)
        {
            return new Rot2d(-rot.Angle);
        }

        /// <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public static V2d TransformDir(Rot2d rot, V2d v)
        {
            return (M22d)rot * v;
        }


        /// <summary>
        /// Inverse Transforms a Direction Vector
        /// </summary>
        public static V2d InvTransformDir(Rot2d rot, V2d v)
        {
            return (M22d)Rot2d.Negate(rot) * v;
        }


        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public static V2d TransformPos(Rot2d rot, V2d v)
        {
            return (M22d)rot * v;
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public static V2d InvTransformPos(Rot2d rot, V2d v)
        {
            return (M22d)Rot2d.Negate(rot) * v;
        }

        /// <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public V2d TransformDir(V2d v)
        {
            return Rot2d.TransformDir(this, v);
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public V2d TransformPos(V2d v)
        {
            return Rot2d.TransformPos(this, v);
        }

        // <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public V2d InvTransformDir(V2d v)
        {
            return Rot2d.InvTransformDir(this, v);
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public V2d InvTransformPos(V2d v)
        {
            return Rot2d.InvTransformPos(this, v);
        }

        #endregion

        #region Arithmetic operators

        /// <summary>
        /// Negates the rotation.
        /// </summary>
        public static Rot2d operator -(Rot2d rot)
        {
            return Rot2d.Negate(rot);
        }

        /// <summary>
        /// Adds 2 rotations.
        /// </summary>
        public static Rot2d operator +(Rot2d r0, Rot2d r1)
        {
            return Rot2d.Add(r0, r1);
        }

        /// <summary>
        /// Adds a rotation and a scalar value.
        /// </summary>
        public static Rot2d operator +(Rot2d rot, double angle)
        {
            return Rot2d.Add(rot, angle);
        }

        /// <summary>
        /// Adds a rotation and a scalar value.
        /// </summary>
        public static Rot2d operator +(double angle, Rot2d rot)
        {
            return Rot2d.Add(rot, angle);
        }

        /// <summary>
        /// Subtracts 2 rotations.
        /// </summary>
        public static Rot2d operator -(Rot2d r0, Rot2d r1)
        {
            return Rot2d.Subtract(r0, r1);
        }

        /// <summary>
        /// Subtracts a rotation from a scalar value.
        /// </summary>
        public static Rot2d operator -(Rot2d rot, double angle)
        {
            return Rot2d.Subtract(rot, angle);
        }

        /// <summary>
        /// Subtracts a rotation from a scalar value.
        /// </summary>
        public static Rot2d operator -(double angle, Rot2d rot)
        {
            return Rot2d.Subtract(angle, rot);
        }

        /// <summary>
        /// Multiplies rotation with scalar value.
        /// </summary>
        public static Rot2d operator *(Rot2d rot, double val)
        {
            return Rot2d.Multiply(rot, val);
        }

        /// <summary>
        /// Multiplies rotation with scalar value.
        /// </summary>
        public static Rot2d operator *(double val, Rot2d rot)
        {
            return Rot2d.Multiply(rot, val);
        }

        /// <summary>
        /// </summary>
        public static V2d operator *(Rot2d rot, V2d vec)
        {
            return Rot2d.Multiply(rot, vec);
        }

        /// <summary>
        /// </summary>
        public static V3d operator *(Rot2d rot, V3d vec)
        {
            return Rot2d.Multiply(rot, vec);
        }

        /// <summary>
        /// </summary>
        public static V4d operator *(Rot2d rot, V4d vec)
        {
            return Rot2d.Multiply(rot, vec);
        }

        public static M22d operator *(Rot2d rot, M22d mat)
        {
            return Rot2d.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M33d operator *(Rot2d rot, M33d mat)
        {
            return Rot2d.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M34d operator *(Rot2d rot, M34d mat)
        {
            return Rot2d.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M44d operator *(Rot2d rot, M44d mat)
        {
            return Rot2d.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M33d operator *(Rot2d rot2, Rot3d rot3)
        {
            return Rot2d.Multiply(rot2, rot3);
        }

        /// <summary>
        /// </summary>
        public static Rot2d operator *(Rot2d r0, Rot2d r1)
        {
            return Rot2d.Multiply(r0, r1);
        }

        /// <summary>
        /// </summary>
        public static M33d operator *(Rot2d rot, Scale3d scale)
        {
            return Rot2d.Multiply(rot, scale);
        }

        /// <summary>
        /// </summary>
        public static M34d operator *(Rot2d rot, Shift3d shift)
        {
            return Rot2d.Multiply(rot, shift);
        }

        /// <summary>
        /// Divides rotation by scalar value.
        /// </summary>
        public static Rot2d operator /(Rot2d rot, double val)
        {
            return Rot2d.Divide(rot, val);
        }

        #endregion

        #region Comparison Operators

        /// <summary>
        /// Checks if 2 rotations are equal.
        /// </summary>
        /// <returns>Result of comparison.</returns>
        public static bool operator ==(Rot2d rotation1, Rot2d rotation2)
        {
            return (rotation1.Angle == rotation2.Angle);
        }

        /// <summary>
        /// Checks if 2 rotations are not equal.
        /// </summary>
        /// <returns>Result of comparison.</returns>
        public static bool operator !=(Rot2d rotation1, Rot2d rotation2)
        {
            return !(rotation1.Angle == rotation2.Angle);
        }


        public static bool ApproxEqual(Rot2d r0, Rot2d r1)
        {
            return ApproxEqual(r0, r1, Constant<double>.PositiveTinyValue);
        }

        // [todo ISSUE 20090225 andi : andi] Wir sollten auch folgendes ber�cksichtigen -q == q, weil es die selbe rotation definiert.
        // [todo ISSUE 20090427 andi : andi] use an angle-tolerance
        // [todo ISSUE 20090427 andi : andi] add Rot3d.ApproxEqual(Rot3d other);
        public static bool ApproxEqual(Rot2d r0, Rot2d r1, double tolerance)
        {
            return (r0.Angle - r1.Angle).Abs() <= tolerance;
        }

        #endregion

        #region Creator Function

        //WARNING: untested

        public static Rot2d FromM22d(M22d m)
        { 
            // cos(a) sin(a)
            //-sin(a) cos(a)

            if (m.M00 >= -1.0 && m.M00 <= 1.0)
            {
                return new Rot2d((double)System.Math.Acos(m.M00));
            }
            else throw new ArgumentException("Given M22d is not a Rotation-Matrix");
        }

        #endregion

        #region Conversion Operators

        public static explicit operator M22d(Rot2d r)
        {
            var ca = (double)System.Math.Cos(r.Angle);
            var sa = (double)System.Math.Sin(r.Angle);

            return new M22d(ca, sa, -sa, ca);
        }

        public static explicit operator M23d(Rot2d r)
        {
            var ca = (double)System.Math.Cos(r.Angle);
            var sa = (double)System.Math.Sin(r.Angle);

            return new M23d(ca, sa, 0.0f, -sa, ca, 0.0f);
        }

        public static explicit operator M33d(Rot2d r)
        {
            var ca = (double)System.Math.Cos(r.Angle);
            var sa = (double)System.Math.Sin(r.Angle);

            return new M33d(ca, sa, 0,
                            -sa, ca, 0,
                            0, 0, 1);
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Calculates Hash-code of the given rotation.
        /// </summary>
        /// <returns>Hash-code.</returns>
        public override int GetHashCode()
        {
            return Angle.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(Localization.FormatEnUS, "[{0}]", Angle);
        }

        public static Rot2d Parse(string s)
        {
            var x = s.NestedBracketSplitLevelOne().ToArray();
            return new Rot2d(
                double.Parse(x[0], Localization.FormatEnUS)
            );
        }

        /// <summary>
        /// Checks if 2 objects are equal.
        /// </summary>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Rot2d)
            {
                Rot2d rotation = (Rot2d)obj;
                return (Angle == rotation.Angle);
            }
            return false;
        }

        #endregion
    }
}
