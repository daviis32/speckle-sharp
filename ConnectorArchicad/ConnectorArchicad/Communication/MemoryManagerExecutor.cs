using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Archicad.Communication
{
  class MemoryManagerExecutor
  {
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

    public static TResult Execute<TParameters, TResult>(string commandName, TParameters parameters) where TParameters : class where TResult : class
    {
      AddOnCommandRequest<TParameters> request = new AddOnCommandRequest<TParameters>(commandName, parameters);

      string requestMsg = SerializeRequest(request);

      Console.WriteLine("sending request: " + requestMsg + "\n");

      string responseMsg = SharedMemoryManager.Instance.ExecuteRequest(requestMsg);

      Console.WriteLine("received response: " + responseMsg + "\n");

      AddOnCommandResponse<TResult> response = DeserializeResponse<AddOnCommandResponse<TResult>>(responseMsg);


      return response.Result;
    }
  }
}
