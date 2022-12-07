## ETWFilter
  Etl file can be 30 G, which took too long to read every time. What I really need is just certain data in certain time range. So by given a simple text file contains line of time str : 12/05/2022 19:39:11. C# can directly parse the time str and add range to the time
  Etw event is composed by head meta data and payload which is user defined data. By using the time stamp in meta data, we can check if the event is in time range.

Cmd Usage: EtwFilter.exe <InputFile.etl> <Time.txt> <Output.etl>
