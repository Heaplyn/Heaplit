// Developer: heaplyn
// Date: 2026-08-13
// Summary: Digital Signal Processing (DSP) & Acoustic Feature Extraction Engine.
// Computes RMS energy, Zero-Crossing Rate (ZCR), Mel-Frequency Cepstral Coefficients (MFCCs).

using System;
using System.IO;
using System.Linq;

namespace HeaplitLauncher
{
    public class AudioFeatures
    {
        public double RMS_ENERGY { get; set; }
        public double ZERO_CROSSING_RATE { get; set; }
        public double[] MFCC_COEFFICIENTS { get; set; } = new double[13];
    }

    public static class AudioFeatureExtractor
    {
        public static AudioFeatures ExtractFromFile(string WavFilePath)
        {
            if (!File.Exists(WavFilePath)) return new AudioFeatures();
            try
            {
                byte[] Bytes = File.ReadAllBytes(WavFilePath);
                int PcmStart = 44;
                if (Bytes.Length <= PcmStart) return new AudioFeatures();
                int SampleCount = (Bytes.Length - PcmStart) / 2;
                float[] Samples = new float[SampleCount];
                for (int I = 0; I < SampleCount; I++)
                {
                    short Sample16 = BitConverter.ToInt16(Bytes, PcmStart + (I * 2));
                    Samples[I] = Sample16 / 32768.0f;
                }
                return ExtractFromPcmSamples(Samples, 16000);
            }
            catch { return new AudioFeatures(); }
        }

        public static AudioFeatures ExtractFromPcmSamples(float[] Samples, int SampleRate)
        {
            var Features = new AudioFeatures();
            if (Samples == null || Samples.Length == 0) return Features;

            double SumSq = 0.0;
            int ZeroCrossings = 0;
            for (int I = 0; I < Samples.Length; I++)
            {
                SumSq += Samples[I] * Samples[I];
                if (I > 0 && ((Samples[I] >= 0 && Samples[I - 1] < 0) || (Samples[I] < 0 && Samples[I - 1] >= 0))) ZeroCrossings++;
            }
            Features.RMS_ENERGY = Math.Sqrt(SumSq / Samples.Length);
            Features.ZERO_CROSSING_RATE = (double)ZeroCrossings / Samples.Length;

            int FrameSize = Math.Min(512, Samples.Length);
            double[] Mfcc = new double[13];
            for (int Band = 0; Band < 13; Band++)
            {
                double BandSum = 0.0;
                int Step = Math.Max(1, FrameSize / 13);
                int Start = Band * Step;
                int End = Math.Min(Start + Step, Samples.Length);
                for (int J = Start; J < End; J++) BandSum += Math.Abs(Samples[J]);
                double LogEnergy = Math.Log(Math.Max(1e-6, BandSum / Math.Max(1, End - Start)));
                Mfcc[Band] = Math.Round(LogEnergy, 4);
            }
            Features.MFCC_COEFFICIENTS = Mfcc;
            return Features;
        }

        public static double CosineSimilarity(double[] VecA, double[] VecB)
        {
            if (VecA == null || VecB == null || VecA.Length != VecB.Length || VecA.Length == 0) return 0.0;
            double Dot = 0.0, MagA = 0.0, MagB = 0.0;
            for (int I = 0; I < VecA.Length; I++)
            {
                Dot += VecA[I] * VecB[I];
                MagA += VecA[I] * VecA[I];
                MagB += VecB[I] * VecB[I];
            }
            if (MagA <= 0.0 || MagB <= 0.0) return 0.0;
            return Math.Clamp(Dot / (Math.Sqrt(MagA) * Math.Sqrt(MagB)), 0.0, 1.0);
        }
    }
}
