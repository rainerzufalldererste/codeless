using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
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
        string arg = Clipboard.GetText(TextDataFormat.Text).Trim('"');

        if (arg.EndsWith("!"))
        {
          string data = NtfsStream.Read(arg, "0");
          Environment.CurrentDirectory = Directory.GetParent(arg).FullName;
          List<string> stack = args.ToList();
          var instructions = data.Split(new char[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
          for (int statementIndex = 0; statementIndex < instructions.Length && statementIndex >= 0; ++statementIndex)
          {
            var e = instructions[statementIndex];
            try
            {
              var s = e.Split(' ');
              if (s.Length == 1)
              {
                switch (s[0])
                {
                  case "POPN": stack.RemoveAt(stack.Count() - 1); break;
                  case "FLUSH": stack.Clear(); break;
                  case "CR": Console.WriteLine(); break;
                  case "RRUN": statementIndex = -1; break;
                  case "EXIT": goto exit;
                }
              }
              else if (s.Length == 2)
              {
                switch (s[0])
                {
                  case "CLRL": NtfsStream.Delete(arg, s[1]); break;
                  case "PUTS": Console.Write(NtfsStream.Read(arg, s[1])); break;
                  case "PUSH": stack.Add(NtfsStream.Read(arg, s[1])); break;
                  case "PUSHF": stack.Insert(0, NtfsStream.Read(arg, s[1])); break;
                  case "NOT": stack.Add(_true == NtfsStream.Read(arg, s[1]) ? _false : _true); break;
                  case "POP": NtfsStream.Write(arg, s[1], stack.Count() > 0 ? stack.Last() : ""); if (stack.Count() > 0) stack.RemoveAt(stack.Count() - 1); break;
                  case "POPF": NtfsStream.Write(arg, s[1], stack.Count() > 0 ? stack.First() : ""); if (stack.Count() > 0) stack.RemoveAt(stack.Count() - 1); break;
                  case "CALL": CallFunc(s[1], ref stack); break;
                  case "IFRRUN": if (NtfsStream.Read(arg, s[1]) == _true) statementIndex = -1; break;
                  case "IFEXIT": if (NtfsStream.Read(arg, s[1]) == _true) goto exit; break;
                  case "SETSP": NtfsStream.Write(arg, s[1], " "); break;
                  case "JMP": statementIndex = int.Parse(s[1]) - 2; break;
                  case "DJMP": statementIndex = int.Parse(NtfsStream.Read(arg, s[1])) - 2; break;
                }
              }
              else if (s.Length == 3)
              {
                switch (s[0])
                {
                  case "SET": NtfsStream.Write(arg, s[1], s[2]); break;
                  case "MOV": NtfsStream.Write(arg, s[1], NtfsStream.Read(arg, s[2])); break;
                  case "IADD": stack.Add((int.Parse(NtfsStream.Read(arg, s[1])) + int.Parse(NtfsStream.Read(arg, s[2]))).ToString()); break;
                  case "ISUB": stack.Add((int.Parse(NtfsStream.Read(arg, s[1])) - int.Parse(NtfsStream.Read(arg, s[2]))).ToString()); break;
                  case "IMUL": stack.Add((int.Parse(NtfsStream.Read(arg, s[1])) * int.Parse(NtfsStream.Read(arg, s[2]))).ToString()); break;
                  case "IDIV": stack.Add((int.Parse(NtfsStream.Read(arg, s[1])) / int.Parse(NtfsStream.Read(arg, s[2]))).ToString()); break;
                  case "EQ": stack.Add((NtfsStream.Read(arg, s[1]) == NtfsStream.Read(arg, s[2])) ? _true : _false); break;
                  case "NE": stack.Add((NtfsStream.Read(arg, s[1]) != NtfsStream.Read(arg, s[2])) ? _true : _false); break;
                  case "IGCMP": stack.Add((int.Parse(NtfsStream.Read(arg, s[1])) > int.Parse(NtfsStream.Read(arg, s[2]))) ? _true : _false); break;
                  case "IFCALL": if (NtfsStream.Read(arg, s[1]) == _true) CallFunc(s[2], ref stack); break;
                  case "IFJMP": if (NtfsStream.Read(arg, s[1]) == _true) statementIndex = int.Parse(s[2]) - 2; break;
                  case "IFDJMP": if (NtfsStream.Read(arg, s[1]) == _true) statementIndex = int.Parse(NtfsStream.Read(arg, s[2])) - 2; break;
                }
              }
              if (s.Length > 1)
              {
                switch (s[0])
                {
                  case "SETS": string a = ""; for (int i = 2; i < s.Length; i++) { a += s[i]; if (i + 1 < s.Length) a += " "; } NtfsStream.Write(arg, s[1], a); break;
                }
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine($"\n\nFailed in Statement {statementIndex} ('{e}').\n{(ex != null ? ex.ToString() : "<NULL>\n\nVariables:\n")}");
              foreach (var stream in new FileInfo(arg).ListAlternateDataStreams())
                if (stream.Name != "0")
                  Console.WriteLine($"{stream.Name} = {NtfsStream.Read(arg, stream.Name)}");
              break;
            }
          }
          exit:
          Clipboard.SetText(stack.Any() ? stack.Last() : "-1");
        }
        else
        {
          try
          {
            NtfsStream.Write(arg + "!", "0", File.ReadAllText(arg));
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
      Clipboard.SetText(function + "!");
      string a = "";
      stack.ForEach(x => a += x + " ");
      var t = new Process { StartInfo = new ProcessStartInfo(AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName, a.Trim(' ')) { UseShellExecute = false, WorkingDirectory = Environment.CurrentDirectory, RedirectStandardOutput = true, RedirectStandardInput = true, RedirectStandardError = true } };
      t.OutputDataReceived += (qq, q) => Console.WriteLine(q.Data);
      t.Start();
      t.BeginOutputReadLine();
      t.WaitForExit();
      stack.Add(Clipboard.GetText());
    }
  }
}

public class NtfsStream
{
  const string SubStreamMask = ":";
  public static string Read(string file, string stream)
  {
    var handle = Native.CreateFileW(file + SubStreamMask + stream, Native.GENERIC_READ, Native.FILE_SHARE_READ, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
    if (handle == null)
      throw new IOException();
    try
    {
      return new StreamReader(new FileStream(handle, FileAccess.Read)).ReadToEnd();
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
    var handle = Native.CreateFileW(file + SubStreamMask + stream, Native.GENERIC_WRITE, Native.FILE_SHARE_WRITE, IntPtr.Zero, Native.CREATE_ALWAYS, 0, IntPtr.Zero);
    if (handle == null)
      throw new IOException();
    try
    {
      var sw = new StreamWriter(new FileStream(handle, FileAccess.Write));
      sw.Write(data);
      sw.Close();
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