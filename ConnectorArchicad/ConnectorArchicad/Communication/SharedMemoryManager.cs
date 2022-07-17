using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Archicad.Communication
{
  class SharedMemoryManager
  {
    static MemoryMappedFile mmf;

    static Semaphore addonSemaphore = new Semaphore(0, 2, "connector_ac_addon");
    static Semaphore uiSemaphore    = new Semaphore(0, 2, "connector_ac_ui");

    public static SharedMemoryManager Instance { get; } = new SharedMemoryManager();

    MemoryMappedFile GetMapFile()
    {
      if (mmf == null)
      {
        mmf = MemoryMappedFile.OpenExisting("ac_connector", MemoryMappedFileRights.FullControl, System.IO.HandleInheritability.None);

        if (mmf == null)
        {
          Console.WriteLine("ERROR: Unable to open memory mapped file!!\n");
        }
      }

      return mmf;
    }

    public string ExecuteRequest (string requestMsg)
    {
      string responseMsg = "{}";

      try
      {
        using (var accessor = GetMapFile().CreateViewAccessor(0, 0))
        {
          byte[] reqArray = Encoding.ASCII.GetBytes(requestMsg);

          accessor.Write(0, reqArray.Length);
          accessor.WriteArray<byte>(2, reqArray, 0, reqArray.Length);
          accessor.Write(reqArray.Length + 2, 0);

          uiSemaphore.Release();
          addonSemaphore.WaitOne();

          byte s1, s2;
          accessor.Read(0, out s1);
          accessor.Read(1, out s2);

          int size = s1 * 256 + s2;

          var bytes = new byte[size + 1];
          accessor.ReadArray<byte>(2, bytes, 0, bytes.Length);

          responseMsg = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }


      }
      catch (System.Exception ex)
      {
        Console.WriteLine("Exception: " + ex.ToString());
      }

      return responseMsg;
    }

  }
}
