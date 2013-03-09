﻿using System;
using System.IO;

namespace PuyoTools2.Compression
{
    public class LZ11 : CompressionBase
    {
        public override bool Compress(byte[] source, long offset, int length, string fname, Stream destination)
        {
            throw new NotImplementedException();
        }

        public override bool Decompress(byte[] source, long offset, int length, Stream destination)
        {
            // Set up information for decompression
            int sourcePointer = 0x4;
            int destPointer = 0x0;
            int destLength = (int)(BitConverter.ToUInt32(source, (int)offset) >> 8);

            // If the destination length is 0, then the length is stored in the next 4 bytes
            if (destLength == 0)
            {
                destLength = (int)(BitConverter.ToUInt32(source, (int)offset + 4));
                sourcePointer += 4;
            }

            byte[] destBuffer = new byte[destLength];

            // Start Decompression
            while (sourcePointer < length && destPointer < destLength)
            {
                byte flag = source[offset + sourcePointer]; // Compression Flag
                sourcePointer++;

                for (int i = 0; i < 8; i++)
                {
                    if ((flag & 0x80) == 0) // Data is not compressed
                    {
                        destBuffer[destPointer] = source[offset + sourcePointer];
                        sourcePointer++;
                        destPointer++;
                    }
                    else // Data is compressed
                    {
                        int distance, amount;

                        // Let's determine how many bytes the distance & length pair take up
                        switch (source[offset + sourcePointer] >> 4)
                        {
                            case 0: // 3 bytes
                                distance = (((source[offset + sourcePointer + 1] & 0xF) << 8) | source[offset + sourcePointer + 2]) + 1;
                                amount = (((source[offset + sourcePointer] & 0xF) << 4) | (source[offset + sourcePointer + 1] >> 4)) + 17;
                                sourcePointer += 3;
                                break;

                            case 1: // 4 bytes
                                distance = (((source[offset + sourcePointer + 2] & 0xF) << 8) | source[offset + sourcePointer + 3]) + 1;
                                amount = (((source[offset + sourcePointer] & 0xF) << 12) | (source[offset + sourcePointer + 1] << 4) | (source[offset + sourcePointer + 2] >> 4)) + 273;
                                sourcePointer += 4;
                                break;

                            default: // 2 bytes
                                distance = (((source[offset + sourcePointer] & 0xF) << 8) | source[offset + sourcePointer + 1]) + 1;
                                amount = (source[offset + sourcePointer] >> 4) + 1;
                                sourcePointer += 2;
                                break;
                        }

                        // Copy the data
                        for (int j = 0; j < amount; j++)
                        {
                            destBuffer[destPointer] = destBuffer[destPointer - distance];
                            destPointer++;
                        }
                    }

                    // Check for out of range
                    if (sourcePointer >= length || destPointer >= destLength)
                        break;

                    flag <<= 1;
                }
            }

            destination.Write(destBuffer, 0, destLength);

            return true;
        }

        public override bool Is(Stream source, int length, string fname)
        {
            return (length > 4 && PTStream.Contains(source, 0, new byte[] { 0x11 }));
        }

        public override bool CanCompress()
        {
            throw new NotImplementedException();
        }
    }
}