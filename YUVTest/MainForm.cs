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

		private void pictureBox1_Click ( object sender, EventArgs e )
		{
			if ( pictureBox1.Image != original )
			{
				Image temp = pictureBox1.Image;
				pictureBox1.Image = original;
				temp.Dispose ();
			}
			else pictureBox1.Image = ColorspaceConverter.ColorDifference ( original, original );
		}

		private void pictureBox2_Click ( object sender, EventArgs e )
		{
			if ( pictureBox2.Image != yuv444 )
			{
				Image temp = pictureBox2.Image;
				pictureBox2.Image = yuv444;
				temp.Dispose ();
			}
			else pictureBox2.Image = ColorspaceConverter.ColorDifference ( original, yuv444 );
		}

		private void pictureBox3_Click ( object sender, EventArgs e )
		{
			if ( pictureBox3.Image != yuv422 )
			{
				Image temp = pictureBox3.Image;
				pictureBox3.Image = yuv422;
				temp.Dispose ();
			}
			else pictureBox3.Image = ColorspaceConverter.ColorDifference ( original, yuv422 );
		}

		private void pictureBox4_Click ( object sender, EventArgs e )
		{
			if ( pictureBox4.Image != nv12 )
			{
				Image temp = pictureBox4.Image;
				pictureBox4.Image = nv12;
				temp.Dispose ();
			}
			else pictureBox4.Image = ColorspaceConverter.ColorDifference ( original, nv12 );
		}

		private void pictureBox5_Click ( object sender, EventArgs e )
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

			original = new Bitmap ( Resources.untitled );
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
