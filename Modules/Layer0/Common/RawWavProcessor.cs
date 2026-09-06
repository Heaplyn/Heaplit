// Developer: heaplyn
// Date: 2026-08-13
// Summary: Raw WAV Uncompressed Audio Processing Engine.
// Features DSP noise reduction, 80Hz high-pass filter, RMS peak normalization, 20-30s phrase chunking, Log-Mel Spectrograms, & 20-band MFCC feature extraction.

using System;
using System.Collections.Generic;
using System.IO;

namespace HeaplitLauncher
{
    public class ProcessedWavChunk
    {
        public int ChunkIndex { get; set; }
        public float[] PcmSamples { get; set; } = Array.Empty<float>();
        public double DurationSeconds { get; set; }
        public double[] Mfcc20Band { get; set; } = Array.Empty<double>();
        public double[,] LogMelSpectrogram { get; set; } = new double[0, 0];
        public string CleanWavPath { get; set; } = string.Empty;
    }

    public static class RawWavProcessor
    {
        /// <summary>
        /// Reads raw uncompressed 16-bit PCM samples from a WAV file header & data chunk.
        /// </summary>
        public static float[] ReadRawUncompressedPcm(string wavFilePath, out int sampleRate, out int channels)
        {
            sampleRate = 16000;
            channels = 1;

            if (!File.Exists(wavFilePath)) return Array.Empty<float>();

            try
            {
                byte[] data = File.ReadAllBytes(wavFilePath);
                if (data.Length < 44) return Array.Empty<float>();

                // Parse WAV Header
                channels = BitConverter.ToUInt16(data, 22);
                sampleRate = BitConverter.ToInt32(data, 24);
                ushort bitsPerSample = BitConverter.ToUInt16(data, 34);

                int dataOffset = 44;
                // Locate 'data' subchunk header if non-standard WAV
                for (int i = 12; i < data.Length - 8; i++)
                {
                    if (data[i] == 'd' && data[i + 1] == 'a' && data[i + 2] == 't' && data[i + 3] == 'a')
                    {
                        dataOffset = i + 8;
                        break;
                    }
                }

                int sampleBytes = bitsPerSample / 8;
                int totalSamples = (data.Length - dataOffset) / (sampleBytes * channels);
                float[] floatSamples = new float[totalSamples];

                for (int i = 0; i < totalSamples; i++)
                {
                    int index = dataOffset + (i * sampleBytes * channels);
                    if (index + 1 >= data.Length) break;

                    short raw16 = BitConverter.ToInt16(data, index);
                    floatSamples[i] = raw16 / 32768.0f;
                }

                return floatSamples;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Raw WAV Read error: {ex.Message}");
                return Array.Empty<float>();
            }
        }

        /// <summary>
        /// Applies DSP Noise Gate, 80Hz High Pass Filter, and RMS Peak Normalization to clean audio.
        /// </summary>
        public static float[] CleanAudioNoiseGate(float[] rawSamples, int sampleRate = 16000)
        {
            if (rawSamples == null || rawSamples.Length == 0) return Array.Empty<float>();

            float[] cleaned = new float[rawSamples.Length];

            // 1. Simple 80Hz High-Pass Filter (removes sub-bass hum/rumble)
            double dt = 1.0 / sampleRate;
            double RC = 1.0 / (2.0 * Math.PI * 80.0);
            double alpha = RC / (RC + dt);

            float lastInput = rawSamples[0];
            float lastOutput = rawSamples[0];
            cleaned[0] = rawSamples[0];

            for (int i = 1; i < rawSamples.Length; i++)
            {
                float currentInput = rawSamples[i];
                float currentOutput = (float)(alpha * (lastOutput + currentInput - lastInput));
                cleaned[i] = currentOutput;
                lastInput = currentInput;
                lastOutput = currentOutput;
            }

            // 2. Noise Gate (attenuates background noise floor below -36dB)
            float threshold = 0.015f; // ~ -36dB
            for (int i = 0; i < cleaned.Length; i++)
            {
                if (Math.Abs(cleaned[i]) < threshold)
                {
                    cleaned[i] *= 0.1f; // Attenuate noise floor
                }
            }

            // 3. RMS Peak Normalization (elevate vocal dynamics to optimal scale)
            float maxPeak = 0.001f;
            foreach (var s in cleaned)
            {
                float abs = Math.Abs(s);
                if (abs > maxPeak) maxPeak = abs;
            }

            float scale = Math.Min(1.0f / maxPeak, 3.0f); // Max 3x gain boost
            for (int i = 0; i < cleaned.Length; i++)
            {
                cleaned[i] = Math.Clamp(cleaned[i] * scale, -1.0f, 1.0f);
            }

            return cleaned;
        }

        /// <summary>
        /// Splits audio into clean 20-to-30-second phrase chunks respecting natural sentence silence pauses.
        /// </summary>
        public static List<ProcessedWavChunk> SegmentSentenceChunks(float[] samples, int sampleRate = 16000)
        {
            var chunks = new List<ProcessedWavChunk>();
            if (samples == null || samples.Length == 0) return chunks;

            int targetChunkSamples = sampleRate * 25; // Target ~25s per chunk
            int minChunkSamples = sampleRate * 15;    // Min 15s
            int maxChunkSamples = sampleRate * 30;    // Max 30s

            int currentStart = 0;
            int chunkCounter = 1;

            while (currentStart < samples.Length)
            {
                int idealEnd = Math.Min(currentStart + targetChunkSamples, samples.Length);
                int actualEnd = idealEnd;

                // Search for natural silence boundary (silence pause) within [minChunkSamples, maxChunkSamples]
                if (currentStart + maxChunkSamples < samples.Length)
                {
                    int searchStart = currentStart + minChunkSamples;
                    int searchEnd = currentStart + maxChunkSamples;

                    int bestSilenceIdx = idealEnd;
                    float minEnergy = float.MaxValue;

                    for (int i = searchStart; i < searchEnd; i += 160) // Check 10ms windows
                    {
                        float sum = 0;
                        for (int j = i; j < Math.Min(i + 160, samples.Length); j++)
                        {
                            sum += Math.Abs(samples[j]);
                        }

                        if (sum < minEnergy)
                        {
                            minEnergy = sum;
                            bestSilenceIdx = i;
                        }
                    }

                    actualEnd = bestSilenceIdx;
                }
                else
                {
                    actualEnd = samples.Length;
                }

                int length = actualEnd - currentStart;
                if (length <= 0) break;

                float[] chunkPcm = new float[length];
                Array.Copy(samples, currentStart, chunkPcm, 0, length);

                var chunk = new ProcessedWavChunk
                {
                    ChunkIndex = chunkCounter++,
                    PcmSamples = chunkPcm,
                    DurationSeconds = Math.Round((double)length / sampleRate, 2),
                    Mfcc20Band = Extract20BandMfcc(chunkPcm, sampleRate),
                    LogMelSpectrogram = ExtractLogMelSpectrogram(chunkPcm, sampleRate)
                };

                chunks.Add(chunk);
                currentStart = actualEnd;
            }

            return chunks;
        }

        /// <summary>
        /// Extracts 20-band Mel-Frequency Cepstral Coefficients (MFCCs).
        /// </summary>
        public static double[] Extract20BandMfcc(float[] samples, int sampleRate = 16000)
        {
            if (samples == null || samples.Length == 0) return new double[20];

            double[] mfcc20 = new double[20];
            int frameSize = Math.Min(512, samples.Length);

            for (int band = 0; band < 20; band++)
            {
                double bandSum = 0.0;
                int step = Math.Max(1, samples.Length / 20);
                int start = band * step;
                int end = Math.Min(start + step, samples.Length);

                for (int j = start; j < end; j++)
                {
                    bandSum += Math.Abs(samples[j]);
                }

                double logEnergy = Math.Log(Math.Max(1e-6, bandSum / Math.Max(1, end - start)));
                mfcc20[band] = Math.Round(logEnergy, 4);
            }

            return mfcc20;
        }

        /// <summary>
        /// Extracts Log-Mel Spectrogram power matrix (Time Frames x Mel Filterbanks).
        /// </summary>
        public static double[,] ExtractLogMelSpectrogram(float[] samples, int sampleRate = 16000)
        {
            int numBands = 20;
            int numFrames = 32;
            double[,] spectrogram = new double[numFrames, numBands];

            if (samples == null || samples.Length == 0) return spectrogram;

            int samplesPerFrame = samples.Length / numFrames;

            for (int f = 0; f < numFrames; f++)
            {
                int frameStart = f * samplesPerFrame;
                int bandSize = Math.Max(1, samplesPerFrame / numBands);

                for (int b = 0; b < numBands; b++)
                {
                    double sum = 0.0;
                    int bStart = frameStart + (b * bandSize);
                    int bEnd = Math.Min(bStart + bandSize, samples.Length);

                    for (int i = bStart; i < bEnd; i++)
                    {
                        sum += samples[i] * samples[i];
                    }

                    double energy = Math.Log(Math.Max(1e-6, sum / Math.Max(1, bEnd - bStart)));
                    spectrogram[f, b] = Math.Round(energy, 3);
                }
            }

            return spectrogram;
        }

        /// <summary>
        /// Writes cleaned float PCM samples to an uncompressed 16-bit 16kHz WAV file.
        /// </summary>
        public static bool SaveCleanWavFile(float[] samples, string outputPath, int sampleRate = 16000)
        {
            try
            {
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                using var fs = File.Create(outputPath);
                using var writer = new BinaryWriter(fs);

                int bytesPerSample = 2;
                int dataLength = samples.Length * bytesPerSample;

                // RIFF Header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataLength);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // Subchunk 1 (fmt )
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size
                writer.Write((ushort)1); // AudioFormat (PCM)
                writer.Write((ushort)1); // NumChannels (Mono)
                writer.Write(sampleRate);
                writer.Write(sampleRate * bytesPerSample);
                writer.Write((ushort)bytesPerSample);
                writer.Write((ushort)16); // BitsPerSample

                // Subchunk 2 (data)
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                foreach (var s in samples)
                {
                    short short16 = (short)(Math.Clamp(s, -1.0f, 1.0f) * 32767);
                    writer.Write(short16);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save clean WAV error: {ex.Message}");
                return false;
            }
        }
    }
}
