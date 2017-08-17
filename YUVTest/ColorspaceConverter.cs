using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YUVTest
{
	public static class ColorspaceConverter
	{
		#region Utilities
		private static readonly float [,] CosineTable = new float [ 8, 8 ];
		static ColorspaceConverter ()
		{
			const double inv16 = 1.0 / 16.0;
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					CosineTable [ x, y ] = ( float ) Math.Cos ( Math.PI * x * ( 2.0 * y + 1 ) * inv16 );
		}
		private static void RGB2YUV ( out byte y, out byte u, out byte v, byte r, byte g, byte b )
		{
			y = ( byte ) Math.Max ( Math.Min ( ( 0.257 * r ) + ( 0.504 * g ) + ( 0.098 * b ) + 16, 255 ), 0 );
			u = ( byte ) Math.Max ( Math.Min ( -( 0.148 * r ) - ( 0.291 * g ) + ( 0.439 * b ) + 128, 255 ), 0 );
			v = ( byte ) Math.Max ( Math.Min ( ( 0.439 * r ) - ( 0.368 * g ) - ( 0.071 * b ) + 128, 255 ), 0 );
		}
		private static void YUV2RGB ( out byte r, out byte g, out byte b, byte y, byte u, byte v )
		{
			r = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) + 1.596 * ( v - 128 ), 255 ), 0 );
			g = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) - 0.813 * ( v - 128 ) - 0.392 * ( u - 128 ), 255 ), 0 );
			b = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) + 2.018 * ( u - 128 ), 255 ), 0 );
		}
		private static void ReadPixels ( out byte r, out byte g, out byte b, IntPtr ptr, int offset )
		{
			r = Marshal.ReadByte ( ptr, offset + 0 );
			g = Marshal.ReadByte ( ptr, offset + 1 );
			b = Marshal.ReadByte ( ptr, offset + 2 );
		}
		private static void WritePixels ( IntPtr ptr, int offset, byte r, byte g, byte b )
		{
			Marshal.WriteByte ( ptr, offset + 0, r );
			Marshal.WriteByte ( ptr, offset + 1, g );
			Marshal.WriteByte ( ptr, offset + 2, b );
			Marshal.WriteByte ( ptr, offset + 3, 255 );
		}
		private static void GetColours ( out float [,] ys, out float [,] us, out float [,] vs, Bitmap original )
		{
			var locked = original.LockBits ( new Rectangle ( 0, 0, original.Width, original.Height ), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
			ys = new float [ original.Width, original.Height ];
			us = new float [ original.Width, original.Height ];
			vs = new float [ original.Width, original.Height ];
			for ( int y = 0; y < original.Height; ++y )
			{
				for ( int x = 0; x < original.Width; ++x )
				{
					ReadPixels ( out byte r, out byte g, out byte b, locked.Scan0, ( y * original.Width * 4 ) + ( x * 4 ) );
					RGB2YUV ( out byte yy, out byte uu, out byte vv, r, g, b );
					ys [ x, y ] = yy;
					us [ x, y ] = uu;
					vs [ x, y ] = vv;
				}
			}
			original.UnlockBits ( locked );
		}
		private static void CopyTo ( float [,] target, float [,] original, int tx, int ty )
		{
			for ( int y = 0; y < target.GetLength ( 1 ); ++y )
			{
				for ( int x = 0; x < target.GetLength ( 0 ); ++x )
				{
					int ttx = tx + x, tty = ty + y;
					if ( ttx >= original.GetLength ( 0 ) )
						continue;
					if ( tty >= original.GetLength ( 1 ) )
						continue;
					target [ x, y ] = original [ ttx, tty ] - 128;
				}
			}
		}
		private static void CopyFrom ( float [,] target, float [,] from, int tx, int ty )
		{
			for ( int y = 0; y < from.GetLength ( 1 ); ++y )
			{
				for ( int x = 0; x < from.GetLength ( 0 ); ++x )
				{
					int ttx = tx + x, tty = ty + y;
					if ( ttx >= target.GetLength ( 0 ) )
						continue;
					if ( tty >= target.GetLength ( 1 ) )
						continue;
					target [ ttx, tty ] = from [ x, y ] + 128;
				}
			}
		}
		static Func<int, float> alpha = ( int i ) => i == 0 ? 1 / ( float ) Math.Sqrt ( 2 ) : 1;
		static float beta = ( 1f / 8 + 1f / 8 );
		static Func<float, float, float, float, float, float> bf = ( a, u, v, x, y ) =>
		{
			float b = ( float ) Math.Cos ( ( ( 2d * x + 1d ) * u * Math.PI ) / 16 );
			float c = ( float ) Math.Cos ( ( ( 2d * y + 1d ) * v * Math.PI ) / 16 );
			return a * b * c;
		};
		private static void DiscreteCosineTransform ( float [,] dest, float [,] src )
		{
			for ( int y = 0; y < 8; ++y )
			{
				for ( int x = 0; x < 8; ++x )
				{
					dest [ x, y ] = 0;
					for ( int u = 0; u < 8; ++u )
						for ( int v = 0; v < 8; ++v )
							dest [ x, y ] += bf ( src [ u, v ], x, y, u, v );
					dest [ x, y ] *= beta * alpha ( x ) * alpha ( y );
				}
			}
		}
		private static void InvertedDiscreteCosineTransform ( float [,] dest, float [,] src )
		{
			for ( int y = 0; y < 8; ++y )
			{
				for ( int x = 0; x < 8; ++x )
				{
					dest [ x, y ] = 0;
					for ( int u = 0; u < 8; ++u )
						for ( int v = 0; v < 8; ++v )
							dest [ x, y ] += bf ( src [ u, v ], u, v, x, y ) * alpha ( u ) * alpha ( v );
					dest [ x, y ] *= beta;
				}
			}
		}
		private static float GetQuantizedValue ( float value, float q )
		{
			return ( float ) Math.Round ( value / q );
		}
		private static void MultiplyScalar ( float [,] dest, float scalar )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] *= scalar;
		}
		private static void Divide8x8 ( float [,] dest, float [,] divide )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] = ( float ) Math.Round ( dest [ x, y ] / divide [ x, y ] );
		}
		private static void Multiply8x8 ( float [,] dest, float[,] divide )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] *= divide [ x, y ];
		}
		#endregion

		public static Bitmap ColorDifference ( Bitmap bitmap1, Bitmap bitmap2 )
		{
			var locked1 = bitmap1.LockBits ( new Rectangle ( 0, 0, bitmap1.Width, bitmap1.Height ), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
			var locked2 = ( bitmap1 == bitmap2 ) ? locked1 : bitmap2.LockBits ( new Rectangle ( 0, 0, bitmap2.Width, bitmap2.Height ), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );
			Bitmap ret = new Bitmap ( bitmap1.Width, bitmap1.Height );
			var locked3 = ret.LockBits ( new Rectangle ( 0, 0, ret.Width, ret.Height ), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb );

			for ( int y = 0; y < ret.Height; ++y )
			{
				for ( int x = 0; x < ret.Width; ++x )
				{
					int offset = ( y * ret.Width * 4 ) + ( x * 4 );
					ReadPixels ( out byte r1, out byte g1, out byte b1, locked1.Scan0, offset );
					ReadPixels ( out byte r2, out byte g2, out byte b2, locked2.Scan0, offset );
					WritePixels ( locked3.Scan0, offset, ( byte ) Math.Abs ( r1 - r2 ), ( byte ) Math.Abs ( g1 - g2 ), ( byte ) Math.Abs ( b1 - b2 ) );
				}
			}

			ret.UnlockBits ( locked3 );
			if ( locked2 != locked1 )
				bitmap2.UnlockBits ( locked2 );
			bitmap1.UnlockBits ( locked1 );

			return ret;
		}

		public static Bitmap ConvertToYUV444 ( Bitmap original )
		{
			GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );

			Bitmap ret = new Bitmap ( original.Width, original.Height );
			var locked = ret.LockBits ( new Rectangle ( 0, 0, ret.Width, ret.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
			for ( int y = 0; y < original.Height; ++y )
			{
				for ( int x = 0; x < original.Width; ++x )
				{
					int offset = ( y * original.Width * 4 ) + ( x * 4 );
					YUV2RGB ( out byte r, out byte g, out byte b, ( byte ) ys [ x, y ], ( byte ) us [ x, y ], ( byte ) vs [ x, y ] );
					WritePixels ( locked.Scan0, offset, r, g, b );
				}
			}
			ret.UnlockBits ( locked );
			return ret;
		}

		public static Bitmap ConvertToYUV422 ( Bitmap original )
		{
			GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );

			Bitmap ret = new Bitmap ( original.Width, original.Height );
			var locked = ret.LockBits ( new Rectangle ( 0, 0, ret.Width, ret.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
			for ( int y = 0; y < original.Height; ++y )
			{
				for ( int x = 0; x < original.Width; x += 2 )
				{
					byte u = ( byte ) us [ x, y ];
					byte v = ( byte ) vs [ x, y ];

					int offset = ( y * original.Width * 4 ) + ( x * 4 );
					byte y1 = ( byte ) ys [ x, y ];
					YUV2RGB ( out byte r1, out byte g1, out byte b1, y1, u, v );
					WritePixels ( locked.Scan0, offset, r1, g1, b1 );
					if ( x + 1 < original.Width )
					{
						byte y2 = ( byte ) ys [ x + 1, y ];
						YUV2RGB ( out byte r2, out byte g2, out byte b2, y2, u, v );
						WritePixels ( locked.Scan0, offset + 4, r2, g2, b2 );
					}
				}
			}
			ret.UnlockBits ( locked );
			return ret;
		}

		public static Bitmap ConvertToNV12 ( Bitmap original )
		{
			GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );

			Bitmap ret = new Bitmap ( original.Width, original.Height );
			var locked = ret.LockBits ( new Rectangle ( 0, 0, ret.Width, ret.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
			for ( int y = 0; y < original.Height; y += 2 )
			{
				for ( int x = 0; x < original.Width; x += 2 )
				{
					byte u = ( byte ) us [ x, y ];
					byte v = ( byte ) vs [ x, y ];

					byte y1 = ( byte ) ys [ x, y ];
					int offset1 = ( y * original.Width * 4 ) + ( x * 4 );
					YUV2RGB ( out byte r1, out byte g1, out byte b1, y1, u, v );
					WritePixels ( locked.Scan0, offset1, r1, g1, b1 );
					if ( x + 1 < original.Width )
					{
						byte y2 = ( byte ) ys [ x + 1, y ];
						YUV2RGB ( out byte r2, out byte g2, out byte b2, y2, u, v );
						WritePixels ( locked.Scan0, offset1 + 4, r2, g2, b2 );
					}

					if ( y + 1 < original.Height )
					{
						byte y3 = ( byte ) ys [ x, y + 1 ];
						int offset2 = ( ( y + 1 ) * original.Width * 4 ) + ( x * 4 );
						YUV2RGB ( out byte r3, out byte g3, out byte b3, y3, u, v );
						WritePixels ( locked.Scan0, offset2, r3, g3, b3 );
						if ( x + 1 < original.Width )
						{
							byte y4 = ( byte ) ys [ x + 1, y + 1 ];
							YUV2RGB ( out byte r4, out byte g4, out byte b4, y4, u, v );
							WritePixels ( locked.Scan0, offset2 + 4, r4, g4, b4 );
						}
					}
				}
			}
			ret.UnlockBits ( locked );
			return ret;
		}

		public static Bitmap ConvertToQuantizedYUV444 ( Bitmap original )
		{
			GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );

			float [,] qbuffer, cbuffer;
			qbuffer = new float [ 8, 8 ];
			cbuffer = new float [ 8, 8 ];

			for ( int y = 0; y < original.Height; y += 8 )
			{
				for ( int x = 0; x < original.Height; x += 8 )
				{
					float [,] divide = new float [ 8, 8 ]
					{
						{ 16, 11, 10, 16, 24, 40, 51, 61 },
						{ 12, 12, 14, 19, 26, 58, 60, 55 },
						{ 14, 13, 16, 24, 40, 57, 69, 56 },
						{ 14, 17, 22, 29, 51, 87, 80, 62 },
						{ 18, 22, 37, 56, 68, 109, 103, 77 },
						{ 24, 35, 55, 64, 81, 104, 113, 92 },
						{ 49, 64, 78, 87, 103, 121, 120, 101 },
						{ 72, 92, 95, 98, 112, 100, 103, 99 },
					};

					CopyTo ( cbuffer, ys, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, divide );
					Multiply8x8 ( qbuffer, divide );
					InvertedDiscreteCosineTransform ( cbuffer, qbuffer );
					CopyFrom ( ys, cbuffer, x, y );

					float [,] divide2 = new float [ 8, 8 ]
					{
						{ 17, 18, 24, 47, 99, 99, 99, 99 },
						{ 18, 21, 26, 66, 99, 99, 99, 99 },
						{ 24, 26, 56, 99, 99, 99, 99, 99 },
						{ 47, 66, 99, 99, 99, 99, 99, 99 },
						{ 99, 99, 99, 99, 99, 99, 99, 99 },
						{ 99, 99, 99, 99, 99, 99, 99, 99 },
						{ 99, 99, 99, 99, 99, 99, 99, 99 },
						{ 99, 99, 99, 99, 99, 99, 99, 99 },
					};

					CopyTo ( cbuffer, us, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, divide2 );
					Multiply8x8 ( qbuffer, divide2 );
					InvertedDiscreteCosineTransform ( cbuffer, qbuffer );
					CopyFrom ( us, cbuffer, x, y );

					CopyTo ( cbuffer, vs, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, divide2 );
					Multiply8x8 ( qbuffer, divide2 );
					InvertedDiscreteCosineTransform ( cbuffer, qbuffer );
					CopyFrom ( vs, cbuffer, x, y );
				}
			}

			Bitmap ret = new Bitmap ( original.Width, original.Height );
			var locked = ret.LockBits ( new Rectangle ( 0, 0, ret.Width, ret.Height ), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb );
			for ( int y = 0; y < original.Height; ++y )
			{
				for ( int x = 0; x < original.Width; ++x )
				{
					int offset = ( y * original.Width * 4 ) + ( x * 4 );
					YUV2RGB ( out byte r, out byte g, out byte b, ( byte ) ys [ x, y ], ( byte ) us [ x, y ], ( byte ) vs [ x, y ] );
					WritePixels ( locked.Scan0, offset, r, g, b );
				}
			}
			ret.UnlockBits ( locked );
			return ret;
		}
	}
}
