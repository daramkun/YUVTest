using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DCTLib
{
	public class DCT
	{
		public DCT ( int width, int height )
		{
			Width = width;
			Height = height;
		}

		//size of all matrices
		public int Width;
		public int Height;

		private const int normOffset = 128;

		//Turn DCT matrices into an RGB bitmap for output
		public Bitmap MatricesToBitmap ( double [] [,] matrices, bool offset = true )
		{
			Bitmap bitmap = new Bitmap ( Width, Height );
			for ( int x = 0; x < Width; x++ )
			{
				for ( int y = 0; y < Height; y++ )
				{
					double r = matrices [ 0 ] [ x, y ];
					double g = matrices [ 1 ] [ x, y ];
					double b = matrices [ 2 ] [ x, y ];

					byte R = ( byte ) ( normOut ( r, offset ) );
					byte G = ( byte ) ( normOut ( g, offset ) );
					byte B = ( byte ) ( normOut ( b, offset ) );
					bitmap.SetPixel ( x, y, Color.FromArgb ( R, G, B ) );
				}
			}
			return bitmap;
		}

		private double normOut ( double a, bool offset )
		{
			double o = offset ? normOffset : 0d;

			return Math.Min ( Math.Max ( a + o, 0 ), 255 );
		}

		//Create matrices from an RGB bitmap
		public double [] [,] BitmapToMatrices ( Bitmap b )
		{
			double [] [,] matrices = new double [ 3 ] [,];

			for ( int i = 0; i < 3; i++ )
			{
				matrices [ i ] = new double [ Width, Height ];
			}

			for ( int x = 0; x < Width; x++ )
			{
				for ( int y = 0; y < Height; y++ )
				{
					matrices [ 0 ] [ x, y ] = b.GetPixel ( x, y ).R - normOffset;
					matrices [ 1 ] [ x, y ] = b.GetPixel ( x, y ).G - normOffset;
					matrices [ 2 ] [ x, y ] = b.GetPixel ( x, y ).B - normOffset;
				}
			}
			return matrices;
		}

		//Run the DCT2D on 3-channeled group of matrices
		public double [] [,] DCTMatrices ( double [] [,] matrices )
		{
			var outMatrices = new double [ 3 ] [,];
			Parallel.For ( 0, 3, i =>
			{
				outMatrices [ i ] = DCT2D ( matrices [ i ] );
			} );
			return outMatrices;
		}

		//Run the inverse DCT2D on 3-channeled group of matrices
		public double [] [,] IDCTMatrices ( double [] [,] matrices )
		{
			var outMatrices = new double [ 3 ] [,];
			Parallel.For ( 0, 3, i =>
			{
				outMatrices [ i ] = IDCT2D ( matrices [ i ] );
			} );
			return outMatrices;
		}

		//Run a DCT2D on a single matrix
		public double [,] DCT2D ( double [,] input )
		{
			double [,] coeffs = new double [ Width, Height ];

			//To initialise every [u,v] value in the coefficient table...
			for ( int u = 0; u < Width; u++ )
			{
				for ( int v = 0; v < Height; v++ )
				{
					//...sum the basisfunction for every [x,y] value in the bitmap input
					double sum = 0d;



					for ( int x = 0; x < Width; x++ )
					{
						for ( int y = 0; y < Height; y++ )
						{
							double a = input [ x, y ];
							sum += BasisFunction ( a, u, v, x, y );
						}
					}
					coeffs [ u, v ] = sum * beta * alpha ( u ) * alpha ( v );
				}
			}
			return coeffs;
		}

		//Run an inverse DCT2D on a single matrix
		public double [,] IDCT2D ( double [,] coeffs )
		{
			double [,] output = new double [ Width, Height ];

			//To initialise every [x,y] value in the bitmap output...
			for ( int x = 0; x < Width; x++ )
			{
				for ( int y = 0; y < Height; y++ )
				{
					//...sum the basisfunction for every [u,v] value in the coefficient table
					double sum = 0d;

					for ( int u = 0; u < Width; u++ )
					{
						for ( int v = 0; v < Height; v++ )
						{
							double a = coeffs [ u, v ];
							sum += BasisFunction ( a, u, v, x, y ) * alpha ( u ) * alpha ( v );
						}
					}

					output [ x, y ] = sum * beta;
				}
			}
			return output;
		}

		public double BasisFunction ( double a, double u, double v, double x, double y )
		{
			double b = Math.Cos ( ( ( 2d * x + 1d ) * u * Math.PI ) / ( 2 * Width ) );
			double c = Math.Cos ( ( ( 2d * y + 1d ) * v * Math.PI ) / ( 2 * Height ) );

			return a * b * c;
		}

		//return 1/sqrt(2) if u is not 0
		private double alpha ( int u )
		{
			if ( u == 0 )
				return 1 / Math.Sqrt ( 2 );
			return 1;
		}

		//normalising value
		private double beta
		{
			get { return ( 1d / Width + 1d / Height ); }
		}

	}
}