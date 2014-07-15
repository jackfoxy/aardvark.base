using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Aardvark.Base
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rot2f
    {
        public float Angle;

        #region Constructors

        public Rot2f(float angleInRadians)
        {
            Angle = angleInRadians;
        }

        #endregion

        #region Constants

        public static readonly Rot2f Identity = new Rot2f(0);

        #endregion

        #region Rotation Arithmetics

        public void Invert()
        {
            Angle = -Angle;
        }

        public Rot2f Inverse
        {
            get
            {
                return new Rot2f(-Angle);
            }
        }

        /// <summary>
        /// Adds 2 rotations.
        /// </summary>
        public static Rot2f Add(Rot2f r0, Rot2f r1)
        {
            return new Rot2f(r0.Angle + r1.Angle);
        }

        /// <summary>
        /// Adds scalar to a rotation.
        /// </summary>
        public static Rot2f Add(Rot2f rot, float val)
        {
            return new Rot2f(rot.Angle + val);
        }

        /// <summary>
        /// Subtracts 2 rotations.
        /// </summary>
        public static Rot2f Subtract(Rot2f r0, Rot2f r1)
        {
            return new Rot2f(r0.Angle - r1.Angle);
        }

        /// <summary>
        /// Subtracts scalar from a rotation.
        /// </summary>
        public static Rot2f Subtract(Rot2f rot, float angle)
        {
            return new Rot2f(rot.Angle - angle);
        }

        /// <summary>
        /// Subtracts rotation from a scalar.
        /// </summary>
        public static Rot2f Subtract(float angle, Rot2f rot)
        {
            return new Rot2f(angle - rot.Angle);
        }

        /// <summary>
        /// Multiplies scalar with a rotation.
        /// </summary>
        public static Rot2f Multiply(Rot2f rot, float val)
        {
            return new Rot2f(rot.Angle * val);
        }

        public static V2f Multiply(Rot2f rot, V2f vec)
        {

            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new V2f( a * vec.X +
                            b * vec.Y,

                           -b * vec.X +
                            a * vec.Y);
        }

        public static V3f Multiply(Rot2f rot, V3f vec)
        {
            float ca = (float)System.Math.Cos(rot.Angle);
            float sa = (float)System.Math.Sin(rot.Angle);

            return new V3f( ca * vec.X + sa * vec.Y,
                           -sa * vec.X + ca * vec.Y,
                           vec.Z);
        }

        public static V4f Multiply(Rot2f rot, V4f vec)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new V4f( a * vec.X +
                            b * vec.Y,

                           -b * vec.X +
                            a * vec.Y,

                            vec.Z,

                            vec.W);
        }

        public static M22f Multiply(Rot2f rot, M22f mat)
        {
            return (M22f)rot * mat;
        }

        public static M33f Multiply(Rot2f rot, M33f mat)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new M33f( a * mat.M00 +
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

        public static M34f Multiply(Rot2f rot, M34f mat)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new M34f( a * mat.M00 +
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

        public static M44f Multiply(Rot2f rot, M44f mat)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new M44f( a * mat.M00 +
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

        public static M33f Multiply(Rot2f rot2, Rot3f rot3)
        {
            return Rot2f.Multiply(rot2, (M33f)rot3);
        }

        /// <summary>
        /// Multiplies 2 rotations.
        /// </summary>
        public static Rot2f Multiply(Rot2f r0, Rot2f r2)
        {
            return new Rot2f(r0.Angle * r2.Angle);
        }

        public static M33f Multiply(Rot2f rot, Scale3f scale)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new M33f( a * scale.X,
                             b * scale.Y,
                             0,

                            -b * scale.X,
                             a * scale.Y,
                             0,

                             0,
                             0,
                             scale.Z);
        }

        public static M34f Multiply(Rot2f rot, Shift3f shift)
        {
            float a = (float)System.Math.Cos(rot.Angle);
            float b = (float)System.Math.Sin(rot.Angle);

            return new M34f( a, b, 0, a * shift.X + b * shift.Y,
                            -b, a, 0, -b * shift.X + a * shift.Y,
                             0, 0, 1, shift.Z);

        }

        /// <summary>
        /// Divides scalar by a rotation.
        /// </summary>
        public static Rot2f Divide(Rot2f rot, float val)
        {
            return new Rot2f(rot.Angle / val);
        }

        /// <summary>
        /// Negates rotation.
        /// </summary>
        public static Rot2f Negate(Rot2f rot)
        {
            return new Rot2f(-rot.Angle);
        }

        /// <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public static V2f TransformDir(Rot2f rot, V2f v)
        {
            return (M22f)rot * v;
        }


        /// <summary>
        /// Inverse Transforms a Direction Vector
        /// </summary>
        public static V2f InvTransformDir(Rot2f rot, V2f v)
        {
            return (M22f)Rot2f.Negate(rot) * v;
        }


        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public static V2f TransformPos(Rot2f rot, V2f v)
        {
            return (M22f)rot * v;
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public static V2f InvTransformPos(Rot2f rot, V2f v)
        {
            return (M22f)Rot2f.Negate(rot) * v;
        }

        /// <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public V2f TransformDir(V2f v)
        {
            return Rot2f.TransformDir(this, v);
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public V2f TransformPos(V2f v)
        {
            return Rot2f.TransformPos(this, v);
        }

        // <summary>
        /// Transforms a Direction Vector
        /// </summary>
        public V2f InvTransformDir(V2f v)
        {
            return Rot2f.InvTransformDir(this, v);
        }

        /// <summary>
        /// Transforms a Position Vector
        /// </summary>
        public V2f InvTransformPos(V2f v)
        {
            return Rot2f.InvTransformPos(this, v);
        }

        #endregion

        #region Arithmetic operators

        /// <summary>
        /// Negates the rotation.
        /// </summary>
        public static Rot2f operator -(Rot2f rot)
        {
            return Rot2f.Negate(rot);
        }

        /// <summary>
        /// Adds 2 rotations.
        /// </summary>
        public static Rot2f operator +(Rot2f r0, Rot2f r1)
        {
            return Rot2f.Add(r0, r1);
        }

        /// <summary>
        /// Adds a rotation and a scalar value.
        /// </summary>
        public static Rot2f operator +(Rot2f rot, float angle)
        {
            return Rot2f.Add(rot, angle);
        }

        /// <summary>
        /// Adds a rotation and a scalar value.
        /// </summary>
        public static Rot2f operator +(float angle, Rot2f rot)
        {
            return Rot2f.Add(rot, angle);
        }

        /// <summary>
        /// Subtracts 2 rotations.
        /// </summary>
        public static Rot2f operator -(Rot2f r0, Rot2f r1)
        {
            return Rot2f.Subtract(r0, r1);
        }

        /// <summary>
        /// Subtracts a rotation from a scalar value.
        /// </summary>
        public static Rot2f operator -(Rot2f rot, float angle)
        {
            return Rot2f.Subtract(rot, angle);
        }

        /// <summary>
        /// Subtracts a rotation from a scalar value.
        /// </summary>
        public static Rot2f operator -(float angle, Rot2f rot)
        {
            return Rot2f.Subtract(angle, rot);
        }

        /// <summary>
        /// Multiplies rotation with scalar value.
        /// </summary>
        public static Rot2f operator *(Rot2f rot, float val)
        {
            return Rot2f.Multiply(rot, val);
        }

        /// <summary>
        /// Multiplies rotation with scalar value.
        /// </summary>
        public static Rot2f operator *(float val, Rot2f rot)
        {
            return Rot2f.Multiply(rot, val);
        }

        /// <summary>
        /// </summary>
        public static V2f operator *(Rot2f rot, V2f vec)
        {
            return Rot2f.Multiply(rot, vec);
        }

        /// <summary>
        /// </summary>
        public static V3f operator *(Rot2f rot, V3f vec)
        {
            return Rot2f.Multiply(rot, vec);
        }

        /// <summary>
        /// </summary>
        public static V4f operator *(Rot2f rot, V4f vec)
        {
            return Rot2f.Multiply(rot, vec);
        }

        public static M22f operator *(Rot2f rot, M22f mat)
        {
            return Rot2f.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M33f operator *(Rot2f rot, M33f mat)
        {
            return Rot2f.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M34f operator *(Rot2f rot, M34f mat)
        {
            return Rot2f.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M44f operator *(Rot2f rot, M44f mat)
        {
            return Rot2f.Multiply(rot, mat);
        }

        /// <summary>
        /// </summary>
        public static M33f operator *(Rot2f rot2, Rot3f rot3)
        {
            return Rot2f.Multiply(rot2, rot3);
        }

        /// <summary>
        /// </summary>
        public static Rot2f operator *(Rot2f r0, Rot2f r1)
        {
            return Rot2f.Multiply(r0, r1);
        }

        /// <summary>
        /// </summary>
        public static M33f operator *(Rot2f rot, Scale3f scale)
        {
            return Rot2f.Multiply(rot, scale);
        }

        /// <summary>
        /// </summary>
        public static M34f operator *(Rot2f rot, Shift3f shift)
        {
            return Rot2f.Multiply(rot, shift);
        }

        /// <summary>
        /// Divides rotation by scalar value.
        /// </summary>
        public static Rot2f operator /(Rot2f rot, float val)
        {
            return Rot2f.Divide(rot, val);
        }

        #endregion

        #region Comparison Operators

        /// <summary>
        /// Checks if 2 rotations are equal.
        /// </summary>
        /// <returns>Result of comparison.</returns>
        public static bool operator ==(Rot2f rotation1, Rot2f rotation2)
        {
            return (rotation1.Angle == rotation2.Angle);
        }

        /// <summary>
        /// Checks if 2 rotations are not equal.
        /// </summary>
        /// <returns>Result of comparison.</returns>
        public static bool operator !=(Rot2f rotation1, Rot2f rotation2)
        {
            return !(rotation1.Angle == rotation2.Angle);
        }


        public static bool ApproxEqual(Rot2f r0, Rot2f r1)
        {
            return ApproxEqual(r0, r1, Constant<float>.PositiveTinyValue);
        }

        // [todo ISSUE 20090225 andi : andi] Wir sollten auch folgendes ber�cksichtigen -q == q, weil es die selbe rotation definiert.
        // [todo ISSUE 20090427 andi : andi] use an angle-tolerance
        // [todo ISSUE 20090427 andi : andi] add Rot3f.ApproxEqual(Rot3f other);
        public static bool ApproxEqual(Rot2f r0, Rot2f r1, float tolerance)
        {
            return (r0.Angle - r1.Angle).Abs() <= tolerance;
        }

        #endregion

        #region Creator Function

        //WARNING: untested

        public static Rot2f FromM22f(M22f m)
        { 
            // cos(a) sin(a)
            //-sin(a) cos(a)

            if (m.M00 >= -1.0 && m.M00 <= 1.0)
            {
                return new Rot2f((float)System.Math.Acos(m.M00));
            }
            else throw new ArgumentException("Given M22f is not a Rotation-Matrix");
        }

        #endregion

        #region Conversion Operators

        public static explicit operator M22f(Rot2f r)
        {
            var ca = (float)System.Math.Cos(r.Angle);
            var sa = (float)System.Math.Sin(r.Angle);

            return new M22f(ca, sa, -sa, ca);
        }

        public static explicit operator M23f(Rot2f r)
        {
            var ca = (float)System.Math.Cos(r.Angle);
            var sa = (float)System.Math.Sin(r.Angle);

            return new M23f(ca, sa, 0.0f, -sa, ca, 0.0f);
        }

        public static explicit operator M33f(Rot2f r)
        {
            var ca = (float)System.Math.Cos(r.Angle);
            var sa = (float)System.Math.Sin(r.Angle);

            return new M33f(ca, sa, 0,
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

        public static Rot2f Parse(string s)
        {
            var x = s.NestedBracketSplitLevelOne().ToArray();
            return new Rot2f(
                float.Parse(x[0], Localization.FormatEnUS)
            );
        }

        /// <summary>
        /// Checks if 2 objects are equal.
        /// </summary>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Rot2f)
            {
                Rot2f rotation = (Rot2f)obj;
                return (Angle == rotation.Angle);
            }
            return false;
        }

        #endregion
    }
}
