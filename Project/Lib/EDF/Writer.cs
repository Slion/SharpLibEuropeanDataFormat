//#define TRACE_BYTES
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace SharpLib.EuropeanDataFormat
{
    class Writer : BinaryWriter
    {
        public Writer(FileStream fs) : base(fs) { }

        public void WriteEDF(File edf, string edfFilePath)
        {
            edf.Header.SizeInBytes.Value = CalcNumOfBytesInHeader(edf);

            //----------------- Fixed length header items -----------------
            WriteItem(edf.Header.Version);
            WriteItem(edf.Header.PatientID);
            WriteItem(edf.Header.RecordID);
            WriteItem(edf.Header.RecordingStartDate);
            WriteItem(edf.Header.RecordingStartTime);
            WriteItem(edf.Header.SizeInBytes);
            WriteItem(edf.Header.Reserved);
            WriteItem(edf.Header.RecordCount);
            WriteItem(edf.Header.RecordDurationInSeconds);
            WriteItem(edf.Header.SignalCount);

            //----------------- Variable length header items -----------------
            var headerSignalsLabel = edf.Signals.Select(s => s.Label);
            if (edf.AnnotationSignals != null)
                headerSignalsLabel = headerSignalsLabel.Concat(edf.AnnotationSignals.Select(x => x.Label));
            WriteItem(headerSignalsLabel);

            var trandsducerTypes = edf.Signals.Select(s => s.TransducerType);
            if (edf.AnnotationSignals != null)
                trandsducerTypes = trandsducerTypes.Concat(edf.AnnotationSignals.Select(x => x.TransducerType));
            WriteItem(trandsducerTypes);

            var physicalDimensions = edf.Signals.Select(s => s.PhysicalDimension);
            if (edf.AnnotationSignals != null)
                physicalDimensions = physicalDimensions.Concat(edf.AnnotationSignals.Select(x => x.PhysicalDimension));
            WriteItem(physicalDimensions);

            var physicalMinimums = edf.Signals.Select(s => s.PhysicalMinimum);
            if (edf.AnnotationSignals != null)
                physicalMinimums = physicalMinimums.Concat(edf.AnnotationSignals.Select(x => x.PhysicalMinimum));
            WriteItem(physicalMinimums);

            var physicalMaximuns = edf.Signals.Select(s => s.PhysicalMaximum);
            if (edf.AnnotationSignals != null)
                physicalMaximuns = physicalMaximuns.Concat(edf.AnnotationSignals.Select(x => x.PhysicalMaximum));
            WriteItem(physicalMaximuns);

            var digitalMinimuns = edf.Signals.Select(s => s.DigitalMinimum);
            if (edf.AnnotationSignals != null)
                digitalMinimuns = digitalMinimuns.Concat(edf.AnnotationSignals.Select(x => x.DigitalMinimum));
            WriteItem(digitalMinimuns);

            var digitalMaximuns = edf.Signals.Select(s => s.DigitalMaximum);
            if (edf.AnnotationSignals != null)
                digitalMaximuns = digitalMaximuns.Concat(edf.AnnotationSignals.Select(x => x.DigitalMaximum));
            WriteItem(digitalMaximuns);

            var prefilterings = edf.Signals.Select(s => s.Prefiltering);
            if (edf.AnnotationSignals != null)
                prefilterings = prefilterings.Concat(edf.AnnotationSignals.Select(x => x.Prefiltering));
            WriteItem(prefilterings);

            var samplesCountPerRecords = edf.Signals.Select(s => s.SampleCountPerRecord);
            if (edf.AnnotationSignals != null)
                samplesCountPerRecords = samplesCountPerRecords.Concat(edf.AnnotationSignals.Select(x => x.SampleCountPerRecord));
            WriteItem(samplesCountPerRecords);

            var reservedValues = edf.Signals.Select(s => s.Reserved);
            if (edf.AnnotationSignals != null)
                reservedValues = reservedValues.Concat(edf.AnnotationSignals.Select(x => x.Reserved));
            WriteItem(reservedValues);

            Console.WriteLine("Writer position after header: " + BaseStream.Position);

            Console.WriteLine("Writing signals.");
            WriteSignals(edf);

            Close();
            Console.WriteLine("File size: " + new System.IO.FileInfo(edfFilePath).Length);
        }



        private int CalcNumOfBytesInHeader(File edf)
        {
            int totalFixedLength = 256;
            int ns = edf.Signals.Length;
            ns = edf.AnnotationSignals != null ? ns + edf.AnnotationSignals.Count() : ns;
            int totalVariableLength = ns * 16 + (ns * 80) * 2 + (ns * 8) * 6 + (ns * 32);
            return totalFixedLength + totalVariableLength;
        }

        private void WriteItem(HeaderItem headerItem)
        {
            string strItem = headerItem.ToAscii();
            if (strItem == null) strItem = "";
            byte[] itemBytes = AsciiToBytes(strItem);
            this.Write(itemBytes);
#if TRACE_BYTES
            Console.WriteLine(headerItem.Name + " [" + strItem + "] \n\n-- ** BYTES LENGTH: " + itemBytes.Length
                + "> Position after write item: " + this.BaseStream.Position + "\n");
#endif
        }

        private void WriteItem(IEnumerable<HeaderItem> headerItems)
        {
            string joinedItems = StrJoin(headerItems);
            if (joinedItems == null) joinedItems = "";
            byte[] itemBytes = AsciiToBytes(joinedItems);
            this.Write(itemBytes);
#if TRACE_BYTES
            Console.WriteLine("[" + joinedItems + "] \n\n-- ** BYTES LENGTH: " + itemBytes.Length
                + ", Position after write item: " + this.BaseStream.Position + "\n");
#endif
        }

        private string StrJoin(IEnumerable<HeaderItem> list)
        {
            string joinedString = "";

            foreach (var item in list)
            {
                joinedString += item.ToAscii();
            }

            return joinedString;
        }

        private static byte[] AsciiToBytes(string strItem)
        {
            return Encoding.ASCII.GetBytes(strItem);
        }

        private static byte[] AsciiToIntBytes(string strItem, int length)
        {
            string strInt = "";
            string str = strItem.Substring(0, length);
            double val = Convert.ToDouble(str);
            strInt += val.ToString("0").PadRight(length, ' ');
            return Encoding.ASCII.GetBytes(strInt);
        }

        private void WriteSignals(File edf)
        {
            if (!edf.Signals.Any())
            {
                Console.WriteLine("There are no signals to write");
                return;
            }
#if TRACE_BYTES
            Console.WriteLine("Write position before signal: " + this.BaseStream.Position);
#endif
            int numberOfRecords = edf.Header.RecordCount.Value;

            for (int recordIndex = 0; recordIndex < numberOfRecords; recordIndex++)
            {
                foreach (Signal signal in edf.Signals)
                {
                    int signalStartPos = recordIndex * signal.SampleCountPerRecord.Value;
                    int signalEndPos = Math.Min(signalStartPos + signal.SampleCountPerRecord.Value, signal.Samples.Count);
                    for (; signalStartPos < signalEndPos; signalStartPos++)
                        this.Write(BitConverter.GetBytes(signal.Samples[signalStartPos]));
                }
                if (edf.AnnotationSignals != null && edf.AnnotationSignals.Any())
                {
                    foreach (var annotationSignal in edf.AnnotationSignals)
                        WriteAnnotations(recordIndex, annotationSignal.Samples, annotationSignal.SampleCountPerRecord.Value);
                }

            }
#if TRACE_BYTES
            Console.WriteLine("Write position after signals: " + this.BaseStream.Position);
#endif
        }

        /// <summary>
        /// If the EDF file has annotations inside it, for each record there has to be an "annotation index"
        /// and an annotation value. If there is not annotation value, the index has to be written anyway.
        /// Given a record index and the whole collection of Time-stamped Annotations it writes the annotation 
        /// index and its value. The rest of bytes are filled by 0 based on the sampleCountPerRecord.
        /// </summary>
        /// <param name="index">Record index, necessary to locate the TAL and to write index</param>
        /// <param name="annotations">List of Time-stamped Annotations</param>
        /// <param name="sampleCountPerRecord"></param>
        private void WriteAnnotations(int index, List<TAL> annotations, int sampleCountPerRecord)
        {
            var bytesWritten = 0;
            bytesWritten += WriteAnnotationIndex(index);
            if (index < annotations.Count)
                bytesWritten += WriteAnnotation(annotations[index]);

            //Fills block size left with 0
            var blockSize = sampleCountPerRecord * 2;
#if TRACE_BYTES
            Console.WriteLine($"Total bytes for Annotation index {0} is {bytesWritten}");
#endif
            Debug.Assert(bytesWritten <= blockSize, "Annotation signal too big for SampleCountPerRecord");
#if TRACE_BYTES
            Console.WriteLine($"Filling with {blockSize - bytesWritten} bytes");
#endif
            for (int i = bytesWritten; i < blockSize; i++)
                this.Write(TAL.byte_0);
#if TRACE_BYTES
            Console.WriteLine("Write position after filled by 0: " + this.BaseStream.Position);
#endif
        }

        private int WriteAnnotation(TAL annotations)
        {
#if TRACE_BYTES
            Console.WriteLine("Write position before annotation: " + this.BaseStream.Position);
#endif
            var bytesToWrite = TALExtensions.GetBytes(annotations);
#if TRACE_BYTES
            Console.WriteLine("Bytes to write: " + bytesToWrite.Length);
#endif
            this.Write(bytesToWrite);
#if TRACE_BYTES
            Console.WriteLine("Write position after annotation: " + this.BaseStream.Position);
#endif
            return bytesToWrite.Length;
        }

        private int WriteAnnotationIndex(int index)
        {
#if TRACE_BYTES
            Console.WriteLine("Write position before annotation index: " + this.BaseStream.Position);
#endif
            var bytesToWrite = TALExtensions.GetBytesForTALIndex(index);
#if TRACE_BYTES
            Console.WriteLine("Bytes to write: " + bytesToWrite.Length);
#endif
            this.Write(bytesToWrite);
#if TRACE_BYTES
            Console.WriteLine("Write position after annotation index: " + this.BaseStream.Position);
#endif
            return bytesToWrite.Length;


        }
    }
}