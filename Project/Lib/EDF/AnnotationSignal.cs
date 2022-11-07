
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpLib.EuropeanDataFormat
{
    public class AnnotationSignal : IBaseSignal<TAL>
    {
        /// <summary>
        /// Provided sample value after scaling.
        /// </summary>
        /// <param name="aIndex"></param>
        /// <returns></returns>
        public TAL ScaledSample(int aIndex) { return Samples[aIndex]; }

        /// <summary>
        /// Provide sample scaling factor.
        /// </summary>
        /// <returns></returns>
        public double ScaleFactor() { return (PhysicalMaximum.Value - PhysicalMinimum.Value) / (DigitalMaximum.Value - DigitalMinimum.Value); }

        public override string ToString()
        {
            return Label.Value + " " + SampleCountPerRecord.Value.ToString() + "/" + Samples.Count().ToString() + " ["
                + string.Join<TAL>(",", Samples.Skip(0).Take(10).ToArray()) + " ...]";
        }

        public int Index { get; set; }

        public FixedLengthString Label { get; } = new FixedLengthString(HeaderItems.Label);

        public FixedLengthString TransducerType { get; } = new FixedLengthString(HeaderItems.TransducerType);

        public FixedLengthString PhysicalDimension { get; } = new FixedLengthString(HeaderItems.PhysicalDimension);

        public FixedLengthDouble PhysicalMinimum { get; } = new FixedLengthDouble(HeaderItems.PhysicalMinimum);

        public FixedLengthDouble PhysicalMaximum { get; } = new FixedLengthDouble(HeaderItems.PhysicalMaximum);

        public FixedLengthInt DigitalMinimum { get; } = new FixedLengthInt(HeaderItems.DigitalMinimum);

        public FixedLengthInt DigitalMaximum { get; } = new FixedLengthInt(HeaderItems.DigitalMaximum);

        public FixedLengthString Prefiltering { get; } = new FixedLengthString(HeaderItems.Prefiltering);

        public FixedLengthInt SampleCountPerRecord { get; } = new FixedLengthInt(HeaderItems.NumberOfSamplesInDataRecord);

        public FixedLengthString Reserved { get; } = new FixedLengthString(HeaderItems.SignalsReserved);

        public List<TAL> Samples { get; set; } = new List<TAL> { };

        public AnnotationSignal()
        {
            /* /// https://www.edfplus.info/specs/edfplus.html#annotationssignal section 2.2.1
             * For the sake of EDF compatibility, the fields 'digital minimum' and 'digital maximum' must be filled with -32768 and 32767, respectively. 
             * The 'Physical maximum' and 'Physical minimum' fields must contain values that differ from each other. 
             * The other fields of this signal are filled with spaces*/
            this.Label.Value = "EDF Annotations";
            this.DigitalMinimum.Value = -32768;
            this.DigitalMaximum.Value = 32767;
            this.PhysicalMinimum.Value = -1;
            this.PhysicalMaximum.Value = 1;
            this.PhysicalDimension.Value = String.Empty;
            this.TransducerType.Value = String.Empty;
            this.Prefiltering.Value = String.Empty;
            this.Reserved.Value = String.Empty;
        }
    }
    /// <summary>
    /// Represents a Time-stamped Annotation (TAL)
    /// </summary>
    public class TAL
    {
        private const String StringDoubleFormat = "0.###";
        //Standard TAL separators
        public static readonly byte byte_21 = BitConverter.GetBytes(21)[0];
        public static readonly byte byte_20 = BitConverter.GetBytes(20)[0];
        public static readonly byte byte_0 = BitConverter.GetBytes(0)[0];
        public static readonly byte byte_46 = BitConverter.GetBytes(46)[0];


        private double startSeconds;
        private double durationSeconds;
        public string StartSecondsString => startSeconds < 0 ? 
                                            $"-{startSeconds.ToString(StringDoubleFormat,CultureInfo.InvariantCulture)}" : 
                                            $"+{startSeconds.ToString(StringDoubleFormat, CultureInfo.InvariantCulture)}";
        public string DurationSecondsString => durationSeconds >= 0 ? durationSeconds.ToString(StringDoubleFormat, CultureInfo.InvariantCulture) : null;
        public String AnnotationDescription { get; private set; }

        public TAL(double startSeconds, double durationSeconds, string description)
        {
            this.startSeconds = startSeconds;
            this.durationSeconds = durationSeconds;
            this.AnnotationDescription = description;
        }
    }

    public static class TALExtensions
    {
        /// <summary>
        /// Returns a byte array witch represent a TAL format according to
        /// https://www.edfplus.info/specs/edfplus.html#annotationssignal section 2.2.2.
        /// </summary>
        /// <param name="tal"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this TAL tal)
        {
            List<byte> result = new List<byte>();
            result.AddRange(Encoding.ASCII.GetBytes(tal.StartSecondsString));
            if (tal.DurationSecondsString != null)
            {
                result.Add(TAL.byte_21); //15 in HEX
                result.AddRange(Encoding.ASCII.GetBytes(tal.DurationSecondsString));
            }
            result.Add(TAL.byte_20);
            result.AddRange(Encoding.ASCII.GetBytes(tal.AnnotationDescription));
            result.Add(TAL.byte_20);
            result.Add(TAL.byte_0);
            return result.ToArray();
        }

        public static byte[] GetBytesForTALIndex(int index)
        {
            var strIndex = index.ToString();
            var leftSide =  (strIndex.Length>1)? strIndex.Substring(0, strIndex.Length - 1): "0";
            var rightSide = strIndex.Substring(strIndex.Length - 1);

            List<byte> result = new List<byte>();
            result.AddRange(Encoding.ASCII.GetBytes("+"));
            result.AddRange(Encoding.ASCII.GetBytes(leftSide));
            result.Add(TAL.byte_46);
            result.AddRange(Encoding.ASCII.GetBytes(rightSide));
            result.Add(TAL.byte_20);
            result.Add(TAL.byte_20);
            result.Add(TAL.byte_0);
            /*
            List<byte> result = new List<byte>();
            float indexF = (float)index / 10;
            var leftSide = Math.Truncate(indexF);
            var rightSide = (int)(((decimal)indexF % 1) * 10);
            result.AddRange(Encoding.ASCII.GetBytes($"+{leftSide}"));
            result.Add(TAL.byte_46);
            result.AddRange(Encoding.ASCII.GetBytes($"{rightSide}"));
            result.Add(TAL.byte_20);
            result.Add(TAL.byte_20);
            result.Add(TAL.byte_0);
            */
            return result.ToArray();
        }
    }
}
