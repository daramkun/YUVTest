using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace YUVTest
{
	[StructLayout ( LayoutKind.Sequential )]
	public struct RGB
	{
		public byte R;
		public byte G;
		public byte B;

		public RGB ( float r, float g, float b )
		{
			R = ( byte ) Math.Max ( Math.Min ( r, 255 ), 0 );
			G = ( byte ) Math.Max ( Math.Min ( g, 255 ), 0 );
			B = ( byte ) Math.Max ( Math.Min ( b, 255 ), 0 );
		}
	}

	[StructLayout ( LayoutKind.Sequential )]
	public struct YCbCr444
	{
		public byte Y;
		public byte Cb;
		public byte Cr;

		public YCbCr444 ( float y, float u, float v )
		{
			Y = ( byte ) Math.Max ( Math.Min ( y, 255 ), 0 );
			Cb = ( byte ) Math.Max ( Math.Min ( u, 255 ), 0 );
			Cr = ( byte ) Math.Max ( Math.Min ( v, 255 ), 0 );
		}
	}

	[StructLayout ( LayoutKind.Sequential )]
	public struct YCbCr422
	{
		public byte Y, Cb, Cr, Y2;

		public YCbCr422 ( float y, float u, float v, float y2 )
		{
			Y = ( byte ) Math.Max ( Math.Min ( y, 255 ), 0 );
			Cb = ( byte ) Math.Max ( Math.Min ( u, 255 ), 0 );
			Cr = ( byte ) Math.Max ( Math.Min ( v, 255 ), 0 );
			Y2 = ( byte ) Math.Max ( Math.Min ( y2, 255 ), 0 );
		}
	}

	[StructLayout ( LayoutKind.Sequential )]
	public struct CbCr
	{
		public byte Cb, Cr;

		public CbCr ( float u, float v )
		{
			Cb = ( byte ) Math.Max ( Math.Min ( u, 255 ), 0 );
			Cr = ( byte ) Math.Max ( Math.Min ( v, 255 ), 0 );
		}
	}

	class ZigzagScanner
	{
		List<byte> list = new List<byte> ( 64 );
		Point [] zigzagPosition = new Point [ 64 ]
		{
			new Point ( 0, 0 ), new Point ( 1, 0 ), new Point ( 0, 1 ), new Point ( 0, 2 ),
			new Point ( 1, 1 ), new Point ( 2, 0 ), new Point ( 3, 0 ), new Point ( 2, 1 ), new Point ( 1, 2 ), new Point ( 0, 3 ), new Point ( 0, 4 ), new Point ( 1, 3 ),
			new Point ( 2, 2 ), new Point ( 3, 1 ), new Point ( 4, 0 ), new Point ( 5, 0 ), new Point ( 4, 1 ), new Point ( 3, 2 ), new Point ( 2, 3 ), new Point ( 1, 4 ), new Point ( 0, 5 ), new Point ( 0, 6 ), new Point ( 1, 5 ), new Point ( 2, 4 ),
			new Point ( 3, 3 ), new Point ( 4, 2 ), new Point ( 5, 1 ), new Point ( 6, 0 ), new Point ( 7, 0 ), new Point ( 6, 1 ), new Point ( 5, 2 ), new Point ( 4, 3 ), new Point ( 3, 4 ), new Point ( 2, 5 ), new Point ( 1, 6 ), new Point ( 0, 7 ), new Point ( 1, 7 ), new Point ( 2, 6 ), new Point ( 3, 5 ),
			new Point ( 4, 4 ), new Point ( 5, 3 ), new Point ( 6, 2 ), new Point ( 7, 1 ), new Point ( 7, 2 ), new Point ( 6, 3 ), new Point ( 5, 4 ), new Point ( 4, 5 ), new Point ( 3, 6 ), new Point ( 2, 7 ), new Point ( 3, 7 ), new Point ( 4, 6 ),
			new Point ( 5, 5 ), new Point ( 6, 4 ), new Point ( 7, 3 ), new Point ( 7, 4 ), new Point ( 6, 5 ), new Point ( 5, 6 ), new Point ( 4, 7 ), new Point ( 5, 7 ),
			new Point ( 6, 6 ), new Point ( 7, 5 ), new Point ( 7, 6 ), new Point ( 6, 7 ),
			new Point ( 7, 7 )
		};

		public byte [] ZigzagScanning ( byte [,] arr )
		{
			list.Clear ();

			foreach ( Point p in zigzagPosition )
			{
				byte t = arr [ p.X, p.Y ];
				list.Add ( t );
				//if ( t == 0 ) break;
			}

			int index;
			while ( ( index = list.LastIndexOf ( 0 ) ) != -1 )
				if ( index == list.Count - 1 )
					list.RemoveAt ( index );
				else break;

			return list.ToArray ();
		}

		public byte [,] ZigzagRestore ( byte [] arr )
		{
			byte [,] ret = new byte [ 8, 8 ];
			for ( int i = 0; i < arr.Length; ++i )
				ret [ zigzagPosition [ i ].X, zigzagPosition [ i ].Y ] = arr [ i ];
			return ret;
		}
	}

	public static class ColorspaceConverter
	{
		#region Utilities
		public static readonly float [,] QuantizeTableForY = new float [ 8, 8 ]
		{
			/*{ 16, 11, 10, 16, 24, 40, 51, 61 },
			{ 12, 12, 14, 19, 26, 58, 60, 55 },
			{ 14, 13, 16, 24, 40, 57, 69, 56 },
			{ 14, 17, 22, 29, 51, 87, 80, 62 },
			{ 18, 22, 37, 56, 68, 109, 103, 77 },
			{ 24, 35, 55, 64, 81, 104, 113, 92 },
			{ 49, 64, 78, 87, 103, 121, 120, 101 },
			{ 72, 92, 95, 98, 112, 100, 103, 99 },/**/
			/**/{ 1, 1, 1, 1, 2, 3, 4, 5 },
			{ 1, 1, 1, 2, 2, 5, 5, 4 },
			{ 1, 1, 1, 2, 3, 5, 6, 4 },
			{ 1, 1, 2, 2, 4, 7, 6, 5 },
			{ 1, 2, 3, 4, 5, 9, 8, 6 },
			{ 2, 3, 4, 5, 6, 8, 9, 7 },
			{ 4, 5, 6, 7, 8, 10, 10, 8 },
			{ 6, 7, 8, 8, 9, 8, 8, 8 },/**/
		};
		public static readonly float [,] QuantizeTableForCbCr = new float [ 8, 8 ]
		{
			/*{ 17, 18, 24, 47, 99, 99, 99, 99 },
			{ 18, 21, 26, 66, 99, 99, 99, 99 },
			{ 24, 26, 56, 99, 99, 99, 99, 99 },
			{ 47, 66, 99, 99, 99, 99, 99, 99 },
			{ 99, 99, 99, 99, 99, 99, 99, 99 },
			{ 99, 99, 99, 99, 99, 99, 99, 99 },
			{ 99, 99, 99, 99, 99, 99, 99, 99 },
			{ 99, 99, 99, 99, 99, 99, 99, 99 },/**/
			/**/{ 1, 1, 2, 4, 8, 8, 8, 8 },
			{ 1, 2, 2, 5, 8, 8, 8, 8 },
			{ 2, 2, 4, 8, 8, 8, 8, 8 },
			{ 4, 5, 8, 8, 8, 8, 8, 8 },
			{ 8, 8, 8, 8, 8, 8, 8, 8 },
			{ 8, 8, 8, 8, 8, 8, 8, 8 },
			{ 8, 8, 8, 8, 8, 8, 8, 8 },
			{ 8, 8, 8, 8, 8, 8, 8, 8 },/**/
		};
		private static readonly ZigzagScanner zigzag = new ZigzagScanner ();
		private static readonly float [,] CosineTable = new float [ 8, 8 ];
		static ColorspaceConverter ()
		{
			const double inv16 = 1.0 / 16.0;
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					CosineTable [ x, y ] = ( float ) Math.Cos ( Math.PI * x * ( 2.0 * y + 1 ) * inv16 );
		}
		public static void RGB2YUV ( out byte y, out byte u, out byte v, byte r, byte g, byte b )
		{
			y = ( byte ) Math.Max ( Math.Min ( ( 0.257 * r ) + ( 0.504 * g ) + ( 0.098 * b ) + 16, 255 ), 0 );
			u = ( byte ) Math.Max ( Math.Min ( -( 0.148 * r ) - ( 0.291 * g ) + ( 0.439 * b ) + 128, 255 ), 0 );
			v = ( byte ) Math.Max ( Math.Min ( ( 0.439 * r ) - ( 0.368 * g ) - ( 0.071 * b ) + 128, 255 ), 0 );
		}
		public static void YUV2RGB ( out byte r, out byte g, out byte b, byte y, byte u, byte v )
		{
			r = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) + 1.596 * ( v - 128 ), 255 ), 0 );
			g = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) - 0.813 * ( v - 128 ) - 0.392 * ( u - 128 ), 255 ), 0 );
			b = ( byte ) Math.Max ( Math.Min ( 1.164 * ( y - 16 ) + 2.018 * ( u - 128 ), 255 ), 0 );
		}
		public static void ReadPixels ( out byte r, out byte g, out byte b, IntPtr ptr, int offset )
		{
			r = Marshal.ReadByte ( ptr, offset + 0 );
			g = Marshal.ReadByte ( ptr, offset + 1 );
			b = Marshal.ReadByte ( ptr, offset + 2 );
		}
		public static void WritePixels ( IntPtr ptr, int offset, byte r, byte g, byte b )
		{
			Marshal.WriteByte ( ptr, offset + 0, r );
			Marshal.WriteByte ( ptr, offset + 1, g );
			Marshal.WriteByte ( ptr, offset + 2, b );
			Marshal.WriteByte ( ptr, offset + 3, 255 );
		}
		public static void GetColours ( out float [,] ys, out float [,] us, out float [,] vs, Bitmap original )
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
		public static void CopyTo ( float [,] target, float [,] original, int tx, int ty )
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
		public static void CopyFrom ( float [,] target, float [,] from, int tx, int ty )
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
		static Func<float, float, float, float, float, float> bf = ( a, u, v, x, y ) =>  a * CosineTable [ ( int ) u, ( int ) x ] * CosineTable [ ( int ) v, ( int ) y ];
		public static void DiscreteCosineTransform ( float [,] dest, float [,] src )
		{
			for ( int y = 0; y < 8; ++y )
			{
				for ( int x = 0; x < 8; ++x )
				{
					dest [ x, y ] = 0;
					for ( int u = 0; u < 8; ++u )
						for ( int v = 0; v < 8; ++v )
							dest [ x, y ] += bf ( src [ u, v ], x, y, u, v );
					dest [ x, y ] *= 0.25f * alpha ( x ) * alpha ( y );
				}
			}
		}
		public static void InvertedDiscreteCosineTransform ( float [,] dest, float [,] src )
		{
			for ( int y = 0; y < 8; ++y )
			{
				for ( int x = 0; x < 8; ++x )
				{
					dest [ x, y ] = 0;
					for ( int u = 0; u < 8; ++u )
						for ( int v = 0; v < 8; ++v )
							dest [ x, y ] += bf ( src [ u, v ], u, v, x, y ) * alpha ( u ) * alpha ( v );
					dest [ x, y ] *= 0.25f;
				}
			}
		}
		public static void MultiplyScalar ( float [,] dest, float scalar )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] *= scalar;
		}
		public static void Divide8x8 ( float [,] dest, float [,] divide )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] = ( float ) Math.Round ( dest [ x, y ] / divide [ x, y ] );
		}
		public static void Multiply8x8 ( float [,] dest, float[,] divide )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					dest [ x, y ] *= divide [ x, y ];
		}
		public static void FloatingPointToByte ( byte[,] target, float [,] arr )
		{
			for ( int y = 0; y < 8; ++y )
				for ( int x = 0; x < 8; ++x )
					target [ x, y ] = ( byte ) Math.Round ( arr [ x, y ] );
		}
		#endregion

		#region Arrange
		public static void RGBArrange ( out RGB [,] rgb, float [,] ys, float [,] us, float [,] vs )
		{
			rgb = new RGB [ ys.GetLength ( 0 ), ys.GetLength ( 1 ) ];
			for ( int y = 0; y < ys.GetLength ( 1 ); ++y )
			{
				for ( int x = 0; x < ys.GetLength ( 0 ); ++x )
				{
					YUV2RGB ( out byte r, out byte g, out byte b,
						 ( byte ) Math.Max ( Math.Min ( ys [ x, y ], 255 ), 0 ),
						 ( byte ) Math.Max ( Math.Min ( us [ x, y ], 255 ), 0 ),
						 ( byte ) Math.Max ( Math.Min ( vs [ x, y ], 255 ), 0 )
						 );
					rgb [ x, y ] = new RGB ( r, g, b );
				}
			}
		}
		public static void YCbCr444Arrange ( out YCbCr444 [,] yuv, float [,] ys, float [,] us, float [,] vs )
		{
			yuv = new YCbCr444 [ ys.GetLength ( 0 ), ys.GetLength ( 1 ) ];
			for ( int y = 0; y < ys.GetLength ( 1 ); ++y )
				for ( int x = 0; x < ys.GetLength ( 0 ); ++x )
					yuv [ x, y ] = new YCbCr444 ( ys [ x, y ], us [ x, y ], vs [ x, y ] );
		}
		public static void YCbCr422Arrange ( out YCbCr422 [,] yuv, float [,] ys, float [,] us, float [,] vs )
		{
			yuv = new YCbCr422 [ ( int ) Math.Ceiling ( ys.GetLength ( 0 ) / 2f ), ys.GetLength ( 1 ) ];
			for ( int y = 0; y < ys.GetLength ( 1 ); ++y )
				for ( int x = 0; x < ys.GetLength ( 0 ); x += 2 )
					yuv [ x / 2, y ] = new YCbCr422 ( ys [ x, y ], us [ x, y ], vs [ x, y ], ( ( x + 1 ) < ys.GetLength ( 0 ) ) ? ys [ x + 1, y ] : 0 );
		}
		public static void NV12Arrange ( out byte [,] ya, out CbCr [,] uv, float [,] ys, float [,] us, float [,] vs )
		{
			ya = new byte [ ys.GetLength ( 0 ), ys.GetLength ( 1 ) ];
			uv = new CbCr [ ( int ) Math.Ceiling ( ys.GetLength ( 0 ) / 2f ), ( int ) Math.Ceiling ( ys.GetLength ( 1 ) / 2f ) ];
			for ( int y = 0; y < ys.GetLength ( 1 ); y += 2 )
			{
				for ( int x = 0; x < ys.GetLength ( 0 ); x += 2 )
				{
					ya [ x, y ] = ( byte ) Math.Max ( Math.Min ( ys [ x, y ], 255 ), 0 );
					if ( x + 1 < ys.GetLength ( 0 ) )
						ya [ x + 1, y ] = ( byte ) Math.Max ( Math.Min ( ys [ x + 1, y ], 255 ), 0 );
					if ( y + 1 < ys.GetLength ( 1 ) )
					{
						ya [ x, y + 1 ] = ( byte ) Math.Max ( Math.Min ( ys [ x, y + 1 ], 255 ), 0 );
						if ( x + 1 < ys.GetLength ( 0 ) )
							ya [ x + 1, y + 1 ] = ( byte ) Math.Max ( Math.Min ( ys [ x + 1, y + 1 ], 255 ), 0 );
					}
					uv [ x / 2, y / 2 ] = new CbCr ( us [ x, y ], vs [ x, y ] );
				}
			}
		}
		#endregion

		#region Generate Bitmap
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
					CopyTo ( cbuffer, ys, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, QuantizeTableForY );
					Multiply8x8 ( qbuffer, QuantizeTableForY );
					InvertedDiscreteCosineTransform ( cbuffer, qbuffer );
					CopyFrom ( ys, cbuffer, x, y );

					CopyTo ( cbuffer, us, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, QuantizeTableForCbCr );
					Multiply8x8 ( qbuffer, QuantizeTableForCbCr );
					InvertedDiscreteCosineTransform ( cbuffer, qbuffer );
					CopyFrom ( us, cbuffer, x, y );

					CopyTo ( cbuffer, vs, x, y );
					DiscreteCosineTransform ( qbuffer, cbuffer );
					Divide8x8 ( qbuffer, QuantizeTableForCbCr );
					Multiply8x8 ( qbuffer, QuantizeTableForCbCr );
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
		#endregion

		#region Calculate CompressionRate
		public static void CalculateCompressionRGB ( out int original, out int deflated, RGB [,] rgb )
		{
			unsafe
			{
				original = sizeof ( RGB ) * rgb.GetLength ( 0 ) * rgb.GetLength ( 1 );
			}
			using ( MemoryStream memStream = new MemoryStream () )
			{
				using ( DeflateStream stream = new DeflateStream ( memStream, CompressionLevel.Optimal, true ) )
				{
					byte [] rgbArr = new byte [ 3 ];
					for ( int y = 0; y < rgb.GetLength ( 1 ); ++y )
					{
						for ( int x = 0; x < rgb.GetLength ( 0 ); ++x )
						{
							rgbArr [ 0 ] = rgb [ x, y ].R;
							rgbArr [ 1 ] = rgb [ x, y ].G;
							rgbArr [ 2 ] = rgb [ x, y ].B;
							stream.Write ( rgbArr, 0, 3 );
						}
					}
				}
				deflated = ( int ) memStream.Length;
			}
		}
		public static void CalculateCompressionYUV ( out int original, out int deflated, YCbCr444 [,] yuv )
		{
			unsafe
			{
				original = sizeof ( YCbCr444 ) * yuv.GetLength ( 0 ) * yuv.GetLength ( 1 );
			}
			using ( MemoryStream memStream = new MemoryStream () )
			{
				using ( DeflateStream stream = new DeflateStream ( memStream, CompressionLevel.Optimal, true ) )
				{
					byte [] yuvArr = new byte [ 3 ];
					for ( int y = 0; y < yuv.GetLength ( 1 ); ++y )
					{
						for ( int x = 0; x < yuv.GetLength ( 0 ); ++x )
						{
							yuvArr [ 0 ] = yuv [ x, y ].Y;
							yuvArr [ 1 ] = yuv [ x, y ].Cb;
							yuvArr [ 2 ] = yuv [ x, y ].Cr;
							stream.Write ( yuvArr, 0, 3 );
						}
					}
				}
				deflated = ( int ) memStream.Length;
			}
		}
		public static void CalculateCompressionYUV ( out int original, out int deflated, YCbCr422 [,] yuv )
		{
			unsafe
			{
				original = sizeof ( YCbCr422 ) * yuv.GetLength ( 0 ) * yuv.GetLength ( 1 );
			}
			using ( MemoryStream memStream = new MemoryStream () )
			{
				using ( DeflateStream stream = new DeflateStream ( memStream, CompressionLevel.Optimal, true ) )
				{
					byte [] yuvArr = new byte [ 4 ];
					for ( int y = 0; y < yuv.GetLength ( 1 ); ++y )
					{
						for ( int x = 0; x < yuv.GetLength ( 0 ); ++x )
						{
							yuvArr [ 0 ] = yuv [ x, y ].Y;
							yuvArr [ 1 ] = yuv [ x, y ].Cb;
							yuvArr [ 2 ] = yuv [ x, y ].Cr;
							yuvArr [ 3 ] = yuv [ x, y ].Y2;
							stream.Write ( yuvArr, 0, 4 );
						}
					}
				}
				deflated = ( int ) memStream.Length;
			}
		}
		public static void CalculateCompressionNV12 ( out int original, out int deflated, byte [,] ya, CbCr [,] uv )
		{
			unsafe
			{
				original = ( ya.GetLength ( 0 ) * ya.GetLength ( 1 ) ) +
					( sizeof ( CbCr ) * uv.GetLength ( 0 ) * uv.GetLength ( 1 ) );
			}
			using ( MemoryStream memStream = new MemoryStream () )
			{
				using ( DeflateStream stream = new DeflateStream ( memStream, CompressionLevel.Optimal, true ) )
				{
					for ( int y = 0; y < ya.GetLength ( 1 ); ++y )
					{
						for ( int x = 0; x < ya.GetLength ( 0 ); ++x )
						{
							stream.WriteByte ( ya [ x, y ] );
						}
					}
					byte [] uvArr = new byte [ 4 ];
					for ( int y = 0; y < uv.GetLength ( 1 ); ++y )
					{
						for ( int x = 0; x < uv.GetLength ( 0 ); ++x )
						{
							uvArr [ 0 ] = uv [ x, y ].Cb;
							uvArr [ 1 ] = uv [ x, y ].Cr;
							stream.Write ( uvArr, 0, 2 );
						}
					}
				}
				deflated = ( int ) memStream.Length;
			}
		}
		public static void CalculateCompressionYUV ( out int original, out int dctQuantize, out int dctQuantizeDeflated, float [,] ys, float [,] us, float [,] vs )
		{
			original = ys.GetLength ( 0 ) * ys.GetLength ( 1 ) * 3;
			dctQuantize = 0;

			using ( MemoryStream memStream = new MemoryStream () )
			{
				using ( DeflateStream stream = new DeflateStream ( memStream, CompressionLevel.Optimal, true ) )
				{
					float [,] qbuffer = new float [ 8, 8 ], cbuffer = new float [ 8, 8 ];
					byte [,] arr = new byte [ 8, 8 ];

					for ( int y = 0; y < ys.GetLength ( 1 ); y += 8 )
					{
						for ( int x = 0; x < ys.GetLength ( 0 ); x += 8 )
						{
							CopyTo ( cbuffer, ys, x, y );
							DiscreteCosineTransform ( qbuffer, cbuffer );
							Divide8x8 ( qbuffer, QuantizeTableForY );
							FloatingPointToByte ( arr, qbuffer );
							byte [] zigzaged = zigzag.ZigzagScanning ( arr );
							stream.Write ( zigzaged, 0, zigzaged.Length );
							dctQuantize += zigzaged.Length;

							CopyTo ( cbuffer, us, x, y );
							DiscreteCosineTransform ( qbuffer, cbuffer );
							Divide8x8 ( qbuffer, QuantizeTableForCbCr );
							FloatingPointToByte ( arr, qbuffer );
							zigzaged = zigzag.ZigzagScanning ( arr );
							stream.Write ( zigzaged, 0, zigzaged.Length );
							dctQuantize += zigzaged.Length;

							CopyTo ( cbuffer, vs, x, y );
							DiscreteCosineTransform ( qbuffer, cbuffer );
							Divide8x8 ( qbuffer, QuantizeTableForCbCr );
							FloatingPointToByte ( arr, qbuffer );
							zigzaged = zigzag.ZigzagScanning ( arr );
							stream.Write ( zigzaged, 0, zigzaged.Length );
							dctQuantize += zigzaged.Length;
						}
					}
				}
				dctQuantizeDeflated = ( int ) memStream.Length;
			}
		}
		#endregion
	}
}
