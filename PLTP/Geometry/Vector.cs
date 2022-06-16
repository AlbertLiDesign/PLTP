using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLTP
{
    public class Vector
    {
        /// <summary>
        /// The components of the vector
        /// </summary>
        protected double[] _data;

        #region Public Properties
        /// <summary>
        /// The dimension of the vector
        /// </summary>
        public int Dimension => _data.Length;

        /// <summary>
        /// X dimension
        /// </summary>
        public double X => _data[0];

        /// <summary>
        /// Y dimension
        /// </summary>
        public double Y => _data[1];

        /// <summary>
        /// Z dimension
        /// </summary>
        public double Z => _data[2];

        /// <summary>
        /// Get the length of the vector
        /// </summary>    
        public double Length => (double)Math.Sqrt(_data[0] * _data[0] + _data[1] * _data[1] + _data[2] * _data[2]);

        /// <summary>
        /// Get the square length of a vector
        /// </summary>        
        /// <returns>The length</returns>
        public double SqrLength => _data[0] * _data[0] + _data[1] * _data[1] + _data[2] * _data[2];
        #endregion

        #region Constructors
        /// <summary>
        /// Construct an empty vector
        /// </summary>
        public Vector()
        {
            _data = new double[0];
        }

        /// <summary>
        /// Construct a vector, the default elements are 0;
        /// If fillOne is true, all elements are 1.
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="fillOne"></param>
        public Vector(int dimension, bool fillOne = false)
        {
            _data = new double[dimension];
            if (fillOne)
                for (int i = 0; i < dimension; i++)
                    _data[i] = 1;
        }

        /// <summary>
        /// Construct a vector using given values
        /// </summary>
        /// <param name="values"></param>
        public Vector(params double[] values)
        {
            _data = new double[values.Length];
            Array.Copy(values, _data, values.Length);
        }
        #endregion

        #region Operators
        /// <summary>
        /// Determines whether two vectors have equal values.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>true if the components of the two vectors are exactly equal; otherwise false.</returns>
        public static bool operator ==(Vector a, Vector b)
        {
            if (a.Dimension != b.Dimension)
            {
                return false;
            }
            else
            {
                int flag = 0;
                for (int i = 0; i < a.Dimension; i++)
                {
                    flag += a[i] == b[i] ? 1 : 0;
                }
                return flag == a.Dimension;
            }
        }

        /// <summary>
        /// Determines whether two vectors have different values.
        /// </summary>
        /// <param name="a">The first vector.</param>
        /// <param name="b">The second vector.</param>
        /// <returns>true if the two vectors differ in any component; false otherwise.</returns>
        public static bool operator !=(Vector a, Vector b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Positive, return the copy of the input vector.
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vector operator +(Vector v1)
        {
            return v1.Copy();
        }

        /// <summary>
        /// Negative，return the reverse vector of the input vector.
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vector operator -(Vector v1)
        {
            Vector v = new Vector(v1.Length);
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = -v1[i];
            }
            return v;
        }

        /// <summary>
        /// Summation
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Vector operator +(Vector v1, Vector v2)
        {
            if (v1.Dimension != v2.Dimension)
                throw new Exception("Different vector dimensions");
            Vector v = new Vector(v1.Dimension);
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = v1[i] + v2[i];
            }
            return v;
        }

        /// <summary>
        /// Subtraction
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Vector operator -(Vector v1, Vector v2)
        {
            if (v1.Dimension != v2.Dimension)
                throw new Exception("Different vector dimensions");
            Vector v = new Vector(v1.Dimension);
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = v1[i] - v2[i];
            }
            return v;
        }

        /// <summary>
        /// Dot product
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static double operator *(Vector v1, Vector v2)
        {
            if (v1.Dimension != v2.Dimension)
                throw new Exception("Different vector dimensions！");
            double sum = 0;
            for (int i = 0; i < v1.Dimension; i++)
            {
                sum += v1[i] * v2[i];
            }
            return sum;
        }

        /// <summary>
        /// Scalar multiplication of vectors
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vector operator *(double scale, Vector v1)
        {
            Vector v = new Vector(v1.Dimension);
            for (int i = 0; i < v1.Dimension; i++)
            {
                v[i] = v1[i] * scale;
            }
            return v;
        }

        /// <summary>
        /// Scalar multiplication of vectors
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Vector operator *(Vector v1, double scale)
        {
            return scale * v1;
        }

        /// <summary>
        /// Division
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Vector operator /(Vector v1, double scale)
        {
            return 1.0 / scale * v1;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Return the origin
        /// </summary>
        /// <param name="dimension"></param>
        /// <returns></returns>
        public static Vector Origin(int dimension)
        {
            return new Vector(dimension);
        }

        /// <summary>
        /// Determines whether the specified System.Object is a Vector3f and has the same values as the present vector.
        /// </summary>
        /// <param name="obj">The specified object.</param>
        /// <returns>true if obj is Vector3f and has the same components as this; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return (obj is Vector && this == (Vector)obj);
        }

        /// <summary>
        /// Get an element using its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value of the element.</returns>
        public double this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }


        /// <summary>
        /// Multiply element by element
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Vector Mutiply(Vector v1)
        {
            if (Dimension != v1.Dimension)
                throw new Exception("Different vector dimensions");
            Vector v = new Vector(v1.Dimension);
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = this[i] * v1[i];
            }
            return v;
        }

        /// <summary>
        /// Division element by element
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Vector Divide(Vector v1)
        {
            if (Dimension != v1.Dimension)
                throw new Exception("Different vector dimensions");
            Vector v = new Vector(v1.Dimension);
            for (int i = 0; i < v.Dimension; i++)
            {
                v[i] = this[i] / v1[i];
            }
            return v;
        }

        /// <summary>
        /// If it is a zero vector
        /// </summary>
        /// <returns></returns>
        public bool IsZero()
        {
            for (int i = 0; i < Length; i++)
            {
                if (this[i] != 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// If the two vectors are equal
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        public bool Equals(Vector v1)
        {
            if (Dimension != v1.Dimension)
                return false;
            for (int i = 0; i < Dimension; i++)
            {
                if (this[i] != v1[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return _data.GetHashCode();
        }

        /// <summary>
        /// Return a sub-vector with end-begin elements. The indices of elements is from begin to end-1.
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Vector SubVector(int begin, int end)
        {
            Vector v = new Vector(end - begin);
            Array.Copy(_data, begin, v._data, 0, end - begin);
            return v;
        }

        /// <summary>
        /// Splice vectors, current vector + v1
        /// </summary>
        /// <param name="v1"></param>
        /// <returns></returns>
        public Vector Concat(Vector v1)
        {
            Vector v = new Vector(Dimension + v1.Dimension);
            CopyTo(v);
            v1.CopyTo(v, Dimension);
            return v;
        }

        /// <summary>
        /// Copy and return a new vector
        /// </summary>
        /// <returns></returns>
        public Vector Copy()
        {
            return new Vector(_data);
        }

        /// <summary>
        /// Copy to the target vector, des
        /// </summary>
        /// <param name="des"></param>
        /// <param name="desDex"></param>
        public void CopyTo(Array des, int desDex = 0)
        {
            Buffer.BlockCopy(_data, 0, des, desDex * sizeof(double),
                Dimension * sizeof(double));
        }

        /// <summary>
        /// Copy to the target vector, des
        /// </summary>
        /// <param name="des"></param>
        /// <param name="desDex"></param>
        public void CopyTo(Vector des, int desDex = 0)
        {
            Array.Copy(_data, 0, des._data, desDex, _data.Length);
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", _data) + "]";
        }

        /// <summary>
        /// Get the distance between two vectors.
        /// </summary>
        /// <param name="vector"> Another vector. </param>
        /// <returns>Return the distance between two vectors.</returns>
        public double DistanceTo(Vector vector)
        {
            return (this - vector).Length;
        }

        /// <summary>
        /// Unitize the vector.
        /// </summary>
        /// <returns></returns>
        public Vector Unitize()
        {
            return this / Length;
        }

        /// <summary>
        /// [Only for 3D vector]Computes the cross product (or vector product, or exterior product) of two vectors.
        /// <para>This operation is not commutative.</para>
        /// </summary>
        /// <param name="other"> Another vector.</param>
        /// <returns>A new vector that is perpendicular to both this vector and another vector,
        /// <para>has Length == a.Length * b.Length and</para>
        /// <para>with a result that is oriented following the right hand rule.</para>
        /// </returns>
        public Vector CrossProduct(Vector other)
        {
            if (!(Dimension == 3 && other.Dimension == 3))
            {
                throw new Exception("Cross product is only for 3D vector");
            }
            return new Vector(
                _data[1] * other._data[2] - other._data[1] * _data[2],
                _data[2] * other._data[0] - other._data[2] * _data[0],
                _data[0] * other._data[1] - other._data[0] * _data[1]);
        }
        #endregion
    }
}
