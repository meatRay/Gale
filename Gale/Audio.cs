﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;

namespace Gale
{
	public class SineWaveProvider32 : WaveProvider32
	{
		int sample;

		public SineWaveProvider32()
		{
			Frequency = 1000;
			Amplitude = 0.25f; // let's not hurt our ears            
		}

		public float Frequency { get; set; }
		public float Amplitude { get; set; }

		public override int Read( float[] buffer, int offset, int sampleCount )
		{
			int sampleRate = WaveFormat.SampleRate;
			for (int n = 0; n < sampleCount; n++)
			{
				buffer[n + offset] = (float)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / sampleRate));
				sample++;
				if (sample >= sampleRate)
					sample = 0;
			}
			return sampleCount;
		}
	}
	public abstract class WaveProvider32 : IWaveProvider
	{
		private WaveFormat waveFormat;

		public WaveProvider32()
			: this(44100, 1)
		{
		}

		public WaveProvider32( int sampleRate, int channels )
		{
			SetWaveFormat(sampleRate, channels);
		}

		public void SetWaveFormat( int sampleRate, int channels )
		{
			this.waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
		}

		public int Read( byte[] buffer, int offset, int count )
		{
			WaveBuffer waveBuffer = new WaveBuffer(buffer);
			int samplesRequired = count / 4;
			int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
			return samplesRead * 4;
		}

		public abstract int Read( float[] buffer, int offset, int sampleCount );

		public WaveFormat WaveFormat
		{
			get { return waveFormat; }
		}
	}

	class Audio : IDisposable
    {
		public ALSourceState State { get; private set; }
		private int _buffer;
		private int _source;
		public Audio( int source, int buffer )
		{
			_source = source;
			_buffer = buffer;
			State = ALSourceState.Initial;
		}

		public async Task Play()
		{
			AL.SourcePlay(_source);
			do
			{
				await Task.Delay(100);
				State = AL.GetSourceState(_source);
			} while (State == ALSourceState.Playing);
			AL.SourceStop(_source);
		}

		public static Audio LoadAudio( string audio_filename )
		{
			int buffer = AL.GenBuffer();
			int source = AL.GenSource();

			int channels, bits_per_sample, sample_rate;
			byte[] sound_data = LoadWave(File.Open(audio_filename, FileMode.Open), out channels, out bits_per_sample, out sample_rate);
			AL.BufferData(buffer, GetSoundFormat(channels, bits_per_sample), sound_data, sound_data.Length, sample_rate);

			AL.Source(source, ALSourcei.Buffer, buffer);

			return new Audio(source, buffer);
		}

		// Loads a wave/riff audio file.
		public static byte[] LoadWave( Stream stream, out int channels, out int bits, out int rate )
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			using (BinaryReader reader = new BinaryReader(stream))
			{
				// RIFF header
				string signature = new string(reader.ReadChars(4));
				if (signature != "RIFF")
					throw new NotSupportedException("Specified stream is not a wave file.");

				int riff_chunck_size = reader.ReadInt32();

				string format = new string(reader.ReadChars(4));
				if (format != "WAVE")
					throw new NotSupportedException("Specified stream is not a wave file.");

				// WAVE header
				string format_signature = new string(reader.ReadChars(4));
				if (format_signature != "fmt ")
					throw new NotSupportedException("Specified wave file is not supported.");

				int format_chunk_size = reader.ReadInt32();
				int audio_format = reader.ReadInt16();
				int num_channels = reader.ReadInt16();
				int sample_rate = reader.ReadInt32();
				int byte_rate = reader.ReadInt32();
				int block_align = reader.ReadInt16();
				int bits_per_sample = reader.ReadInt16();

				string data_signature = new string(reader.ReadChars(4));
				if (data_signature != "data")
					throw new NotSupportedException("Specified wave file is not supported.");

				int data_chunk_size = reader.ReadInt32();

				channels = num_channels;
				bits = bits_per_sample;
				rate = sample_rate;

				return reader.ReadBytes((int)reader.BaseStream.Length);
			}
		}

		public static ALFormat GetSoundFormat( int channels, int bits )
		{
			switch (channels)
			{
			case 1:
				return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
			case 2:
				return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
			default:
				throw new NotSupportedException("The specified sound format is not supported.");
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if (!disposedValue)
			{
				AL.DeleteSource(_source);
				AL.DeleteBuffer(_buffer);
				disposedValue = true;
			}
		}

		~Audio() {
		  Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

	}
}
