using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text;
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Archicad.Communication.Commands
{
  internal interface ICommand<TResult>
  {
    Task<TResult> Execute();
  }

}
