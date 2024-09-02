using System;
using System.Windows;
using System.Windows.Media.Imaging;
using ComputeSharp;

namespace ComputeSharpWpf
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			GenerateImage();
		}

		private void GenerateImage()
		{
			int width = 512;
			int height = 512;

			// Allocate a texture with float4, which the shader will work on
			using (var texture = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<float4>(width, height))
			{
				// Apply the shader
				texture.GraphicsDevice.For(texture.Width, texture.Height, new MyShader(texture));

				// Create a WriteableBitmap and set its pixels
				WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
				bitmap.Lock();

				unsafe
				{
					var dataPointer = (byte*)bitmap.BackBuffer.ToPointer();

					// Since textureData is a 2D array
					float4[,] textureData = texture.ToArray();

					for (int y = 0; y < height; y++)
					{
						for (int x = 0; x < width; x++)
						{
							// Directly access the pixel with 2D indexing
							float4 pixel = textureData[x, y];

							// Create the Rgba32 from the float4 values
							Rgba32 rgba = new Rgba32(
								(byte)(pixel.X * 255), // Red
								(byte)(pixel.Y * 255), // Green
								(byte)(pixel.Z * 255)  // Blue
													   // Alpha is automatically set to 255
							);

							// Calculate the linear index for writing into the WriteableBitmap
							int pixelIndex = (y * width + x) * 4;
							dataPointer[pixelIndex + 0] = rgba.R;
							dataPointer[pixelIndex + 1] = rgba.G;
							dataPointer[pixelIndex + 2] = rgba.B;
							dataPointer[pixelIndex + 3] = rgba.A;
						}
					}
				}

				bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
				bitmap.Unlock();

				// Set the image to a WPF Image control
				MyImageControl.Source = bitmap;
			}
		}
	}

	[ThreadGroupSize(8, 8, 1)]
	[GeneratedComputeShaderDescriptor]
	public readonly partial struct MyShader : IComputeShader
	{
		private readonly ReadWriteTexture2D<float4> texture;

		public MyShader(ReadWriteTexture2D<float4> texture)
		{
			this.texture = texture;
		}

		public void Execute()
		{
			float u = ThreadIds.X / (float)DispatchSize.X;
			float v = ThreadIds.Y / (float)DispatchSize.Y;

			// Correct indexing using 2D indices
			texture[ThreadIds.X, ThreadIds.Y] = new float4(u, v, 0.5f, 1.0f);
		}
	}
}
