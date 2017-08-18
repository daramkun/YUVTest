using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YUVTest.Properties;

namespace YUVTest
{
	public partial class MainForm : Form
	{
		Bitmap original, yuv444, yuv422, nv12, dctyuv444;

		private void 포맷별크기및Deflate압축크기계산DToolStripMenuItem_Click ( object sender, EventArgs e )
		{
			ColorspaceConverter.GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );

			ColorspaceConverter.RGBArrange ( out RGB [,] rgbArr, ys, us, vs );
			ColorspaceConverter.CalculateCompressionRGB ( out int rgb, out int rgbd, rgbArr );
			rgbArr = null;

			ColorspaceConverter.YCbCr444Arrange ( out YCbCr444 [,] yuv444Arr, ys, us, vs );
			ColorspaceConverter.CalculateCompressionYUV ( out int ycbcr444, out int ycbcr444d, yuv444Arr );
			yuv444Arr = null;

			ColorspaceConverter.YCbCr422Arrange ( out YCbCr422 [,] yuv422Arr, ys, us, vs );
			ColorspaceConverter.CalculateCompressionYUV ( out int ycbcr422, out int ycbcr422d, yuv422Arr );
			yuv422Arr = null;

			ColorspaceConverter.NV12Arrange ( out byte [,] nv12yArr, out CbCr [,] nv12uvArr, ys, us, vs );
			ColorspaceConverter.CalculateCompressionNV12 ( out int nv12, out int nv12d, nv12yArr, nv12uvArr );
			nv12yArr = null;
			nv12uvArr = null;

			MessageBox.Show ( $@"RGB: {rgb}, RGB Deflated: {rgbd}
YCbCr444: {ycbcr444}, YCbCr444 Deflated: {ycbcr444d}
YCbCr422: {ycbcr422}, YCbCr422 Deflated: {ycbcr422d}
NV12: {nv12}, NV12 Deflated: {nv12d}" );
		}

		private void DCT양자화크기계산QToolStripMenuItem_Click ( object sender, EventArgs e )
		{
			ColorspaceConverter.GetColours ( out float [,] ys, out float [,] us, out float [,] vs, original );
			ColorspaceConverter.CalculateCompressionYUV ( out int yuv, out int dctq, out int dctqd, ys, us, vs );
			MessageBox.Show ( $@"YCbCr444: {yuv}
YCbCr444 DCT Quantization: {dctq}
YCbCr444 DCT Quantization Deflate: {dctqd}" );
		}

		private void PictureBox1_Click ( object sender, EventArgs e )
		{
			if ( pictureBox1.Image != original )
			{
				Image temp = pictureBox1.Image;
				pictureBox1.Image = original;
				temp.Dispose ();
			}
			else pictureBox1.Image = ColorspaceConverter.ColorDifference ( original, original );
		}

		private void PictureBox2_Click ( object sender, EventArgs e )
		{
			if ( pictureBox2.Image != yuv444 )
			{
				Image temp = pictureBox2.Image;
				pictureBox2.Image = yuv444;
				temp.Dispose ();
			}
			else pictureBox2.Image = ColorspaceConverter.ColorDifference ( original, yuv444 );
		}

		private void PictureBox3_Click ( object sender, EventArgs e )
		{
			if ( pictureBox3.Image != yuv422 )
			{
				Image temp = pictureBox3.Image;
				pictureBox3.Image = yuv422;
				temp.Dispose ();
			}
			else pictureBox3.Image = ColorspaceConverter.ColorDifference ( original, yuv422 );
		}

		private void PictureBox4_Click ( object sender, EventArgs e )
		{
			if ( pictureBox4.Image != nv12 )
			{
				Image temp = pictureBox4.Image;
				pictureBox4.Image = nv12;
				temp.Dispose ();
			}
			else pictureBox4.Image = ColorspaceConverter.ColorDifference ( original, nv12 );
		}

		private void PictureBox5_Click ( object sender, EventArgs e )
		{
			if ( pictureBox5.Image != dctyuv444 )
			{
				Image temp = pictureBox5.Image;
				pictureBox5.Image = dctyuv444;
				temp.Dispose ();
			}
			else pictureBox5.Image = ColorspaceConverter.ColorDifference ( original, dctyuv444 );
		}

		public MainForm ()
		{
			InitializeComponent ();

			original = new Bitmap ( Resources.lenna1 );
			yuv444 = ColorspaceConverter.ConvertToYUV444 ( original );
			yuv422 = ColorspaceConverter.ConvertToYUV422 ( original );
			nv12 = ColorspaceConverter.ConvertToNV12 ( original );
			dctyuv444 = ColorspaceConverter.ConvertToQuantizedYUV444 ( original );

			pictureBox1.Image = original;
			pictureBox2.Image = yuv444;
			pictureBox3.Image = yuv422;
			pictureBox4.Image = nv12;
			pictureBox5.Image = dctyuv444;
		}
	}
}
