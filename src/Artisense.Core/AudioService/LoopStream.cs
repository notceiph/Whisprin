// Copyright (c) Artisense. All rights reserved.

using System;
using System.IO;
using NAudio.Wave;

namespace Artisense.Core.AudioService
{
    /// <summary>
    /// A wave stream that loops audio data seamlessly with 128-sample alignment.
    /// </summary>
    public class LoopStream : WaveStream
    {
        private readonly WaveStream sourceStream;
        private readonly long loopStart;
        private readonly long loopEnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopStream"/> class.
        /// </summary>
        /// <param name="sourceStream">The source audio stream to loop.</param>
        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
            
            // Align loop points to 128-sample boundaries for optimal performance
            var sampleSize = sourceStream.WaveFormat.BlockAlign;
            var samplesPerBlock = 128;
            var blockSize = samplesPerBlock * sampleSize;
            
            loopStart = 0;
            loopEnd = (sourceStream.Length / blockSize) * blockSize;
            
            // Ensure we have at least one complete block
            if (loopEnd <= loopStart)
            {
                loopEnd = sourceStream.Length;
            }
        }

        /// <inheritdoc/>
        public override WaveFormat WaveFormat => sourceStream.WaveFormat;

        /// <inheritdoc/>
        public override long Length => long.MaxValue; // Infinite loop

        /// <inheritdoc/>
        public override long Position
        {
            get => sourceStream.Position;
            set
            {
                var newPosition = value;
                
                // Keep position within loop bounds
                if (newPosition >= loopEnd)
                {
                    newPosition = loopStart + ((newPosition - loopStart) % (loopEnd - loopStart));
                }
                else if (newPosition < loopStart)
                {
                    newPosition = loopStart;
                }
                
                sourceStream.Position = newPosition;
            }
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalBytesRead = 0;
            
            while (totalBytesRead < count)
            {
                var remainingInLoop = (int)(loopEnd - sourceStream.Position);
                var bytesToRead = Math.Min(count - totalBytesRead, remainingInLoop);
                
                if (bytesToRead <= 0)
                {
                    // Reset to loop start
                    sourceStream.Position = loopStart;
                    continue;
                }
                
                var bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, bytesToRead);
                
                if (bytesRead == 0)
                {
                    // End of stream, loop back
                    sourceStream.Position = loopStart;
                    continue;
                }
                
                totalBytesRead += bytesRead;
                
                // Check if we've reached the loop end
                if (sourceStream.Position >= loopEnd)
                {
                    sourceStream.Position = loopStart;
                }
            }
            
            return totalBytesRead;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sourceStream?.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
