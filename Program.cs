using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Trinet.Core.IO.Ntfs;

namespace codeless
{
  class Program
  {
    const string _true = "TRUE", _false = "FALSE";

    [STAThread]
    static void Main(string[] args)
    {
      try
      {
        string arg = args[0];

        if (arg.EndsWith("!"))
        {
          bool debug = arg.StartsWith("?");
          if (debug)
            arg = arg.Remove(0, 1);
          var breakpoints = new List<int>();
          bool running = !debug;
          string data = DataStream.Read(arg, "0");
          Environment.CurrentDirectory = Directory.GetParent(arg).FullName;
          List<string> stack = args.ToList();
          stack.RemoveAt(0);
          var instructions = data.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
          for (int statementIndex = 0; statementIndex < instructions.Length && statementIndex >= 0; ++statementIndex)
          {
            var e = instructions[statementIndex];
            if (debug)
            {
              if (!running || breakpoints.Contains(statementIndex))
              {
                running = false;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n [{statementIndex + 1}] >> {e}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("\nVariables:");
                PrintVars(arg, stack);
                Console.ForegroundColor = ConsoleColor.Yellow;
                debug_io:
                Console.Write("\n > ");
                var s = Console.ReadLine().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length >= 1)
                {
                  try
                  {
                    switch (s[0])
                    {
                      case "EXEC": statementIndex--; string a = ""; for (int i = 1; i < s.Length; i++) a += s[i] + " "; e = a; break;
                      case "RUN": running = true; break;
                      case "CODE": for (int i = statementIndex; i < Math.Min(statementIndex + 10, instructions.Length); i++) Console.WriteLine($" [{i + 1}] {instructions[i]}"); goto debug_io;
                      case "CODEAT": for (int i = int.Parse(s[1]) - 1; i < Math.Min(int.Parse(s[1]) + 10, instructions.Length); i++) Console.WriteLine($" [{i + 1}] {instructions[i]}"); goto debug_io;
                      case "ADDB": for (int i = 1; i < s.Length; i++) breakpoints.Add(int.Parse(s[i]) - 1); goto debug_io;
                      case "PUTB": foreach (var b in breakpoints) Console.WriteLine($" Breakpoint set at {b + 1}."); goto debug_io;
                      case "RMB": for (int i = breakpoints.Count - 1; i >= 0; i--) if (breakpoints[i] == int.Parse(s[1]) - 1) breakpoints.RemoveAt(i); goto debug_io;
                      case "CLRB": breakpoints.Clear(); goto debug_io;
                      default: Console.WriteLine("Invalid Parameter.\n\n Valid Parameters:\n\n Press enter to continue to next statement.\n\n EXEC <STATEMENT>\n\tMove the Instruction Pointer to the previous instruction and execute the given instruction <STATEMENT>.\n\n RUN\n\tContinue Execution.\n\n CODE\n\tShow the next few lines of code.\n\n CODEAT <INSTRUCTION_INDEX>\n\tShow the next few lines of code starting at <INSTRUCTION_INDEX>.\n\n ADDB <INSTRUCTION_INDEX ...>\n\tAdd breakpoints at all specified instruction indices.\n\n PUTB\n\tShow all breakpoints.\n\n RMB <INSTRUCTION_INDEX>\n\tRemove the breakpoint at <INSTRUCTION_INDEX>.\n\n CLRB\n\tRemove All Breakpoints."); goto debug_io;
                    }
                  }
                  catch (Exception ex)
                  {
                    Console.WriteLine($"Error in debugger: {(ex != null ? ex.ToString() : " < NULL > ")}");
                  }
                }
                Console.ResetColor();
              }
            }
            try
            {
              var s = e.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
              if (s.Length == 1)
              {
                switch (s[0])
                {
                  case "FLUSH": stack.Clear(); break;
                  case "CR": Console.WriteLine(); break;
                  case "EXIT": goto exit;
                }
              }
              else if (s.Length == 2)
              {
                switch (s[0])
                {
                  case "CLR": DataStream.Delete(arg, s[1]); break;
                  case "PUTS": Console.Write(DataStream.Read(arg, s[1])); break;
                  case "PUSH": stack.Add(DataStream.Read(arg, s[1])); break;
                  case "PUSHF": stack.Insert(0, DataStream.Read(arg, s[1])); break;
                  case "NOT": stack.Add(_true == DataStream.Read(arg, s[1]) ? _false : _true); break;
                  case "POP": DataStream.Write(arg, s[1], stack.Count() > 0 ? stack.Last() : ""); if (stack.Count() > 0) stack.RemoveAt(stack.Count() - 1); break;
                  case "POPF": DataStream.Write(arg, s[1], stack.Count() > 0 ? stack.First() : ""); if (stack.Count() > 0) stack.RemoveAt(stack.Count() - 1); break;
                  case "CALL": CallFunc(s[1], ref stack); break;
                  case "SETSP": DataStream.Write(arg, s[1], " "); break;
                  case "JMP": statementIndex = int.Parse(s[1]) - 2; break;
                  case "DJMP": statementIndex = int.Parse(DataStream.Read(arg, s[1])) - 2; break;
                  case "SETIP": DataStream.Write(arg, s[1], (statementIndex - 1).ToString()); break;
                }
              }
              else if (s.Length == 3)
              {
                switch (s[0])
                {
                  case "SAT": var v = DataStream.Read(arg, s[1]); int x = int.Parse(DataStream.Read(arg, s[2])); if (v.Length > x && x >= 0) stack.Add(v[x].ToString()); else stack.Add(""); break;
                  case "SET": DataStream.Write(arg, s[1], s[2]); break;
                  case "MOV": DataStream.Write(arg, s[1], DataStream.Read(arg, s[2])); break;
                  case "IADD": stack.Add((int.Parse(DataStream.Read(arg, s[1])) + int.Parse(DataStream.Read(arg, s[2]))).ToString()); break;
                  case "ISUB": stack.Add((int.Parse(DataStream.Read(arg, s[1])) - int.Parse(DataStream.Read(arg, s[2]))).ToString()); break;
                  case "IMUL": stack.Add((int.Parse(DataStream.Read(arg, s[1])) * int.Parse(DataStream.Read(arg, s[2]))).ToString()); break;
                  case "IDIV": stack.Add((int.Parse(DataStream.Read(arg, s[1])) / int.Parse(DataStream.Read(arg, s[2]))).ToString()); break;
                  case "SCAT": stack.Add(DataStream.Read(arg, s[1]) + DataStream.Read(arg, s[2])); break;
                  case "EQ": stack.Add((DataStream.Read(arg, s[1]) == DataStream.Read(arg, s[2])) ? _true : _false); break;
                  case "NE": stack.Add((DataStream.Read(arg, s[1]) != DataStream.Read(arg, s[2])) ? _true : _false); break;
                  case "IGCMP": stack.Add((int.Parse(DataStream.Read(arg, s[1])) > int.Parse(DataStream.Read(arg, s[2]))) ? _true : _false); break;
                  case "IFCALL": if (DataStream.Read(arg, s[1]) == _true) CallFunc(s[2], ref stack); break;
                  case "IFJMP": if (DataStream.Read(arg, s[1]) == _true) statementIndex = int.Parse(s[2]) - 2; break;
                  case "IFDJMP": if (DataStream.Read(arg, s[1]) == _true) statementIndex = int.Parse(DataStream.Read(arg, s[2])) - 2; break;
                }
              }
              if (s.Length > 1)
              {
                switch (s[0])
                {
                  case "SETS": string a = ""; for (int i = 2; i < s.Length; i++) { a += s[i]; if (i + 1 < s.Length) a += " "; } DataStream.Write(arg, s[1], a); break;
                }
              }
            }
            catch (Exception ex)
            {
              if (debug)
              {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n\nFailed in Statement {statementIndex} ('{e}').\n{(ex != null ? ex.ToString() : "<NULL>\n\nVariables:\n")}");
                PrintVars(arg, stack);
                Console.ResetColor();
                running = false;
                statementIndex--;
              }
              else
              {
                Console.WriteLine($"\n\nFailed in Statement {statementIndex} ('{e}').");
                break;
              }
            }
          }
          exit:
          Clipboard.SetText(stack.Any() ? stack.Last() : "-1");
        }
        else
        {
          try
          {
            var file = File.ReadAllText(arg);
            var dict = new Dictionary<string, string>();
            var labels = new Dictionary<string, int>();
            string a = "";
            int statementIndex = 0;
            foreach (var e in file.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
              try
              {
                if (e.StartsWith("#"))
                {
                  if (!e.StartsWith("##"))
                  {
                    var s = e.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length == 2)
                    {
                      switch (s[0])
                      {
                        case "#LABEL": labels.Add(s[1], statementIndex + 1); break;
                        default: Console.WriteLine($"Warning: Invalid Preprocessor Token '{e}'."); break;
                      }
                    }
                    else if (s.Length == 3)
                    {
                      switch (s[0])
                      {
                        case "#SET": dict.Add(s[1], s[2]); break;
                        case "#SETX": dict.Add(s[1], s[2].Replace('_', ' ')); break;
                        case "#AP": dict.Add(s[1], dict[s[1]] + s[2]); break;
                        case "#APX": dict.Add(s[1], dict[s[1]] + s[2].Replace('_', ' ')); break;
                        default: Console.WriteLine($"Warning: Invalid Preprocessor Token '{e}'."); break;
                      }
                    }
                    else
                    {
                      Console.WriteLine($"Warning: Invalid Preprocessor Token '{e}'.");
                    }
                  }
                }
                else
                {
                  ++statementIndex;
                  a += e + "\n";
                }
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Failed to compile statement '{e}'.\n{(ex != null ? ex.ToString() : "<NULL>")}");
              }
            }
            try
            {
              foreach (var l in labels)
                a = a.Replace($"${l.Key}", l.Value.ToString());
              DataStream.Write(arg + "!", "0", a);
              foreach (var v in dict)
                DataStream.Write(arg + "!", v.Key, v.Value);
            }
            catch (Exception) { throw new IOException(); }
          }
          catch (Exception e)
          {
            Console.WriteLine($"Failed to compile object '{arg}'.\n{(e != null ? e.ToString() : "<NULL>")}");
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"Invalid Parameter.\n{(e != null ? e.ToString() : "<NULL>")}");
      }
    }

    static void CallFunc(string function, ref List<string> stack)
    {
      string a = function + "! ";
      stack.ForEach(x => a += x + " ");
      var t = new Process { StartInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName, a.Trim(' ')) { UseShellExecute = false, WorkingDirectory = Environment.CurrentDirectory, RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true } };
      t.OutputDataReceived += (qq, q) => Console.WriteLine(q.Data);
      t.Start();
      t.BeginOutputReadLine();
      t.WaitForExit();
      stack.Add(Clipboard.GetText());
    }

    static void PrintVars(string arg, List<string> stack)
    {
      foreach (var stream in new FileInfo(arg).ListAlternateDataStreams())
        if (stream.Name != "0")
          Console.WriteLine($"{stream.Name} = '{DataStream.Read(arg, stream.Name)}'");
      Console.WriteLine("\nStack:\n");
      for (int i = stack.Count() - 1; i >= 0; i--)
        Console.WriteLine($"[{i}]: '{stack[i]}'");
    }
  }
}

public class DataStream
{
  const string SubStreamMask = ":";
  public static string Read(string file, string stream)
  {
    var handle = Native.CreateFileW(file + SubStreamMask + stream, Native.GENERIC_READ, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
    if (handle == null)
      throw new IOException();
    try
    {
      using (var sr = new StreamReader(new FileStream(handle, FileAccess.Read)))
        return sr.ReadToEnd();
    }
    finally
    {
      try
      {
        Native.CloseHandle(handle);
      }
      catch { }
    }
  }

  public static void Write(string file, string stream, string data)
  {
    var handle = Native.CreateFileW(file + SubStreamMask + stream, Native.GENERIC_WRITE, Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.CREATE_ALWAYS, 0, IntPtr.Zero);
    if (handle == null)
      throw new IOException();
    try
    {
      using (var sw = new StreamWriter(new FileStream(handle, FileAccess.Write)))
        sw.Write(data);
    }
    finally
    {
      try
      {
        Native.CloseHandle(handle);
      }
      catch { }
    }
  }

  public static void Delete(string file, string stream)
  {
    Native.DeleteFileW(file + SubStreamMask + stream);
  }
}

public class Native
{
  [DllImport("kernel32.dll", EntryPoint = "CreateFileW")]
  public static extern IntPtr CreateFileW(
      [In] [MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
      uint dwDesiredAccess,
      uint dwShareMode,
      [In] IntPtr lpSecurityAttributes,
      uint dwCreationDisposition,
      uint dwFlagsAndAttributes,
      [In] IntPtr hTemplateFile
  );

  [DllImport("kernel32.dll", EntryPoint = "DeleteFileW")]
  public static extern int DeleteFileW([In] [MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

  [DllImport("kernel32.dll", EntryPoint = "CloseHandle")]
  public static extern int CloseHandle([In] IntPtr handle);

  public const uint GENERIC_READ = 0x80000000;
  public const int GENERIC_WRITE = 0x40000000;
  public const uint FILE_SHARE_DELETE = 0x00000004;
  public const uint FILE_SHARE_WRITE = 0x00000002;
  public const uint FILE_SHARE_READ = 0x00000001;
  public const uint OPEN_ALWAYS = 4;
  public const uint OPEN_EXISTING = 3;
  public const uint CREATE_ALWAYS = 2;
}