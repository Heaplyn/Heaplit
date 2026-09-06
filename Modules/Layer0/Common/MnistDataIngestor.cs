// Developer: heaplyn
// Date: 2026-08-19
// Summary: High-Accuracy MNIST Dataset Ingestor for Godellian Intelligence.
//          Downloads and parses the IDX3-UBYTE format to provide raw training patterns.
//          Bridges the gap between computer vision benchmarks and local neural evolution.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public static class MnistDataIngestor
    {
        private static readonly string MnistDir = Path.Combine(PathHandler.GetDataDirectory(), "Intelligence", "MNIST");
        private const string TrainImagesUrl = "http://yann.lecun.com/exdb/mnist/train-images-idx3-ubyte.gz";
        private const string TrainLabelsUrl = "http://yann.lecun.com/exdb/mnist/train-labels-idx1-ubyte.gz";

        public static async Task StartIngestionAsync()
        {
            if (!Directory.Exists(MnistDir)) Directory.CreateDirectory(MnistDir);

            string imgPath = Path.Combine(MnistDir, "train-images.idx3-ubyte");
            string lblPath = Path.Combine(MnistDir, "train-labels.idx1-ubyte");

            if (!File.Exists(imgPath)) await DownloadAndDecompressAsync(TrainImagesUrl, imgPath);
            if (!File.Exists(lblPath)) await DownloadAndDecompressAsync(TrainLabelsUrl, lblPath);

            if (File.Exists(imgPath) && File.Exists(lblPath))
            {
                DebugConsoleOverlay.Log("MNIST", "Found local MNIST dataset. Extracting patterns...");
                await IngestMnistPatternsAsync(imgPath, lblPath);
            }
        }

        private static async Task DownloadAndDecompressAsync(string url, string destPath)
        {
            try
            {
                DebugConsoleOverlay.Log("MNIST-Download", $"Fetching: {url}");
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);
                var bytes = await client.GetByteArrayAsync(url);

                using var ms = new MemoryStream(bytes);
                using var gzs = new GZipStream(ms, CompressionMode.Decompress);
                using var fs = File.Create(destPath);
                await gzs.CopyToAsync(fs);
                DebugConsoleOverlay.Log("MNIST-Download", $"Saved and decompressed: {destPath}");
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("MNIST-Error", $"Download failed: {ex.Message}");
            }
        }

        private static async Task IngestMnistPatternsAsync(string imgPath, string lblPath)
        {
            try
            {
                using var imgFs = File.OpenRead(imgPath);
                using var lblFs = File.OpenRead(lblPath);
                using var imgBr = new BinaryReader(imgFs);
                using var lblBr = new BinaryReader(lblFs);

                // Read Headers
                int magicImg = ReadBigEndianInt32(imgBr);
                int countImg = ReadBigEndianInt32(imgBr);
                int rows = ReadBigEndianInt32(imgBr);
                int cols = ReadBigEndianInt32(imgBr);

                int magicLbl = ReadBigEndianInt32(lblBr);
                int countLbl = ReadBigEndianInt32(lblBr);

                int batchSize = 100; // Ingest 100 random patterns per pass
                int dim = NeuralVectorizationKernels.CurrentDimension;

                var inputs = new List<double[]>();
                var targets = new List<double[]>();

                var rand = new Random();
                for (int i = 0; i < batchSize; i++)
                {
                    int index = rand.Next(countImg);
                    imgFs.Seek(16 + index * rows * cols, SeekOrigin.Begin);
                    lblFs.Seek(8 + index, SeekOrigin.Begin);

                    byte[] pixels = imgBr.ReadBytes(rows * cols);
                    byte label = lblBr.ReadByte();

                    // Flatten and normalize 28x28 -> 784 -> project to current brain dimension
                    double[] rawInput = pixels.Select(p => (double)p / 255.0).ToArray();
                    double[] projectedInput = NeuralVectorizationKernels.ProjectVector(rawInput, dim);

                    // One-hot label vector (10-dim) -> project to current output dimension
                    double[] rawLabel = new double[10];
                    rawLabel[label] = 1.0;
                    double[] projectedTarget = NeuralVectorizationKernels.ProjectVector(rawLabel, dim); // Assuming output dim matches dim for simple auto-association

                    inputs.Add(projectedInput);
                    targets.Add(projectedTarget);
                }

                if (inputs.Count > 0)
                {
                    // Neural brain removed — MNIST data ingested but not trained
                    DebugConsoleOverlay.Log("MNIST-Ingest", $"Collected {inputs.Count} visual patterns (neural training disabled).");
                }
            }
            catch (Exception ex)
            {
                DebugConsoleOverlay.Log("MNIST-Error", $"Ingestion failed: {ex.Message}");
            }
        }

        private static int ReadBigEndianInt32(BinaryReader br)
        {
            var bytes = br.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
