// So I had this 4GB ETL trace that I wanted to open in Message Analyzer.
// Message Analyzer couldn't handle it. Crapped out. Because of a hard-coded
// limit on the size of .NET dictionary or something like that. It's 2017...
// a 4GB file isn't that big anymore. I should be able to open a 4GB file when
// I have 32GB of free RAM. 
//
//
// This code uses the Microsoft.Diagnostics.Tracing.TraceEvent which is downloadable as a NuGet package.
// This code itself is scrap. It's the TraceEvent library where the magic really is.

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;


namespace ETWSplitter
{
    class ETWSplitter
    {
        //时间窗口范围
        const int C_TIME_RANGE_SECONDS = 1;
        static DateTime[,] TimeRangeList = null;
        

        static bool IsInTimeThreshold(DateTime time, DateTime[,] TimeRange)
        {
            if (TimeRange.GetLength(1) == 0)
                return false;

            for (int i = 0; i < TimeRange.GetLength(0); i++)
            {

                bool IsLateThenMin = time.CompareTo(TimeRange[i, 0]) > 0;
                bool IsEarlierThenMax = time.CompareTo(TimeRange[i, 1]) < 0;

                if (IsLateThenMin && IsEarlierThenMax)
                    return true;
            }

            return false;
        }

        static bool GenerateDataTime(string FilePath_abs, out DateTime[,] OutTimeRangeList)
        {
            OutTimeRangeList = null;

            if (!File.Exists(FilePath_abs))
            {
                Console.WriteLine("Time File is not exist! ");
                return false;
            }

            List<DateTime> TempTimeList = new List<DateTime>();

            using (var sr = new StreamReader(FilePath_abs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    DateTime jag_time = DateTime.Parse(line);
                    DateTime min = jag_time.AddSeconds(-C_TIME_RANGE_SECONDS);
                    DateTime max= jag_time.AddSeconds(C_TIME_RANGE_SECONDS);
                    TempTimeList.Add(min);
                    TempTimeList.Add(max);

                    Console.WriteLine("Jag Time: {0}, MinTime:{1}, MaxTime:{2}", jag_time, min, max);
                }
            }

            int ArrayRowCount = (TempTimeList.Count + 1) / 2;
            OutTimeRangeList = new DateTime[ArrayRowCount, 2];
            for (int row_idx = 0; row_idx < ArrayRowCount; row_idx++)
            {
                OutTimeRangeList[row_idx, 0] = TempTimeList[row_idx * 2 + 0];
                OutTimeRangeList[row_idx, 1] = TempTimeList[row_idx * 2 + 1];
            }

            Console.WriteLine("Generate Time Range Num: {0} ", ArrayRowCount);

            return true;
        }

        static void Main(string[] args)
        {
            string inputFileName = args[0];
            string jagTimeFile = args[1];
            string exportFile = args[2];

            if (!File.Exists(inputFileName))
            {
                Console.WriteLine("ERROR: Input etl file {0} was not found!", inputFileName);
                return;
            }

            if (!File.Exists(jagTimeFile))
            {
                Console.WriteLine("ERROR: Input jag time txt file {0} was not found!", inputFileName);
                return;
            }

            bool IsSuc = GenerateDataTime(jagTimeFile, out TimeRangeList);
            if (!IsSuc)
                return;

            int TotalEventCount = 0;
            int FinalEventCount = 0;
            using (var src = new ETWReloggerTraceEventSource(inputFileName, exportFile))
            {
                src.AllEvents += delegate (TraceEvent data)
                {
                    //Console.WriteLine("TimeStamp:{0}, TimeReleativeMs:{1}", data.TimeStamp, data.TimeStampRelativeMSec);
                    bool IsInRange = IsInTimeThreshold(data.TimeStamp, TimeRangeList);
                    if (IsInRange)
                    {
                        src.WriteEvent(data);
                        Interlocked.Increment(ref FinalEventCount);
                    }
                    
                    Interlocked.Increment(ref TotalEventCount);

                };

                src.Process();

                Console.WriteLine("Total Event Count={0}, Final Event Count = {1}", TotalEventCount, FinalEventCount);
            };
        }
    }
}
