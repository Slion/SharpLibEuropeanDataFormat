using System;
using System.Globalization;

namespace SharpLib.EuropeanDataFormat
{
    public class Field
    {
        public string Name { get; set; }
        public int AsciiLength { get; set; }

        public Field() { }

        public Field(string name, int asciiLength) {
            Name = name;
            AsciiLength = asciiLength;
        }
    }

    public class HeaderItems
    {
        //Fixed length header items
        public static Field Version { get; } = new Field("Version", 8);
        public static Field PatientID { get; } = new Field("PatientID", 80);
        public static Field RecordID { get; private set; } = new Field("RecordID", 80);
        public static Field RecordingStartDate { get; private set; } = new Field("StartDate", 8);
        public static Field RecordingStartTime { get; private set; } = new Field("StartTime", 8);
        public static Field SizeInBytes { get; private set; } = new Field("NumberOfBytesInHeader", 8);
        public static Field Reserved { get; private set; }  = new Field("Reserved", 44);
        public static Field NumberOfDataRecords { get; private set; } = new Field("NumberOfDataRecords", 8);
        public static Field RecordDurationInSeconds { get; private set; } = new Field("DurationOfDataRecord", 8);
        public static Field SignalCount { get; private set; } = new Field("NumberOfSignals", 4);

        //Variable size signal header items
        public static Field Label { get; private set; } = new Field("Labels", 16);
        public static Field TransducerType { get; private set; } = new Field("TransducerType", 80);
        public static Field PhysicalDimension { get; private set; } = new Field("PhysicalDimension", 8);
        public static Field PhysicalMinimum { get; private set; } = new Field("PhysicalMinimum", 8);
        public static Field PhysicalMaximum { get; private set; } = new Field("PhysicalMaximum", 8);
        public static Field DigitalMinimum { get; private set; } = new Field("DigitalMinimum", 8);
        public static Field DigitalMaximum { get; private set; } = new Field("DigitalMaximum", 8);
        public static Field Prefiltering { get; private set; } = new Field("Prefiltering", 80);
        public static Field NumberOfSamplesInDataRecord { get; private set; } = new Field("NumberOfSamplesInDataRecord", 8);
        public static Field SignalsReserved { get; private set; } = new Field("SignalsReserved", 32);
    }

    public abstract class HeaderItem
    {
        public HeaderItem(Field info) {
            Name = info.Name;
            AsciiLength = info.AsciiLength;
        }
        public string Name { get; set; }
        public int AsciiLength { get; set; }
        public abstract string ToAscii();
    }

    public class FixedLengthString : HeaderItem
    {
        public string Value { get; set; }
        public FixedLengthString(Field info) : base(info) { }

        public override string ToAscii() {
            string asciiString = "";
            if (Value != null)
                asciiString = Value.PadRight(AsciiLength, ' ');
            else
                asciiString = asciiString.PadRight(AsciiLength, ' ');

            return asciiString;
        }
    }

    public class FixedLengthInt : HeaderItem
    {
        public int Value { get; set; }
        public FixedLengthInt(Field info) : base(info) { }

        public override string ToAscii()
        {
            string asciiString = "";
            if (Value != null)
                asciiString = Value.ToString().PadRight(AsciiLength, ' ');
            else
                asciiString = asciiString.PadRight(AsciiLength, ' ');

            return asciiString;
        }
    }

    public class FixedLengthDouble : HeaderItem
    {
        public double Value { get; set; }
        public FixedLengthDouble(Field info) : base(info) { }

        public override string ToAscii()
        {
            string asciiString = "";
            if (Value != null)
            {
                asciiString = Value.ToString();
                if (asciiString.Length >= AsciiLength)
                    asciiString = asciiString.Substring(0, AsciiLength);
                else
                    asciiString = Value.ToString().PadRight(AsciiLength, ' ');
            }
                
            else
                asciiString = asciiString.PadRight(AsciiLength, ' ');

            return asciiString;
        }
    }

    public class VariableLengthString : HeaderItem
    {
        public string[] Value { get; set; }
        public VariableLengthString(Field info) : base(info) { }

        public override string ToAscii() {
            string ascii = "";
            foreach (var strVal in Value)
            {
                string temp = strVal.ToString();
                if (strVal.Length > AsciiLength)
                    temp = temp.Substring(0, AsciiLength);
                ascii += temp;
            }

            return ascii;
        }
    }

    public class VariableLengthInt : HeaderItem
    {
        public int[] Value { get; set; }
        public VariableLengthInt(Field info) : base(info) { }

        public override string ToAscii() {
            string ascii = "";
            foreach (var intVal in Value)
            {
                string temp = intVal.ToString();
                if (temp.Length > AsciiLength)
                    temp = temp.Substring(0, AsciiLength);
                ascii += temp;
            }
            return ascii;
        }
    }

    public class VariableLengthDouble : HeaderItem
    {
        public double[] Value { get; set; }
        public VariableLengthDouble(Field info) : base(info) { }

        public override string ToAscii() {
            string ascii = "";
            foreach (var doubleVal in Value)
            {
                string temp = doubleVal.ToString();
                if (temp.Length > AsciiLength)
                    temp = temp.Substring(0, AsciiLength);
                ascii += temp;
            }
            return ascii;
        }
    }

    public class Header
    {
        /// <summary>
        /// The time at which the first record was obtained.
        /// </summary>
        public DateTime FirstRecordTime;

        public FixedLengthString Version { get; private set; } = new FixedLengthString(HeaderItems.Version);
        public FixedLengthString PatientID { get; private set; } = new FixedLengthString(HeaderItems.PatientID);
        public FixedLengthString RecordID { get; private set; } = new FixedLengthString(HeaderItems.RecordID);
        public FixedLengthString RecordingStartDate { get; private set; } = new FixedLengthString(HeaderItems.RecordingStartDate);
        public FixedLengthString RecordingStartTime { get; private set; } = new FixedLengthString(HeaderItems.RecordingStartTime);
        public FixedLengthInt SizeInBytes { get; private set; } = new FixedLengthInt(HeaderItems.SizeInBytes);
        public FixedLengthString Reserved { get; private set; } = new FixedLengthString(HeaderItems.Reserved);
        public FixedLengthInt RecordCount { get; private set; } = new FixedLengthInt(HeaderItems.NumberOfDataRecords);
        public FixedLengthDouble RecordDurationInSeconds { get; private set; } = new FixedLengthDouble(HeaderItems.RecordDurationInSeconds);
        public FixedLengthInt SignalCount { get; private set; } = new FixedLengthInt(HeaderItems.SignalCount);

        public class Signal
        {        
            public VariableLengthString Labels { get; private set; } = new VariableLengthString(HeaderItems.Label);
            public VariableLengthString TransducerTypes { get; private set; } = new VariableLengthString(HeaderItems.TransducerType);
            public VariableLengthString PhysicalDimensions { get; private set; } = new VariableLengthString(HeaderItems.PhysicalDimension);
            public VariableLengthDouble PhysicalMinimums { get; private set; } = new VariableLengthDouble(HeaderItems.PhysicalMinimum);
            public VariableLengthDouble PhysicalMaximums { get; private set; } = new VariableLengthDouble(HeaderItems.PhysicalMaximum);
            public VariableLengthInt DigitalMinimums { get; private set; } = new VariableLengthInt(HeaderItems.DigitalMinimum);
            public VariableLengthInt DigitalMaximums { get; private set; } = new VariableLengthInt(HeaderItems.DigitalMaximum);
            public VariableLengthString Prefilterings { get; private set; } = new VariableLengthString(HeaderItems.Prefiltering);
            public VariableLengthInt SampleCountPerRecords { get; private set; } = new VariableLengthInt(HeaderItems.NumberOfSamplesInDataRecord);
            public VariableLengthString Reserveds { get; private set; } = new VariableLengthString(HeaderItems.SignalsReserved);
        }

        public Signal Signals = new Header.Signal();

        public Header() { }

        /// <summary>
        /// Parse record start date and time string to obtain a DateTime object.
        /// </summary>
        public void ParseRecordingStartTime()
        {
            string timeString = RecordingStartDate.Value + " " + RecordingStartTime.Value.Replace('.', ':');
            // As days comes before months use German culture explicitly
            FirstRecordTime = DateTime.Parse(timeString, CultureInfo.GetCultureInfo("de-DE"));
        }

        /// <summary>
        /// Provides the time corresponding to the given record index.
        /// </summary>
        /// <param name="aRecordIndex"></param>
        /// <returns></returns>
        public DateTime RecordTime(int aRecordIndex)
        {
            return FirstRecordTime.AddSeconds(aRecordIndex * RecordDurationInSeconds.Value);
        }

        
        /// <summary>
        /// Provides the time corresponding to the given signal sample with millisecond precision.
        /// </summary>
        /// <param name="aSignal"></param>
        /// <param name="aSampleIndex"></param>
        /// <returns></returns>
        public DateTime SampleTime(EuropeanDataFormat.Signal aSignal, int aSampleIndex)
        {
            int recordIndex = aSampleIndex / aSignal.SampleCountPerRecord.Value;
            int modulo = aSampleIndex % aSignal.SampleCountPerRecord.Value;
            DateTime recordTime = RecordTime(recordIndex);
            // That will only give us milliseconds precision
            DateTime sampleTime = recordTime.AddMilliseconds(RecordDurationInSeconds.Value * 1000 * modulo / aSignal.SampleCountPerRecord.Value);
            return sampleTime;
        }



        /// <summary>
        /// Useful for debug and inspection.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string strOutput = "";

            strOutput += "\n---------- Header ---------\n";            
            strOutput += "8b\tVersion [" + Version.Value + "]\n";
            strOutput += "80b\tPatient ID [" + PatientID.Value + "]\n";
            strOutput += "80b\tRecording ID [" + RecordID.Value + "]\n";
            strOutput += "8b\tRecording start date [" + RecordingStartDate.Value + "]\n";
            strOutput += "8b\tRecording start time [" + RecordingStartTime.Value + "]\n";
            strOutput += "8b\tHeader size (bytes) [" + SizeInBytes.Value + "]\n";
            strOutput += "44b\tReserved [" + Reserved.Value + "]\n";
            strOutput += "8b\tRecord count [" + RecordCount.Value + "]\n";
            strOutput += "8b\tRecord duration in seconds [" + RecordDurationInSeconds.Value + "]\n";
            strOutput += "4b\tSignal count [" + SignalCount.Value + "]\n\n";
            //strOutput += "First record time: " + FirstRecordTime + "\n\n";

            // For each signal
            for (int i=0;i<SignalCount.Value;i++)
            {
                strOutput += "\tSignal " + i + ": "+ Signals.Labels.Value[i] + "\n\n";
                //strOutput += "\tLabel [" + Signals.Labels.Value[i] + "]\n";
                strOutput += "\t\tTransducer type [" + Signals.TransducerTypes.Value[i] + "]\n";
                strOutput += "\t\tPhysical dimension [" + Signals.PhysicalDimensions.Value[i] + "]\n";
                strOutput += "\t\tPhysical minimum [" + Signals.PhysicalMinimums.Value[i] + "]\n";
                strOutput += "\t\tPhysical maximum [" + Signals.PhysicalMaximums.Value[i] + "]\n";
                strOutput += "\t\tDigital minimum [" + Signals.DigitalMinimums.Value[i] + "]\n";
                strOutput += "\t\tDigital maximum [" + Signals.DigitalMaximums.Value[i] + "]\n";
                strOutput += "\t\tPrefiltering [" + Signals.Prefilterings.Value[i] + "]\n";
                strOutput += "\t\tSample count per record [" + Signals.SampleCountPerRecords.Value[i] + "]\n";
                strOutput += "\t\tSignals reserved [" + Signals.Reserveds.Value[i] + "]\n\n";
            }

            strOutput += "\n-----------------------------------\n";

            //Console.WriteLine();

            return strOutput;
        }
    }
}
