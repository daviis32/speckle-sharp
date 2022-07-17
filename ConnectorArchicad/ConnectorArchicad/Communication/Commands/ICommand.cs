using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Archicad.Communication.Commands
{

  public class CommandComm
  {
    static MemoryMappedFile mmf;
    
    static Semaphore addonSemaphore = new Semaphore(0, 2, "connector_ac_addon");
    static Semaphore uiSemaphore = new Semaphore(0, 2, "connector_ac_ui");

    static MemoryMappedFile GetMapFile()
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


    private static string SerializeRequest<TRequest>(TRequest request)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore,
        Context = new StreamingContext(StreamingContextStates.Remoting)
      };

      return JsonConvert.SerializeObject(request, settings);
    }

    private static TResponse DeserializeResponse<TResponse>(string obj)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        Context = new StreamingContext(StreamingContextStates.Remoting)
      };

      return JsonConvert.DeserializeObject<TResponse>(obj, settings);
    }

    public static TResult Exec<TParameters, TResult>(string commandName, TParameters parameters) where TParameters : class where TResult : class
    {
      AddOnCommandRequest<TParameters> request = new AddOnCommandRequest<TParameters>(commandName, parameters);

      string requestMsg = SerializeRequest(request);

      string responseMsg = "{}";

      Console.WriteLine(" - request: " + requestMsg + "\n");


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

        Console.WriteLine(" - response: " + responseMsg + "\n");
      }
      catch (System.Exception ex)
      {
        Console.WriteLine("Exception: " + ex.ToString());
      }

      AddOnCommandResponse<TResult> response = DeserializeResponse<AddOnCommandResponse<TResult>>(responseMsg);


      return response.Result;
    }
  }
  internal interface ICommand<TResult>
  {
    Task<TResult> Execute();
  }

}
