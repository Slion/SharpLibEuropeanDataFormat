using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SharpLib.EuropeanDataFormat
{
    class Reader : BinaryReader
    {
        public Reader(FileStream fs) : base(fs) { }
        public Reader(byte[] edfBytes) : base(new MemoryStream(edfBytes)) { }

        public Header ReadHeader()
        {
            Header h = new Header();

            this.BaseStream.Seek(0, SeekOrigin.Begin);

            // Fixed size header
            h.Version.Value = ReadAscii(HeaderItems.Version);
            h.PatientID.Value = ReadAscii(HeaderItems.PatientID);
            h.RecordID.Value = ReadAscii(HeaderItems.RecordID);
            h.RecordingStartDate.Value = ReadAscii(HeaderItems.RecordingStartDate);
            h.RecordingStartTime.Value = ReadAscii(HeaderItems.RecordingStartTime);
            h.SizeInBytes.Value = ReadInt16(HeaderItems.SizeInBytes);
            h.Reserved.Value = ReadAscii(HeaderItems.Reserved);
            h.RecordCount.Value = ReadInt16(HeaderItems.NumberOfDataRecords);
            h.RecordDurationInSeconds.Value = ReadDouble(HeaderItems.RecordDurationInSeconds);
            h.SignalCount.Value = ReadInt16(HeaderItems.SignalCount);

            // Variable size header
            // Contains signal headers
            int ns = h.SignalCount.Value;
            h.Signals.Labels.Value = ReadMultipleAscii(HeaderItems.Label, ns);
            h.Signals.TransducerTypes.Value = ReadMultipleAscii(HeaderItems.TransducerType, ns);
            h.Signals.PhysicalDimensions.Value = ReadMultipleAscii(HeaderItems.PhysicalDimension, ns);
            h.Signals.PhysicalMinimums.Value = ReadMultipleDouble(HeaderItems.PhysicalMinimum, ns);
            h.Signals.PhysicalMaximums.Value = ReadMultipleDouble(HeaderItems.PhysicalMaximum, ns);
            h.Signals.DigitalMinimums.Value = ReadMultipleInt(HeaderItems.DigitalMinimum, ns);
            h.Signals.DigitalMaximums.Value = ReadMultipleInt(HeaderItems.DigitalMaximum, ns);
            h.Signals.Prefilterings.Value = ReadMultipleAscii(HeaderItems.Prefiltering, ns);
            h.Signals.SampleCountPerRecords.Value = ReadMultipleInt(HeaderItems.NumberOfSamplesInDataRecord, ns);
            h.Signals.Reserveds.Value = ReadMultipleAscii(HeaderItems.SignalsReserved, ns);

            h.ParseRecordingStartTime();

            return h;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aHeader"></param>
        /// <returns></returns>
        public Signal[] AllocateSignals(Header aHeader)
        {
            Signal[] signals = new Signal[aHeader.SignalCount.Value];

            for (int i = 0; i < signals.Length; i++)
            {
                signals[i] = new Signal();
                // Just copy data from the header, ugly architecture really...
                signals[i].Index = i;
                signals[i].Label.Value = aHeader.Signals.Labels.Value[i];
                signals[i].TransducerType.Value = aHeader.Signals.TransducerTypes.Value[i];
                signals[i].PhysicalDimension.Value = aHeader.Signals.PhysicalDimensions.Value[i];
                signals[i].PhysicalMinimum.Value = aHeader.Signals.PhysicalMinimums.Value[i];
                signals[i].PhysicalMaximum.Value = aHeader.Signals.PhysicalMaximums.Value[i];
                signals[i].DigitalMinimum.Value = aHeader.Signals.DigitalMinimums.Value[i];
                signals[i].DigitalMaximum.Value = aHeader.Signals.DigitalMaximums.Value[i];
                signals[i].Prefiltering.Value = aHeader.Signals.Prefilterings.Value[i];
                signals[i].Reserved.Value = aHeader.Signals.Reserveds.Value[i];
                signals[i].SampleCountPerRecord.Value = aHeader.Signals.SampleCountPerRecords.Value[i];
            }

            return signals;
        }

        /// <summary>
        /// Read the requested signal for our file
        /// </summary>
        /// <param name="aHeader"></param>
        /// <param name="aSignal"></param>
        public void ReadSignal(Header aHeader, Signal aSignal)
        {
            // Make sure we start just after our header
            this.BaseStream.Seek(aHeader.SizeInBytes.Value, SeekOrigin.Begin);

            aSignal.Samples.Clear();
            // Compute capacity thus pre-allocating memory to avoid resizing
            aSignal.Samples.Capacity = aHeader.RecordCount.Value * aSignal.SampleCountPerRecord.Value;
            int capacity = aSignal.Samples.Capacity;
            // For each record
            for (int j = 0; j < aHeader.RecordCount.Value; j++)
            {
                // For each signal
                for (int i = 0; i < aHeader.SignalCount.Value; i++)
                {
                    // Read that signal samples
                    if (i==aSignal.Index)
                    {
                        ReadNextSignalSamples(aSignal.Samples, aSignal.SampleCountPerRecord.Value);
                    }
                    else
                    {
                        // Just skip it
                        SkipSignalSamples(aHeader.Signals.SampleCountPerRecords.Value[i]);
                    }
                }
            }

            if (capacity != aSignal.Samples.Capacity)
            {
                // We should never get there
                Debug.WriteLine("ERROR: signal array was resized");
            }

        }

        /// <summary>
        /// Read all signal sample value from our file.
        /// </summary>
        /// <returns></returns>
        public Signal[] ReadSignals(Header aHeader)
        {            
            Signal[] signals = AllocateSignals(aHeader);
            // For each record
            for (int j = 0; j < aHeader.RecordCount.Value; j++)
            {
                // For each signal
                for (int i = 0; i < signals.Length; i++)
                {
                    // Read that signal samples
                    ReadNextSignalSamples(signals[i].Samples, signals[i].SampleCountPerRecord.Value);                    
                }
            }

            return signals;
        }
        
        /// <summary>
        /// Read n next samples
        /// </summary>
        /// <param name="aSamples"></param>
        /// <param name="aSampleCount"></param>
        private void ReadNextSignalSamples(ICollection<short> aSamples, int aSampleCount)
        {
            // Single file read operation per record
            byte[] intBytes = this.ReadBytes(sizeof(short) * aSampleCount);
            for (int i=0;i<aSampleCount;i++)
            {
                // Fetch our sample short from our record buffer
                short intVal = BitConverter.ToInt16(intBytes, i* sizeof(short));
                // TODO: use a static array for better performance? I guess it's not needed since we prealloc using Capacity.
                aSamples.Add(intVal);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSampleCount"></param>
        private void SkipSignalSamples(int aSampleCount)
        {
            BaseStream.Seek(aSampleCount * sizeof(short), SeekOrigin.Current);
        }



        /// <summary>
        /// TODO: Is this still being used?
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="numberOfSamples"></param>
        /// <returns></returns>
        private short[] ReadSignalSamples(int startPosition, int numberOfSamples)
        {
            var samples = new List<short>();
            int countBytesRead = 0;

            this.BaseStream.Seek(startPosition, SeekOrigin.Begin);

            while (countBytesRead < numberOfSamples * 2) //2 bytes per integer
            {
                byte[] intBytes = this.ReadBytes(2);
                short intVal = BitConverter.ToInt16(intBytes, 0);
                samples.Add(intVal);
                countBytesRead += intBytes.Length;
            }

            return samples.ToArray();
        }

        private Int16 ReadInt16(Field itemInfo)
        {
            string strInt = ReadAscii(itemInfo).Trim();
            Int16 intResult = -1;
            try { intResult = Convert.ToInt16(strInt); }
            catch (Exception ex) { Console.WriteLine("Error, could not convert string to integer. " + ex.Message); }
            return intResult;
        }
        
        private Double ReadDouble(Field itemInfo)
        {
            String value = ReadAscii(itemInfo).Trim();
            try {
                return Double.Parse(value, CultureInfo.InvariantCulture);
            } catch (FormatException ex) {
                Console.WriteLine("Error, could not convert string to integer: " + ex.Message);
                return -1;
            }
        }

        private string ReadAscii(Field itemInfo)
        {
            byte[] bytes = this.ReadBytes(itemInfo.AsciiLength);
            return AsciiString(bytes).Trim();
        }

        private string[] ReadMultipleAscii(Field itemInfo, int numberOfParts)
        {
            var parts = new List<string>();

            for (int i = 0; i < numberOfParts; i++) {
                byte[] bytes = this.ReadBytes(itemInfo.AsciiLength);
                parts.Add(AsciiString(bytes).Trim());
            }
            
            return parts.ToArray();
        }

        private int[] ReadMultipleInt(Field itemInfo, int numberOfParts)
        {
            var parts = new List<int>();

            for (int i = 0; i < numberOfParts; i++)
            {
                byte[] bytes = this.ReadBytes(itemInfo.AsciiLength);
                string ascii = AsciiString(bytes);
                parts.Add(Convert.ToInt32(ascii));
            }

            return parts.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemInfo"></param>
        /// <param name="numberOfParts"></param>
        /// <returns></returns>
        private double[] ReadMultipleDouble(Field itemInfo, int numberOfParts)
        {
            var parts = new List<double>();

            for (int i = 0; i < numberOfParts; i++)
            {
                byte[] bytes = this.ReadBytes(itemInfo.AsciiLength);
                string ascii = AsciiString(bytes);
                // Use invariant culure as we have a '.' as decimal separator
                parts.Add(double.Parse(ascii, CultureInfo.InvariantCulture));
            }

            return parts.ToArray();
        }

        private static string AsciiString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }
    }
}
